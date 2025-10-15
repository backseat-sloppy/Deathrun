using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class AutoLanConnector : MonoBehaviour
{
    [Header("Networking")]
    [SerializeField] ushort gamePort = 7777;        // NGO/UTP port
    [SerializeField] int beaconPort = 47777;     // UDP discovery port
    [SerializeField] float discoverWindow = 1.5f;  // seconds to listen for an existing host
    [SerializeField] Vector2 hostBackoffMs = new Vector2(150, 650);

    [Header("Debug")]
    [SerializeField] bool verboseLogs = true;

    // Optional inspector overrides (can also be set via command line flags)
    [Header("Overrides (optional)")]
    [SerializeField] bool forceHost = false;      // --host
    [SerializeField] string forceJoinIp = "";       // --join <ip>

    // Internals
    UdpClient _udpRx;
    UdpClient _udpTx;
    IPEndPoint _rxAnyEp = new IPEndPoint(IPAddress.Any, 0);
    string _myGuid;
    bool _isHosting;
    bool _connected;

    const string MAGIC = "MYGAME_V1"; // change per project/family if you want isolation

    // -------- Unity lifecycle --------
    void Awake()
    {
        Application.runInBackground = true;
        _myGuid = Guid.NewGuid().ToString("N");
        ParseArgs();                 // allow command-line to override inspector
    }

    void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnServerStarted += OnServerStarted;
            nm.OnClientConnectedCallback += OnClientConnected;
            nm.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnServerStarted -= OnServerStarted;
            nm.OnClientConnectedCallback -= OnClientConnected;
            nm.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    IEnumerator Start()
    {
        EnsureTransportAssigned();
        StartUdp();

        // Early overrides (command-line or inspector)
        if (!string.IsNullOrEmpty(forceJoinIp))
        {
            Log("[AutoLAN] DEBUG: Forcing direct client join " + forceJoinIp);
            StartClient(forceJoinIp, gamePort);
            yield break;
        }
        if (forceHost)
        {
            Log("[AutoLAN] DEBUG: Forcing host");
            if (StartHost())
            {
                StartCoroutine(BroadcastBeacons());
                StartCoroutine(HostTiebreakMonitor());
            }
            yield break;
        }

        // 1) Passive discovery
        Log("[AutoLAN] Listening for existing host…");
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < discoverWindow && !_connected)
        {
            if (TryReceiveBeacon(out Beacon b, out IPEndPoint from) && b.IsValid)
            {
                Log($"[AutoLAN] Found host {b.hostGuid}@{from.Address}:{b.port} → joining");
                StartClient(from.Address.ToString(), b.port);
                yield break;
            }
            yield return null;
        }

        // 2) No host heard → random backoff, double-check
        int delayMs = UnityEngine.Random.Range((int)hostBackoffMs.x, (int)hostBackoffMs.y);
        Log($"[AutoLAN] No host heard. Backing off {delayMs} ms, then attempting to host…");
        yield return new WaitForSeconds(delayMs / 1000f);

        if (TryReceiveBeacon(out Beacon b2, out IPEndPoint from2) && b2.IsValid && !_connected)
        {
            Log($"[AutoLAN] Host appeared during backoff {b2.hostGuid}@{from2.Address}. Joining.");
            StartClient(from2.Address.ToString(), b2.port);
            yield break;
        }

        // 3) Become host
        if (StartHost())
        {
            Log($"[AutoLAN] Hosting as GUID={_myGuid} on port {gamePort}. Broadcasting beacons.");
            StartCoroutine(BroadcastBeacons());
            StartCoroutine(HostTiebreakMonitor());
        }
        else
        {
            Warn("[AutoLAN] Failed to bind as host. Listening briefly for a host…");
            float t1 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t1 < 2f && !_connected)
            {
                if (TryReceiveBeacon(out Beacon b3, out IPEndPoint from3) && b3.IsValid)
                {
                    StartClient(from3.Address.ToString(), b3.port);
                    yield break;
                }
                yield return null;
            }
            Error("[AutoLAN] No host found and cannot host. Check firewall/LAN settings.");
        }
    }

    void OnDestroy()
    {
        try { _udpRx?.Close(); } catch { }
        try { _udpTx?.Close(); } catch { }
    }

    // -------- NGO wiring --------
    void EnsureTransportAssigned()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Error("[AutoLAN] NetworkManager.Singleton is null. Add NetworkManager to the scene.");
            return;
        }

        var utp = nm.GetComponent<UnityTransport>();
        if (utp == null) utp = nm.gameObject.AddComponent<UnityTransport>();

        if (nm.NetworkConfig.NetworkTransport == null)
            nm.NetworkConfig.NetworkTransport = utp;

        Log("[AutoLAN] Using transport: " + nm.NetworkConfig.NetworkTransport?.GetType().Name);

        if (nm.NetworkConfig.PlayerPrefab == null)
            Warn("[AutoLAN] Player Prefab is NOT assigned on NetworkManager (ok for transport test, but required when you spawn players).");
    }

    bool StartHost()
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        // listen on all interfaces on port 7777
        utp.SetConnectionData("0.0.0.0", gamePort, "0.0.0.0");
        bool ok = NetworkManager.Singleton.StartHost();
        if (ok) _isHosting = true;
        return ok;
    }

    void StartClient(string hostIp, ushort port)
    {
        if (_connected) return;
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData(hostIp, port);   // connect to host IP:port
        bool ok = NetworkManager.Singleton.StartClient();
        if (ok)
        {
            _connected = true;
            if (verboseLogs) Debug.Log($"[AutoLAN] Client connecting to {hostIp}:{port}");
        }
        else
        {
            Debug.LogError("[AutoLAN] Client failed to start.");
        }
    }

    void StopHostingAndJoin(string hostIp, ushort port)
    {
        Log("[AutoLAN] Demoting self (tiebreak lost). Reconnecting as client…");
        NetworkManager.Singleton.Shutdown();
        _isHosting = false;
        _connected = false;
        StartCoroutine(DelayedJoin(hostIp, port, 0.25f));
    }

    IEnumerator DelayedJoin(string ip, ushort port, float delay)
    {
        yield return new WaitForSeconds(delay);
        StartClient(ip, port);
    }

    // -------- Beacons (discovery) --------
    struct Beacon
    {
        public string magic;
        public string hostGuid;
        public ushort port;

        public bool IsValid => magic == MAGIC && !string.IsNullOrEmpty(hostGuid) && port > 0;
        public static Beacon Create(string guid, ushort p) => new Beacon { magic = MAGIC, hostGuid = guid, port = p };
        public override string ToString() => $"{magic}|{hostGuid}|{port}";
        public static bool TryParse(string s, out Beacon b)
        {
            b = default;
            if (string.IsNullOrEmpty(s)) return false;
            var parts = s.Split('|');
            if (parts.Length != 3) return false;
            if (parts[0] != MAGIC) return false;
            if (!ushort.TryParse(parts[2], out ushort p)) return false;
            b = new Beacon { magic = parts[0], hostGuid = parts[1], port = p };
            return true;
        }
    }

    void StartUdp()
    {
        try
        {
            // RX socket that allows multiple processes to bind the same UDP port (handy for same-PC testing)
            _udpRx = new UdpClient(AddressFamily.InterNetwork);
            _udpRx.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpRx.Client.ExclusiveAddressUse = false;
            _udpRx.Client.Bind(new IPEndPoint(IPAddress.Any, beaconPort));
            _udpRx.EnableBroadcast = true;
            _udpRx.Client.ReceiveTimeout = 1;

            // TX socket
            _udpTx = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
        }
        catch (Exception e)
        {
            Error($"[AutoLAN] UDP init failed: {e.Message}");
        }
    }

    IEnumerator BroadcastBeacons()
    {
        var payload = Encoding.UTF8.GetBytes(Beacon.Create(_myGuid, gamePort).ToString());
        var epBroadcast = new IPEndPoint(IPAddress.Broadcast, beaconPort);
        var epLoopback = new IPEndPoint(IPAddress.Loopback, beaconPort); // ensures second instance on same PC hears it

        while (_isHosting)
        {
            try
            {
                _udpTx.Send(payload, payload.Length, epBroadcast);
                _udpTx.Send(payload, payload.Length, epLoopback);
            }
            catch { /* ignore */ }
            yield return new WaitForSeconds(1f);
        }
    }

    bool TryReceiveBeacon(out Beacon b, out IPEndPoint from)
    {
        b = default; from = null;
        if (_udpRx == null) return false;
        try
        {
            if (_udpRx.Available > 0)
            {
                from = _rxAnyEp;
                byte[] data = _udpRx.Receive(ref from);
                string msg = Encoding.UTF8.GetString(data);
                if (Beacon.TryParse(msg, out b)) return true;
            }
        }
        catch { /* benign */ }
        return false;
    }

    IEnumerator HostTiebreakMonitor()
    {
        while (_isHosting)
        {
            if (TryReceiveBeacon(out Beacon b, out IPEndPoint from) && b.IsValid)
            {
                bool otherWins = string.CompareOrdinal(b.hostGuid, _myGuid) < 0;
                if (otherWins)
                {
                    Log($"[AutoLAN] Detected competing host {b.hostGuid}. It wins. Joining {from.Address}:{b.port}");
                    StopHostingAndJoin(from.Address.ToString(), b.port);
                    yield break;
                }
            }
            yield return null;
        }
    }

    // -------- NGO event logs (nice for Player.log) --------
    void OnServerStarted()
    {
        Log("[AutoLAN] Server started (host). Local IPv4s:");
        foreach (var ip in GetLocalIPv4sAll())
            Log($"[AutoLAN]  - {ip}:{gamePort}");
    }

    void OnClientConnected(ulong clientId)
    {
        Log($"[AutoLAN] Client connected: {clientId} (IsHost={NetworkManager.Singleton.IsHost})");
    }

    void OnClientDisconnected(ulong clientId)
    {
        string reason = NetworkManager.Singleton.DisconnectReason;
        Warn($"[AutoLAN] Client disconnected: {clientId}. Reason='{reason}'");
    }

    // -------- Helpers --------
    static string[] GetLocalIPv4sAll()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                          nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses
                .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(ua => $"{ua.Address} [{nic.Name}]"))
            .Distinct()
            .ToArray();
    }

    void ParseArgs()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a.Equals("--host", StringComparison.OrdinalIgnoreCase)) forceHost = true;
                else if (a.Equals("--join", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    forceJoinIp = args[++i];
                else if (a.Equals("--port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length
                         && ushort.TryParse(args[i + 1], out var p)) { gamePort = p; i++; }
                else if (a.Equals("--beaconPort", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length
                         && int.TryParse(args[i + 1], out var bp)) { beaconPort = bp; i++; }
                else if (a.Equals("--discover", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length
                         && float.TryParse(args[i + 1], out var dw)) { discoverWindow = Mathf.Max(0.1f, dw); i++; }
                else if (a.Equals("--verbose", StringComparison.OrdinalIgnoreCase)) verboseLogs = true;
            }
        }
        catch { /* ignore */ }
    }

    void Log(string msg) { if (verboseLogs) Debug.Log(msg); }
    void Warn(string msg) { if (verboseLogs) Debug.LogWarning(msg); }
    void Error(string msg) { Debug.LogError(msg); }
}
