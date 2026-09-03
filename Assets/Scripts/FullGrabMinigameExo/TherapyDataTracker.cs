using UnityEngine;
using Autohand;
using Unity.Hierarchy;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// Core class for tracking therapy session data, including repetitions, scores, drops, misses, and various metrics related to hand movement and interaction with objects.
/// Saves the data to a local JSON file for analysis, and can be seen through a dashboard. Currently, not in real-time.
/// </summary>
public class TherapyDataTracker : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the AutoHand component for hand interactions")]
    [SerializeField] private Hand autoHand;

    [Tooltip("The targetzone boundaries used to calculate ideal path lengths and success conditions.")]
    [SerializeField] private Transform fixedTargetBoundaries;

    [Header("Data Gathering Settings")]
    [SerializeField] private bool sendStats = false;
    [SerializeField] private float spatialTrackingInterval = 0.05f; 
    [SerializeField] private string dataDirectory = @"C:\Tecnico\Tese\Data";

    [Header("Missed Grab Detection Settings")]
    [Tooltip("Distance from tip to palm to count as a closed fist")]
    [SerializeField] private float attemptThreshold = 0.05f;
    [Tooltip("Distance from tip to palm to reset the attempt")]
    [SerializeField] private float resetThreshold = 0.12f;

    // Session Analytics Variables
    private int totalScores = 0;  
    private int totalDrops = 0;
    private int totalMisses = 0;
    private int totalRepetitions = 0;
    private float totalAccuracy = 0f;

    // Repetition Analytics Variables
    private float repTotalTime = 0f;
    private float repReactionTime = 0f;
    private float repMovingTime = 0f;
    private float repSpaceExplored = 0f;
    private float repMaxHorizontalReach = 0f;
    private float repIdealPathLength = 0f;
    private float repTightestGrip = 1.0f;
    private float repPeakVelocity = 0f;

    // Other Analytics Variables
    private int frameCount = 0;


    // State Variables
    private bool isRepActive = false;
    private bool hasMovedThisRep = false;
    private bool hasAttemptedGrab = false;


    private Rigidbody handRb;
    private Vector3 lastHandPosition;
    private Vector2 repStartXZ;
    private float initialCupY;
    private Vector3 currentTargetPos;
        

    private const float MOVEMENT_THRESHOLD = 0.05f;
    private float timer;
    private float trajectoryTimer;

    // Session Data Memory
    private List<float> allTotalTimes = new List<float>();
    private List<float> allReactionTimes = new List<float>();
    private List<float> allMovingTimes = new List<float>();
    private List<float> allSpaceExplored = new List<float>();
    private List<float> allMaxHorizontalReach = new List<float>();
    private List<float> allIdealPathLength = new List<float>();

    // JSON Data Structure (currently saving on own machine for analysis)
    private SessionData currentSessionData;
    private List<TrajectoryPoint> currentTrajectory;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (fixedTargetBoundaries == null)
        {
            Debug.LogWarning($"[{gameObject.name}] fixedTargetBoundaries is missing. Target position will record as 0,0,0 in JSON.");
        }
    }
