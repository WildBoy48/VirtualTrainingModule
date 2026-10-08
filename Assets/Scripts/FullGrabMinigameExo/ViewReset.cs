using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class ViewReset : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Press this key on the PC keyboard to recenter the patient's view.")]
    public Key recenterKey = Key.M;

    [Header("References")]
    [Tooltip("Drag your XR Origin GameObject here.")]
    public XROrigin xrOrigin;
    [Tooltip("Drag the empty GameObject representing the ideal head position here.")]
    public Transform targetHeadPosition;


    private void Update()
    {
        // Check if a keyboard is connected and the assigned key was just pressed
        if (Keyboard.current != null && Keyboard.current[recenterKey].wasPressedThisFrame)
        {
            ManualRecenter();
        }
    }

    private void ManualRecenter()
    {
        if (xrOrigin == null || targetHeadPosition == null)
        {
            Debug.LogWarning("Missing XR Origin or Target Head Position references!");
            return;
        }

        // 1. ROTATION: Rotate the XR Origin around the Camera so the user faces the desk
        float angleDifference = targetHeadPosition.eulerAngles.y - xrOrigin.Camera.transform.eulerAngles.y;
        xrOrigin.transform.RotateAround(xrOrigin.Camera.transform.position, Vector3.up, angleDifference);

        // 2. POSITION: Calculate the horizontal distance between the current head and the target chair
        Vector3 positionOffset = targetHeadPosition.position - xrOrigin.Camera.transform.position;

        // We set Y to 0 because we want to preserve the headset's real-world floor height tracking.
        // This ensures shorter/taller patients don't get forced to the exact same vertical pixel.
        positionOffset.y = 0;

        // Move the XR Origin to snap the camera to the target X/Z coordinates
        xrOrigin.transform.position += positionOffset;

        Debug.Log($"<color=cyan>[VR]</color> Manually shifted user to the chair via '{recenterKey}' key.");
    }
}
