using UnityEngine;
using Autohand;
using Unity.VisualScripting;

/// <summary>
/// Manages the out-of-bounds behavior of objects in the scene. When an object goes out of bounds, 
/// it is returned to the last recorded table location, only for interactables object.
/// Works together with KillZone triggers, without interrupting the active game loop or scoring staTES.
/// </summary>

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Grabbable))]
public class ObjectRespawn : MonoBehaviour
{

    [Header("Spawn Configuration")]
    /// <summary>
    /// The area within which the object can respawn. This should be a BoxCollider that defines the safe spawn area.
    /// </summary>
    [Tooltip("Boundary area used to determine the initial random spawn position.")]
    [SerializeField] private BoxCollider spawnArea;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    private Rigidbody rb;
    private Grabbable grabbable;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        
        respawnRotation = transform.rotation;

        if (spawnArea != null)
        {
            respawnPosition = GetRandomSafeSpawnPosition(spawnArea);
            transform.position = respawnPosition;
        }
        else
        {
            respawnPosition = transform.position;
        }
    }

    /// <summary>
    /// Resets the object to the most recently recorded respawn position and rotation. If the object is currently held by a player, it will be released first. 
    /// Releases any active hand grips and snaps position.
    /// The object's velocities are also reset to zero to prevent any unintended motion after respawning.
    /// </summary>
    public void Respawn()
    {
      
        if (grabbable != null && grabbable.IsHeld())
        {
            grabbable.ForceHandsRelease();
        }
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
        }

        transform.position = respawnPosition;
        transform.rotation = respawnRotation;
        //Debug.Log($"[ObjectRespawn] {gameObject.name} was returned to the table.");
    }


    /// <summary>
    /// Updates the respawn position and rotation to a new target world position and rotation. 
    /// Invoked by GameLoop whenever the object is successfully placed on the table, ensuring that it will appear at the same location if fallen out-of-bounds.
    /// </summary>
    /// <param name="position">The new target world position</param>
    /// <param name="rotation">The target world rotation</param>
    public void UpdateRespawnPoint(Vector3 position, Quaternion rotation) {
        respawnPosition = position;
        respawnRotation = rotation; 
    }

    /// <summary>
    /// Calculates a random position within the bounds of the provided BoxCollider. This position is used as a safe spawn point for the object, 
    /// ensuring it does not spawn outside of the defined area. Constricted to the X and Z axes, the Y position is fixed to keep the object at table height.
    /// </summary>
    /// <param name="areaCollider">The colldier defining the spawnArea</param>
    /// <returns>Returns a position within bounds of the collider, preserving the Y height of the collider.</returns>
    public Vector3 GetRandomSafeSpawnPosition(BoxCollider areaCollider)
    {
        if (areaCollider == null)
        {
            Debug.LogWarning("[ObjectRespawn] Spawn area collider is not assigned. Using current position as spawn point.");
            return transform.position;
        }
        Bounds bounds = areaCollider.bounds;

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, transform.position.y ,randomZ);
    }
}
