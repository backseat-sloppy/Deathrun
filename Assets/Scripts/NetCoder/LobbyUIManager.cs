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
        [SerializeField] private Camera mainCamera;

        [Header("Main Menu")]
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button quickJoinButton;
        [SerializeField] private TMP_InputField playerNameInput;

        [Header("Role Selection")]
        [SerializeField] private Button pcRunnerButton;
        [SerializeField] private Button arDirectorButton;
        [SerializeField] private TextMeshProUGUI roleDisplayText;
        [SerializeField] private Color selectedButtonColor = Color.green;
        [SerializeField] private Color deselectedButtonColor = Color.white;

        [Header("Create Lobby")]
        [SerializeField] private TMP_InputField lobbyNameInput;
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

        [Header("Auto Join")]
        [SerializeField] private float autoJoinDelay = 10f;

        private bool isJoiningLobby = false;
        private bool isARDirectorSelected = false; // Track selected role
        private float autoJoinTimer = 0f;
        private bool hasAutoJoined = false;

        private void Start()
        {
            SetupButtons();
            SubscribeToLobbyEvents();
            LoadPlayerName();
            ShowMainMenu();
            
            // Initialize role selection to PC Runner
            SelectPCRunner();
            
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromLobbyEvents();
        }

        private void Update()
        {
            // Auto-join after delay if not in lobby
            if (!hasAutoJoined && !LobbyManager.Instance.IsInLobby())
            {
                autoJoinTimer += Time.deltaTime;
                
                if (autoJoinTimer >= autoJoinDelay)
                {
                    hasAutoJoined = true;
                    OnQuickJoin();
                }
            }
        }

        #region Setup

        private void SetupButtons()
        {
            // Main Menu
            createLobbyButton.onClick.AddListener(ShowCreateLobby);
            joinLobbyButton.onClick.AddListener(ShowBrowseLobbies);
            quickJoinButton.onClick.AddListener(OnQuickJoin);
            playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);

            // Role Selection Buttons
            if (pcRunnerButton != null)
                pcRunnerButton.onClick.AddListener(SelectPCRunner);
            
            if (arDirectorButton != null)
                arDirectorButton.onClick.AddListener(SelectARDirector);

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

        #region Role Selection

        private void SelectPCRunner()
        {
            isARDirectorSelected = false;
            UpdateRoleButtonVisuals();
            
            if (roleDisplayText != null)
                roleDisplayText.text = "Goblin Runner";
            
            Debug.Log("Role selected: PC Runner");
        }

        private void SelectARDirector()
        {
            isARDirectorSelected = true;
            UpdateRoleButtonVisuals();
            
            if (roleDisplayText != null)
                roleDisplayText.text = "TIKI GOD";
            
            Debug.Log("Role selected: AR Director");
        }

        private void UpdateRoleButtonVisuals()
        {
            if (pcRunnerButton != null)
            {
                var colors = pcRunnerButton.colors;
                colors.normalColor = isARDirectorSelected ? deselectedButtonColor : selectedButtonColor;
                pcRunnerButton.colors = colors;
            }

            if (arDirectorButton != null)
            {
                var colors = arDirectorButton.colors;
                colors.normalColor = isARDirectorSelected ? selectedButtonColor : deselectedButtonColor;
                arDirectorButton.colors = colors;
            }
        }

        private LobbyManager.PlayerRole GetSelectedRole()
        {
            return isARDirectorSelected ? LobbyManager.PlayerRole.ARDirector : LobbyManager.PlayerRole.PCRunner;
        }

        #endregion

        #region Panel Navigation

        private void ShowMainMenu()
        {
            HideAllPanels();
            mainMenuPanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
            }
            
            if (UIVisual != null)
            {
                UIVisual.SetActive(true);
            }
        }

        private void ShowCreateLobby()
        {
            HideAllPanels();
            createLobbyPanel.SetActive(true);
            lobbyNameInput.text = $"Lobby_{Random.Range(1000, 9999)}";
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

            createButton.interactable = false;
            ShowLoading("Creating lobby...");

            bool success = await LobbyManager.Instance.CreateLobby(lobbyName, GetSelectedRole());

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
                bool success = await LobbyManager.Instance.JoinLobby(lobbies[0].Id, GetSelectedRole());

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

            bool success = await LobbyManager.Instance.JoinLobbyByCode(code, GetSelectedRole());

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

                if (copyCodeButton.GetComponentInChildren<TextMeshProUGUI>() != null)
                {
                    var buttonText = copyCodeButton.GetComponentInChildren<TextMeshProUGUI>();
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
            foreach (Transform child in lobbyListContent)
            {
                Destroy(child.gameObject);
            }

            if (noLobbiesText != null)
            {
                noLobbiesText.gameObject.SetActive(lobbies.Count == 0);
            }

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

            bool success = await LobbyManager.Instance.JoinLobby(lobbyId, GetSelectedRole());

            HideLoading();
            isJoiningLobby = false;

            if (success)
            {
                ShowLobbyRoom();
            }
        }

        private void UpdatePlayerList(List<Player> players)
        {
            foreach (Transform child in playerListContent)
            {
                Destroy(child.gameObject);
            }

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

            StartCoroutine(RebuildLayoutNextFrame());
        }

        private System.Collections.IEnumerator RebuildLayoutNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(playerListContent.GetComponent<RectTransform>());
        }

        private void OnGameStarted()
        {
            HideAllPanels();
            
            if (UIVisual != null)
            {
                UIVisual.SetActive(false);
                if (mainCamera != null)
                {
                    mainCamera.orthographic = false;
                    Debug.Log("🎥 Switched to Perspective camera!");
                }
            }
             
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