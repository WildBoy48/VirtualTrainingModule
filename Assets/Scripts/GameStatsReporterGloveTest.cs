using UnityEngine;
using Autohand;

public class TherapyDataTracker : MonoBehaviour
{
    [Header("Target Hand")]
    public Hand autoHand;

    [Header("Live Analytics")]
    public float currentRangeOfMotion = 0f;
    public float maxRangeOfMotion = 0f;
    public float currentVelocity_mps = 0f;

    public int totalSuccesses = 0;
    public int totalDrops = 0;      // Was totalErrors

    [Header("Missed Grab Detection")]
    [Tooltip("How far the finger must bend to count as an attempt")]
    public float attemptThreshold = 0.75f;
    [Tooltip("How far the finger must open to reset the attempt")]
    public float resetThreshold = 0.3f;

    public int totalMisses = 0;
    private bool hasAttemptedGrab = false;

    private Rigidbody handRb;

    void Start()
    {
        if (autoHand != null)
        {
            handRb = autoHand.GetComponent<Rigidbody>();
            autoHand.OnGrabbed += RecordSuccess;
            autoHand.OnGrabJointBreak += RecordDrop;
        }
    }

    void Update()
    {
        if (autoHand == null || autoHand.fingers.Length < 2) return;

        // --- RANGE OF MOTION ---
        currentRangeOfMotion = autoHand.fingers[1].GetCurrentBend();
        if (currentRangeOfMotion > maxRangeOfMotion) maxRangeOfMotion = currentRangeOfMotion;

        // --- VELOCITY ---
        if (handRb != null) currentVelocity_mps = handRb.linearVelocity.magnitude;

        // ==========================================
        // MISSED GRAB DETECTION
        // ==========================================

        // 1. Check if the patient is squeezing their hand hard enough to grab
        if (currentRangeOfMotion >= attemptThreshold && !hasAttemptedGrab)
        {
            // Lock the attempt so we only count this squeeze once
            hasAttemptedGrab = true;

            // 2. Ask AutoHand if it caught anything
            if (!autoHand.IsGrabbing())
            {
                totalMisses++;
                Debug.Log($"<color=orange>[DATA]</color> Missed Grab! Fisted closed on empty air. Total: {totalMisses}");
            }
        }
        // 3. Reset the mechanism when the patient opens their hand
        else if (currentRangeOfMotion <= resetThreshold && hasAttemptedGrab)
        {
            hasAttemptedGrab = false;
        }
    }

    void RecordSuccess(Hand hand, Grabbable grab)
    {
        totalSuccesses++;
        Debug.Log($"<color=green>[DATA]</color> Successful Grab! Total: {totalSuccesses}");
    }

    void RecordDrop(Hand hand, Grabbable grab)
    {
        totalDrops++;
        Debug.Log($"<color=red>[DATA]</color> Object Dropped! Total: {totalDrops}");
    }

    void OnDestroy()
    {
        if (autoHand != null)
        {
            autoHand.OnGrabbed -= RecordSuccess;
            autoHand.OnGrabJointBreak -= RecordDrop;
        }
    }
}