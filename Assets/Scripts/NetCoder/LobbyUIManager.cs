using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace DeathrunGame
{
    /// <summary>
    /// Manages all lobby UI panels and user interactions.
    /// Subscribes to LobbyManager events and updates UI accordingly.
    /// </summary>
    public class LobbyUIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject createLobbyPanel;
        [SerializeField] private GameObject browseLobbiesPanel;
        [SerializeField] private GameObject lobbyRoomPanel;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("UI Visual")]
        [SerializeField] private GameObject UIVisual;
        
        [Header("Camera")]
        [SerializeField] private Camera mainCamera; // Reference to Main Camera

        [Header("Main Menu")]
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button quickJoinButton;
        [SerializeField] private TMP_InputField playerNameInput;

        [Header("Create Lobby")]
        [SerializeField] private TMP_InputField lobbyNameInput;
        [SerializeField] private Toggle pcRunnerToggle;
        [SerializeField] private Toggle arDirectorToggle;
        [SerializeField] private ToggleGroup roleToggleGroup;
        [SerializeField] private Button createButton;
        [SerializeField] private Button createBackButton;

        [Header("Browse Lobbies")]
        [SerializeField] private Transform lobbyListContent;
        [SerializeField] private GameObject lobbyListItemPrefab;
        [SerializeField] private Button refreshLobbiesButton;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private Button joinByCodeButton;
        [SerializeField] private Button browseBackButton;
        [SerializeField] private TextMeshProUGUI noLobbiesText;

        [Header("Lobby Room")]
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private TextMeshProUGUI lobbyCodeText;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private Transform playerListContent;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TextMeshProUGUI waitingForHostText;

        [Header("Error Panel")]
        [SerializeField] private TextMeshProUGUI errorMessageText;
        [SerializeField] private Button closeErrorButton;

        [Header("Loading Panel")]
        [SerializeField] private TextMeshProUGUI loadingText;

        private bool isJoiningLobby = false;

        private void Start()
        {
            SetupButtons();
            SubscribeToLobbyEvents();
            LoadPlayerName();
            ShowMainMenu();
            
            // Ensure camera is orthographic for UI at start
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromLobbyEvents();
        }

        #region Setup

        private void SetupButtons()
        {
            // Main Menu
            createLobbyButton.onClick.AddListener(ShowCreateLobby);
            joinLobbyButton.onClick.AddListener(ShowBrowseLobbies);
            quickJoinButton.onClick.AddListener(OnQuickJoin);
            playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);

            // Create Lobby
            createButton.onClick.AddListener(OnCreateLobby);
            createBackButton.onClick.AddListener(ShowMainMenu);

            // Browse Lobbies
            refreshLobbiesButton.onClick.AddListener(OnRefreshLobbies);
            joinByCodeButton.onClick.AddListener(OnJoinByCode);
            browseBackButton.onClick.AddListener(ShowMainMenu);

            // Lobby Room
            copyCodeButton.onClick.AddListener(OnCopyLobbyCode);
            leaveButton.onClick.AddListener(OnLeaveLobby);
            startGameButton.onClick.AddListener(OnStartGame);

            // Error
            closeErrorButton.onClick.AddListener(HideError);
        }

        private void SubscribeToLobbyEvents()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.OnLobbyCreated += OnLobbyCreated;
                LobbyManager.Instance.OnLobbyJoined += OnLobbyJoined;
                LobbyManager.Instance.OnLobbyListUpdated += OnLobbyListUpdated;
                LobbyManager.Instance.OnLobbyError += ShowError;
                LobbyManager.Instance.OnPlayerListChanged += UpdatePlayerList;
                LobbyManager.Instance.OnGameStarted += OnGameStarted;
            }
        }

        private void UnsubscribeFromLobbyEvents()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.OnLobbyCreated -= OnLobbyCreated;
                LobbyManager.Instance.OnLobbyJoined -= OnLobbyJoined;
                LobbyManager.Instance.OnLobbyListUpdated -= OnLobbyListUpdated;
                LobbyManager.Instance.OnLobbyError -= ShowError;
                LobbyManager.Instance.OnPlayerListChanged -= UpdatePlayerList;
                LobbyManager.Instance.OnGameStarted -= OnGameStarted;
            }
        }

        private void LoadPlayerName()
        {
            string savedName = PlayerPrefs.GetString("PlayerName", "");
            if (!string.IsNullOrEmpty(savedName))
            {
                playerNameInput.text = savedName;
            }
        }

        #endregion

        #region Panel Navigation

        private void ShowMainMenu()
        {
            HideAllPanels();
            mainMenuPanel.SetActive(true);

            // Enable cursor for menu interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Switch back to orthographic for UI
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
            }
            
            // Show UI visual
            if (UIVisual != null)
            {
                UIVisual.SetActive(true);
            }
        }

        private void ShowCreateLobby()
        {
            HideAllPanels();
            createLobbyPanel.SetActive(true);

            // Generate default lobby name
            lobbyNameInput.text = $"Lobby_{Random.Range(1000, 9999)}";

            // Default to PC Runner
            pcRunnerToggle.isOn = true;
        }

        private void ShowBrowseLobbies()
        {
            HideAllPanels();
            browseLobbiesPanel.SetActive(true);
            OnRefreshLobbies();
        }

        private void ShowLobbyRoom()
        {
            HideAllPanels();
            lobbyRoomPanel.SetActive(true);

            // Show/hide UI elements based on host status
            bool isHost = LobbyManager.Instance.IsHost();
            startGameButton.gameObject.SetActive(isHost);

            if (waitingForHostText != null)
            {
                waitingForHostText.gameObject.SetActive(!isHost);
            }
        }

        private void ShowLoading(string message)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                if (loadingText != null)
                {
                    loadingText.text = message;
                }
            }
        }

        private void HideLoading()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        private void HideAllPanels()
        {
            mainMenuPanel.SetActive(false);
            createLobbyPanel.SetActive(false);
            browseLobbiesPanel.SetActive(false);
            lobbyRoomPanel.SetActive(false);
            errorPanel.SetActive(false);
            HideLoading();
            
          
        }

        #endregion

        #region Button Callbacks

        private void OnPlayerNameChanged(string newName)
        {
            if (!string.IsNullOrEmpty(newName))
            {
                LobbyManager.Instance.SetPlayerName(newName);
            }
        }

        private async void OnCreateLobby()
        {
            string lobbyName = lobbyNameInput.text.Trim();

            if (string.IsNullOrEmpty(lobbyName))
            {
                ShowError("Please enter a lobby name!");
                return;
            }

            // Get selected role
            LobbyManager.PlayerRole role = pcRunnerToggle.isOn ?
                LobbyManager.PlayerRole.PCRunner : LobbyManager.PlayerRole.ARDirector;

            createButton.interactable = false;
            ShowLoading("Creating lobby...");

            bool success = await LobbyManager.Instance.CreateLobby(lobbyName, role);

            HideLoading();
            createButton.interactable = true;

            if (success)
            {
                ShowLobbyRoom();
            }
        }

        private async void OnQuickJoin()
        {
            if (isJoiningLobby) return;
            isJoiningLobby = true;

            quickJoinButton.interactable = false;
            ShowLoading("Finding lobby...");

            var lobbies = await LobbyManager.Instance.GetAvailableLobbies();

            if (lobbies.Count > 0)
            {
                loadingText.text = "Joining lobby...";
                bool success = await LobbyManager.Instance.JoinLobby(lobbies[0].Id, LobbyManager.PlayerRole.PCRunner);

                if (success)
                {
                    ShowLobbyRoom();
                }
            }
            else
            {
                ShowError("No available lobbies found!");
            }

            HideLoading();
            quickJoinButton.interactable = true;
            isJoiningLobby = false;
        }

        private async void OnRefreshLobbies()
        {
            refreshLobbiesButton.interactable = false;
            ShowLoading("Refreshing lobbies...");

            await LobbyManager.Instance.GetAvailableLobbies();

            HideLoading();
            refreshLobbiesButton.interactable = true;
        }

        private async void OnJoinByCode()
        {
            string code = joinCodeInput.text.Trim().ToUpper();

            if (string.IsNullOrEmpty(code))
            {
                ShowError("Please enter a lobby code!");
                return;
            }

            if (isJoiningLobby) return;
            isJoiningLobby = true;

            joinByCodeButton.interactable = false;
            ShowLoading("Joining lobby...");

            // Default to PC Runner for quick join
            bool success = await LobbyManager.Instance.JoinLobbyByCode(code, LobbyManager.PlayerRole.PCRunner);

            HideLoading();
            joinByCodeButton.interactable = true;
            isJoiningLobby = false;

            if (success)
            {
                ShowLobbyRoom();
            }
        }

        private void OnCopyLobbyCode()
        {
            string code = LobbyManager.Instance.GetLobbyCode();
            if (!string.IsNullOrEmpty(code))
            {
                GUIUtility.systemCopyBuffer = code;
                Debug.Log($"📋 Copied lobby code: {code}");

                // Optional: Show temporary "Copied!" message
                if (copyCodeButton.GetComponentInChildren<TextMeshProUGUI>() != null)
                {
                    var buttonText = copyCodeButton.GetComponentInChildren<TextMeshProUGUI>();
                    string originalText = buttonText.text;
                    buttonText.text = "Copied!";
                    Invoke(nameof(ResetCopyButtonText), 2f);
                }
            }
        }

        private void ResetCopyButtonText()
        {
            if (copyCodeButton != null && copyCodeButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                copyCodeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Copy Code";
            }
        }

        private async void OnLeaveLobby()
        {
            leaveButton.interactable = false;
            ShowLoading("Leaving lobby...");

            await LobbyManager.Instance.LeaveLobby();

            HideLoading();
            leaveButton.interactable = true;
            ShowMainMenu();
        }

        private async void OnStartGame()
        {
            startGameButton.interactable = false;
            ShowLoading("Starting game...");

            bool success = await LobbyManager.Instance.StartGame();

            if (!success)
            {
                HideLoading();
                startGameButton.interactable = true;
            }
            // If success, OnGameStarted event will be triggered
        }

        #endregion

        #region Lobby Event Handlers

        private void OnLobbyCreated(Lobby lobby)
        {
            lobbyNameText.text = lobby.Name;
            lobbyCodeText.text = $"Code: {lobby.LobbyCode}";
            UpdatePlayerList(lobby.Players);
        }

        private void OnLobbyJoined(Lobby lobby)
        {
            lobbyNameText.text = lobby.Name;
            lobbyCodeText.text = $"Code: {lobby.LobbyCode}";
            UpdatePlayerList(lobby.Players);
        }

        private void OnLobbyListUpdated(List<Lobby> lobbies)
        {
            // Clear existing list
            foreach (Transform child in lobbyListContent)
            {
                Destroy(child.gameObject);
            }

            // Show/hide "no lobbies" message
            if (noLobbiesText != null)
            {
                noLobbiesText.gameObject.SetActive(lobbies.Count == 0);
            }

            // Populate list
            foreach (var lobby in lobbies)
            {
                GameObject item = Instantiate(lobbyListItemPrefab, lobbyListContent);
                var itemUI = item.GetComponent<LobbyListItem>();
                if (itemUI != null)
                {
                    itemUI.Setup(lobby, OnJoinLobbyFromList);
                }
            }
        }

        private async void OnJoinLobbyFromList(string lobbyId)
        {
            if (isJoiningLobby) return;
            isJoiningLobby = true;

            ShowLoading("Joining lobby...");

            // Default to PC Runner when joining from list
            bool success = await LobbyManager.Instance.JoinLobby(lobbyId, LobbyManager.PlayerRole.PCRunner);

            HideLoading();
            isJoiningLobby = false;

            if (success)
            {
                ShowLobbyRoom();
            }
        }

        private void UpdatePlayerList(List<Player> players)
        {
            // Clear existing list
            foreach (Transform child in playerListContent)
            {
                Destroy(child.gameObject);
            }

            // Populate player list
            foreach (var player in players)
            {
                GameObject item = Instantiate(playerListItemPrefab, playerListContent);
                var itemUI = item.GetComponent<PlayerListItem>();
                if (itemUI != null)
                {
                    string playerName = player.Data.ContainsKey("PlayerName") ?
                        player.Data["PlayerName"].Value : "Unknown Player";
                    string role = player.Data.ContainsKey("Role") ?
                        player.Data["Role"].Value : "PCRunner";

                    itemUI.Setup(playerName, role);
                }
            }

            // Force layout rebuild after a frame to ensure proper layout calculation
            StartCoroutine(RebuildLayoutNextFrame());
        }

        private System.Collections.IEnumerator RebuildLayoutNextFrame()
        {
            yield return null; // Wait one frame
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(playerListContent.GetComponent<RectTransform>());
        }

        private void OnGameStarted()
        {
            HideAllPanels();
            
            // Hide UI visual
            if (UIVisual != null)
            {
                UIVisual.SetActive(false);
                // Switch to perspective camera (already done in HideAllPanels)
                if (mainCamera != null)
                {
                    mainCamera.orthographic = false;
                    Debug.Log("🎥 Switched to Perspective camera!");
                }

            }
             
            // Disable the entire lobby UI GameObject
            gameObject.SetActive(false);
            Debug.Log("🎮 Game started! Hiding UI...");
        }

        #endregion

        #region Error Handling

        private void ShowError(string message)
        {
            errorMessageText.text = message;
            errorPanel.SetActive(true);
            HideLoading();
        }

        private void HideError()
        {
            errorPanel.SetActive(false);
        }

        #endregion
    }
}