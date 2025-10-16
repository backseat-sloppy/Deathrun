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
            bool success = NetworkManager.Singleton.StartHost();
            
            Debug.Log($"🎮 HOST STARTED: {success}");
            
            if (success)
            {
                CopyIpAddress.gameObject.SetActive(true);
                StartCoroutine(GetPublicIPAddress((publicIP) =>
                {
                    cachedPublicIP = publicIP;
                }));
            }

            hostButton.interactable = false;
            clientButton.interactable = false;
        });

        clientButton.onClick.AddListener(() =>
        {
            ConfigureTransportForClient();
            
            Debug.Log($"🎮 CLIENT STARTING...");
            Debug.Log($"   IsClient before: {NetworkManager.Singleton.IsClient}");
            Debug.Log($"   IsConnectedClient before: {NetworkManager.Singleton.IsConnectedClient}");
            
            bool success = NetworkManager.Singleton.StartClient();
            
            Debug.Log($"   StartClient() returned: {success}");
            Debug.Log($"   IsClient after: {NetworkManager.Singleton.IsClient}");
            
            if (success)
            {
                StartCoroutine(MonitorClientConnection());
            }
            
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

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("═══════════════════════════════");
        Debug.Log("✅ SERVER STARTED SUCCESSFULLY");
        Debug.Log($"   Player Prefab: {NetworkManager.Singleton.NetworkConfig.PlayerPrefab?.name ?? "NULL"}");
        Debug.Log($"   Prefabs List Count: {NetworkManager.Singleton.NetworkConfig.Prefabs.NetworkPrefabsLists.Count}");
        Debug.Log("═══════════════════════════════");
    }

    private void OnClientConnected(ulong clientId)
    {
        bool isLocalClient = clientId == NetworkManager.Singleton.LocalClientId;
        
        Debug.Log("═══════════════════════════════");
        Debug.Log($"✅ CLIENT CONNECTED!");
        Debug.Log($"   Client ID: {clientId}");
        Debug.Log($"   Is Local Client: {isLocalClient}");
        Debug.Log($"   Is Host: {NetworkManager.Singleton.IsHost}");
        Debug.Log($"   Is Server: {NetworkManager.Singleton.IsServer}");
        Debug.Log($"   Total Clients: {NetworkManager.Singleton.ConnectedClients.Count}");
        
        // Check for player object
        if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) != null)
        {
            var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            Debug.Log($"   ✅ PLAYER SPAWNED!");
            Debug.Log($"      GameObject: {playerObj.gameObject.name}");
            Debug.Log($"      NetworkObjectId: {playerObj.NetworkObjectId}");
            Debug.Log($"      IsOwner: {playerObj.IsOwner}");
        }
        else
        {
            Debug.LogWarning($"   ⚠️ PLAYER NOT SPAWNED FOR CLIENT {clientId}");
            
            // Additional diagnostics
            Debug.LogWarning($"   Player Prefab Assigned: {NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null}");
            Debug.LogWarning($"   Spawned Objects Count: {NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count}");
        }
        Debug.Log("═══════════════════════════════");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        string reason = NetworkManager.Singleton.DisconnectReason;
        Debug.LogError($"❌ CLIENT DISCONNECTED!");
        Debug.LogError($"   Client ID: {clientId}");
        Debug.LogError($"   Reason: {reason}");
    }

    private IEnumerator MonitorClientConnection()
    {
        float timeout = 10f;
        float elapsed = 0f;
        
        Debug.Log("⏳ Monitoring client connection...");
        
        while (elapsed < timeout)
        {
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log($"✅ Client connected after {elapsed:F2} seconds!");
                yield break;
            }
            
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.LogError($"❌ Client connection TIMEOUT after {timeout} seconds");
        Debug.LogError($"   IsClient: {NetworkManager.Singleton.IsClient}");
        Debug.LogError($"   IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
    }

    private void ConfigureTransportForHost()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            Debug.Log($"[HOST] Listening on 0.0.0.0:{port}");
        }
        else
        {
            Debug.LogError("[HOST] UnityTransport component not found!");
        }
    }

    private void ConfigureTransportForClient()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            string targetIP = connectToIP;
            ushort targetPort = port;

            if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
            {
                string[] parts = ipInputField.text.Split(':');
                targetIP = parts[0].Trim();
                if (parts.Length > 1 && ushort.TryParse(parts[1], out ushort parsedPort))
                {
                    targetPort = parsedPort;
                }
            }

            transport.SetConnectionData(targetIP, targetPort);
            Debug.Log($"[CLIENT] Connecting to {targetIP}:{targetPort}");
        }
        else
        {
            Debug.LogError("[CLIENT] UnityTransport component not found!");
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
