using UnityEngine;
using Autohand;

public class WebcamHandLink : MonoBehaviour
{
    [Header("AutoHand References")]
    [Tooltip("Drag the physics Hand Prefab here.")] 
    public Hand autoHand;

    [Header("Desktop Prototyping Offsets")]
    [Tooltip("Lifts the hand off the floor (e.g., Y=1.2) and pushes it forward (e.g., Z=0.5)")]
    public Vector3 positionOffset = new Vector3(0f, 1.2f, 0.5f);
    public Vector3 rotationOffset = new Vector3(0f,0f,0f); 

    [Tooltip("Multiplies the tiny MediaPipe movements so they fit the VR world")]
    public float movementMultiplier = 3.0f;

    [Tooltip("Drag the Follow Target (the invisible tracker object) here")]
    public Transform followTarget;

    [Header("Grasp Detection Settings")]
    [Tooltip("Minimum curl value on the Index finger to consider it a grasp")]
    public float graspThreshold = 0.7f;
    [Tooltip("Minimum curl value on the Index finger to consider it an open hand")]
    public float releaseThreshold = 0.3f;
    private bool isGrasping = false;

    [Header("Live MediaPipe Data")]
    // You will feed your network/webcam data into these variables
    public Vector3 wristPos;
    public Vector3 middleKnucklePos;
    public Vector3 pinkyKnucklePos;

    [Tooltip("0=Thumb, 1=Index, 2=Middle, 3=Ring, 4=Pinky")]
    [Range(0f, 1f)]
    public float[] fingerCurls = new float[5];


    void Update()
    {
        UpdatePalmPositionAndRotation();
        CheckForGrabIntent();

        // Use AutoHand 's built-in finger control, but only if we're not currently grasping something.
        if (!isGrasping && autoHand.holdingObj == null)
        {
            UpdateFingerBending();
        }
    }

    private void UpdatePalmPositionAndRotation()
    {
        //followTarget.position = wristPos;
        followTarget.position = wristPos * movementMultiplier + positionOffset;

        // Direction from wrist to middle knuckle (Forward)
        Vector3 forwardDir = (middleKnucklePos - wristPos).normalized;

        // Direction from wrist to pinky knuckle (Right/Side)
        Vector3 rightDir = (pinkyKnucklePos - wristPos).normalized;

        // Calculate the up direction using cross product
        Vector3 upDir = Vector3.Cross(rightDir, forwardDir).normalized;

        // Only update if valid vectors
        if(forwardDir != Vector3.zero && upDir != Vector3.zero)
        {
            Quaternion rawRotation = Quaternion.LookRotation(forwardDir, upDir);
            followTarget.rotation = rawRotation * Quaternion.Euler(rotationOffset);
        }
    }

    private void UpdateFingerBending()
    {
        // AutoHand stores fingers in an array. Loop through and inject the 0-1 curl values from webcam data.
        for (int i = 0; i < autoHand.fingers.Length && i < fingerCurls.Length; i++)
        {
            // Bypasses Standard VR controllers
            autoHand.fingers[i].bendOffset = fingerCurls[i];
            // Depending on your specific AutoHand version (V3 vs V4), 
            // you might need to use this instead:
            // autoHand.fingers[i].UpdateFinger(fingerCurls[i]);
        }
    }

    private void CheckForGrabIntent()
    {
        float currentCurl = fingerCurls[1]; // Index finger curl value
        // Simple heuristic: If the Index finger is heavily curled, we assume a grasp
        if (currentCurl > graspThreshold && !isGrasping)
        {
            isGrasping = true;
            autoHand.Grab();
        }
        // Reset the toggle if the hand naturally opens (or gets forced open by the release state)
        else if (currentCurl < releaseThreshold && isGrasping)
        {
            isGrasping = false;
            autoHand.Release();
        }
    }
}
