using UnityEngine;

public class LeftRightDropScore : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The trigger object used to define left/right relative to its Z axis.")]
    public Transform triggerZone;

    [Tooltip("The movable object to score, typically your Cube.")]
    public Transform targetObject;

    [Tooltip("Optional: fallback tag to identify the cube if targetObject is not assigned.")]
    public string targetTag = "Cube";

    [Header("Scoring")]
    [Tooltip("Points awarded each time the object is dropped on the right side after entering from the left side.")]
    public int score = 0;

    private bool wasOnLeftSide = false;
    private bool hasPassedToRightSide = false;
    private bool hasScoredThisDrop = false;

    private void Awake()
    {
        if (triggerZone == null)
        {
            Debug.LogError("[LeftRightDropScore] triggerZone is not assigned. Please assign the trigger object transform.", this);
        }

        if (targetObject == null && string.IsNullOrEmpty(targetTag))
        {
            Debug.LogError("[LeftRightDropScore] targetObject and targetTag are both missing. Please assign one.", this);
        }
    }

    private void Update()
    {
        Transform target = ResolveTargetObject();
        if (target == null || triggerZone == null)
            return;

        float localZ = triggerZone.InverseTransformPoint(target.position).z;

        if (localZ < 0f)
        {
            if (!wasOnLeftSide)
            {
                wasOnLeftSide = true;
                hasPassedToRightSide = false;
                hasScoredThisDrop = false;
                Debug.Log($"[LeftRightDropScore] Target entered left side of trigger (local Z = {localZ:F2}).");
            }
        }
        else if (localZ > 0f && wasOnLeftSide && !hasPassedToRightSide)
        {
            hasPassedToRightSide = true;
            Debug.Log($"[LeftRightDropScore] Target moved from left side to right side of trigger (local Z = {localZ:F2}). Ready for drop detection.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform target = ResolveTargetObject();
        if (target == null)
            return;

        if (other.transform == target || other.transform.IsChildOf(target) || other.CompareTag(targetTag))
        {
            Debug.Log($"[LeftRightDropScore] Target entered drop counter collider: {other.name}.");

            //if (hasPassedToRightSide &&  !hasScoredThisDrop)
            if(wasOnLeftSide && !hasScoredThisDrop)
            {
                score += 1;
                ScoreManager.Instance.AddScore(1);
                hasScoredThisDrop = true;
                Debug.Log($"[LeftRightDropScore] Score awarded! Current score = {score}.");
            }
            else if (!wasOnLeftSide)
            {
                Debug.Log("[LeftRightDropScore] No score: object never entered from the left side of the trigger.");
            }
            else if (!hasPassedToRightSide)
            {
                Debug.Log("[LeftRightDropScore] No score: object has not yet crossed to the right side of the trigger.");
            }
            else if (hasScoredThisDrop)
            {
                Debug.Log("[LeftRightDropScore] Object already scored for this drop.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Transform target = ResolveTargetObject();
        if (target == null)
            return;

        if (other.transform == target || other.transform.IsChildOf(target) || other.CompareTag(targetTag))
        {
            Debug.Log($"[LeftRightDropScore] Target exited drop counter collider: {other.name}.");
            if (hasScoredThisDrop)
            {
                ResetTracking();
                Debug.Log("[LeftRightDropScore] Drop completed and tracking reset for the next attempt.");
            }
        }
    }

    private Transform ResolveTargetObject()
    {
        if (targetObject != null)
            return targetObject;

        if (!string.IsNullOrEmpty(targetTag))
        {
            GameObject found = GameObject.FindWithTag(targetTag);
            if (found != null)
                return found.transform;
        }

        return null;
    }

    private void ResetTracking()
    {
        wasOnLeftSide = false;
        hasPassedToRightSide = false;
        hasScoredThisDrop = false;
    }
}
