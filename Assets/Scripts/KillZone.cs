using UnityEngine;

public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        ObjectRespawn respawnScript = other.GetComponent<ObjectRespawn>();
        
        if(respawnScript != null)
        {
            respawnScript.Respawn();
        }
        else
        {
            Debug.Log("[Killzone.cs] No RespawnScript found!");
        }
    }
}