#endif


    private void Start()
    {
        // Initialize Session Data
        currentSessionData = new SessionData
        {
            session_id = "Session_" + DateTime.Now.ToString("yyyyMMdd_HHmm"),
            timestamp = DateTime.UtcNow.ToString("O"),
            repetitions = new List<RepetitionData>()

        };
        currentTrajectory = new List<TrajectoryPoint>();

        if (autoHand != null)
        {
            handRb = autoHand.GetComponent<Rigidbody>();
            autoHand.OnGrabJointBreak += RecordDrop;
        }

        GameObject startingCup = GameObject.FindWithTag("Cup");
        if (startingCup != null)
        {
            initialCupY = startingCup.transform.position.y;

            StartNewRepetition(startingCup.transform.position);
        }
        
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (!isRepActive || autoHand == null) return;

        repTotalTime += Time.deltaTime;
        trajectoryTimer += Time.deltaTime;
        frameCount++;

        float currentVelocity = handRb.linearVelocity.magnitude;

        if (currentVelocity > repPeakVelocity) repPeakVelocity = currentVelocity;

        // Spatial Coordinates Tracking 
        if (trajectoryTimer >= spatialTrackingInterval)
        {
            Vector3 currentPos = autoHand.transform.position;
            Vector3 currentRot = autoHand.transform.eulerAngles;
            currentTrajectory.Add(new TrajectoryPoint
            {
                t_ms = Mathf.RoundToInt(repTotalTime * 1000f),
                x = currentPos.x,
                y = currentPos.y,
                z = currentPos.z,
                pitch = currentRot.x,
                yaw = currentRot.y,
                roll = currentRot.z
            });
            trajectoryTimer = 0f; // Reset for next sample
        }


        // Initial Movement Detection for Reaction Time
        if (currentVelocity > MOVEMENT_THRESHOLD)
        {
            repMovingTime += Time.deltaTime;

            if (!hasMovedThisRep)
            {
                repReactionTime = repTotalTime;
                hasMovedThisRep = true;
            }
        }

        // Update Space Explored and Max Horizontal Reach
        Vector3 currentHandPos = autoHand.transform.position;
        repSpaceExplored += Vector2.Distance(lastHandPosition, currentHandPos);
        lastHandPosition = currentHandPos;

        Vector2 currentXZ = new Vector2(currentHandPos.x, currentHandPos.z);
        float currentReach = Vector2.Distance(repStartXZ,currentXZ);
        if(currentReach > repMaxHorizontalReach) 
        {
            repMaxHorizontalReach = currentReach;
        }

        // Grip Detection for Missed Grab
        if (autoHand.fingers.Length > 1 && autoHand.palmTransform != null)
        {
            Transform indexTip = autoHand.fingers[1].tip;

            float currentTipDistance = Vector3.Distance(indexTip.position, autoHand.palmTransform.position);
            
            if(currentTipDistance < repTightestGrip) repTightestGrip = currentTipDistance;

            if(currentTipDistance <= attemptThreshold && !hasAttemptedGrab)
            {
                hasAttemptedGrab = true;
                if (!autoHand.IsGrabbing())
                {
                    totalMisses++;
                    Debug.Log($"<color=cyan>[DATA GRAB]</color> User attempted to grab, grabbed empty air");
                }
            }
            else if(currentTipDistance >= resetThreshold && hasAttemptedGrab)
            {
                hasAttemptedGrab = false;
            }
        }
    }

   
    /// <summary>
    /// Resets variables for a new attempt.
    /// Triggered by GameLoop after the repetition is a success, when it teleports to the new location.
    /// </summary>
    /// <param name="cupSpawnPosition">The 3D coordinates where the cup was teleported.</param>
    public void StartNewRepetition(Vector3 cupSpawnPosition)
    {
        isRepActive = true;
        hasMovedThisRep = false;

        repTotalTime = 0f;
        repReactionTime = 0f;
        repMovingTime = 0f;
        repSpaceExplored = 0f;
        repMaxHorizontalReach = 0f;
        repTightestGrip = 1.0f;
        repPeakVelocity = 0f;
        frameCount = 0;

        currentTrajectory.Clear();
        trajectoryTimer = 0f;

        if (autoHand != null)
        {
            lastHandPosition = autoHand.transform.position;
            repStartXZ = new Vector2(lastHandPosition.x, lastHandPosition.z);

            Vector3 trueCupPosition = new Vector3(cupSpawnPosition.x,initialCupY, cupSpawnPosition.z);
            currentTargetPos = trueCupPosition;

            repIdealPathLength =  Vector3.Distance(lastHandPosition, currentTargetPos);
        }

        totalRepetitions++;
        Debug.Log($"<color=green>[DATA]</color> Started Repetition  Num of Reps {totalRepetitions}");
    }

    /// <summary>
    /// Finalizes the current repetition, calculates metrics, and records the data for the session.
    /// Disables tracking until GameLoop calls StartNewRepetition again.
    /// </summary>
    public void RecordScore()
    {
        totalScores++;
        CalculateAccuracy();

        Debug.Log($"<color=green>[DATA REP SUMMARY]</color> Time: {repTotalTime:F2}s | ReactionTime: {repReactionTime:F2}s | Reach: {repMaxHorizontalReach} | Space: {repSpaceExplored:F2}m | Ideal Space: {repIdealPathLength}m");

        allTotalTimes.Add(repTotalTime);
        allReactionTimes.Add(repReactionTime);
        allMovingTimes.Add(repMovingTime);
        allSpaceExplored.Add(repSpaceExplored);
        allMaxHorizontalReach.Add(repMaxHorizontalReach);
        allIdealPathLength.Add(repIdealPathLength);

        float averageFPS = (repTotalTime > 0f) ? (frameCount / repTotalTime) : 0f;

        // SAFE TARGET POSITION CHECK
        Vector3 targetPos = Vector3.zero;
        if (fixedTargetBoundaries != null)
        {
            targetPos = fixedTargetBoundaries.position;
        }
        else
        {
            Debug.LogWarning("[TherapyDataTracker] fixedTargetBoundaries is null! Defaulting target_position to Vector3.zero for JSON.");
        }

        // Save the current repetition data to the session data List
        RepetitionData repData = new RepetitionData
        {
            rep_id = totalRepetitions,
            spawn_position = new PositionData(currentTargetPos),
            target_position = new PositionData(targetPos),
            metrics = new RepMetrics
            {
                total_time_ms = repTotalTime * 1000f,
                reaction_time_ms = repReactionTime * 1000f,
                moving_time_ms = repMovingTime * 1000f,
                space_explored = repSpaceExplored,
                max_horizontal_reach = repMaxHorizontalReach,
                ideal_path_length = repIdealPathLength,
                peak_velocity = repPeakVelocity,
                average_fps = averageFPS
            },
            trajectory = new List<TrajectoryPoint>(currentTrajectory)
        };
        currentSessionData.repetitions.Add(repData);
        isRepActive = false;
    }

    /// <summary>
    /// Listens to the event triggered by AutoHand, when the user drops the objects.
    /// Used to track accidental drops during a repetition.
    /// </summary>
    private void RecordDrop(Hand hand, Grabbable grab)
    {
        if(isRepActive)
        {
            totalDrops++;
            CalculateAccuracy();
        }
        Debug.Log($"<color=red>[DATA]</color> Object Dropped! Total: {totalDrops}");
    }

    /// <summary>
    /// Recalculates the total accuracy based on the current scores, drops, and misses.
    /// </summary>
    private void CalculateAccuracy()
    {
        int totalAttempts = totalScores + totalDrops + totalMisses;
        if(totalAttempts > 0)
        {
            totalAccuracy = ((float) totalScores/totalAttempts) * 100f;
        }
    }

    private void OnDestroy()
    {
        if (autoHand != null)
        {
            //autoHand.OnGrabbed -= RecordScore;
            autoHand.OnGrabJointBreak -= RecordDrop;
        }
    }



    /// <summary>
    /// Lifecycle event that fires when the application closes.
    /// Serializes all recorded session and repetition data into a JSON file for local storage.
    /// </summary>
    private void OnApplicationQuit()
    { 

        // Save the session data to a JSON file
        currentSessionData.session_metrics = new SessionMetrics
        {
            total_score = totalScores,
            total_drops = totalDrops,
            total_misses = totalMisses,
            total_reps = totalRepetitions,
            total_accuracy = totalAccuracy
        };

        if(!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }

        string json = JsonUtility.ToJson(currentSessionData, true);
        string filePath = Path.Combine(dataDirectory, currentSessionData.session_id + ".json");
        File.WriteAllText(filePath, json);

        Debug.Log($"<color=cyan><b>[DATA SAVED]</b></color> Session JSON saved to: {filePath}");
    }
}


