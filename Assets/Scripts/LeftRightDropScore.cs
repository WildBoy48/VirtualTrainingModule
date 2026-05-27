using Autohand;
using UnityEngine;

public class LeftRightDropScore : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The trigger object used to define left/right relative to its Z axis.")]
    public Transform triggerZone;

    [Tooltip("The movable object to score, depending on game objective.")]
    public Transform targetObject;

    [Tooltip("Optional: fallback tag to identify the cube if targetObject is not assigned.")]
    public string targetTag = "Cup";

    [Header("Scoring")]
    [Tooltip("Points awarded each time the object is dropped on the right side after entering from the left side.")]
    public int score = 0;

    [Header("Game Loop Integration")]
    public GameLoop gameLoop;

    private bool wasOnLeftSide = false;
    private bool hasPassedToRightSide = false;
    private bool hasScoredThisDrop = false;

    private bool isInDropZone = false;
    private Grabbable targetGrabbable;

    private bool isScoreLocked = false;
    private float scoreCooldown = 0f;
    private float cooldownDuration = 1.5f;
    private int collidersInZoneCount = 0;

    // Tracker Script
    public TherapyDataTracker dataTracker;

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
        else
        {
            Transform target = ResolveTargetObject();
            if(target != null)
            {
                targetGrabbable = target.GetComponent<Grabbable>();
            }
        }
    }

    private void Update()
    {
        if (isScoreLocked)
        {
            scoreCooldown -= Time.deltaTime;
            if(scoreCooldown <= 0f)
            {
                isScoreLocked = false;
                hasScoredThisDrop = false;
            }
        }
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

        if(isInDropZone && wasOnLeftSide && !hasScoredThisDrop && !isScoreLocked)
        {
            if(targetGrabbable != null && !targetGrabbable.IsHeld())
            {
                isScoreLocked = true;
                scoreCooldown = cooldownDuration;

                score += 1;
                if (dataTracker != null) dataTracker.RecordScore();
                ScoreManager.Instance.AddScore(1);
                hasScoredThisDrop = true;
                Debug.Log($"<color=green>[LeftRightDropScore]</color> Score awarded! Object dropped in zone. Current score = {score}.");
            
                if(gameLoop != null)
                {
                    gameLoop.ProcessSuccessRoutine(targetGrabbable.gameObject);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform target = ResolveTargetObject();
        if (target == null)
            return;

        if (other.transform.root == target || other.transform.IsChildOf(target) || other.CompareTag(targetTag))
        {
            collidersInZoneCount++;
            Debug.Log($"[LeftRightDropScore] Target entered drop counter collider: {other.name}.");
            isInDropZone = true;

        }
    }

    private void OnTriggerExit(Collider other)
    {
        Transform target = ResolveTargetObject();
        if (target == null)
            return;

        if (other.transform.root == target || other.transform.IsChildOf(target) || other.CompareTag(targetTag))
        {
            collidersInZoneCount--;

            if (collidersInZoneCount <= 0)
            {
                collidersInZoneCount = 0;
                isInDropZone = false;
                Debug.Log($"[LeftRightDropScore] Target exited drop counter collider: {other.name}.");

                if (hasScoredThisDrop)
                {
                    ResetTracking();

                    Debug.Log("[LeftRightDropScore] Drop completed and tracking reset for the next attempt.");
                }
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
            {
                targetObject = found.transform;
                targetGrabbable = targetObject.GetComponent<Grabbable>();
                return targetObject;
            }
                
        }

        return null;
    }

    private void ResetTracking()
    {
        wasOnLeftSide = false;
        hasPassedToRightSide = false;
        //hasScoredThisDrop = false;
        //isScoreLocked = false;
    }
}
