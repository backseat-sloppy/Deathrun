using UnityEngine;
using TMPro;

public class NameInputDropdown : MonoBehaviour
{
    public TMP_InputField nameInput;
    public RectTransform quickJoinButton;
    public RectTransform createLobbyButton;
    public RectTransform joinLobbyButton;
    public float spacing = 80f;          // vertical space between buttons
    public float slideDuration = 0.6f;   // time for dropdown to fully open

    private RectTransform[] buttons;
    private Vector2[] targetPositions;
    private float timer;
    private bool dropdownOpening;

    void Start()
    {
        buttons = new RectTransform[] { quickJoinButton, createLobbyButton, joinLobbyButton };
        targetPositions = new Vector2[buttons.Length];

        // Store where each button should end up (beneath the input)
        for (int i = 0; i < buttons.Length; i++)
        {
            targetPositions[i] = new Vector2(
                nameInput.GetComponent<RectTransform>().anchoredPosition.x,
                nameInput.GetComponent<RectTransform>().anchoredPosition.y - ((i + 1) * spacing)
            );

            // Start hidden at the input line
            buttons[i].anchoredPosition = nameInput.GetComponent<RectTransform>().anchoredPosition;
            buttons[i].gameObject.SetActive(false);
        }

        nameInput.textComponent.fontSize = 100;
    }

    void Update()
    {
        if (!dropdownOpening && Input.GetKeyDown(KeyCode.Return))
        {
            string playerName = nameInput.text.Trim();

            if (!string.IsNullOrEmpty(playerName))
            {
                nameInput.textComponent.fontSize = 40;
                nameInput.interactable = false;

                foreach (var btn in buttons)
                    btn.gameObject.SetActive(true);

                dropdownOpening = true;
                timer = 0f;
            }
        }

        if (dropdownOpening)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / slideDuration);
            t = EaseOutCubic(t);

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].anchoredPosition = Vector2.Lerp(
                    nameInput.GetComponent<RectTransform>().anchoredPosition,
                    targetPositions[i],
                    t
                );
            }

            if (t >= 1f)
                dropdownOpening = false;
        }
    }

    float EaseOutCubic(float t)
    {
        return 1 - Mathf.Pow(1 - t, 3);
    }
}