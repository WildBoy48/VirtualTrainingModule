using Autohand;
using UnityEngine;

/// <summary>
/// Evaluates if a patient successfully holds an object within a specific spatial zone 
/// for a continuous required duration.
/// Used for session stability training in the exoskeleton minigame, designed for Mode 1, where the patient is unable to show movement intention,
/// and the system must detect if the patient is holding the object in the target zone for a set time.
/// </summary>
public class HoldInZoneScore : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The trigger volume where the object must be held (extend its Y-axis to cover 'above').")]
    [SerializeField] private Transform triggerZone;

    [Tooltip("The Collider that defines the scoring zone. The target object must be fully inside this zone to score.")]
    [SerializeField] private Collider triggerZoneCollider;

    [Tooltip("The movable object to score.")]
    [SerializeField] private Transform targetObject;

    [Tooltip("The tag used to identify the target object. This is a fallback in case the targetObject reference is not set.")]
    [SerializeField] private string targetTag = "Cup";


    [Header("Scoring Requirements")]
    [Tooltip("How many seconds the patient must hold the object in the zone continuously.")]
    [SerializeField] private float requiredHoldTime = 3.0f;

    [Tooltip("The score value awarded when the patient successfully holds the object in the zone for the required time.")]
    [SerializeField] private int scoreValue = 1;


    [Header("Game Loop Integration")]
    [Tooltip("Reference to the GameLoop script that manages the respawn and animation of the target object.")]
    [SerializeField] private GameLoop gameLoop;

    [Tooltip("Reference to the TherapyDataTracker script that records therapy-related data.")]
    [SerializeField] private TherapyDataTracker dataTracker;

    // Tracking variables
    private Grabbable targetGrabbable;
    
    // Timer state
    private float currentHoldTime = 0f;
    private bool hasScoredThisCycle = false;

    private void Awake()
    {
        // Fallback
        if (targetObject == null && !string.IsNullOrEmpty(targetTag))
        {
            GameObject found = GameObject.FindWithTag(targetTag);
            if (found != null) targetObject = found.transform;
        }

        if (targetObject != null)
        {
            targetGrabbable = targetObject.GetComponent<Grabbable>();
        }
    }

    private void Update()
    {
        if (targetGrabbable == null || hasScoredThisCycle || triggerZoneCollider == null) return;

        bool isInDropZone = triggerZoneCollider.bounds.Contains(targetObject.position);

        // Condition: The object is inside the trigger volume AND the patient is physically holding it
        if (isInDropZone && targetGrabbable.IsHeld())
        {
            // Accumulate time
            currentHoldTime += Time.deltaTime;

            if (currentHoldTime >= requiredHoldTime)
            {
                TriggerSuccess();
            }
        }
        else
        {
            // If the patient drops it or leaves the zone, reset the timer to zero
            if (currentHoldTime > 0)
            {
                currentHoldTime = 0f;
                Debug.Log("[HoldInZoneScore] Hold interrupted. Timer reset.");
            }
        }
    }

    /// <summary>
    /// Awards the score, notifies the tracker and the ScoreManager, and initiates the respawn routine in the GameLoop.
    /// Locks scoring to prevent multiple triggers for the same event.
    /// </summary>
    private void TriggerSuccess()
    {
        hasScoredThisCycle = true;

        if (dataTracker != null) dataTracker.RecordScore();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        Debug.Log($"<color=green>[HoldInZoneScore]</color> Success! Object held for {requiredHoldTime} seconds.");

        currentHoldTime = 0f;

        if (gameLoop != null)
        {
            gameLoop.ProcessSuccessRoutine(targetGrabbable.gameObject);
        }

        // Unlock the score after 2 seconds to allow the GameLoop time to teleport it away
        Invoke(nameof(ResetScoringState), 2.0f);
    }

    /// <summary>
    /// Resets the scoring state for the next repetition.
    /// Called automatically via Invoke after a successful score.
    /// </summary>
    public void ResetScoringState()
    {
        hasScoredThisCycle = false;
        currentHoldTime = 0f;
    }
}