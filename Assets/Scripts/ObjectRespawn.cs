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

    public BoxCollider spawnArea;

    void Start()
    {
        //initialPosition = transform.position;
        initialRotation = transform.rotation;

        initialPosition = GetRandomSafeSpawnPosition(spawnArea);
        UpdateRespawnPoint(initialPosition, initialRotation);
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

    public Vector3 GetRandomSafeSpawnPosition(BoxCollider collider)
    {
        Bounds bounds = collider.bounds;

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, transform.position.y ,randomZ);
    }
}
