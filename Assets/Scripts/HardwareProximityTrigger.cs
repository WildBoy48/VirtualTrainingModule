using UnityEngine;
using Autohand;
using System.Runtime.CompilerServices;

[RequireComponent(typeof(Grabbable))]
public class HardwareProximityTrigger : MonoBehaviour
{ 
    [Header("Proximity Trigger Settings")]
    [TooltipAttribute("Distance in meters to trigger the Arduino Open command)")]
    public float proximityRadius = 0.2f;

    private SphereCollider proximityCollider;
    private Grabbable grabbable;
    private bool isOpenCommandSent = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grabbable = GetComponent<Grabbable>();

        proximityCollider = gameObject.AddComponent<SphereCollider>();
        proximityCollider.isTrigger = true;
        proximityCollider.radius = proximityRadius;
    }

    void OnTriggerEnter(Collider other)
    {
        Hand hand = other.GetComponentInParent<Hand>();

        if(hand != null && !isOpenCommandSent)
        {
            // Send the Open command to the Arduino
            //ArduinoController.Instance.SendOpenCommand();
            Debug.Log("<color=green>HardwareProximityTrigger:</color> Open command sent to Arduino.");
            isOpenCommandSent = true;

            hand.OnHandCollisionStart += OnHandTouchedObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        Hand hand = other.GetComponentInParent<Hand>();
        if(hand != null)
        {
            isOpenCommandSent = false;
            hand.OnHandCollisionStart -= OnHandTouchedObject;
        }
    }

    void OnHandTouchedObject(Hand hand, GameObject touchedObject)
    {
        if(touchedObject == gameObject)
        {
            // Send the Close command to the Arduino
            //ArduinoController.Instance.SendCloseCommand();
            Debug.Log("<color=red>HardwareProximityTrigger:</color> Close command sent to Arduino.");

            hand.OnHandCollisionStart -= OnHandTouchedObject;
            hand.TryGrab(grabbable);
        }
    }
}


