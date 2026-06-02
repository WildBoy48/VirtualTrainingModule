using UnityEngine;
using UnityEngine.InputSystem;

public class TherapistSetupManager : MonoBehaviour
{
    [Header("References")]
    public BoxCollider spawnVolumeCollider;
    public Transform controllerTransform;
    public Transform visualizerTransform;
    public MeshRenderer visualizerMesh;

    [Header("Pointer Visuals")]
    [Tooltip("Add a LineRenderer to your GameManager and assign it here to act as the laser pointer.")]
    public LineRenderer laserPointer;
    public Color laserNormalColor = Color.red;
    public Color laserGrabColor = Color.green;

    [Header("Setup State")]
    public bool isSetupModeActive = false;

    [Header("Movement Settings")]
    public float moveSpeed = 1.0f;
    public float minBoxSize = 0.2f;

    [Header("VR Controller Inputs")]
    public InputActionReference toggleSetupButton;
    public InputActionReference moveThumbstick;
    public InputActionReference scaleTrigger;

    [Header("VR Rig Swap")]
    public GameObject patientHandRig;
    public GameObject therapistControllerRig;

    // Internal logic for edge grabbing
    private enum GrabbedEdge { None, Right, Left, Forward, Back }
    private GrabbedEdge activeEdge = GrabbedEdge.None;
    private bool wasTriggerPressedLastFrame = false;

    // NEW: Variables for smooth, offset-based dragging
    private Vector3 initialHitLocalPos;
    private Vector3 initialBoxCenter;
    private Vector3 initialBoxSize;

    // Mathematical plane to ensure smooth dragging from a distance
    private Plane tablePlane;

    private void OnEnable()
    {
        if (toggleSetupButton != null) toggleSetupButton.action.Enable();
        if (moveThumbstick != null) moveThumbstick.action.Enable();
        if (scaleTrigger != null) scaleTrigger.action.Enable();
    }

    private void OnDisable()
    {
        if (toggleSetupButton != null) toggleSetupButton.action.Disable();
        if (moveThumbstick != null) moveThumbstick.action.Disable();
        if (scaleTrigger != null) scaleTrigger.action.Disable();
    }

    private void Start()
    {
        if (visualizerMesh != null) visualizerMesh.enabled = isSetupModeActive;
        if (laserPointer != null) laserPointer.enabled = isSetupModeActive;

        if (laserPointer != null)
        {
            //laserPointer.enabled = false;
            laserPointer.startWidth = 0.005f;
            laserPointer.endWidth = 0.005f;
        }
    }

    private void Update()
    {
        // 1. Toggle Setup Mode
        if (toggleSetupButton != null && toggleSetupButton.action.WasPressedThisFrame())
        {
            isSetupModeActive = !isSetupModeActive;
            Debug.Log($"<color=yellow>[SETUP]</color> Toggled Mode. Active: {isSetupModeActive}");

            if (visualizerMesh != null) visualizerMesh.enabled = isSetupModeActive;
            if (laserPointer != null) laserPointer.enabled = isSetupModeActive;

            if (patientHandRig != null) patientHandRig.SetActive(!isSetupModeActive);
            if(therapistControllerRig != null) therapistControllerRig.SetActive(isSetupModeActive);

            activeEdge = GrabbedEdge.None;
            wasTriggerPressedLastFrame = false;
        }

        if (!isSetupModeActive || spawnVolumeCollider == null || controllerTransform == null) return;

        // 2. Thumbstick Movement (Controller-Relative sliding)
        Vector2 stickInput = moveThumbstick != null ? moveThumbstick.action.ReadValue<Vector2>() : Vector2.zero;
        if (stickInput.magnitude > 0.1f)
        {
            // Step A: Find out which way the controller is aiming, flattened to the table (ignoring up/down tilt)
            Vector3 aimForward = controllerTransform.forward;
            aimForward.y = 0;
            aimForward.Normalize();

            Vector3 aimRight = controllerTransform.right;
            aimRight.y = 0;
            aimRight.Normalize();

            // Step B: Calculate the exact real-world movement based on the controller's aim
            Vector3 worldMoveAmount = (aimRight * stickInput.x) + (aimForward * stickInput.y);
            worldMoveAmount *= (moveSpeed * Time.deltaTime);

            // Step C: Translate that physical world movement into the box's local math
            Vector3 localMoveAmount = spawnVolumeCollider.transform.InverseTransformDirection(worldMoveAmount);
            spawnVolumeCollider.center += localMoveAmount;
        }

        // 3. The Laser Pointer Logic
        float triggerValue = scaleTrigger != null ? scaleTrigger.action.ReadValue<float>() : 0f;
        bool isTriggerPressed = triggerValue > 0.5f;

        Ray pointerRay = new Ray(controllerTransform.position, controllerTransform.forward);
        tablePlane = new Plane(Vector3.up, spawnVolumeCollider.transform.TransformPoint(spawnVolumeCollider.center));

        if (isTriggerPressed && !wasTriggerPressedLastFrame)
        {
            // TRIGGER JUST PRESSED: Shoot a ray to lock onto a wall
            if (spawnVolumeCollider.Raycast(pointerRay, out RaycastHit hit, 10f))
            {
                Vector3 localHit = spawnVolumeCollider.transform.InverseTransformPoint(hit.point);
                activeEdge = GetClosestEdge(localHit);

                // ==========================================
                // FIX: Take a snapshot of the box and laser!
                // ==========================================
                initialHitLocalPos = localHit;
                initialBoxCenter = spawnVolumeCollider.center;
                initialBoxSize = spawnVolumeCollider.size;

                if (laserPointer != null) laserPointer.material.color = laserGrabColor;
            }
        }
        else if (isTriggerPressed && activeEdge != GrabbedEdge.None)
        {
            // TRIGGER HELD: Calculate the drag delta
            if (tablePlane.Raycast(pointerRay, out float distanceToPlane))
            {
                Vector3 worldHitOnPlane = pointerRay.GetPoint(distanceToPlane);
                Vector3 currentLocalHit = spawnVolumeCollider.transform.InverseTransformPoint(worldHitOnPlane);

                // ==========================================
                // FIX: Calculate how far the laser MOVED
                // ==========================================
                Vector3 dragDelta = currentLocalHit - initialHitLocalPos;

                DragBoxEdgeSmoothly(activeEdge, dragDelta);
            }
            UpdateLaser(pointerRay.origin, pointerRay.GetPoint(10f));
        }
        else
        {
            // NOT GRABBING: Just draw a normal laser pointer
            activeEdge = GrabbedEdge.None;
            if (laserPointer != null) laserPointer.material.color = laserNormalColor;

            // If aiming at the box, stop the laser on the box. Otherwise, shoot it out 10 meters.
            if (spawnVolumeCollider.Raycast(pointerRay, out RaycastHit hit, 10f))
            {
                UpdateLaser(pointerRay.origin, hit.point);
            }
            else
            {
                UpdateLaser(pointerRay.origin, pointerRay.GetPoint(10f));
            }
        }

        wasTriggerPressedLastFrame = isTriggerPressed;

        // 4. Update the Hologram Visualizer
        if (visualizerTransform != null)
        {
            //visualizerTransform.localPosition = spawnVolumeCollider.center;
            //visualizerTransform.localScale = spawnVolumeCollider.size;
            visualizerTransform.position = spawnVolumeCollider.transform.TransformPoint(spawnVolumeCollider.center);
            visualizerTransform.rotation = spawnVolumeCollider.transform.rotation;

            Vector3 worldScale = spawnVolumeCollider.transform.lossyScale;
            visualizerTransform.localScale = new Vector3(
                spawnVolumeCollider.size.x * worldScale.x,
                spawnVolumeCollider.size.y * worldScale.y,
                spawnVolumeCollider.size.z * worldScale.z);
        }
    }

