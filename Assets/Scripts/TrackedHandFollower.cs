using UnityEngine;

public class TrackedHandFollower : MonoBehaviour
{
    [Header("Tracking Source")]
    public Transform trackedHand;

    [Header("Follow Settings")]
    public bool followRotation = true;
    public bool followPosition = true;

    [Header("Offsets")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    private void LateUpdate()
    {
        if(trackedHand == null) return;

        if(followPosition)
        {
            transform.position = trackedHand.position + positionOffset;
        }
        if(followRotation)
        {
            transform.rotation = trackedHand.rotation * Quaternion.Euler(rotationOffset);
        }
    }
}
