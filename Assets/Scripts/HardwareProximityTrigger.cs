using UnityEngine;
using Autohand;
using System.Runtime.CompilerServices;

[RequireComponent(typeof(Grabbable))]
public class HardwareProximityTrigger : MonoBehaviour
{

    [Header("Arduino Communication")]
    [TooltipAttribute("Reference to the ArduinoCommunication script that handles serial communication.")]
    public ArduinoCommunication arduinoCommunication;

    [Tooltip("The VR Hand or tracked object")]
    public Transform playerHand;

    [Header("Interaction Zones Settings")]
    [TooltipAttribute("Distance in meters to trigger the Open state)")]
    public float approachRadius = 0.2f;

    [TooltipAttribute("Distance in meters to trigger the Close state)")]
    public float touchRadius = 0.05f;

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


