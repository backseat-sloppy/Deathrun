using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class buttonDisable : MonoBehaviour
{
    [SerializeField] private Button Host;
    [SerializeField] private Button Client;

    private IEnumerator coroutine;

    private void Start()
    {
        Host.onClick.AddListener(() =>
        {
            // Play click sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayClick();
            }

            Host.interactable = false;
            Client.interactable = false;
        });
        Client.onClick.AddListener(() =>
        {
            // Play click sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayClick();
            }

            Host.interactable = false;
            Client.interactable = false;
        });
    }


}
