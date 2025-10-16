using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using UnityEngine.Networking;
using TMPro;

public class NetworkButtons : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button CopyIpAddress;
    [SerializeField] private TMP_InputField ipInputField; 

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

            StartCoroutine(GetPublicIPAddress((publicIP) =>
            {
                cachedPublicIP = publicIP;
            }));

            hostButton.interactable = false;
            clientButton.interactable = false;
        });

        clientButton.onClick.AddListener(() =>
        {
            ConfigureTransportForClient();
            NetworkManager.Singleton.StartClient();
            
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
            transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            Debug.Log($"Host configured to listen on port {port}");
        }
    }

    private void ConfigureTransportForClient()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // Use input field value if provided, otherwise use Inspector default
            string targetIP = connectToIP;
            ushort targetPort = port;

            if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
            {
                // Parse "IP:PORT" format
                string[] parts = ipInputField.text.Split(':');
                targetIP = parts[0];
                if (parts.Length > 1 && ushort.TryParse(parts[1], out ushort parsedPort))
                {
                    targetPort = parsedPort;
                }
            }

            transport.SetConnectionData(targetIP, targetPort);
            Debug.Log($"Client configured to connect to {targetIP}:{targetPort}");
        }
    }

    private IEnumerator GetPublicIPAddress(System.Action<string> callback)
    {
        string[] ipServices = new string[]
        {
            "https://api.ipify.org",
            "https://icanhazip.com",
            "https://checkip.amazonaws.com"
        };

        foreach (string service in ipServices)
        {
            UnityWebRequest request = UnityWebRequest.Get(service);
            request.timeout = 5;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string publicIP = request.downloadHandler.text.Trim();
                Debug.Log($"Public IP retrieved from {service}: {publicIP}");
                callback?.Invoke(publicIP);
                yield break;
            }
            else
            {
                Debug.LogWarning($"Failed to get IP from {service}: {request.error}");
            }
        }

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
