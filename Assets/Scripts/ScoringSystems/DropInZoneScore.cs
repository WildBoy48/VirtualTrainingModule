using Autohand;
using UnityEngine;

/// <summary>
/// Handles the scoring logic for the target object.
/// Detection of a valid drop inside the target zone, using physics triggers to ensure the object is fully released,
/// and resting inside the desinated boundary before awarding a score.
/// </summary>
public class DropInZoneScore : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Collider that defines the scoring zone. The target object must be fully inside this zone to score.")]
    [SerializeField] private Collider triggerZoneCollider;

    [Tooltip("The specific target object that will be scored when dropped inside the zone.")]
    [SerializeField] private Transform targetObject;

    [Tooltip("The tag used to identify the target object. This is a fallback in case the targetObject reference is not set.")]
    [SerializeField] private string targetTag = "Cup";

    [Header("Scoring")]
    public int score = 0;

    [Tooltip("Reference to the GameLoop script that manages the respawn and animation of the target object.")]
    [SerializeField] private GameLoop gameLoop;

    [Tooltip("Reference to the TherapyDataTracker script that records therapy-related data.")]
    [SerializeField] private TherapyDataTracker dataTracker;

    private Grabbable targetGrabbable;
    private bool isScoreLocked = false;

    private void Awake()
    {
        Transform target = ResolveTargetObject();
        if (target != null)
        {
            targetGrabbable = target.GetComponent<Grabbable>();
        }
    }

    /// <summary>
    /// Continuously monitors objects inside the zone,
    /// Checks if the target object is fully released and resting inside the scoring zone, and awards a score if conditions are met.
    /// </summary>
    /// <param name="other"The collider currently in the trigger zone></param>
    private void OnTriggerStay(Collider other)
    {
        if (isScoreLocked || targetGrabbable == null) return;

        if (IsTarget(other))
        {
            if (!targetGrabbable.IsHeld())
            {
                if (triggerZoneCollider.bounds.Contains(targetObject.position))
                {
                    TriggerScore();
                }
            }
        }
    }

    /// <summary>
    /// Awards the score, notifies the tracker and the ScoreManager, and initiates the respawn routine in the GameLoop. 
    /// Locks scoring to prevent multiple triggers for the same event.
    /// </summary>
    private void TriggerScore()
    {
        isScoreLocked = true;
        score += 1;
        if (dataTracker != null) dataTracker.RecordScore();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(1);
        }
        else
        {
            Debug.LogWarning("[Scoring] ScoreManager is missing! Bypassing to keep loop alive.");
        }

        Debug.Log($"<color=green>[DropInZoneScore]</color> Score awarded! Cup landed in the zone.");

        if (gameLoop != null)
        {
            gameLoop.ProcessSuccessRoutine(targetGrabbable.gameObject);
        }

        // We unlock the score after 2 seconds to allow the GameLoop time to teleport it away
        Invoke(nameof(UnlockScore), 2.0f);
    }

    /// <summary>
    /// Unlocks the scoring mechanism after a delay, allowing for future scoring events to be registered. 
    /// This is called after the respawn routine has had time to complete.
    /// </summary>
    private void UnlockScore()
    {
        isScoreLocked = false;
    }

    /// <summary>
    /// Validates if the collider belongs to the target object, either by direct reference or by tag comparison.
    /// </summary>
    /// <param name="other">The collider to be verified</param>
    /// <returns>True if the collider belongs to the target cup, false otherwise</returns>
    private bool IsTarget(Collider other)
    {
        Grabbable grabbableTouchingZone = other.GetComponentInParent<Grabbable>();

        if (grabbableTouchingZone != null && targetObject != null)
        {
            if (grabbableTouchingZone.gameObject == targetObject.gameObject)
            {
                return true;
            }
        }

        if (other.gameObject.name.Contains(targetObject.name) || other.CompareTag(targetTag))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ensures the script has a valid target object to monitor, 
    /// either from a direct reference or by searching for a GameObject with the specified tag.
    /// </summary>
    /// <returns>The transforms of the validated target object</returns>
    private Transform ResolveTargetObject()
    {
        if (targetObject != null) return targetObject;
        if (!string.IsNullOrEmpty(targetTag))
        {
            GameObject found = GameObject.FindWithTag(targetTag);
            if (found != null)
            {
                targetObject = found.transform;
                return targetObject;
            }
        }
        return null;
    }
}