using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Autohand;


public class HandPinchMeta : MonoBehaviour
{
    // <summary>

    // <summary>

    [Header("Pinch Settings")]
    public float pinchStartDistance = 0.02f;
    public float pinchEndDistance = 0.05f;
    public float pinchCooldown = 0.2f;
    private float lastPinchTime;

    [Header("Debug")]
    public bool debugLogs = true;
   

    private XRHandSubsystem handSubsystem;

    [Header("Auto Hand")]
    public Hand hand;

    public enum HandType { Left,Right };
    public HandType handType;

    private bool isLeftHand;
    private bool isPinching;
    public bool isPinchingIntent;


    void Start()
    {
        handSubsystem = XRGeneralSettings.Instance?
            .Manager?
            .activeLoader?
            .GetLoadedSubsystem<XRHandSubsystem>();

        if ( handSubsystem == null) { Debug.Log("XR HAND SUBSYSTEM NOT FOUND."); }
    }

    // Update is called once per frame
    void Update()
    {
        if (handSubsystem == null)
            return;

        XRHand xrhand = handType == HandType.Left ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!xrhand.isTracked)
            return;

        XRHandJoint thumbTip = xrhand.GetJoint(XRHandJointID.ThumbTip);
        XRHandJoint indexTip = xrhand.GetJoint(XRHandJointID.IndexTip);

        if (!thumbTip.TryGetPose(out Pose thumbPose))
            return;
        if(!indexTip.TryGetPose(out Pose indexPose))
            return;

        float distance = Vector3.Distance(thumbPose.position, indexPose.position);
        //Debug.Log($"Pinch: {isPinching}... Distance: {distance}");

        if (!isPinching && distance < pinchStartDistance && Time.time - lastPinchTime > pinchCooldown)
        {
            isPinching = true;
            lastPinchTime = Time.time;
            if (debugLogs) Debug.Log($"{gameObject.name} Pinch Start");

            if(hand != null)
            {
              hand.Grab();
                Debug.Log($"Attempting grab, objectts in range: {hand.holdingObj}");
            }
        }

        if (isPinching && distance > pinchEndDistance)
        {
            isPinching = false;
            if (debugLogs)
            {
                Debug.Log($"{gameObject.name} Pinch End");
            }
            if (hand != null)
            {
                hand.Release();
            }
        }


        // Get Finger Tip Positions

        // Calculate Distance

        // Compare Against Threshold

        // Print Pinch State 

    }
}
