using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
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

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
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

        // Attempt initial connection
        Debug.Log("[WaitingLobbyManager] Start — attempting initial connection to " + _currentServerIp);
        await ConnectToServerAsync();
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
        Debug.Log("[WaitingLobbyManager] OnDestroy — closing WebSocket connection");
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

        if (_isConnected)
        {
            serverStatusText.text = "● Connected";
            serverStatusText.color = _connectedColor;
        }
        else
        {
            serverStatusText.text = "● Disconnected";
            serverStatusText.color = _disconnectedColor;
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

/// <summary>
/// Serializable class for the client identification message sent to the server
/// </summary>
[System.Serializable]
public class ClientIdentification
{
    public string client = "unity";
}
