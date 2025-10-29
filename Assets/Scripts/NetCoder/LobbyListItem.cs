using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using System;

namespace DeathrunGame
{
    /// <summary>
    /// UI component for individual lobby entries in the browse list.
    /// </summary>
    public class LobbyListItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI arStatusText;
        [SerializeField] private Image arStatusIcon;
        [SerializeField] private Button joinButton;
        [SerializeField] private Color arAvailableColor = Color.green;
        [SerializeField] private Color arTakenColor = Color.red;

        private string lobbyId;
        private Action<string> onJoinCallback;

        public void Setup(Lobby lobby, Action<string> onJoin)
        {
            lobbyId = lobby.Id;
            onJoinCallback = onJoin;

            // Set lobby name
            lobbyNameText.text = lobby.Name;

            // Set player count
            playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

            // Set AR status
            bool arTaken = lobby.Data.ContainsKey("ARSlotTaken") &&
                          lobby.Data["ARSlotTaken"].Value == "True";

            if (arStatusText != null)
            {
                arStatusText.text = arTaken ? "AR: Taken" : "AR: Available";
                arStatusText.color = arTaken ? arTakenColor : arAvailableColor;
            }

            if (arStatusIcon != null)
            {
                arStatusIcon.color = arTaken ? arTakenColor : arAvailableColor;
            }

            // Set button interactable based on if lobby is full
            joinButton.interactable = lobby.Players.Count < lobby.MaxPlayers;
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinClicked);
        }

        private void OnJoinClicked()
        {
            onJoinCallback?.Invoke(lobbyId);
        }
    }
}