using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Manages Unity Lobby Service integration for asymmetric multiplayer.
    /// Handles lobby creation, joining, player roles (PC Runner vs AR Director), and Relay integration.
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        [Header("Lobby Settings")]
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private int maxARPlayers = 1; // Only 1 AR Director allowed
        [SerializeField] private float lobbyHeartbeatInterval = 15f;
        [SerializeField] private float lobbyPollInterval = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Events for UI to subscribe to
        public event Action<Lobby> OnLobbyCreated;
        public event Action<Lobby> OnLobbyJoined;
        public event Action<List<Lobby>> OnLobbyListUpdated;
        public event Action<string> OnLobbyError;
        public event Action<List<Player>> OnPlayerListChanged;
        public event Action OnGameStarted;

        private Lobby currentLobby;
        private string currentRelayJoinCode;
        private bool isHost = false;
        private float nextHeartbeat;
        private float nextPollTime;

        // Player role data
        public enum PlayerRole
        {
            PCRunner,
            ARDirector
        }

        private PlayerRole myRole = PlayerRole.PCRunner;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Log("LobbyManager initialized");
        }

        private void Update()
        {
            HandleLobbyHeartbeat();
            HandleLobbyPolling();
        }

        private void OnApplicationQuit()
        {
            if (currentLobby != null)
            {
                LeaveLobby().GetAwaiter().GetResult();
            }
        }

        #endregion

        #region Lobby Creation

        /// <summary>
        /// Create a new lobby with Relay integration
        /// </summary>
        /// <param name="lobbyName">Name of the lobby</param>
        /// <param name="hostRole">Role of the host (PC or AR)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> CreateLobby(string lobbyName, PlayerRole hostRole)
        {
            try
            {
                myRole = hostRole;

                Log($"Creating lobby: {lobbyName} as {hostRole}");

                // Create Relay allocation first
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
                currentRelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                Log($"✅ Relay allocation created - Join Code: {currentRelayJoinCode}");

                // Create lobby options
                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = GetPlayerData(hostRole),
                    Data = new Dictionary<string, DataObject>
                    {
                        { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, currentRelayJoinCode) },
                        { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Asymmetric") },
                        { "ARSlotTaken", new DataObject(DataObject.VisibilityOptions.Public, (hostRole == PlayerRole.ARDirector).ToString()) },
                        { "StartTime", new DataObject(DataObject.VisibilityOptions.Member, "0") },
                        { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "false") }
                    }
                };

                currentLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                isHost = true;

                Log($"✅ Lobby created successfully!");
                Log($"   Lobby Name: {currentLobby.Name}");
                Log($"   Lobby ID: {currentLobby.Id}");
                Log($"   Lobby Code: {currentLobby.LobbyCode}");
                Log($"   Max Players: {currentLobby.MaxPlayers}");

                // Setup Relay transport
                var relayServerData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                OnLobbyCreated?.Invoke(currentLobby);
                return true;
            }
            catch (LobbyServiceException e)
            {
                LogError($"Failed to create lobby: {e.Message} (Code: {e.ErrorCode})");
                OnLobbyError?.Invoke($"Failed to create lobby: {e.Message}");
                return false;
            }
            catch (RelayServiceException e)
            {
                LogError($"Failed to create Relay allocation: {e.Message}");
                OnLobbyError?.Invoke($"Failed to setup network: {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                LogError($"Unexpected error creating lobby: {e.Message}");
                OnLobbyError?.Invoke($"Unexpected error: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Lobby Joining

        /// <summary>
        /// Get list of available lobbies
        /// </summary>
        public async Task<List<Lobby>> GetAvailableLobbies()
        {
            try
            {
                Log("Fetching available lobbies...");

                var queryOptions = new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
                    },
                    Order = new List<QueryOrder>
                    {
                        new QueryOrder(false, QueryOrder.FieldOptions.Created)
                    }
                };

                var response = await Lobbies.Instance.QueryLobbiesAsync(queryOptions);

                Log($"✅ Found {response.Results.Count} available lobbies");

                OnLobbyListUpdated?.Invoke(response.Results);
                return response.Results;
            }
            catch (LobbyServiceException e)
            {
                LogError($"Failed to query lobbies: {e.Message}");
                OnLobbyError?.Invoke($"Failed to get lobbies: {e.Message}");
                return new List<Lobby>();
            }
        }

        /// <summary>
        /// Join an existing lobby by ID
        /// </summary>
        public async Task<bool> JoinLobby(string lobbyId, PlayerRole desiredRole)
        {
            try
            {
                Log($"Attempting to join lobby {lobbyId} as {desiredRole}");

                // Get lobby details first to check AR slot
                var lobby = await Lobbies.Instance.GetLobbyAsync(lobbyId);

                // Check if AR slot is available
                if (desiredRole == PlayerRole.ARDirector)
                {
                    if (lobby.Data["ARSlotTaken"].Value == "True")
                    {
                        LogError("AR Director slot is already taken!");
                        OnLobbyError?.Invoke("AR Director slot is already taken! Please join as PC Runner.");
                        return false;
                    }
                }

                myRole = desiredRole;

                var joinOptions = new JoinLobbyByIdOptions
                {
                    Player = GetPlayerData(desiredRole)
                };

                currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
                isHost = false;

                // Update AR slot status if we're taking it (only host can update lobby data)
                // This will be handled by polling - the host sees us join and updates it

                // Get Relay join code
                currentRelayJoinCode = currentLobby.Data["RelayJoinCode"].Value;

                Log($"✅ Joined lobby: {currentLobby.Name}");
                Log($"   Relay Join Code: {currentRelayJoinCode}");

                // Join Relay
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(currentRelayJoinCode);
                var relayServerData = new RelayServerData(joinAllocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                Log("✅ Connected to Relay server");

                OnLobbyJoined?.Invoke(currentLobby);

                return true;
            }
            catch (LobbyServiceException e)
            {
                LogError($"Failed to join lobby: {e.Message} (Code: {e.ErrorCode})");
                OnLobbyError?.Invoke($"Failed to join lobby: {e.Message}");
                return false;
            }
            catch (RelayServiceException e)
            {
                LogError($"Failed to join Relay: {e.Message}");
                OnLobbyError?.Invoke($"Failed to connect to game: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Join lobby by code (quick join)
        /// </summary>
        public async Task<bool> JoinLobbyByCode(string lobbyCode, PlayerRole desiredRole)
        {
            try
            {
                Log($"Attempting to join lobby with code: {lobbyCode} as {desiredRole}");

                myRole = desiredRole;

                var joinOptions = new JoinLobbyByCodeOptions
                {
                    Player = GetPlayerData(desiredRole)
                };

                currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
                isHost = false;

                // Check AR slot after joining
                if (desiredRole == PlayerRole.ARDirector)
                {
                    // Count AR players
                    int arCount = 0;
                    foreach (var player in currentLobby.Players)
                    {
                        if (player.Data.ContainsKey("Role") && player.Data["Role"].Value == PlayerRole.ARDirector.ToString())
                        {
                            arCount++;
                        }
                    }

                    if (arCount > maxARPlayers)
                    {
                        LogError("AR Director slot was already taken!");
                        await LeaveLobby();
                        OnLobbyError?.Invoke("AR Director slot is already taken! Please join as PC Runner.");
                        return false;
                    }
                }

                // Get Relay join code and join
                currentRelayJoinCode = currentLobby.Data["RelayJoinCode"].Value;

                Log($"✅ Joined lobby by code: {currentLobby.Name}");
                Log($"   Relay Join Code: {currentRelayJoinCode}");

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(currentRelayJoinCode);
                var relayServerData = new RelayServerData(joinAllocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                Log("✅ Connected to Relay server");

                OnLobbyJoined?.Invoke(currentLobby);

                return true;
            }
            catch (LobbyServiceException e)
            {
                LogError($"Failed to join lobby by code: {e.Message} (Code: {e.ErrorCode})");

                // Fixed: Use integer comparison instead of LobbyExceptionReason enum
                if (e.ErrorCode == 16006) // 16006 = Lobby not found
                {
                    OnLobbyError?.Invoke("Lobby code not found! Please check the code and try again.");
                }
                else
                {
                    OnLobbyError?.Invoke($"Failed to join: {e.Message}");
                }
                return false;
            }
            catch (RelayServiceException e)
            {
                LogError($"Failed to join Relay: {e.Message}");
                OnLobbyError?.Invoke($"Failed to connect to game: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Lobby Management

        /// <summary>
        /// Start the game (host only)
        /// </summary>
        public async Task<bool> StartGame()
        {
            if (!isHost || currentLobby == null)
            {
                OnLobbyError?.Invoke("Only the host can start the game!");
                return false;
            }

            try
            {
                Log("Starting game...");

                // Update lobby to indicate game is starting
                await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "StartTime", new DataObject(DataObject.VisibilityOptions.Member, DateTime.UtcNow.ToString()) },
                        { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "true") }
                    },
                    IsLocked = true // Lock lobby so no one else can join
                });

                Log("✅ Lobby locked and marked as started");

                // Start Netcode host
                bool started = NetworkManager.Singleton.StartHost();

                if (started)
                {
                    Log("✅ Netcode Host started successfully!");
                    OnGameStarted?.Invoke();
                    return true;
                }
                else
                {
                    LogError("Failed to start Netcode Host!");
                    OnLobbyError?.Invoke("Failed to start game network!");
                    return false;
                }
            }
            catch (LobbyServiceException e)
            {
                LogError($"Failed to start game: {e.Message}");
                OnLobbyError?.Invoke($"Failed to start game: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Leave current lobby
        /// </summary>
        public async Task LeaveLobby()
        {
            if (currentLobby == null) return;

            try
            {
                string playerId = AuthenticationService.Instance.PlayerId;
                string lobbyId = currentLobby.Id;

                Log($"Leaving lobby: {currentLobby.Name}");

                // If host, delete lobby; otherwise just leave
                if (isHost)
                {
                    await Lobbies.Instance.DeleteLobbyAsync(lobbyId);
                    Log("🗑️ Lobby deleted (host left)");
                }
                else
                {
                    await Lobbies.Instance.RemovePlayerAsync(lobbyId, playerId);
                    Log("👋 Left lobby");
                }

                currentLobby = null;
                isHost = false;
                currentRelayJoinCode = null;
            }
            catch (LobbyServiceException e)
            {
                LogError($"Failed to leave lobby: {e.Message}");
            }
        }

        /// <summary>
        /// Update AR slot status (host only)
        /// </summary>
        private async Task UpdateLobbyARSlot(bool isTaken)
        {
            if (currentLobby == null || !isHost) return;

            try
            {
                await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "ARSlotTaken", new DataObject(DataObject.VisibilityOptions.Public, isTaken.ToString()) }
                    }
                });

                Log($"AR slot updated: {(isTaken ? "Taken" : "Available")}");
            }
            catch (LobbyServiceException e)
            {
                LogError($"Failed to update AR slot: {e.Message}");
            }
        }

        #endregion

        #region Heartbeat & Polling

        private void HandleLobbyHeartbeat()
        {
            if (!isHost || currentLobby == null) return;

            if (Time.time >= nextHeartbeat)
            {
                nextHeartbeat = Time.time + lobbyHeartbeatInterval;
                SendHeartbeat();
            }
        }

        private async void SendHeartbeat()
        {
            try
            {
                await Lobbies.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                Log("💓 Heartbeat sent");
            }
            catch (LobbyServiceException e)
            {
                LogError($"Heartbeat failed: {e.Message}");
            }
        }

        private void HandleLobbyPolling()
        {
            if (currentLobby == null) return;

            if (Time.time >= nextPollTime)
            {
                nextPollTime = Time.time + lobbyPollInterval;
                PollLobbyUpdates();
            }
        }

        private async void PollLobbyUpdates()
        {
            try
            {
                currentLobby = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);

                // Update AR slot if we're host
                if (isHost)
                {
                    int arCount = 0;
                    foreach (var player in currentLobby.Players)
                    {
                        if (player.Data.ContainsKey("Role") && player.Data["Role"].Value == PlayerRole.ARDirector.ToString())
                        {
                            arCount++;
                        }
                    }

                    bool arTaken = arCount > 0;
                    if (currentLobby.Data["ARSlotTaken"].Value != arTaken.ToString())
                    {
                        await UpdateLobbyARSlot(arTaken);
                    }
                }

                OnPlayerListChanged?.Invoke(currentLobby.Players);

                // Check if game has started (for clients)
                if (!isHost && currentLobby.Data["GameStarted"].Value == "true")
                {
                    Log("🎮 Game started by host - connecting as client...");

                    // Game started, connect as client
                    bool started = NetworkManager.Singleton.StartClient();

                    if (started)
                    {
                        Log("✅ Connected as client!");
                        OnGameStarted?.Invoke();
                    }
                    else
                    {
                        LogError("Failed to connect as client!");
                    }
                }
            }
            catch (LobbyServiceException e)
            {
                LogError($"Poll failed: {e.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private Player GetPlayerData(PlayerRole role)
        {
            return new Player
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "Role", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, role.ToString()) },
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, GetPlayerName()) }
                }
            };
        }

        private string GetPlayerName()
        {
            // Check if player has saved name
            string savedName = PlayerPrefs.GetString("PlayerName", "");
            if (!string.IsNullOrEmpty(savedName))
            {
                return savedName;
            }

            // Generate random name
            return $"Player_{UnityEngine.Random.Range(1000, 9999)}";
        }

        public void SetPlayerName(string name)
        {
            PlayerPrefs.SetString("PlayerName", name);
            PlayerPrefs.Save();
        }

        public string GetLobbyCode()
        {
            return currentLobby?.LobbyCode ?? "";
        }

        public string GetLobbyId()
        {
            return currentLobby?.Id ?? "";
        }

        public bool IsHost()
        {
            return isHost;
        }

        public PlayerRole GetMyRole()
        {
            return myRole;
        }

        public Lobby GetCurrentLobby()
        {
            return currentLobby;
        }

        public bool IsInLobby()
        {
            return currentLobby != null;
        }

        #endregion

        #region Debug Logging

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[LobbyManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[LobbyManager] ❌ {message}");
        }

        #endregion
    }
}