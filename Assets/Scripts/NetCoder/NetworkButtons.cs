using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using System.Threading.Tasks;

public class NetworkButtons : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button copyJoinCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;

    [Header("UI Management")]
    [SerializeField] private Canvas menuCanvas; // Add this in Inspector - drag your main UI canvas here

    [Header("Relay Settings")]
    [SerializeField] private int maxConnections = 3;

    private string currentJoinCode;
    private bool isInitialized = false;

    private async void Start()
    {
        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            Debug.Log("🔄 Initializing Unity Services...");
            
            await UnityServices.InitializeAsync();
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"✅ Signed in as: {AuthenticationService.Instance.PlayerId}");
            }
            
            isInitialized = true;
            Debug.Log("✅ Unity Services initialized successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to initialize Unity Services: {e.Message}");
            isInitialized = false;
        }
    }

    private void Awake()
    {
        hostButton.onClick.AddListener(async () =>
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ Unity Services not initialized yet!");
                return;
            }

            hostButton.interactable = false;
            clientButton.interactable = false;
            
            await StartHostWithRelay();
        });

        clientButton.onClick.AddListener(async () =>
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ Unity Services not initialized yet!");
                return;
            }

            hostButton.interactable = false;
            clientButton.interactable = false;
            
            await StartClientWithRelay();
        });

        copyJoinCodeButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(currentJoinCode))
            {
                GUIUtility.systemCopyBuffer = currentJoinCode;
                Debug.Log($"📋 Copied join code to clipboard: {currentJoinCode}");
                Debug.Log("Share this code with friends!");
            }
            else
            {
                Debug.LogWarning("⚠️ No join code available!");
            }
        });
    }

    private async Task StartHostWithRelay()
    {
        try
        {
            Debug.Log("🎮 Creating Relay allocation...");
            
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            Debug.Log("═══════════════════════════════");
            Debug.Log("✅ RELAY ALLOCATION CREATED!");
            Debug.Log($"   Join Code: {currentJoinCode}");
            Debug.Log($"   Max Players: {maxConnections + 1}");
            Debug.Log($"   Region: {allocation.Region}");
            Debug.Log("═══════════════════════════════");
            
            var relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            bool success = NetworkManager.Singleton.StartHost();
            
            if (success)
            {
                Debug.Log("✅ Host started successfully with Relay!");
                copyJoinCodeButton.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("❌ Failed to start host!");
                hostButton.interactable = true;
                clientButton.interactable = true;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"❌ Relay allocation failed: {e.Message}");
            Debug.LogError($"   Error Code: {e.ErrorCode}");
            hostButton.interactable = true;
            clientButton.interactable = true;
        }
    }

    private async Task StartClientWithRelay()
    {
        try
        {
            string joinCode = joinCodeInputField.text.Trim().ToUpper();
            
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError("❌ Join code is empty! Please enter a join code.");
                hostButton.interactable = true;
                clientButton.interactable = true;
                return;
            }
            
            Debug.Log($"🎮 Joining Relay with code: {joinCode}");
            
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            Debug.Log("═══════════════════════════════");
            Debug.Log("✅ JOINED RELAY ALLOCATION!");
            Debug.Log($"   Host Address: {joinAllocation.RelayServer.IpV4}");
            Debug.Log($"   Region: {joinAllocation.Region}");
            Debug.Log("═══════════════════════════════");
            
            var relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            bool success = NetworkManager.Singleton.StartClient();
            
            if (success)
            {
                Debug.Log("✅ Client started successfully with Relay!");
                StartCoroutine(MonitorClientConnection());
            }
            else
            {
                Debug.LogError("❌ Failed to start client!");
                hostButton.interactable = true;
                clientButton.interactable = true;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"❌ Failed to join Relay: {e.Message}");
            Debug.LogError($"   Error Code: {e.ErrorCode}");
            
            if (e.ErrorCode == (int)RelayExceptionReason.JoinCodeNotFound)
            {
                Debug.LogError("   The join code is invalid or expired!");
            }
            
            hostButton.interactable = true;
            clientButton.interactable = true;
        }
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
        Debug.Log("✅ SERVER STARTED");
        Debug.Log($"   Using Unity Relay: YES");
        Debug.Log($"   Join Code: {currentJoinCode}");
        Debug.Log("═══════════════════════════════");
    }

    private void OnClientConnected(ulong clientId)
    {
        bool isLocalClient = clientId == NetworkManager.Singleton.LocalClientId;
        
        Debug.Log("═══════════════════════════════");
        Debug.Log($"✅ CLIENT CONNECTED (via Relay)");
        Debug.Log($"   Client ID: {clientId}");
        Debug.Log($"   Is Local: {isLocalClient}");
        Debug.Log($"   Total Clients: {NetworkManager.Singleton.ConnectedClients.Count}");
        
        if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) != null)
        {
            var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            Debug.Log($"   ✅ PLAYER SPAWNED!");
            Debug.Log($"      GameObject: {playerObj.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"   ⚠️ Player not spawned yet for client {clientId}");
        }
        Debug.Log("═══════════════════════════════");   

        // Hide UI canvas when local player connects
        if (isLocalClient)
        {
            HideMenuCanvas();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        string reason = NetworkManager.Singleton.DisconnectReason;
        Debug.LogWarning($"❌ CLIENT {clientId} DISCONNECTED");
        Debug.LogWarning($"   Reason: {reason}");

        // Show UI canvas again on disconnect (optional - for re-connection)
        bool wasLocalClient = clientId == NetworkManager.Singleton.LocalClientId;
        if (wasLocalClient)
        {
            ShowMenuCanvas();
        }
    }

    private void HideMenuCanvas()
    {
        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(false);
            Debug.Log("🎨 Menu canvas hidden - game started!");
            
            // Unlock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Debug.LogWarning("⚠️ Menu canvas reference not set in NetworkButtons!");
        }
    }

    private void ShowMenuCanvas()
    {
        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(true);
            Debug.Log("🎨 Menu canvas shown");
            
            // Unlock cursor for menu interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private IEnumerator MonitorClientConnection()
    {
        float timeout = 15f;
        float elapsed = 0f;
        
        Debug.Log("⏳ Monitoring client connection via Relay...");
        
        while (elapsed < timeout)
        {
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log($"✅ Client connected via Relay after {elapsed:F2}s!");
                yield break;
            }
            
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.LogError($"❌ Client connection TIMEOUT after {timeout}s");
    }
}
