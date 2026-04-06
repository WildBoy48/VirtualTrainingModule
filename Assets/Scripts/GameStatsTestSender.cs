using UnityEngine;

/// <summary>
/// Test script: attach to any GameObject alongside GameStatsReporter.
/// Sends randomised stats every <sendInterval> seconds so you can verify
/// the live display on the Angular therapy-session page.
/// </summary>
public class GameStatsTestSender : MonoBehaviour
{
    [Header("Send Settings")]
    [Tooltip("Seconds between each stats update")]
    [SerializeField] private float sendInterval = 1f;

    [Header("Stat Ranges")]
    [SerializeField] private int maxScore = 200;
    [SerializeField] private int maxErrors = 10;

    private static readonly string[] Tasks =
    {
        "Picking up objects",
        "Sorting items by colour",
        "Placing objects on shelf",
        "Following the path",
        "Reaching targets",
    };

    private float _timer;
    private int _score;
    private int _timeElapsed;
    private int _errors;
    private int _taskIndex;

    private void Update()
    {
        _timer += Time.deltaTime;
        _timeElapsed = Mathf.FloorToInt(Time.timeSinceLevelLoad);

        if (_timer >= sendInterval)
        {
            _timer = 0f;
            SendRandomStats();
        }
    }

    private void SendRandomStats()
    {
        if (GameStatsReporter.Instance == null) return;

        // Gradually increase score and errors, occasionally switch task
        _score     = Mathf.Min(_score + Random.Range(0, 6), maxScore);
        _errors    = Mathf.Min(_errors + (Random.value < 0.2f ? 1 : 0), maxErrors);
        _taskIndex = Random.value < 0.1f ? (_taskIndex + 1) % Tasks.Length : _taskIndex;

        bool completed = _score >= maxScore;

        GameStatsReporter.Instance.ReportStats(
            score:       _score,
            timeElapsed: _timeElapsed,
            errors:      _errors,
            currentTask: Tasks[_taskIndex],
            completed:   completed
        );

        if (completed)
        {
            Debug.Log("[GameStatsTestSender] Max score reached – sending session_end.");
            GameStatsReporter.Instance.ReportSessionEnd();
            enabled = false; // stop sending
        }
    }
}
