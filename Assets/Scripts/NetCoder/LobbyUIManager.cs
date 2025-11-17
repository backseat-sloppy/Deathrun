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
        [SerializeField] private GameObject lobbyRoomPanel;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("UI Visual")]
        [SerializeField] private GameObject UIVisual;
        
        [Header("Camera")]
        [SerializeField] private Camera mainCamera;

        [Header("Main Menu")]
        [SerializeField] private Button quickJoinButton;

        [Header("Lobby Room")]
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

        [Header("Auto Lobby")]
        [SerializeField] private bool autoCreateLobby = false;
        [SerializeField] private float autoCreateDelay = 5f;
        [SerializeField] private float autoJoinRetryDelay = 10f;
        [SerializeField] private int minPlayersToStart = 2;
        [SerializeField] private float startGameCountdown = 5f;

        private bool isJoiningLobby = false;
        private float autoTimer = 0f;
        private bool hasAttemptedAuto = false;
        private float countdownTimer = 0f;
        private bool isCountingDown = false;

        private void Start()
        {
            SetupButtons();
            SubscribeToLobbyEvents();
            
            ShowMainMenu();
            
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
            HandleAutoLobby();
            HandleAutoStart();
        }

        private void HandleAutoLobby()
        {
            if (!hasAttemptedAuto && !LobbyManager.Instance.IsInLobby())
            {
                autoTimer += Time.deltaTime;
                
                if (autoCreateLobby)
                {
                    // AR Director: Create lobby after delay
                    if (autoTimer >= autoCreateDelay)
                    {
                        hasAttemptedAuto = true;
                        AutoCreateLobby();
                    }
                }
                else
                {
                    // PC Runner: Try to join every X seconds
                    if (autoTimer >= autoJoinRetryDelay)
                    {
                        autoTimer = 0f;
                        AutoJoinLobby();
                    }
                }
            }
        }

        private void HandleAutoStart()
        {
            if (!LobbyManager.Instance.IsHost() || !LobbyManager.Instance.IsInLobby()) return;

            var lobby = LobbyManager.Instance.GetCurrentLobby();
            if (lobby == null) return;

            int playerCount = lobby.Players.Count;

            if (playerCount >= minPlayersToStart)
            {
                if (!isCountingDown)
                {
                    isCountingDown = true;
                    countdownTimer = startGameCountdown;
                    Debug.Log($"🎮 Auto-start countdown initiated! {playerCount}/{minPlayersToStart} players ready.");
                }

                countdownTimer -= Time.deltaTime;

                if (countdownTimer <= 0f)
                {
                    isCountingDown = false;
                    OnStartGame();
                }
            }
            else
            {
                if (isCountingDown)
                {
                    isCountingDown = false;
                    Debug.Log($"⏸️ Auto-start cancelled. Not enough players ({playerCount}/{minPlayersToStart}).");
                }
            }
        }

        private async void AutoCreateLobby()
        {
            Debug.Log("🤖 Auto-creating lobby as AR Director (Host)...");
            ShowLoading("Auto-creating lobby...");

            string lobbyName = $"AR_Lobby_{Random.Range(1000, 9999)}";
            bool success = await LobbyManager.Instance.CreateLobby(lobbyName, LobbyManager.PlayerRole.ARDirector);

            HideLoading();

            if (success)
            {
                ShowLobbyRoom();
            }
        }

        private async void AutoJoinLobby()
        {
            Debug.Log("🤖 Auto-joining lobby as PC Runner...");
            ShowLoading("Auto-joining lobby...");

            var lobbies = await LobbyManager.Instance.GetAvailableLobbies();

            if (lobbies.Count > 0)
            {
                bool success = await LobbyManager.Instance.JoinLobby(lobbies[0].Id, LobbyManager.PlayerRole.PCRunner);

                HideLoading();

                if (success)
                {
                    ShowLobbyRoom();
                }
            }
            else
            {
                HideLoading();
                Debug.Log("⚠️ No lobbies found. Retrying...");
            }
        }

        #region Setup

        private void SetupButtons()
        {
            // Main Menu
            if (quickJoinButton != null)
                quickJoinButton.onClick.AddListener(OnQuickJoin);

            // Lobby Room
            if (copyCodeButton != null)
                copyCodeButton.onClick.AddListener(OnCopyLobbyCode);
            
            if (leaveButton != null)
                leaveButton.onClick.AddListener(OnLeaveLobby);
            
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGame);

            // Error
            if (closeErrorButton != null)
                closeErrorButton.onClick.AddListener(HideError);
        }

        private void SubscribeToLobbyEvents()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.OnLobbyCreated += OnLobbyCreated;
                LobbyManager.Instance.OnLobbyJoined += OnLobbyJoined;
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
                LobbyManager.Instance.OnLobbyError -= ShowError;
                LobbyManager.Instance.OnPlayerListChanged -= UpdatePlayerList;
                LobbyManager.Instance.OnGameStarted -= OnGameStarted;
            }
        }

        #endregion

        #region Panel Navigation    

        private void ShowMainMenu()
        {
            HideAllPanels();
            mainMenuPanel.SetActive(true);
            
            // Set game phase to Menu
            if (GamePhaseManager.Instance != null)
            {
                GamePhaseManager.Instance.SetMenu();
            }

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

        private void ShowLobbyRoom()
        {
            HideAllPanels();
            lobbyRoomPanel.SetActive(true);

            bool isHost = LobbyManager.Instance.IsHost();
            
            if (startGameButton != null)
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
            lobbyRoomPanel.SetActive(false);
            errorPanel.SetActive(false);
            HideLoading();
        }

        #endregion

        #region Button Callbacks

        private async void OnQuickJoin()
        {
            if (isJoiningLobby) return;
            isJoiningLobby = true;

            // Play click sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayClick();
            }

            if (quickJoinButton != null)
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
            
            if (quickJoinButton != null)
                quickJoinButton.interactable = true;
            
            isJoiningLobby = false;
        }

        private void OnCopyLobbyCode()
        {
            // Play click sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayClick();
            }

            string code = LobbyManager.Instance.GetLobbyCode();
            if (!string.IsNullOrEmpty(code))
            {
                GUIUtility.systemCopyBuffer = code;
                Debug.Log($"📋 Copied lobby code: {code}");

                if (copyCodeButton != null && copyCodeButton.GetComponentInChildren<TextMeshProUGUI>() != null)
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
            // Play back sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayBack();
            }

            if (leaveButton != null)
                leaveButton.interactable = false;
            
            ShowLoading("Leaving lobby...");

            await LobbyManager.Instance.LeaveLobby();

            HideLoading();
            
            if (leaveButton != null)
                leaveButton.interactable = true;
            
            ShowMainMenu();
        }

        private async void OnStartGame()
        {
            // Play click sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayClick();
            }

            // Set game phase to Playing
            if (GamePhaseManager.Instance != null)
            {
                GamePhaseManager.Instance.SetPlaying();
            }

            if (startGameButton != null)
                startGameButton.interactable = false;
            
            ShowLoading("Starting game...");

            bool success = await LobbyManager.Instance.StartGame();

            if (!success)
            {
                HideLoading();
                
                if (startGameButton != null)
                    startGameButton.interactable = true;
            }
        }

        #endregion

        #region Lobby Event Handlers

        private void OnLobbyCreated(Lobby lobby)
        {
            if (lobbyCodeText != null)
                lobbyCodeText.text = $"Code: {lobby.LobbyCode}";
            
            UpdatePlayerList(lobby.Players);
        }

        private void OnLobbyJoined(Lobby lobby)
        {
            if (lobbyCodeText != null)
                lobbyCodeText.text = $"Code: {lobby.LobbyCode}";
            
            UpdatePlayerList(lobby.Players);
        }

        private void UpdatePlayerList(List<Player> players)
        {
            if (playerListContent == null) return;

            foreach (Transform child in playerListContent)
            {
                Destroy(child.gameObject);
            }

            if (playerListItemPrefab != null)
            {
                foreach (var player in players)
                {
                    GameObject item = Instantiate(playerListItemPrefab, playerListContent);
                    var itemUI = item.GetComponent<PlayerListItem>();
                    if (itemUI != null)
                    {
                        string playerName = player.Data.ContainsKey("PlayerName") ?
                            player.Data["PlayerName"].Value : "Player";
                        string role = player.Data.ContainsKey("Role") ?
                            player.Data["Role"].Value : "PCRunner";

                        itemUI.Setup(playerName, role);
                    }
                }
            }

            StartCoroutine(RebuildLayoutNextFrame());
        }

        private System.Collections.IEnumerator RebuildLayoutNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            
            if (playerListContent != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(playerListContent.GetComponent<RectTransform>());
            }
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
            if (errorMessageText != null)
                errorMessageText.text = message;
            
            errorPanel.SetActive(true);
            HideLoading();
        }

        private void HideError()
        {
            // Play back sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayBack();
            }

            errorPanel.SetActive(false);
        }

        #endregion
    }
}