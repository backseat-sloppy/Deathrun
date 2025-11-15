using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks players who are on the boat using trigger collisions.
/// Communicates with BoatController to determine when all players are aboard.
/// Parents players to boat so they move with it.
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
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    
    private HashSet<GameObject> playersOnBoard = new HashSet<GameObject>();
    private Dictionary<GameObject, Transform> playerOriginalParents = new Dictionary<GameObject, Transform>();
    private BoxCollider triggerZone;
    private BoatController boatController;
    
    // Event fired when all players board
    public event System.Action OnAllPlayersBoarded;
    public event System.Action OnPlayerBoarded;
    public event System.Action OnPlayerLeft;
    
    public int PlayersOnBoard => playersOnBoard.Count;
    public int TotalAlivePlayers => CountAlivePlayers();
    public bool AllPlayersAboard => playersOnBoard.Count >= TotalAlivePlayers && TotalAlivePlayers > 0;
    
    private void Awake()
    {
        triggerZone = GetComponent<BoxCollider>();
        triggerZone.isTrigger = true;
        
        boatController = GetComponentInParent<BoatController>();
        if (boatController == null)
        {
            Debug.LogError("PassengerTracker requires a BoatController component in parent!");
        }
        
        // If no specific deck transform, use boat root
        if (boatDeck == null)
        {
            boatDeck = boatController?.transform;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            GameObject player = other.gameObject;
            
            // Only track if player is alive (GameObject is active)
            if (!player.activeInHierarchy)
            {
                Log($"⚠️ Ignoring dead player: {player.name}");
                return;
            }
            
            if (playersOnBoard.Add(player))
            {
                // Parent player to boat
                if (parentPlayersToBoat)
                {
                    AttachPlayerToBoat(player);
                }
                
                int alivePlayers = CountAlivePlayers();
                Log($"✅ Player boarded! ({playersOnBoard.Count}/{alivePlayers} alive players)");
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
            GameObject player = other.gameObject;
            
            if (playersOnBoard.Remove(player))
            {
                // Unparent player from boat
                if (parentPlayersToBoat)
                {
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
    /// Counts the number of alive players in the scene.
    /// Alive = active GameObject with Player tag.
    /// Dead = inactive GameObject (disabled when dead).
    /// </summary>
    private int CountAlivePlayers()
    {
        // Find all objects with Player tag (includes inactive)
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag(playerTag);
        
        int aliveCount = 0;
        foreach (GameObject player in allPlayers)
        {
            // Only count if the GameObject is active (alive)
            if (player.activeInHierarchy)
            {
                aliveCount++;
            }
        }
        
        return aliveCount;
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
                
                if (parentPlayersToBoat && deadPlayer != null)
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
    
    private void OnDestroy()
    {
        // Detach all players on destroy
        foreach (GameObject player in playersOnBoard)
        {
            if (player != null)
            {
                DetachPlayerFromBoat(player);
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
