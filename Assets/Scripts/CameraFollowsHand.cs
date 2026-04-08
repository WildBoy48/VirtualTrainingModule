using UnityEngine;

public class CameraFollowHand : MonoBehaviour
{
    [Header("What to Follow")]
    public Transform handTarget;

    [Header("Camera Positioning")]
    // This places the camera slightly above (0.5) and behind (-1.5) the hand
    public Vector3 offset = new Vector3(0f, 0.5f, -1.5f);
    public float followSpeed = 5f;

    // We use LateUpdate for cameras so it moves AFTER the hand has finished moving this frame
    void LateUpdate()
    {
        if (handTarget != null)
        {
            // 1. Calculate where the camera SHOULD be
            Vector3 desiredPosition = handTarget.position + offset;

            // 2. Smoothly glide the camera to that position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

            // 3. Force the camera lens to always look directly at the center of the hand
            transform.LookAt(handTarget);
        }
    }
}