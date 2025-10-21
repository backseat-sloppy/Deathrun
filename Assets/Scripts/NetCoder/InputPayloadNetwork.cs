using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Network serializable input data structure
/// </summary>
public struct InputPayload : INetworkSerializable
{
    public int Tick;
    public Vector3 InputVector;
    public bool Jump;
    public Quaternion CameraRotation;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref InputVector);
        serializer.SerializeValue(ref Jump);
        serializer.SerializeValue(ref CameraRotation);
    }
}

/// <summary>
/// Network serializable state data structure
/// </summary>
public struct StatePayload : INetworkSerializable
{
    public int Tick;
    public Vector3 Position;
    public Vector3 Velocity;
    public Quaternion Rotation;
    public bool IsGrounded;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Velocity);
        serializer.SerializeValue(ref Rotation);
        serializer.SerializeValue(ref IsGrounded);
    }
}

/// <summary>
/// Handles client-side prediction, server reconciliation, and physics-based networking synchronization.
/// Works in conjunction with PlayerMovementController.
/// </summary>
public class InputPayloadNetwork : NetworkBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private float reconciliationThreshold = 0.1f;
    [SerializeField] private int maxBufferSize = 1024;

    // References
    private PlayerMovementController _movementController;
    private Rigidbody _rb;

    // Network state
    private int _currentTick = 0;
    private int _serverTick = 0;
    
    // Client-side buffers for reconciliation
    private Queue<InputPayload> _inputQueue = new Queue<InputPayload>();
    private Queue<StatePayload> _stateBuffer = new Queue<StatePayload>();

    public int CurrentTick => _currentTick;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        _movementController = GetComponent<PlayerMovementController>();
        _rb = GetComponent<Rigidbody>();
        
        if (_movementController == null)
        {
            Debug.LogError("❌ InputPayloadNetwork requires PlayerMovementController component!");
        }
    }

    /// <summary>
    /// Processes input with client-side prediction and server synchronization
    /// </summary>
    public void ProcessInput(InputPayload input)
    {
        if (!IsOwner) return;

        _currentTick++;
        input.Tick = _currentTick;

        // Store input for reconciliation
        _inputQueue.Enqueue(input);
        while (_inputQueue.Count > maxBufferSize)
            _inputQueue.Dequeue();

        // HOST OPTIMIZATION: If we're the host, process locally AND send to clients
        if (IsHost)
        {
            // Host processes movement immediately (no round-trip needed)
            _movementController.ProcessMovement(input);
            
            // Broadcast authoritative state to other clients
            BroadcastStateToClientsClientRpc(CreateStatePayload());
        }
        else
        {
            // CLIENT: Apply prediction immediately, then send to server/host for authority
            _movementController.ProcessMovement(input);
            SendInputToServerServerRpc(input);
        }
    }

    private StatePayload CreateStatePayload()
    {
        return new StatePayload
        {
            Tick = _currentTick,
            Position = transform.position,
            Velocity = _rb.linearVelocity,
            Rotation = transform.rotation,
            IsGrounded = _movementController.IsGrounded
        };
    }

    // ============================================
    // P2P/RELAY NETWORKING: Client → Host/Server
    // ============================================
    [ServerRpc]
    private void SendInputToServerServerRpc(InputPayload input)
    {
        // HOST/SERVER receives client input and processes it authoritatively
        _movementController.ProcessMovement(input);
        
        // Send corrected state back to the specific client
        StatePayload state = CreateStatePayload();
        state.Tick = input.Tick;
        
        // Get the sender's client ID using ServerRpcParams
        ulong senderClientId = NetworkManager.Singleton.LocalClientId;
        
        // Send state back ONLY to the client who sent the input using ClientRpcParams
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { senderClientId }
            }
        };
        
        SendStateToOwnerClientRpc(state, clientRpcParams);
    }

    // ============================================
    // Host → Client: State Correction (for non-host clients)
    // ============================================
    [ClientRpc]
    private void SendStateToOwnerClientRpc(StatePayload state, ClientRpcParams clientRpcParams = default)
    {
        // Only process if we're the owner AND not the host
        if (!IsOwner || IsHost) return;

        _serverTick = state.Tick;
        
        // Check if reconciliation is needed
        float positionError = Vector3.Distance(transform.position, state.Position);
        
        if (positionError > reconciliationThreshold)
        {
            Debug.Log($"🔄 Reconciling: Error={positionError:F3}m at tick {state.Tick}");
            
            // Snap to server state
            transform.position = state.Position;
            _rb.linearVelocity = state.Velocity;
            transform.rotation = state.Rotation;

            // Replay inputs that occurred after this server state
            Queue<InputPayload> replayQueue = new Queue<InputPayload>();
            foreach (var input in _inputQueue)
            {
                if (input.Tick > state.Tick)
                {
                    replayQueue.Enqueue(input);
                }
            }

            // Clear old inputs
            _inputQueue.Clear();
            
            // Replay newer inputs
            foreach (var input in replayQueue)
            {
                _movementController.ProcessMovement(input);
                _inputQueue.Enqueue(input);
            }
        }

        // Prune old states
        while (_stateBuffer.Count > maxBufferSize)
            _stateBuffer.Dequeue();
            
        _stateBuffer.Enqueue(state);
    }

    // ============================================
    // Host → All Clients: Broadcast Authoritative State
    // ============================================
    [ClientRpc]
    private void BroadcastStateToClientsClientRpc(StatePayload state)
    {
        // This is called by the host to inform all OTHER clients about this player's state
        // Only non-owners need to apply this (the owner already has authority)
        if (IsOwner) return;

        // Remote players just accept the state (no prediction/reconciliation needed)
        transform.position = state.Position;
        _rb.linearVelocity = state.Velocity;
        transform.rotation = state.Rotation;
    }
}
