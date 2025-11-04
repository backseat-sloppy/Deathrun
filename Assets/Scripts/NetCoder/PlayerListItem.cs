using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace DeathrunGame
{
    /// <summary>
    /// UI component for individual player entries in the lobby room.
    /// </summary>
    public class PlayerListItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private Image roleIcon;
        [SerializeField] private Sprite pcIcon;
        [SerializeField] private Sprite arIcon;
        [SerializeField] private Color pcColor = new Color(0.3f, 0.6f, 1f); // Blue
        [SerializeField] private Color arColor = new Color(1f, 0.4f, 0.2f); // Orange

        public void Setup(string playerName, string role)
        {
            // Set player name
            playerNameText.text = playerName;

            // Set role text
            bool isAR = role == "ARDirector";
            roleText.text = isAR ? "AR Director" : "PC Runner";
            roleText.color = isAR ? arColor : pcColor;

            // Set role icon
            if (roleIcon != null)
            {
                roleIcon.sprite = isAR ? arIcon : pcIcon;
                roleIcon.color = isAR ? arColor : pcColor;
            }
        }
    }
}