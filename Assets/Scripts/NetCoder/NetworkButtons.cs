using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using UnityEngine.Networking;

public class NetworkButtons : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button CopyIpAddress;

    public NetworkButtons(Button copyIpAddress)
    {
        CopyIpAddress = copyIpAddress;
    }

    [SerializeField] private ushort port = 7777;
    [SerializeField] private string connectToIP;

    private string cachedPublicIP = null;

    private void Awake()
    {
        hostButton.onClick.AddListener(() =>
        {
            ConfigureTransportForHost();
            NetworkManager.Singleton.StartHost();
            CopyIpAddress.gameObject.SetActive(true);

            // Fetch public IP when hosting
            StartCoroutine(GetPublicIPAddress((publicIP) =>
            {
                cachedPublicIP = publicIP;
            }));

            // Disable the host and client buttons (not the whole canvas)
            hostButton.interactable = false;
            clientButton.interactable = false;
        });

        clientButton.onClick.AddListener(() =>
        {
            ConfigureTransportForClient();
            NetworkManager.Singleton.StartClient();
            
            // Disable both buttons when joining as client
            hostButton.interactable = false;
            clientButton.interactable = false;
        });

        CopyIpAddress.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(cachedPublicIP))
            {
                string fullAddress = $"{cachedPublicIP}:{port}";
                GUIUtility.systemCopyBuffer = fullAddress;
                Debug.Log($"=== CONNECTION INFO ===");
                Debug.Log($"Public IP (Internet): {fullAddress}");
                Debug.Log($"Local IP (LAN): {GetLocalIPAddress()}:{port}");
                Debug.Log($"Copied PUBLIC IP to clipboard: {fullAddress}");
                Debug.Log($"Remember to port forward {port} in your router!");
            }
            else
            {
                // Fallback to local IP if public IP fetch failed
                string localIP = GetLocalIPAddress();
                string fullAddress = $"{localIP}:{port}";
                GUIUtility.systemCopyBuffer = fullAddress;
                Debug.LogWarning($"Public IP not available. Copied LOCAL IP: {fullAddress}");
            }
        });
    }

    private void ConfigureTransportForHost()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // Listen on all interfaces (0.0.0.0) so clients from internet can connect
            transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            Debug.Log($"Host configured to listen on port {port}");
        }
    }

    private void ConfigureTransportForClient()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // Connect to the IP specified in inspector
            transport.SetConnectionData(connectToIP, port);
            Debug.Log($"Client configured to connect to {connectToIP}:{port}");
        }
    }

    private IEnumerator GetPublicIPAddress(System.Action<string> callback)
    {
        // Try multiple services in case one is down
        string[] ipServices = new string[]
        {
            "https://api.ipify.org",           // Most reliable
            "https://icanhazip.com",           // Backup
            "https://checkip.amazonaws.com"    // AWS backup
        };

        foreach (string service in ipServices)
        {
            UnityWebRequest request = UnityWebRequest.Get(service);
            request.timeout = 5; // 5 second timeout

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string publicIP = request.downloadHandler.text.Trim();
                Debug.Log($"Public IP retrieved from {service}: {publicIP}");
                callback?.Invoke(publicIP);
                yield break; // Success, stop trying other services
            }
            else
            {
                Debug.LogWarning($"Failed to get IP from {service}: {request.error}");
            }
        }

        // All services failed
        Debug.LogError("Failed to retrieve public IP from all services. Using local IP as fallback.");
        callback?.Invoke(GetLocalIPAddress());
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get IP: {e.Message}");
        }
        return "127.0.0.1";
    }
}
