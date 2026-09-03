using UnityEngine;
using Autohand;
using System.Runtime.CompilerServices;

/// <summary>
/// Monitors de distance between the player's hand and the object this script is attached to, and sends commands to the Arduino based on proximity.
/// Sends Grab/Release commands to the Arduino.
/// </summary>
[RequireComponent(typeof(Grabbable))]
public class HardwareProximityTrigger : MonoBehaviour
{
    [Header("Arduino Communication")]
    [Tooltip("Reference to the ArduinoCommunication script that handles serial communication.")]
    [SerializeField] private ArduinoCommunication arduinoCommunication;

    [Tooltip("The VR Hand or tracked object")]
    [SerializeField] private Transform playerHand;

    [Header("Interaction Zones Settings")]
    [Tooltip("Distance in meters to trigger the Open state)")]
    [SerializeField] private float approachRadius = 0.2f;

    [Tooltip("Distance in meters to trigger the Close state)")]
    [SerializeField] private float touchRadius = 0.05f;

    // State Tracking
    private enum HandState
    {
        None,
        Approaching,
        Touching
    }

    private HandState currentHandState = HandState.None;

    private void Update()
    {
        if (playerHand == null || arduinoCommunication == null) return;
            
        float distanceToHand = Vector3.Distance(transform.position, playerHand.position);

        // Check the distance and update the hand state accordingly
        if (distanceToHand <= touchRadius && currentHandState != HandState.Touching)
        {
            currentHandState = HandState.Touching;
            arduinoCommunication.Grab();
            Debug.Log("<color=green>State: TOUCH -> Hand Closed</color>");
        }
        else if (distanceToHand > touchRadius && distanceToHand <= approachRadius && currentHandState != HandState.Approaching)
        {
            currentHandState = HandState.Approaching;
            arduinoCommunication.Release();
            Debug.Log("<color=yellow>State: APPROACHING -> Hand Opened</color>");
        }
        else if (distanceToHand > approachRadius && currentHandState != HandState.None)
        {
            currentHandState = HandState.None;
            arduinoCommunication.Grab();
            Debug.Log("<color=grey>State: IDLE -> Hand Returned to Closed</color>");
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, approachRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, touchRadius);
    }
}


