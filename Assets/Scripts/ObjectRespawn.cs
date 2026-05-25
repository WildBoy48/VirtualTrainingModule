using UnityEngine;
using Autohand;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Grabbable))]
public class ObjectRespawn : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    private Rigidbody rb;
    private Grabbable grabbable;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        UpdateRespawnPoint(transform.position, transform.rotation);
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
    }

    public void Respawn()
    {
      
        if(grabbable != null && grabbable.IsHeld())
        {
            grabbable.ForceHandsRelease();
        }
        if(rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
        }

        transform.position = respawnPosition;
        transform.rotation = respawnRotation;
        Debug.Log($"[ObjectRespawn] {gameObject.name} was returned to the table.");
    }

    public void UpdateRespawnPoint(Vector3 position, Quaternion rotation) {
        respawnPosition = position;
        respawnRotation = rotation; 
    }
}
