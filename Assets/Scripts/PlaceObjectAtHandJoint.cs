using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;


public class PlaceObjectAtHandJoint : MonoBehaviour
{
    [SerializeField] private GameObject placementPrefab;
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private XRHandJointID jointId = XRHandJointID.IndexTip;
    [SerializeField] private Transform placementParent;

    private XRHandSubsystem handSubsystem;
    private GameObject placementInstance;

    private void OnEnable() => StartCoroutine(WaitForHandSubsystem());

    IEnumerator WaitForHandSubsystem()
    {
        List<XRHandSubsystem> subsystems = new();
        while (handSubsystem == null)
        {
            SubsystemManager.GetSubsystems(subsystems);
            foreach(var sub in subsystems)
            {
                if (sub != null && sub.running)
                {
                    handSubsystem = sub;
                    break;
                }
            }
            yield return null;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (handSubsystem == null) return;

        XRHand hand = (handedness == Handedness.Left) ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!hand.isTracked) return;

        XRHandJoint joint = hand.GetJoint(jointId);
        if (!joint.TryGetPose(out Pose pose)) return;

        if (placementInstance == null)
            placementInstance = Instantiate(placementPrefab, placementParent);

        placementInstance.transform.position = pose.position;
        placementInstance.transform.rotation = pose.rotation;
    
    }
}