    private void UpdateLaser(Vector3 start, Vector3 end)
    {
        if (laserPointer != null)
        {
            laserPointer.SetPosition(0, start);
            laserPointer.SetPosition(1, end);
        }
    }

    private GrabbedEdge GetClosestEdge(Vector3 localHitPoint)
    {
        float halfX = spawnVolumeCollider.size.x / 2f;
        float halfZ = spawnVolumeCollider.size.z / 2f;
        float centerX = spawnVolumeCollider.center.x;
        float centerZ = spawnVolumeCollider.center.z;

        float distRight = Mathf.Abs((centerX + halfX) - localHitPoint.x);
        float distLeft = Mathf.Abs((centerX - halfX) - localHitPoint.x);
        float distForward = Mathf.Abs((centerZ + halfZ) - localHitPoint.z);
        float distBack = Mathf.Abs((centerZ - halfZ) - localHitPoint.z);

        float minDist = Mathf.Min(distRight, distLeft, distForward, distBack);

        if (minDist == distRight) return GrabbedEdge.Right;
        if (minDist == distLeft) return GrabbedEdge.Left;
        if (minDist == distForward) return GrabbedEdge.Forward;
        return GrabbedEdge.Back;
    }

    private void DragBoxEdgeSmoothly(GrabbedEdge edge, Vector3 dragDelta)
    {
        // Start from the perfect snapshot we took when the trigger was pulled
        Vector3 newSize = initialBoxSize;
        Vector3 newCenter = initialBoxCenter;

        if (edge == GrabbedEdge.Right)
        {
            // Calculate new width (ensuring it doesn't shrink below minimum size)
            newSize.x = Mathf.Max(minBoxSize, initialBoxSize.x + dragDelta.x);

            // Shift the center by exactly half the amount the size changed to keep left wall anchored
            float sizeDifference = newSize.x - initialBoxSize.x;
            newCenter.x = initialBoxCenter.x + (sizeDifference / 2f);
        }
        else if (edge == GrabbedEdge.Left)
        {
            // Pulling left means negative delta, so we subtract to increase size
            newSize.x = Mathf.Max(minBoxSize, initialBoxSize.x - dragDelta.x);
            float sizeDifference = newSize.x - initialBoxSize.x;
            newCenter.x = initialBoxCenter.x - (sizeDifference / 2f);
        }
        else if (edge == GrabbedEdge.Forward)
        {
            newSize.z = Mathf.Max(minBoxSize, initialBoxSize.z + dragDelta.z);
            float sizeDifference = newSize.z - initialBoxSize.z;
            newCenter.z = initialBoxCenter.z + (sizeDifference / 2f);
        }
        else if (edge == GrabbedEdge.Back)
        {
            newSize.z = Mathf.Max(minBoxSize, initialBoxSize.z - dragDelta.z);
            float sizeDifference = newSize.z - initialBoxSize.z;
            newCenter.z = initialBoxCenter.z - (sizeDifference / 2f);
        }

        spawnVolumeCollider.size = newSize;
        spawnVolumeCollider.center = newCenter;
    }
}
