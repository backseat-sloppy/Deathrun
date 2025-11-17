using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Tracks players who are on the boat using trigger collisions.
/// Communicates with BoatController to determine when all players are aboard.
/// Supports Rigidbody players by maintaining their position relative to the boat using physics.
/// Only tracks ALIVE players (active GameObjects with Player tag).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class PassengerTracker : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Player Attachment")]
    [SerializeField] private bool parentPlayersToBoat = true;
    [SerializeField] private Transform boatDeck; // Optional: specific deck transform
    
    [Header("Rigidbody Player Settings")]
    [SerializeField] private bool supportRigidbodyPlayers = true;
    [SerializeField] private float rigidbodyAttachmentForce = 500f;
    [SerializeField] private float rigidbodyDamping = 10f;
    [SerializeField] private bool useKinematicAttachment = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private bool showDetailedDebug = false; // Extra debugging info
    
    private HashSet<GameObject> playersOnBoard = new HashSet<GameObject>();
    private Dictionary<GameObject, Transform> playerOriginalParents = new Dictionary<GameObject, Transform>();
    private Dictionary<GameObject, PlayerRigidbodyData> playerRigidbodyData = new Dictionary<GameObject, PlayerRigidbodyData>();
    private BoxCollider triggerZone;
    private BoatController boatController;
    private Rigidbody boatRigidbody;
    
    // Cache of all known player root objects to avoid duplicates
    private HashSet<GameObject> knownPlayers = new HashSet<GameObject>();
    
    // Event fired when all players board
    public event System.Action OnAllPlayersBoarded;
    public event System.Action OnPlayerBoarded;
    public event System.Action OnPlayerLeft;
    
    public int PlayersOnBoard => playersOnBoard.Count;
    public int TotalAlivePlayers => CountAlivePlayers();
    public bool AllPlayersAboard => playersOnBoard.Count >= TotalAlivePlayers && TotalAlivePlayers > 0;
    
    private class PlayerRigidbodyData
    {
        public Rigidbody rigidbody;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public bool wasKinematic;
        public bool wasUsingGravity;
        public float originalDrag;
        public float originalAngularDrag;
    }
    
    private void Awake()
    {
        triggerZone = GetComponent<BoxCollider>();
        triggerZone.isTrigger = true;
        
        boatController = GetComponentInParent<BoatController>();
        if (boatController == null)
        {
            Debug.LogError("PassengerTracker requires a BoatController component in parent!");
        }
        
        boatRigidbody = GetComponentInParent<Rigidbody>();
        
        // If no specific deck transform, use boat root
        if (boatDeck == null)
        {
            boatDeck = boatController?.transform;
        }
    }
    
    private void Start()
    {
        // Initial scan of players in the scene
        RefreshKnownPlayers();
    }
    
    private void FixedUpdate()
    {
        if (supportRigidbodyPlayers)
        {
            UpdateRigidbodyPlayers();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            // Get the root player GameObject (in case collider is on a child)
            GameObject player = GetPlayerRoot(other.gameObject);
            
            // Only track if player is alive (GameObject is active)
            if (player == null || !player.activeInHierarchy)
            {
                Log($"⚠️ Ignoring dead/invalid player: {other.gameObject.name}");
                return;
            }
            
            if (playersOnBoard.Add(player))
            {
                // Check if player has Rigidbody
                Rigidbody playerRb = player.GetComponent<Rigidbody>();
                
                if (supportRigidbodyPlayers && playerRb != null)
                {
                    AttachRigidbodyPlayerToBoat(player, playerRb);
                }
                else if (parentPlayersToBoat)
                {
                    // Fallback to parenting for non-Rigidbody players
                    AttachPlayerToBoat(player);
                }
                
                int alivePlayers = CountAlivePlayers();
                Log($"✅ Player boarded! ({playersOnBoard.Count}/{alivePlayers} alive players)");
                
                if (showDetailedDebug)
                {
                    LogPlayerDetails();
                }
                
                OnPlayerBoarded?.Invoke();
                
                // Check if all ALIVE players are now aboard
                if (AllPlayersAboard)
                {
                    Log($"🚢 ALL ALIVE PLAYERS ABOARD! ({playersOnBoard.Count}/{alivePlayers}) Starting journey...");
                    OnAllPlayersBoarded?.Invoke();
                    
                    if (boatController != null)
                    {
                        boatController.StartJourney();
                    }
                }
                else
                {
                    Log($"⏳ Waiting for {alivePlayers - playersOnBoard.Count} more alive player(s)...");
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            // Get the root player GameObject
            GameObject player = GetPlayerRoot(other.gameObject);
            
            if (player != null && playersOnBoard.Remove(player))
            {
                // Check if player has Rigidbody data
                if (playerRigidbodyData.ContainsKey(player))
                {
                    DetachRigidbodyPlayerFromBoat(player);
                }
                else if (parentPlayersToBoat)
                {
                    // Unparent player from boat
                    DetachPlayerFromBoat(player);
                }
                
                int alivePlayers = CountAlivePlayers();
                Log($"❌ Player left boat! ({playersOnBoard.Count}/{alivePlayers} alive players)");
                OnPlayerLeft?.Invoke();
                
                // Stop the boat if a player leaves
                if (boatController != null && boatController.IsMoving)
                {
                    Log("⚠️ Player left during journey - stopping boat!");
                    boatController.StopJourney();
                }
            }
        }
    }
    
    /// <summary>
    /// Updates Rigidbody players to follow the boat's movement using physics forces.
    /// </summary>
    private void UpdateRigidbodyPlayers()
    {
        if (boatDeck == null) return;
        
        foreach (var kvp in playerRigidbodyData)
        {
            GameObject player = kvp.Key;
            PlayerRigidbodyData data = kvp.Value;
            
            if (player == null || data.rigidbody == null || !player.activeInHierarchy)
                continue;
            
            if (useKinematicAttachment)
            {
                // Kinematic mode: directly set position
                Vector3 targetWorldPosition = boatDeck.TransformPoint(data.localPosition);
                data.rigidbody.MovePosition(targetWorldPosition);
                
                Quaternion targetWorldRotation = boatDeck.rotation * data.localRotation;
                data.rigidbody.MoveRotation(targetWorldRotation);
            }
            else
            {
                // Force mode: use physics forces to keep player with boat
                Vector3 targetWorldPosition = boatDeck.TransformPoint(data.localPosition);
                Vector3 positionError = targetWorldPosition - player.transform.position;
                
                // Calculate velocity needed to reach target
                Vector3 targetVelocity = positionError * rigidbodyDamping;
                
                // Add boat's velocity to maintain relative position
                if (boatRigidbody != null)
                {
                    targetVelocity += boatRigidbody.linearVelocity;
                }
                
                // Apply force to match target velocity
                Vector3 velocityError = targetVelocity - data.rigidbody.linearVelocity;
                Vector3 force = velocityError * rigidbodyAttachmentForce * Time.fixedDeltaTime;
                
                data.rigidbody.AddForce(force, ForceMode.Force);
            }
        }
    }
    
    /// <summary>
    /// Attaches a Rigidbody player to the boat using physics-based movement.
    /// </summary>
    private void AttachRigidbodyPlayerToBoat(GameObject player, Rigidbody playerRb)
    {
        // Store rigidbody data
        PlayerRigidbodyData data = new PlayerRigidbodyData
        {
            rigidbody = playerRb,
            localPosition = boatDeck.InverseTransformPoint(player.transform.position),
            localRotation = Quaternion.Inverse(boatDeck.rotation) * player.transform.rotation,
            wasKinematic = playerRb.isKinematic,
            wasUsingGravity = playerRb.useGravity,
            originalDrag = playerRb.linearDamping,
            originalAngularDrag = playerRb.angularDamping
        };
        
        playerRigidbodyData[player] = data;
        
        if (useKinematicAttachment)
        {
            // Make player kinematic to directly control position
            playerRb.isKinematic = true;
        }
        else
        {
            // Increase drag to stabilize player on boat
            playerRb.linearDamping = rigidbodyDamping;
            playerRb.angularDamping = rigidbodyDamping * 0.5f;
        }
        
        Log($"🔗 Rigidbody player {player.name} attached to boat (Mode: {(useKinematicAttachment ? "Kinematic" : "Force")})");
    }
    
    /// <summary>
    /// Detaches a Rigidbody player from the boat and restores original physics settings.
    /// </summary>
    private void DetachRigidbodyPlayerFromBoat(GameObject player)
    {
        if (playerRigidbodyData.TryGetValue(player, out PlayerRigidbodyData data))
        {
            if (data.rigidbody != null)
            {
                // Restore original rigidbody settings
                data.rigidbody.isKinematic = data.wasKinematic;
                data.rigidbody.useGravity = data.wasUsingGravity;
                data.rigidbody.linearDamping = data.originalDrag;
                data.rigidbody.angularDamping = data.originalAngularDrag;
                
                // Inherit boat's velocity to prevent sudden stop
                if (boatRigidbody != null)
                {
                    data.rigidbody.linearVelocity = boatRigidbody.linearVelocity;
                }
            }
            
            playerRigidbodyData.Remove(player);
            Log($"🔓 Rigidbody player {player.name} detached from boat");
        }
    }
    
    /// <summary>
    /// Gets the root player GameObject from a collider.
    /// Handles cases where the collider is on a child object (e.g., ragdoll limbs).
    /// </summary>
    private GameObject GetPlayerRoot(GameObject obj)
    {
        // Check if this object itself has the player tag on the root
        Transform current = obj.transform;
        
        while (current != null)
        {
            // Look for common player components to identify the root
            if (current.GetComponent<PlayerDeathOnImpact>() != null ||
                current.GetComponent<Rigidbody>() != null && current.CompareTag(playerTag))
            {
                return current.gameObject;
            }
            
            current = current.parent;
        }
        
        // Fallback: return the original object
        return obj;
    }
    
    /// <summary>
    /// Counts the number of alive players in the scene.
    /// Alive = active GameObject with Player tag at the ROOT level.
    /// Dead = inactive GameObject (disabled when dead).
    /// </summary>
    private int CountAlivePlayers()
    {
        RefreshKnownPlayers();
        
        int aliveCount = 0;
        foreach (GameObject player in knownPlayers)
        {
            // Only count if the GameObject is active (alive) and not null
            if (player != null && player.activeInHierarchy)
            {
                aliveCount++;
            }
        }
        
        if (showDetailedDebug)
        {
            Log($"🔍 Alive player count: {aliveCount} (Total known: {knownPlayers.Count})");
        }
        
        return aliveCount;
    }
    
    /// <summary>
    /// Refreshes the list of known players by finding root player objects.
    /// This avoids counting ragdoll limbs or child objects as separate players.
    /// </summary>
    private void RefreshKnownPlayers()
    {
        knownPlayers.Clear();
        
        // Find all PlayerDeathOnImpact components (unique to each player root)
        PlayerDeathOnImpact[] playerScripts = FindObjectsOfType<PlayerDeathOnImpact>(true); // Include inactive
        
        foreach (PlayerDeathOnImpact script in playerScripts)
        {
            if (script.CompareTag(playerTag))
            {
                knownPlayers.Add(script.gameObject);
            }
        }
        
        // Fallback: If no PlayerDeathOnImpact found, use FindGameObjectsWithTag
        if (knownPlayers.Count == 0)
        {
            GameObject[] allTaggedObjects = GameObject.FindGameObjectsWithTag(playerTag);
            
            // Filter to only root-level players (ones with Rigidbody or specific components)
            foreach (GameObject obj in allTaggedObjects)
            {
                if (obj.GetComponent<Rigidbody>() != null || 
                    obj.GetComponent<PlayerDeathOnImpact>() != null)
                {
                    knownPlayers.Add(obj);
                }
            }
        }
        
        if (showDetailedDebug && knownPlayers.Count > 0)
        {
            Log($"🔍 Known players: {string.Join(", ", knownPlayers.Select(p => p.name))}");
        }
    }
    
    /// <summary>
    /// Removes dead players from the tracking list.
    /// Call this when a player dies while on the boat.
    /// </summary>
    public void RemoveDeadPlayers()
    {
        // Create a list of dead players to remove
        List<GameObject> deadPlayers = new List<GameObject>();
        
        foreach (GameObject player in playersOnBoard)
        {
            if (player == null || !player.activeInHierarchy)
            {
                deadPlayers.Add(player);
            }
        }
        
        // Remove dead players
        foreach (GameObject deadPlayer in deadPlayers)
        {
            if (playersOnBoard.Remove(deadPlayer))
            {
                Log($"💀 Removed dead player from tracking: {deadPlayer?.name ?? "null"}");
                
                if (playerRigidbodyData.ContainsKey(deadPlayer))
                {
                    DetachRigidbodyPlayerFromBoat(deadPlayer);
                }
                else if (parentPlayersToBoat && deadPlayer != null)
                {
                    DetachPlayerFromBoat(deadPlayer);
                }
            }
        }
        
        // Check if we should start journey after removing dead players
        if (AllPlayersAboard && boatController != null && !boatController.IsMoving)
        {
            int alivePlayers = CountAlivePlayers();
            Log($"🚢 ALL ALIVE PLAYERS NOW ABOARD! ({playersOnBoard.Count}/{alivePlayers}) Starting journey...");
            OnAllPlayersBoarded?.Invoke();
            boatController.StartJourney();
        }
    }
    
    private void AttachPlayerToBoat(GameObject player)
    {
        // Store original parent
        if (!playerOriginalParents.ContainsKey(player))
        {
            playerOriginalParents[player] = player.transform.parent;
        }
        
        // Parent to boat
        player.transform.SetParent(boatDeck, true);
        Log($"🔗 Player {player.name} attached to boat");
    }
    
    private void DetachPlayerFromBoat(GameObject player)
    {
        // Restore original parent
        if (playerOriginalParents.TryGetValue(player, out Transform originalParent))
        {
            player.transform.SetParent(originalParent, true);
            playerOriginalParents.Remove(player);
            Log($"🔓 Player {player.name} detached from boat");
        }
        else
        {
            // No original parent stored, unparent completely
            player.transform.SetParent(null, true);
        }
    }
    
    /// <summary>
    /// Debug helper to log all player details.
    /// </summary>
    private void LogPlayerDetails()
    {
        Log("═══ PLAYER DEBUG INFO ═══");
        Log($"Players on board: {playersOnBoard.Count}");
        foreach (GameObject player in playersOnBoard)
        {
            Log($"  - {player.name} (Active: {player.activeInHierarchy})");
        }
        
        RefreshKnownPlayers();
        Log($"Total known players: {knownPlayers.Count}");
        foreach (GameObject player in knownPlayers)
        {
            Log($"  - {player.name} (Active: {player.activeInHierarchy})");
        }
        Log("═══════════════════════════");
    }
    
    private void OnDestroy()
    {
        // Detach all players on destroy
        foreach (GameObject player in playersOnBoard.ToList())
        {
            if (player != null)
            {
                if (playerRigidbodyData.ContainsKey(player))
                {
                    DetachRigidbodyPlayerFromBoat(player);
                }
                else
                {
                    DetachPlayerFromBoat(player);
                }
            }
        }
    }
    
    private void Log(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[PassengerTracker] {message}"); 
        }
    }
    
    private void OnDrawGizmos()
    {
        if (triggerZone == null) return;
        
        Gizmos.color = AllPlayersAboard ? Color.green : Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(triggerZone.center, triggerZone.size);
    }
}
