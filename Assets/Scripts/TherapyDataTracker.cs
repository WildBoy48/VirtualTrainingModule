using UnityEngine;
using Autohand;
using Unity.Hierarchy;
using Unity.VisualScripting;

public class TherapyDataTracker : MonoBehaviour
{
    [Header("Target Hand")]
    public Hand autoHand;

    [Header("Total Session Analytics")]
    public int totalScores = 0;  
    public int totalDrops = 0;      
    public int totalMisses = 0;
    public int totalRepetitions = 0;
    public float totalAccuracy = 0f;

    [Header("Task/Repetition Analytics")]
    public float repTotalTime = 0f;
    public float repReactionTime = 0f;
    public float repMovingTime = 0f;
    public float repSpaceExplored = 0f;
    public float repMaxHorizontalReach = 0f;
    public float repMaxFingerBend = 0f; // MIGHT NEED TO CHANGE 
    public float repIdealPathLength = 0f;

    public float repTightestGrip = 1.0f;


    [Header("Missed Grab Detection")]
    [Tooltip("Distance from tip to palm to count as a closed fist")]
    public float attemptThreshold = 0.05f; // 5cm
    [Tooltip("Distance from tip to palm to reset the attempt")]
    public float resetThreshold = 0.12f;

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

    void Start()
    {
        if (autoHand != null)
        {
            handRb = autoHand.GetComponent<Rigidbody>();
            //autoHand.OnGrabbed += RecordSuccess;
            autoHand.OnGrabJointBreak += RecordDrop;
        }

        GameObject startingCup = GameObject.FindWithTag("Cup");
        if (startingCup != null)
        {
            initialCupY = startingCup.transform.position.y;

            StartNewRepetition(startingCup.transform.position);
        }
        
    }

    void Update()
    {
        if (!isRepActive || autoHand == null) return;

        repTotalTime += Time.deltaTime;
        float currentVelocity = handRb.linearVelocity.magnitude;

        if(currentVelocity > MOVEMENT_THRESHOLD)
        {
            repMovingTime += Time.deltaTime;

            if (!hasMovedThisRep)
            {
                repReactionTime = repTotalTime;
                hasMovedThisRep = true;
            }
        }

        Vector3 currentHandPos = autoHand.transform.position;
        repSpaceExplored += Vector2.Distance(lastHandPosition, currentHandPos);
        lastHandPosition = currentHandPos;

        Vector2 currentXZ = new Vector2(currentHandPos.x, currentHandPos.z);
        float currentReach = Vector2.Distance(repStartXZ,currentXZ);
        if(currentReach > repMaxHorizontalReach) 
        {
            repMaxHorizontalReach = currentReach;
        }

        if(autoHand.fingers.Length > 1 && autoHand.palmTransform != null)
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

        // Send information regarding the total time??
        WaitingLobbyManager.totalScores = totalScores;
        WaitingLobbyManager.totalDrops = totalDrops;
        WaitingLobbyManager.totalMisses = totalMisses;
        WaitingLobbyManager.totalRepetitions = totalRepetitions;
        WaitingLobbyManager.totalAccuracy = totalAccuracy;
        
    }

    //
    //
    //

    public void StartNewRepetition(Vector3 cupSpawnPosition)
    {
        isRepActive = true;
        hasMovedThisRep = false;

        // Reset Rep Varibles
        repTotalTime = 0f;
        repReactionTime = 0f;
        repMovingTime = 0f;
        repSpaceExplored = 0f;
        repMaxHorizontalReach = 0f;
        repMaxFingerBend = 0f; // MIGHT NEED TO CHANGE 
        repTightestGrip = 1.0f;
        repIdealPathLength = 0f;

        if(autoHand != null)
        {
            lastHandPosition = autoHand.transform.position;
            repStartXZ = new Vector2(lastHandPosition.x, lastHandPosition.z);

            Vector3 trueCupPosition = new Vector3(cupSpawnPosition.x,initialCupY, cupSpawnPosition.z);
            currentTargetPos = trueCupPosition;

            repIdealPathLength =  Vector3.Distance(lastHandPosition, trueCupPosition);
        }

        totalRepetitions++;
        Debug.Log($"<color=green>[DATA]</color> Started Repetition  Num of Reps {totalRepetitions}");
    }

    public void RecordScore()
    {
        totalScores++;
        CalculateAccuracy();

        Debug.Log($"<color=green>[DATA REP SUMMARY]</color> Time: {repTotalTime:F2}s | ReactionTime: {repReactionTime:F2}s | Reach: {repMaxHorizontalReach} | Space: {repSpaceExplored:F2}m | Ideal Space: {repIdealPathLength}m");


        // Send information of the Rep Parameters
        WaitingLobbyManager.repTotalTime = repTotalTime;
        WaitingLobbyManager.repReactionTime = repReactionTime;
        WaitingLobbyManager.repMovingTime = repMovingTime;
        WaitingLobbyManager.repSpaceExplored = repSpaceExplored;
        WaitingLobbyManager.repMaxHorizontalReach = repMaxHorizontalReach;
        WaitingLobbyManager.repIdealPathLength = repIdealPathLength;

        isRepActive = false;
    }
    void RecordDrop(Hand hand, Grabbable grab)
    {
        if(isRepActive )
        {
            totalDrops++;
            CalculateAccuracy();
        }
       
        Debug.Log($"<color=red>[DATA]</color> Object Dropped! Total: {totalDrops}");
    }

    private void CalculateAccuracy()
    {
        int totalAttempts = totalScores + totalDrops + totalMisses;
        if(totalAttempts > 0)
        {
            totalAccuracy = ((float) totalScores/totalAttempts) * 100f;
        }
    }

    void OnDestroy()
    {
        if (autoHand != null)
        {
            //autoHand.OnGrabbed -= RecordScore;
            autoHand.OnGrabJointBreak -= RecordDrop;
        }
    }

    private void OnDrawGizmos()
    {
        if(isRepActive) 
        {
            Gizmos.color = Color.red;
            Vector3 startPos3D = new Vector3(currentTargetPos.x, currentTargetPos.y, currentTargetPos.z);
            Gizmos.DrawSphere(startPos3D, 0.02f);

            if(autoHand != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(autoHand.transform.position, 0.02f);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPos3D, autoHand.transform.position);
        }
    }
}