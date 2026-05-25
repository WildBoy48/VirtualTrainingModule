using UnityEngine;
using Autohand;
using System.Collections;

public class GameLoop : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The Collider defining the safe spawn zone area")]
    public BoxCollider spawnArea;

    [Header("Animation Settings")]
    [Tooltip("How long it takes to vanish/appear in seconds")]
    public float animationDuration = 0.5f;

   public void ProcessSuccessRoutine(GameObject targetObject)
    {
        StartCoroutine(RespawnRoutine(targetObject));
    }

    private IEnumerator RespawnRoutine(GameObject respawnObject)
    {
        Grabbable grabbable = respawnObject.GetComponent<Grabbable>();
        if (grabbable != null && grabbable.IsHeld()) 
        {
            grabbable.ForceHandsRelease();
        }

        Rigidbody rb = respawnObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Vector3 originalScale = respawnObject.transform.localScale;
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            respawnObject.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero,elapsedTime/animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        respawnObject.transform.localScale = Vector3.zero;

        Vector3 newSpawnPosition = new Vector3(
            Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
            spawnArea.bounds.max.y,
            Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
            );
        respawnObject.transform.position = newSpawnPosition;
        respawnObject.transform.rotation = Quaternion.identity;

        ObjectRespawn respawnScript = respawnObject.GetComponent<ObjectRespawn>();
        if(respawnScript != null) {
            respawnScript.UpdateRespawnPoint(newSpawnPosition, respawnObject.transform.rotation);
        }

        elapsedTime = 0f;
        while(elapsedTime < animationDuration)
        {
            respawnObject.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        respawnObject.transform.localScale = originalScale;

        if(rb != null)
        {
            rb.isKinematic = false;
        }
        Debug.Log("<color=yellow>[GAME LOOP]</color> Cup successfully cycled to new position!");
    }
}
