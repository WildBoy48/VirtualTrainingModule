using UnityEngine;

public class SimpleGrabber : MonoBehaviour
{

    public CleanHandTracking trackingScript;
    public Transform palmCenter;

    private Rigidbody heldObject;
    private bool isGrabbing = false;
    
    // Update is called once per frame
    void Update()
    {
        // Check curl
        bool intentToGrab = trackingScript.currentCurls[1] > 0.7f;
        
        if (intentToGrab && !isGrabbing && heldObject != null)
        {
            GrabObject();
        }
        else
        {
            DropObject();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Mug") && !isGrabbing)
        {
            heldObject = other.GetComponent<Rigidbody>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Mug") && !isGrabbing)
        {
            heldObject = null;
        }
    }

    void GrabObject()
    {
        isGrabbing = true;
        heldObject.isKinematic = true;
        heldObject.transform.SetParent(palmCenter);
    }

    void DropObject()
    {
        isGrabbing = false;
        if (heldObject != null)
        {
            heldObject.isKinematic = false;
            heldObject.transform.SetParent(null);
            heldObject = null;
        }
    }
}
