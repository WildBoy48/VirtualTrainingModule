using UnityEngine;
/// <summary>
/// Defines an out-of-bounds area that triggers a respawn for objects that enter it. 
/// When an object with an ObjectRespawn script enters the KillZone, 
/// it will be respawned to its designated position.
/// </summary>

[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        ObjectRespawn respawnScript = other.GetComponentInParent<ObjectRespawn>();
        
        if(respawnScript != null)
        {
            respawnScript.Respawn();

            // Log a message indicating that the object has fallen out of bounds and has been reset
            // Debug.Log($"<color=red>[KillZone]</color> {respawnScript.gameObject.name} fell out of bounds and was reset.");
        }
        else
        {
            //Debug.Log("[Killzone.cs] No RespawnScript found!");
        }
    }
}