// ==========================================
// JSON Serialization Classes (builtin Unity JsonUtility)
// ==========================================
[Serializable]
public class SessionData
{
    public string session_id;
    public string timestamp;
    public SessionMetrics session_metrics;
    public List<RepetitionData> repetitions;
}

[Serializable]
public class SessionMetrics
{
    public int total_score;
    public int total_drops;
    public int total_misses;
    public int total_reps;
    public float total_accuracy;
}

[Serializable]
public class RepetitionData
{
    public int rep_id;
    public PositionData spawn_position;
    public PositionData target_position;
    public RepMetrics metrics;
    public List<TrajectoryPoint> trajectory;
}

[Serializable]
public class RepMetrics
{
    public float total_time_ms;
    public float reaction_time_ms;
    public float moving_time_ms;
    public float space_explored;
    public float max_horizontal_reach;
    public float ideal_path_length;
    public float peak_velocity;
    public float average_fps;
}

[Serializable]
public class TrajectoryPoint
{
    public int t_ms;
    public float x;
    public float y;
    public float z;
    public float pitch;
    public float yaw;
    public float roll;
}

[Serializable]
public class PositionData
{
    public float x;
    public float y;
    public float z;

    public PositionData(Vector3 position)
    {
        x = position.x;
        y = position.y;
        z = position.z;
    }
}