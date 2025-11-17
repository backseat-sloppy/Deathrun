using UnityEngine;
using Unity.Netcode;
using Oculus.Interaction;

namespace DeathrunGame
{
    /// <summary>
    /// VR trap activator that synchronizes activation across the network.
    /// When the VR player grabs/releases this object, all clients see the trap activate.
    /// Attach this to any GameObject with a Grabbable component.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class VRTrapActivator : NetworkBehaviour
    {
        [Header("Trap Settings")]
        [Tooltip("The trap GameObject to activate")]
        [SerializeField] private GameObject trapObject;

        [Tooltip("Name of the method to call on the trap (e.g., 'ActivateTrap')")]
        [SerializeField] private string activationMethodName = "ActivateTrap";

        [Header("Trigger Options")]
        [Tooltip("Activate when grabbed?")]
        [SerializeField] private bool activateOnGrab = true;

        [Tooltip("Activate when released?")]
        [SerializeField] private bool activateOnRelease = false;

        [Header("Visual Feedback")]
        [Tooltip("Change color to red when activated?")]
        [SerializeField] private bool changeColorOnActivation = true;

        [Tooltip("Color to change to when activated")]
        [SerializeField] private Color activatedColor = Color.red;

        [Header("Movement Lock")]
        [Tooltip("Prevent object from being moved after activation?")]
        [SerializeField] private bool lockMovementOnActivation = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        [Tooltip("Press this button in Inspector to test trap activation")]
        [SerializeField] private bool debugActivateTrap = false;

        // Network synced activation state
        private NetworkVariable<bool> isActivated = new NetworkVariable<bool>(
            false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
        );

        private Grabbable grabbable;
        private Rigidbody rb;
        private Renderer objectRenderer;
        private Material originalMaterial;
        private Material activatedMaterial;

        private void Awake()
        {
            // Get the Grabbable component
            grabbable = GetComponent<Grabbable>();

            // Get Rigidbody (if exists) for locking movement
            rb = GetComponent<Rigidbody>();

            // Get renderer for color change
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer == null)
            {
                objectRenderer = GetComponentInChildren<Renderer>();
            }

            // Store original material
            if (objectRenderer != null && changeColorOnActivation)
            {
                originalMaterial = objectRenderer.material;
                
                // Create a copy of the material for activation state
                activatedMaterial = new Material(originalMaterial);
                
                // Set the activated color
                if (activatedMaterial.HasProperty("_Color"))
                {
                    activatedMaterial.color = activatedColor;
                }
                else if (activatedMaterial.HasProperty("_BaseColor"))
                {
                    activatedMaterial.SetColor("_BaseColor", activatedColor);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Subscribe to network variable changes (runs on all clients)
            isActivated.OnValueChanged += OnActivationStateChanged;

            // Apply current state (in case we joined after activation)
            if (isActivated.Value)
            {
                ApplyActivatedState();
            }

            Log($"OnNetworkSpawn - IsHost: {IsHost}, IsActivated: {isActivated.Value}");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            // Unsubscribe to prevent memory leaks
            isActivated.OnValueChanged -= OnActivationStateChanged;
        }

        private void OnEnable()
        {
            // Subscribe to grab events
            if (grabbable != null)
            {
                grabbable.WhenPointerEventRaised += OnGrabEvent;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            if (grabbable != null)
            {
                grabbable.WhenPointerEventRaised -= OnGrabEvent;
            }
        }

        private void OnValidate()
        {
            // Debug button trigger in Inspector
            if (debugActivateTrap)
            {
                debugActivateTrap = false;
                
                // Only activate in Play mode
                if (!Application.isPlaying)
                {
                    Debug.LogWarning("[VRTrapActivator] Debug activation only works in Play mode!");
                    return;
                }

                // Check if NetworkObject exists
                NetworkObject netObj = GetComponentInParent<NetworkObject>();
                if (netObj == null)
                {
                    Debug.LogError("[VRTrapActivator] No NetworkObject found! Add a NetworkObject component to this GameObject or its parent.");
                    return;
                }

                // Check if spawned
                if (!netObj.IsSpawned)
                {
                    Debug.LogError("[VRTrapActivator] NetworkObject not spawned yet! Start the game as Host/Server first, then try the debug button.");
                    return;
                }

                ActivateTrap();
            }
        }

        /// <summary>
        /// Called when grab/release happens (LOCAL event, VR player only)
        /// </summary>
        private void OnGrabEvent(PointerEvent evt)
        {
            // Only the owner (VR player/host) should handle grab events
            if (!IsOwner && !IsHost)
            {
                return;
            }

            // Prevent interaction if already activated and locked
            if (isActivated.Value && lockMovementOnActivation)
            {
                Log("🔒 Object is locked - cannot be grabbed again!");
                return;
            }

            // Check if object was grabbed
            if (evt.Type == PointerEventType.Select && activateOnGrab)
            {
                Log("🖐️ Object grabbed!");
                ActivateTrap();
            }

            // Check if object was released
            if (evt.Type == PointerEventType.Unselect && activateOnRelease)
            {
                Log("👋 Object released!");
                ActivateTrap();
            }
        }

        /// <summary>
        /// Request trap activation (called locally by VR player)
        /// </summary>
        private void ActivateTrap()
        {
            if (isActivated.Value)
            {
                Log("⚠️ Trap already activated!");
                return;
            }

            if (trapObject == null)
            {
                Debug.LogError("[VRTrapActivator] No trap object assigned!");
                return;
            }

            // Check if network is ready
            if (!IsSpawned)
            {
                Debug.LogError("[VRTrapActivator] Cannot activate - NetworkObject not spawned! Make sure this GameObject has a NetworkObject component and is spawned on the network.");
                return;
            }

            // Send activation request to server
            if (IsServer)
            {
                // We are the server, activate directly
                SetActivationState(true);
            }
            else
            {
                // We are a client, request server to activate
                RequestActivationServerRpc();
            }

            Log($"✅ Requested trap activation: {trapObject.name}");
        }

        /// <summary>
        /// ServerRpc: Client requests server to activate the trap
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void RequestActivationServerRpc(ServerRpcParams rpcParams = default)
        {
            Log($"[Server] Received activation request from client {rpcParams.Receive.SenderClientId}");
            
            if (!isActivated.Value)
            {
                SetActivationState(true);
            }
        }

        /// <summary>
        /// Server sets the activation state (NetworkVariable automatically syncs to all clients)
        /// </summary>
        private void SetActivationState(bool activated)
        {
            if (!IsServer)
            {
                Debug.LogError("[VRTrapActivator] Only server can set activation state!");
                return;
            }

            isActivated.Value = activated;
            Log($"[Server] Set activation state to: {activated}");
        }

        /// <summary>
        /// Called on ALL clients when activation state changes
        /// </summary>
        private void OnActivationStateChanged(bool oldValue, bool newValue)
        {
            Log($"Activation state changed: {oldValue} -> {newValue}");

            if (newValue)
            {
                ApplyActivatedState();
            }
        }

        /// <summary>
        /// Apply visual and functional changes when activated (runs on all clients)
        /// </summary>
        private void ApplyActivatedState()
        {
            // Call the activation method on the trap
            if (trapObject != null)
            {
                trapObject.SendMessage(activationMethodName, SendMessageOptions.DontRequireReceiver);
                Log($"📣 Activated trap: {trapObject.name}");
            }

            // Lock movement
            if (lockMovementOnActivation)
            {
                LockMovement();
            }

            // Change color
            if (changeColorOnActivation && objectRenderer != null && activatedMaterial != null)
            {
                objectRenderer.material = activatedMaterial;
                Log("🎨 Changed color to red!");
            }
        }

        /// <summary>
        /// Locks the object in place (runs on all clients)
        /// </summary>
        private void LockMovement()
        {
            // Disable the grabbable component (only on VR player's machine)
            if (grabbable != null && (IsOwner || IsHost))
            {
                grabbable.enabled = false;
                Log("🔒 Grabbable disabled - object locked!");
            }

            // Lock the Rigidbody
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Log("🔒 Rigidbody locked!");
            }
        }
        
        /// <summary>
        /// Public method to activate trap from other scripts or Unity Events
        /// </summary>
        public void ManualActivate()
        {
            Log("🔧 Manual activation triggered!");
            ActivateTrap();
        }

        /// <summary>
        /// Reset the activator (for testing) - Server only
        /// </summary>
        [ContextMenu("Reset Activator")]
        public void ResetActivator()
        {
            if (!IsServer)
            {
                Debug.LogWarning("[VRTrapActivator] Only server can reset activator!");
                return;
            }

            isActivated.Value = false;

            // Re-enable grabbable
            if (grabbable != null)
            {
                grabbable.enabled = true;
            }

            // Unlock rigidbody
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            // Restore original color
            if (objectRenderer != null && originalMaterial != null)
            {
                objectRenderer.material = originalMaterial;
            }

            Log("🔄 Activator reset!");
        }

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[VRTrapActivator] {message}");
            }
        }

        private void OnDestroy()
        {
            // Clean up materials to prevent memory leaks
            if (activatedMaterial != null)
            {
                Destroy(activatedMaterial);
            }
        }
    }
}
