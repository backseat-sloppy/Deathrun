using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Manager : MonoBehaviour
{
    [Header("Main Menu Content to Hide/Show")]
    [Tooltip("The InputField GameObject for the Player Name.")]
    [SerializeField] private GameObject playerNameInputObject;
    [Tooltip("The GameObject for the Quick Join Button.")]
    [SerializeField] private GameObject quickJoinButtonObject;
    [Tooltip("The GameObject for the Create Lobby Button.")]
    [SerializeField] private GameObject createLobbyButtonObject;
    [Tooltip("The GameObject for the Join Lobby Button.")]
    [SerializeField] private GameObject joinLobbyButtonObject;

    [Header("Panel References")]
    [Tooltip("The parent GameObject for the Create Lobby screen.")]
    [SerializeField] private GameObject createLobbyPanel; 

    [Tooltip("The parent GameObject for the lobby browser screen (used by Quick Join and Join Lobby).")]
    [SerializeField] private GameObject browseLobbiesPanel;
    
    
    // --- Helper Function ---

    /// <summary>
    /// Toggles the visibility of all buttons and input fields in the Main Menu, 
    /// leaving the TitleText (which is a sibling) active.
    /// </summary>
    private void SetMainMenuButtonsVisible(bool visible)
    {
        // Hide/Show individual elements that are children of MainMenuPanel
        if (playerNameInputObject != null) playerNameInputObject.SetActive(visible);
        if (quickJoinButtonObject != null) quickJoinButtonObject.SetActive(visible);
        if (createLobbyButtonObject != null) createLobbyButtonObject.SetActive(visible);
        if (joinLobbyButtonObject != null) joinLobbyButtonObject.SetActive(visible);
    }
    
    // --- Button Callbacks ---

    /// <summary>
    /// Called when the Quick Join button is pressed.
    /// </summary>
    public void OnQuickJoinButtonPressed()
    {
        // Play click sound
        if (UIAudioManager.Exists())
        {
            UIAudioManager.Instance.PlayClick();
        }

        // 1. Hide the Main Menu options (leaving TitleText visible)
        SetMainMenuButtonsVisible(false);

        // 2. Activate the Browse Lobbies Panel
        if (browseLobbiesPanel != null)
        {
            // We assume LobbyUIManager.OnQuickJoin() will handle the loading and joining.
            browseLobbiesPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the Create Lobby button is pressed.
    /// </summary>
    public void OnCreateLobbyButtonPressed()
    {
        // Play click sound
        if (UIAudioManager.Exists())
        {
            UIAudioManager.Instance.PlayClick();
        }

        // 1. Hide the Main Menu options (leaving TitleText visible)
        SetMainMenuButtonsVisible(false);

        // 2. Activate the Create Lobby Panel
        if (createLobbyPanel != null)
        {
            createLobbyPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Called when the Join Lobby button is pressed.
    /// </summary>
    public void OnJoinLobbyButtonPressed()
    {
        // Play click sound
        if (UIAudioManager.Exists())
        {
            UIAudioManager.Instance.PlayClick();
        }

        // 1. Hide the Main Menu options (leaving TitleText visible)
        SetMainMenuButtonsVisible(false);

        // 2. Activate the Browse Lobbies Panel
        if (browseLobbiesPanel != null)
        {
            browseLobbiesPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Called when any 'Back' button is pressed.
    /// Returns the user to the Main Menu.
    /// </summary>
    public void OnBackButtonPressed()
    {
        // Play back sound
        if (UIAudioManager.Exists())
        {
            UIAudioManager.Instance.PlayBack();
        }

        // 1. Deactivate all possible destination panels
        if (browseLobbiesPanel != null)
        {
            browseLobbiesPanel.SetActive(false);
        }
        if (createLobbyPanel != null)
        {
            createLobbyPanel.SetActive(false);
        }
        
        // 2. Activate the Main Menu options (showing the buttons, TitleText is already visible)
        SetMainMenuButtonsVisible(true);
    }
}