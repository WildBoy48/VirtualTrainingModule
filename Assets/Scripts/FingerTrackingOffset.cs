using UnityEngine;

public class FingerTrackingOffset : MonoBehaviour
{
    [System.Serializable]
    public class FingerOffset
    {
        public Transform fingeRootBone;
        public Vector3 rotationOffset;
    }

    public FingerOffset[] fingerOffsets;

    void LateUpdate()
    {
        foreach(var finger in fingerOffsets)
        {
            if(finger.fingeRootBone != null)
            {
                finger.fingeRootBone.localRotation *= Quaternion.Euler(finger.rotationOffset);
            }
        }
    }
}
