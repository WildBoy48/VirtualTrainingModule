    using UnityEngine;
using Autohand;
using System.Collections;
/// <summary>
/// Responsible for managing the game loop, including handling object respawning and animations.
/// After a successful interaction with a target object, this script will initiate the respawn process, 
/// ensuring that the object is safely repositioned within the defined spawn area and that any necessary animations are played.
/// </summary>

public class GameLoop : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The Collider defining the safe spawn zone area")]
    [SerializeField] private BoxCollider spawnArea;

    [Header("Animation Settings")]
    [Tooltip("How long it takes to vanish/appear in seconds")]
    [SerializeField] private float animationDuration = 0.5f;


    [Header ("Dependencies")]
    [Tooltip("Reference to the TherapyDataTracker script that records therapy-related data.")]
    [SerializeField] private TherapyDataTracker dataTracker;


    /// <summary>
    /// Initiates the success routine for the specified target object. 
    /// This method is called when a successful interaction with the object occurs (e.g., when a plastic cup drops inside the targetZone).
    /// Public in order to be called from other scripts (depending on how to score), such as the DropInZoneScore script, when a success event is detected.
    /// </summary>
    /// <param name="targetObject"> The specific GameObject that triggers the SubRoutine (plasticCup)</param>
    public void ProcessSuccessRoutine(GameObject targetObject)
    {
        Debug.Log($"[GAME LOOP] ProcessSuccessRoutine called for '{targetObject.name}' (activeInHierarchy={targetObject.activeInHierarchy}). Starting RespawnRoutine.");
        StartCoroutine(RespawnRoutine(targetObject));
    }

    /// <summary>
    /// A coroutine that handles the respawn process for a given GameObject. Handles the physical and visual transition of the object, including releasing it from any hands if held, disabling its physics temporarily, 
    /// and animating its disappearance and reappearance at a new random position within the defined spawn area.
    /// </summary>
    /// <param name="respawnObject">The GameObject being respawned.</param>
    /// <returns>An IEnumerator for the coroutine that handles the respawn process, including animations and repositioning.</returns>
    private IEnumerator RespawnRoutine(GameObject respawnObject)
    {
        Grabbable grabbable = respawnObject.GetComponent<Grabbable>();
        Rigidbody rb = respawnObject.GetComponent<Rigidbody>();
        Collider[] colliders = respawnObject.GetComponentsInChildren<Collider>();

        if (grabbable != null && grabbable.IsHeld()) 
        {
            grabbable.ForceHandsRelease();
            yield return null; // Wait a frame to ensure release is processed
        }

        // Prevents unwanted movement during the respawn process by disabling physics interactions and resetting velocities.
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (Collider col in colliders) col.enabled = false;

        // Start the shrinking process to make the object appear to vanish before repositioning it.
        Vector3 originalScale = respawnObject.transform.localScale;
        Vector3 invisibleScale = originalScale * 0.01f;

        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            respawnObject.transform.localScale = Vector3.Lerp(originalScale, invisibleScale,elapsedTime/animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        respawnObject.transform.localScale = invisibleScale;

        Vector3 newSpawnPosition = new Vector3(
            Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
            spawnArea.bounds.max.y,
            Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
            );

        
        respawnObject.transform.position = newSpawnPosition;
        respawnObject.transform.rotation = Quaternion.identity;
        if(rb != null)
        {
            rb.position = newSpawnPosition;
            rb.rotation = Quaternion.identity;
        }

        // Update the respawn point, to ensure that the object will respawn at the new position
        ObjectRespawn respawnScript = respawnObject.GetComponent<ObjectRespawn>();
        if(respawnScript != null) {
            respawnScript.UpdateRespawnPoint(newSpawnPosition, respawnObject.transform.rotation);
        }

        // Start the growing process to make the object appear to reappear at its new position.
        elapsedTime = 0f;
        while(elapsedTime < animationDuration)
        {
            respawnObject.transform.localScale = Vector3.Lerp(invisibleScale, originalScale, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        respawnObject.transform.localScale = originalScale;

        foreach (Collider col in colliders) col.enabled = true;


        if(rb != null)
        {
            rb.isKinematic = false;
        }        
        if (dataTracker != null) dataTracker.StartNewRepetition(newSpawnPosition);

        Debug.Log("<color=yellow>[GAME LOOP]</color> Cup successfully cycled to new position!");
    }
}
