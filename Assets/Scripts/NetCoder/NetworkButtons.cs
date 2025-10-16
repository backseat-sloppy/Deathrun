using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public class NetworkButtons : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button serverButton;


    private void Awake()
    {
        hostButton.onClick.AddListener(() => { NetworkManager.Singleton.StartHost(); });
        clientButton.onClick.AddListener(() => { NetworkManager.Singleton.StartClient(); });
        serverButton.onClick.AddListener(() => { NetworkManager.Singleton.StartServer(); });
    }

}
