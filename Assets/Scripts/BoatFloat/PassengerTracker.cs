using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks players who are on the boat using trigger collisions.
/// Communicates with BoatController to determine when all players are aboard.
/// Parents players to boat so they move with it.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class PassengerTracker : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Total number of players expected in the game")]
    [SerializeField] private int totalExpectedPlayers = 2;
    
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
    public bool AllPlayersAboard => playersOnBoard.Count >= totalExpectedPlayers;
    
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
            
            if (playersOnBoard.Add(player))
            {
                // Parent player to boat
                if (parentPlayersToBoat)
                {
                    AttachPlayerToBoat(player);
                }
                
                Log($"✅ Player boarded! ({playersOnBoard.Count}/{totalExpectedPlayers})");
                OnPlayerBoarded?.Invoke();
                
                // Check if all players are now aboard
                if (AllPlayersAboard)
                {
                    Log($"🚢 ALL PLAYERS ABOARD! Starting journey...");
                    OnAllPlayersBoarded?.Invoke();
                    
                    if (boatController != null)
                    {
                        boatController.StartJourney();
                    }
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
                
                Log($"❌ Player left boat! ({playersOnBoard.Count}/{totalExpectedPlayers})");
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
    
    public void SetExpectedPlayerCount(int count)
    {
        totalExpectedPlayers = Mathf.Max(1, count);
        Log($"Expected player count set to: {totalExpectedPlayers}");
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
