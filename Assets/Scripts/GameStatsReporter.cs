using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Globalization;
/// <summary>
/// Attach this component to a persistent GameObject in your scene.
/// It connects to the local relay server and sends live session stats
/// so the Angular therapist app can display them in real time.
///
/// Usage:
///   GameStatsReporter.Instance.ReportStats(score, timeElapsed, errors, currentTask, completed);
///   GameStatsReporter.Instance.ReportSessionEnd();  // call when the session finishes
/// </summary>
public class GameStatsReporter : MonoBehaviour
{
    public static GameStatsReporter Instance { get; private set; }

    [Tooltip("WebSocket URL of the relay server")]
    [SerializeField] string serverIP = "localhost"; // Default to localhost for development
    private string serverUrl = "";

    private ClientWebSocket _ws;
    private CancellationTokenSource _cts;
    private bool _connected = false;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[GameStatsReporter] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (WaitingLobbyManager.Instance != null)
        {
            serverUrl = "ws://" + WaitingLobbyManager._currentServerIp + ":3000";
        }
        else
        {
            serverUrl = "ws://" + serverIP + ":3000";
        }
        Debug.Log("[GameStatsReporter] Awake — instance created, target server: " + serverUrl);
    }

    private async void Start()
    {
        Debug.Log("[GameStatsReporter] Start — initiating connection...");
        await ConnectAsync();
    }

    private async void OnDestroy()
    {
        Debug.Log("[GameStatsReporter] OnDestroy — closing connection.");
        ReportSessionEnd();
        await CloseAsync();
    }

    private async void OnApplicationQuit()
    {
        Debug.Log("[GameStatsReporter] OnApplicationQuit — closing connection.");
        ReportSessionEnd();
        await CloseAsync();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Send the current session stats to the Angular app.</summary>
    public void ReportStats(int score, int timeElapsed, int errors, string currentTask, bool completed)
    {
        if (!_connected)
        {
            Debug.LogWarning("[GameStatsReporter] ReportStats called but not connected — skipping.");
            return;
        }

        string json = $"{{\"type\":\"stats\",\"score\":{score},\"timeElapsed\":{timeElapsed}," +
                      $"\"errors\":{errors},\"currentTask\":\"{EscapeJson(currentTask)}\"," +
                      $"\"completed\":{(completed ? "true" : "false")}}}";

        Debug.Log($"[GameStatsReporter] ReportStats — score:{score} time:{timeElapsed}s errors:{errors} task:\"{currentTask}\" completed:{completed}");
        _ = SendAsync(json);
    }

    /// <summary>Notify the Angular app that the session has ended.</summary>
    public void ReportSessionEnd()
    {
        Debug.Log("[GameStatsReporter] ReportSessionEnd called.");
        _ = SendAsync("{\"type\":\"session_end\"}");
    }

    public void ReportStatsFullGrab(int totalScore, int totalDrops, int totalMisses, int totalReps, float totalAccuracy, 
        float repTotalTime, float repReactionTime, float repMovingTime, float repSpaceExplored, float repMaxHorizontalReach, float repIdealPathLength)
    {
        Debug.Log($"<color=blue>[DATA FULL GRAB]</color> Total Score: {totalScore} | Total Drops: {totalDrops} | Total Misses: {totalMisses} | Total Reps: {totalReps} | Total Accuracy: {totalAccuracy}%");
        Debug.Log($"<color=blue>[DATA REP DETAILS]</color> Rep Time: {repTotalTime}s | Reaction Time: {repReactionTime}s | Moving Time: {repMovingTime}s | Space Explored: {repSpaceExplored}m | Max Reach: {repMaxHorizontalReach}m | Ideal Path Length: {repIdealPathLength}m");

        string json =
            $"{{\"type\":\"stats\"," +
            $"\"totalScore\":{totalScore}," +
            $"\"totalDrops\":{totalDrops}," +
            $"\"totalMisses\":{totalMisses}," +
            $"\"totalReps\":{totalReps}," +
            $"\"totalAccuracy\":{totalAccuracy.ToString(CultureInfo.InvariantCulture)}," +
            $"\"repTotalTime\":{repTotalTime.ToString(CultureInfo.InvariantCulture)}," +
            $"\"repReactionTime\":{repReactionTime.ToString(CultureInfo.InvariantCulture)}," +
            $"\"repMovingTime\":{repMovingTime.ToString(CultureInfo.InvariantCulture)}," +
            $"\"repSpaceExplored\":{repSpaceExplored.ToString(CultureInfo.InvariantCulture)}," +
            $"\"repMaxHorizontalReach\":{repMaxHorizontalReach.ToString(CultureInfo.InvariantCulture)}," +
            $"\"repIdealPathLength\":{repIdealPathLength.ToString(CultureInfo.InvariantCulture)}}}";

        _ = SendAsync(json);
    }
    // ── Internal ───────────────────────────────────────────────────────────

    private async Task ConnectAsync()
    {
        _cts = new CancellationTokenSource();
        _ws = new ClientWebSocket();

        Debug.Log($"[GameStatsReporter] Attempting WebSocket connection to {serverUrl}...");

        try
        {
            await _ws.ConnectAsync(new Uri(serverUrl), _cts.Token);
            Debug.Log("[GameStatsReporter] WebSocket open — sending identity.");

            // Identify this connection as the stats reporter (NOT as "unity" to avoid conflicts with WaitingLobbyManager)
            // The server will treat this as a viewer/stats connection, not the command recipient
            await SendAsync("{\"client\":\"stats_reporter\"}");
            _connected = true;
            Debug.Log("[GameStatsReporter] Connected to relay server and identified as stats reporter.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameStatsReporter] Could not connect to {serverUrl}: {ex.Message}");
        }
    }

    private async Task SendAsync(string message)
    {
        if (_ws == null || _ws.State != WebSocketState.Open)
        {
            Debug.LogWarning($"[GameStatsReporter] SendAsync skipped — WS state: {_ws?.State.ToString() ?? "null"}. Message: {message}");
            return;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            Debug.Log($"[GameStatsReporter] Sent: {message}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameStatsReporter] Send error: {ex.Message}");
            _connected = false;
        }
    }

    private async Task CloseAsync()
    {
        Debug.Log($"[GameStatsReporter] CloseAsync — current WS state: {_ws?.State.ToString() ?? "null"}");
        _connected = false;
        _cts?.Cancel();

        if (_ws != null && _ws.State == WebSocketState.Open)
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Game closing", CancellationToken.None);
                Debug.Log("[GameStatsReporter] WebSocket closed cleanly.");
            }
            catch (Exception ex) { Debug.LogWarning($"[GameStatsReporter] CloseAsync error: {ex.Message}"); }
        }

        _ws?.Dispose();
        _ws = null;
    }

    private static string EscapeJson(string s)
    {
        return s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? string.Empty;
    }
}
