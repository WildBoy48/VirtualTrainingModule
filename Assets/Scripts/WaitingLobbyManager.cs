using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the waiting lobby, handling server connection and UI updates.
/// 
/// Attach this component to a GameObject in the Waiting Lobby scene.
/// Assign the InputField and Text components in the Inspector.
/// 
/// Features:
/// - Displays default server IP address in InputField
/// - Allows player to modify the server IP address
/// - Connects to the server via WebSocket
/// - Displays real-time connection status with color coding
/// - Color: Green when connected, Red when disconnected
/// </summary>
public class WaitingLobbyManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private InputField serverIpInputField;
    [SerializeField] private Text serverStatusText;

    [Header("Server Configuration")]
    [SerializeField] private string defaultServerIp = "localhost";
    [SerializeField] private int serverPort = 3000;
    [SerializeField] private float connectionCheckInterval = 2f; // Check every 2 seconds

    private ClientWebSocket _ws;
    private CancellationTokenSource _cts;
    private bool _isConnected = false;
    private string _currentServerIp;
    private float _lastConnectionCheckTime;

    // Status display colors
    private Color _connectedColor = Color.green;
    private Color _disconnectedColor = Color.red;
    
    private static WaitingLobbyManager _instance;
    public static string CurrentMode { get; set; } = "none";
    public static int CurrentMiniGameID { get; private set; } = -1;
    public static string CurrentPatientID { get; private set; } = string.Empty;
    public static GameConfig CurrentConfig { get; private set; } = new GameConfig();

    // Scene Setup Settings
    public static float SeatHeight { get; set; } = 1f;
    public static int BackgroundDetail { get; set; } = 1;
    public static bool VisualCues { get; set; } = false;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        // Validate references
        if (serverIpInputField == null)
        {
            Debug.LogError("[WaitingLobbyManager] ServerIpInputField is not assigned!");
            return;
        }

        if (serverStatusText == null)
        {
            Debug.LogError("[WaitingLobbyManager] ServerStatusText is not assigned!");
            return;
        }

        // Initialize WebSocket cancellation token
        _cts = new CancellationTokenSource();

        // Set default IP address in input field
        _currentServerIp = defaultServerIp;
        serverIpInputField.text = _currentServerIp;

        Debug.Log("[WaitingLobbyManager] Awake — initialized with default server: " + _currentServerIp);
    }

    private async void Start()
    {
        // Set up input field listener for when the player changes the IP
        serverIpInputField.onEndEdit.AddListener(OnServerIpChanged);

        // Initialize status display
        UpdateStatusDisplay();
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Attempt initial connection
        Debug.Log("[WaitingLobbyManager] Start — attempting initial connection to " + _currentServerIp);
        await ConnectToServerAsync();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex != 0)
        {
            serverIpInputField = null;
            serverStatusText = null;
            return;
        }
        // Rebind UI
        serverIpInputField = FindObjectOfType<InputField>();
        serverStatusText = FindObjectOfType<Text>();

        // Re-evaluate connection state
        _isConnected = _ws != null && _ws.State == WebSocketState.Open;

        if (serverIpInputField != null)
        {
            serverIpInputField.onEndEdit.RemoveAllListeners(); // prevent stacking
            serverIpInputField.onEndEdit.AddListener(OnServerIpChanged);
            serverIpInputField.text = _currentServerIp;
        }

        UpdateStatusDisplay();
        Debug.Log("[WaitingLobbyManager] Seat Height: " + SeatHeight);
        Debug.Log("[WaitingLobbyManager] Background Detail: " + BackgroundDetail);
    }

    private void Update()
    {
        // Periodically check connection status and reconnect if needed
        _lastConnectionCheckTime += Time.deltaTime;
        if (_lastConnectionCheckTime >= connectionCheckInterval)
        {
            _lastConnectionCheckTime = 0f;
            CheckConnectionAndReconnect();
        }
    }
    private async void OnDestroy()
    {
        if (_instance != this) return;

        Debug.Log("[WaitingLobbyManager] OnDestroy — closing WebSocket connection");        
        SceneManager.sceneLoaded -= OnSceneLoaded;
        await DisconnectAsync();
    }

    // ── UI Event Handlers ──────────────────────────────────────────────────

    private async void OnServerIpChanged(string newIp)
    {
        if (string.IsNullOrWhiteSpace(newIp))
        {
            Debug.LogWarning("[WaitingLobbyManager] Server IP cannot be empty");
            serverIpInputField.text = _currentServerIp;
            return;
        }

        if (newIp != _currentServerIp)
        {
            _currentServerIp = newIp;
            Debug.Log("[WaitingLobbyManager] Server IP changed to: " + _currentServerIp);

            // Disconnect from old server and connect to new one
            await DisconnectAsync();
            await ConnectToServerAsync();
        }
    }

    // ── Server Connection ──────────────────────────────────────────────────

    private async Task ConnectToServerAsync()
    {
        try
        {
            // Clean up any existing connection
            if (_ws != null)
            {
                _ws.Dispose();
            }

            _ws = new ClientWebSocket();
            string wsUrl = "ws://" + _currentServerIp + ":" + serverPort;

            Debug.Log("[WaitingLobbyManager] Attempting to connect to: " + wsUrl);

            // Set a timeout for the connection attempt
            using (var timeoutCts = new CancellationTokenSource(5000))
            {
                await _ws.ConnectAsync(new Uri(wsUrl), timeoutCts.Token);
            }

            _isConnected = true;
            Debug.Log("[WaitingLobbyManager] Successfully connected to server!");
            UpdateStatusDisplay();

            // Send identification message to notify server and Angular project that game is ready
            await SendIdentificationMessageAsync();

            // Start listening for messages from the server
            _ = ListenForMessagesAsync();
        }
        catch (OperationCanceledException)
        {
            _isConnected = false;
            Debug.LogWarning("[WaitingLobbyManager] Connection attempt timed out");
            UpdateStatusDisplay();
        }
        catch (Exception ex)
        {
            _isConnected = false;
            Debug.LogWarning("[WaitingLobbyManager] Failed to connect to server: " + ex.Message);
            UpdateStatusDisplay();
        }
    }

    private async Task DisconnectAsync()
    {
        try
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }

            _ws?.Dispose();
            _ws = null;
            _isConnected = false;
            UpdateStatusDisplay();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[WaitingLobbyManager] Error during disconnect: " + ex.Message);
        }
    }

    private async Task SendIdentificationMessageAsync()
    {
        try
        {
            if (_ws == null || _ws.State != WebSocketState.Open)
            {
                Debug.LogWarning("[WaitingLobbyManager] Cannot send identification: WebSocket not open");
                return;
            }

            // Send identification message: { "client": "unity" }
            // This tells the server (and Angular project) that the game is ready
            string identificationMessage = JsonUtility.ToJson(new ClientIdentification { client = "unity" });
            byte[] buffer = Encoding.UTF8.GetBytes(identificationMessage);

            await _ws.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            Debug.Log("[WaitingLobbyManager] Identification message sent to server: " + identificationMessage);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[WaitingLobbyManager] Failed to send identification message: " + ex.Message);
        }
    }

    private async Task ListenForMessagesAsync()
    {
        if (_ws == null || _ws.State != WebSocketState.Open)
        {
            return;
        }

        byte[] buffer = new byte[1024];

        try
        {
            while (_ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await _ws.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cts.Token
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("[WaitingLobbyManager] Server closed the connection");
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    _isConnected = false;
                    UpdateStatusDisplay();
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.Log("[WaitingLobbyManager] Received message from server: " + message);
                    HandleServerCommand(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[WaitingLobbyManager] Listening task was cancelled");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[WaitingLobbyManager] Error while listening for messages: " + ex.Message);
            _isConnected = false;
            UpdateStatusDisplay();
        }
    }

    private void CheckConnectionAndReconnect()
    {
        // If WebSocket is not open, mark as disconnected
        if (_ws == null || _ws.State != WebSocketState.Open)
        {
            if (_isConnected)
            {
                _isConnected = false;
                Debug.LogWarning("[WaitingLobbyManager] Connection lost, will attempt to reconnect");
                UpdateStatusDisplay();
            }

            // Attempt to reconnect
            _ = ConnectToServerAsync();
        }
    }

    // ── UI Display ─────────────────────────────────────────────────────────

    private void UpdateStatusDisplay()
    {
        if (serverStatusText == null)
        {
            return;
        }

        bool connected = IsActuallyConnected();

        serverStatusText.text = connected ? "● Connected" : "● Disconnected";
        serverStatusText.color = connected ? _connectedColor : _disconnectedColor;
    }

    private bool IsActuallyConnected()
    {
        return _ws != null && _ws.State == WebSocketState.Open;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    private void HandleServerCommand(string message)
    {
        try
        {
            var command = JsonUtility.FromJson<ServerCommand>(message);
            if (command == null)
            {
                return;
            }

            if (string.Equals(command.type, "load_scene", StringComparison.OrdinalIgnoreCase))
            {
                SetGameMode(command.mode);
                if (command.config != null)
                {
                    CurrentConfig = command.config;
                }
                if (!string.IsNullOrEmpty(command.patientID))
                {
                    CurrentPatientID = command.patientID;
                }
                if (command.miniGameID > 0)
                {
                    CurrentMiniGameID = command.miniGameID;
                }

                SeatHeight = CurrentConfig.seatHeight;
                BackgroundDetail = CurrentConfig.backgroundDetail;
                VisualCues = CurrentConfig.visualCues;

                Debug.Log($"[WaitingLobbyManager] Loading scene ID {command.sceneID} in mode {CurrentMode}");
                SceneManager.LoadScene(command.sceneID);
            }
            else if (string.Equals(command.type, "stop_mode", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[WaitingLobbyManager] Received stop_mode command, returning to lobby.");
                ResetModeState(true);
                SceneManager.LoadScene(0);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[WaitingLobbyManager] Failed to parse server command: " + ex.Message);
        }
    }

    public async Task ExportParametersAsync()
    {
        if (_ws == null || _ws.State != WebSocketState.Open)
        {
            Debug.LogWarning("[WaitingLobbyManager] Cannot export parameters: not connected to server");
            return;
        }

        // Send only the setup fields required by the therapist app in setup mode
        var setupConfig = new ExportSetupConfig
        {
            backgroundDetail = WaitingLobbyManager.BackgroundDetail,
            seat = WaitingLobbyManager.SeatHeight
        };

        var exportMessage = new ExportParametersMessage
        {
            type = "export_parameters",
            config = setupConfig
        };

        var json = JsonUtility.ToJson(exportMessage);
        var buffer = Encoding.UTF8.GetBytes(json);
        try
        {
            await _ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            Debug.Log("[WaitingLobbyManager] Exported setup parameters to server: " + json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[WaitingLobbyManager] Failed to send export message: " + ex.Message);
        }
    }

    private static void SetGameMode(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            CurrentMode = "none";
            return;
        }

        CurrentMode = mode.ToLowerInvariant();
    }

    public static async Task ExportParametersStaticAsync()
    {
        if (_instance == null)
        {
            Debug.LogWarning("[WaitingLobbyManager] Cannot export parameters: instance is not available");
            return;
        }

        await _instance.ExportParametersAsync();
    }

    private void ResetModeState(bool discardConfig)
    {
        CurrentMode = "none";
        CurrentMiniGameID = -1;
        CurrentPatientID = string.Empty;

        if (discardConfig)
        {
            CurrentConfig = new GameConfig();
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Check if the client is currently connected to the server
    /// </summary>
    public bool IsConnected()
    {
        return _isConnected;
    }

    /// <summary>
    /// Get the current server IP address
    /// </summary>
    public string GetServerIp()
    {
        return _currentServerIp;
    }

    /// <summary>
    /// Manually set colors for connected/disconnected states
    /// </summary>
    public void SetStatusColors(Color connected, Color disconnected)
    {
        _connectedColor = connected;
        _disconnectedColor = disconnected;
        UpdateStatusDisplay();
    }
}

[Serializable]
public class ServerCommand
{
    public string type;
    public int sceneID;
    public string mode;
    public int miniGameID;
    public string patientID;
    public GameConfig config;
}

[Serializable]
public class GameConfig
{
    public bool audioCues;
    public bool visualCues;
    public int sessionDuration;
    public int targetScore;
    public string device;
    public int backgroundDetail;
    public float seatHeight;
    public bool hapticFeedback;
    public int bci_minGripTime;
}

[Serializable]
public class ExportParametersMessage
{
    public string type;
    public ExportSetupConfig config;
}

[Serializable]
public class ExportSetupConfig
{
    public int backgroundDetail;
    public float seat;
}

/// <summary>
/// Serializable class for the client identification message sent to the server
/// </summary>
[System.Serializable]
public class ClientIdentification
{
    public string client = "unity";
}
