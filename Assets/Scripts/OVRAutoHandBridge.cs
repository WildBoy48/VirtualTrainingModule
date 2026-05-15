using UnityEngine;
using Autohand;
using System.Collections;
using UnityEngine.XR.Hands;

public class OVRAutoHandBridge : MonoBehaviour
{
    [Header("AutoHand Reference")]
    public Hand autoHand;

    [Header("Finger References")]
    public Finger[] fingers;

    [Header("Handness")]
    public Handedness handedness = Handedness.Right;

    [Header("Smoothing")]
    [Range(0f, 0.99f)] public float smoothing = 0.1f;
    private static readonly float CurlChangeThreshold = 0.01f;

    [Header("Full Fist Settings")]
    [Range(0f, 1f)] public float fistGrabThreshold = 0.65f;
    [Range(0f, 1f)] public float fistReleaseThreshold = 0.35f;

    [Header("Pinch Settings")]
    [Range(0f, 1f)] public float pinchGrabThreshold = 0.65f;
    [Range(0f, 1f)] public float pinchReleaseThreshold = 0.35f;
    public bool useOVRPinch = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private static readonly XRHandJointID[] ProximalJoints =
    {
       XRHandJointID.ThumbProximal,
       XRHandJointID.IndexProximal,
       XRHandJointID.MiddleProximal,
       XRHandJointID.RingProximal,
       XRHandJointID.LittleProximal,

    };

    private static readonly float[] MaxCurlAngles =
    {
        70f,
        90f,
        90f,
        85f,
        85,
    };

    private XRHandSubsystem _handSubsystem;
    private float[] _curls;
    private float[] _lastSentCurls;

    private bool _isGrabbing = false;
    private bool _fistTriggered = false;
    private bool _pinchTriggered = false;

    IEnumerator Start()
    {
        yield return new WaitUntil(() =>
        {
            var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
            {
                _handSubsystem = subsystems[0];
                return true;
            }
            return false;
        });
        if (showDebugLogs) Debug.Log("[OVRAutoHandBridge] Hand suBsystem found");

    }


    // Update is called once per frame
    void Update()
    {
        if (_handSubsystem == null) return;

        XRHand hand = handedness == Handedness.Right ? _handSubsystem.rightHand : _handSubsystem.leftHand;

        if (!hand.isTracked) return;

        UpdateFingerCurls(hand);
        UpdateGrabState(hand);
    }

    void UpdateFingerCurls(XRHand hand)
    {
        for(int i = 0; i < fingers.Length; i++)
        {
            if (fingers == null) continue;
            if (hand.GetJoint(ProximalJoints[i]).TryGetPose(out Pose pose))
            {
                float angle = pose.rotation.eulerAngles.x;
                if (angle > 180f) angle -= 360f;

                float rawCurl = Mathf.Clamp01(angle / MaxCurlAngles[i]);
                _curls[i] = Mathf.Lerp(_curls[i], rawCurl, 1f - smoothing);

                if (Mathf.Abs(_curls[i]) - _lastSentCurls[i] > CurlChangeThreshold)
                {
                    fingers[i].SetFingerBend(_curls[i]);
                    _lastSentCurls[i] = _curls[i];
                }
            }
            
        }
    }

    private void UpdateGrabState(XRHand hand)
    {
        float fistCurl = (_curls[1] + _curls[2] + _curls[3]) / 3f;
        bool pinchDetected = false;

        if(hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTip) && hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip))
        {
            float pinchDist = Vector3.Distance(thumbTip.position, indexTip.position);
            pinchDetected = pinchDist < 0.02f;
        }
        
        // Hysteresis
        if(!_fistTriggered && fistCurl >= fistGrabThreshold)
        {
            _fistTriggered = true;
            if(showDebugLogs) Debug.Log("[OVRAutoHandBridge] Fist grab Triggered");
        }
        else if(_fistTriggered && fistCurl < fistReleaseThreshold) {
            _fistTriggered = false;
            if (showDebugLogs) Debug.Log("[OVRAutoHandBridge] Fist release Triggered"); 

        }

        if (!_pinchTriggered && pinchDetected)
        {
            _pinchTriggered = true;
            if (showDebugLogs) Debug.Log("[OVRAutoHandBridge] Pinch grab Triggered"); 

        }
        else if(_pinchTriggered && !pinchDetected) 
        {
            _pinchTriggered = false;
            if (showDebugLogs) Debug.Log("[OVRAutoHandBridge] Pinch Released"); 

        }

        bool shouldGrab = _fistTriggered || _pinchTriggered;
        if (shouldGrab && !_isGrabbing)
        {
            autoHand.Grab();
            _isGrabbing = true;
        }
        else if(!shouldGrab && !_isGrabbing)
        {
            autoHand.Release();
            _isGrabbing = false;
        }

    }
        
}
