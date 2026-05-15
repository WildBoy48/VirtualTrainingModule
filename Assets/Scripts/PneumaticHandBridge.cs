using UnityEngine;
using Autohand;


public class PneumaticHandBridge : MonoBehaviour
{
    [Header("Meta Hand Driver")]
    public OVRHand metaHand;

    [Header("AutoHand Physics")]
    public Hand autoHand;

    [Header("Pneumatic Glove Settingsd")]
    [Range(0.0f, 0.99f)] public float pneumaticSmoothing = 0.15f;

    [HideInInspector] public float[] valvePressures = new float[5];

    [Header("Grab Thresholds")]
    [Range(0f, 1f)] public float grabThreshold = 0.7f;
    [Range(0f, 1f)] public float releaseThreshold = 0.3f;
    public bool _isGrabbing = false;

    [Header("Debug Settings")]
    public bool debugLogs = true;

    // Update is called once per frame
    void Update()
    {
        if (metaHand == null || autoHand == null || !metaHand.IsTracked) return;

        if (!metaHand.IsTracked)
        {
            if (!debugLogs) Debug.LogWarning("Waiting for MetaHand data");
            return;
        }
        for (int i = 0; i < 5; i++)
        {
            float rawCurl = metaHand.GetFingerPinchStrength((OVRHand.HandFinger)i);

            if(debugLogs && i == 1)
            {
                Debug.Log($"Index finger curl is :{rawCurl}");
            }
            if (autoHand.fingers.Length > i)
            {
                autoHand.fingers[i].SetFingerBend(rawCurl);
            }
        }

        float indexCurl = metaHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        if (indexCurl >= grabThreshold && !_isGrabbing)
        {
            autoHand.Grab();
            _isGrabbing = true;
        }
        else if (indexCurl <= releaseThreshold && _isGrabbing)
        {
            autoHand.Release();
            _isGrabbing = false;
        }
        //UpdateFingersForValves();
        //HandleVirtualGrabbing();


    }

    void UpdateFingersForValves()
    {
        for(int i = 0; i <5; i++)
        {
            float rawCurl = metaHand.GetFingerPinchStrength((OVRHand.HandFinger)i);

            valvePressures[i] = Mathf.Lerp(valvePressures[i], rawCurl, 1f - pneumaticSmoothing);

            if(autoHand.fingers.Length > i)
            {
                autoHand.fingers[i].SetFingerBend(valvePressures[i]);
            }
        }
    }

    void HandleVirtualGrabbing()
    {
        float indexPressure = valvePressures[1];

        if (indexPressure >= grabThreshold && !_isGrabbing) 
        {
            autoHand.Grab();
            _isGrabbing = true;
        } else if(indexPressure <= releaseThreshold && _isGrabbing)
        {
            autoHand.Release();
            _isGrabbing = false;
        }
    }
}
