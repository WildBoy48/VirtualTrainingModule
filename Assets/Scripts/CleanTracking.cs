using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

public class CleanHandTracking : MonoBehaviour
{
    public int port = 5005;
    private Thread receiveThread;
    private UdpClient client;

    [Header("Bones")]
    public Transform[] thumbJoints;
    public Transform[] indexJoints;
    public Transform[] middleJoints;
    public Transform[] ringJoints;
    public Transform[] pinkyJoints;

    [Header("Settings")]
    public float maxCurlAngle = 75f;
    public float animationSpeed = 15f;
    public float movementScale = 3.0f;

    private float[] targetCurls = new float[5];
    private float[] currentCurls = new float[5];
    private Vector3 targetPos;
    private Vector3 currentPos;

    private Quaternion[] thumbInit, indexInit, middleInit, ringInit, pinkyInit;
    private Vector3 wristPos, middlePos, pinkyPos;
    private Quaternion targetRotation = Quaternion.identity;
    private Quaternion currentRotation = Quaternion.identity;

    [Header("Rotation Offset (Tweak if hand points wrong way)")]
    public Vector3 rotationOffset = new Vector3(0, 0, 0);

    void Start()
    {
        Debug.Log("TRACKING SYSTEM: Script has started successfully!"); // <--- ADD THIS
        // Save initial relaxed pose
        thumbInit = SaveInit(thumbJoints);
        indexInit = SaveInit(indexJoints);
        middleInit = SaveInit(middleJoints);
        ringInit = SaveInit(ringJoints);
        pinkyInit = SaveInit(pinkyJoints);

        // Start server
        receiveThread = new Thread(new ThreadStart(ReceiveData)) { IsBackground = true };
        receiveThread.Start();
    }

    Quaternion[] SaveInit(Transform[] joints)
    {
        Quaternion[] inits = new Quaternion[joints.Length];
        for (int i = 0; i < joints.Length; i++)
            if (joints[i] != null) inits[i] = joints[i].localRotation;
        return inits;
    }

    void ReceiveData()
    {
        client = new UdpClient(port);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, port);

        while (true)
        {
            try
            {
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                Debug.Log("TRACKING SYSTEM: Heard Python! Data: " + text); // <--- ADD THIS
                string[] values = text.Split(',');

                if (values.Length == 14)
                {
                    // 1. Parse Anchors (using InvariantCulture to prevent the decimal bug)
                    wristPos = new Vector3(
                        float.Parse(values[0]),
                        float.Parse(values[1]),
                        float.Parse(values[2]));

                    middlePos = new Vector3(
                        float.Parse(values[3]),
                        float.Parse(values[4]),
                        float.Parse(values[5]));

                    pinkyPos = new Vector3(
                        float.Parse(values[6]),
                        float.Parse(values[7]),
                        float.Parse(values[8]));

                    // 2. Position (Locking depth Z to 0.5f to keep it stable)
                    targetPos.x = (wristPos.x - 0.5f) * movementScale;
                    targetPos.y = (wristPos.y + 0.5f) * movementScale;
                    targetPos.z = 0.5f;

                    // 3. Curls
                    for (int i = 0; i < 5; i++)
                    {
                        targetCurls[i] = float.Parse(values[i + 9], System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }
            catch (System.Exception err) { Debug.LogWarning(err.ToString()); }
        }
    }

    void Update()
    {
        // 1. Position
        currentPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * animationSpeed);
        transform.localPosition = currentPos;

        // --- NEW: 2. Rotation ---
        // Forward direction: from Wrist to Middle Knuckle
        Vector3 forward = middlePos - wristPos;
        // Right direction: roughly from Middle to Pinky
        Vector3 right = pinkyPos - middlePos;
        // Up direction: cross product of Forward and Right
        Vector3 up = Vector3.Cross(forward, right);

        // Prevent math errors if vectors are zero
        if (forward.sqrMagnitude > 0.001f && up.sqrMagnitude > 0.001f)
        {
            // Calculate base rotation
            Quaternion rawRotation = Quaternion.LookRotation(forward, up);

            // Add any custom offset needed for this specific 3D model
            targetRotation = rawRotation * Quaternion.Euler(rotationOffset);

            // Slerp (Spherical Lerp) smoothly rotates the hand, hiding camera jitter
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * animationSpeed);
            transform.localRotation = currentRotation;
        }

        // 3. Fingers
        for (int i = 0; i < 5; i++)
        {
            currentCurls[i] = Mathf.Lerp(currentCurls[i], targetCurls[i], Time.deltaTime * animationSpeed);
        }

        ApplyCurl(thumbJoints, thumbInit, currentCurls[0], new Vector3(1, 0, 0));
        ApplyCurl(indexJoints, indexInit, currentCurls[1], new Vector3(1, 0, 0));
        ApplyCurl(middleJoints, middleInit, currentCurls[2], new Vector3(1, 0, 0));
        ApplyCurl(ringJoints, ringInit, currentCurls[3], new Vector3(1, 0, 0));
        ApplyCurl(pinkyJoints, pinkyInit, currentCurls[4], new Vector3(1, 0, 0));
    }

    void ApplyCurl(Transform[] joints, Quaternion[] inits, float percent, Vector3 axis)
    {
        float angle = percent * maxCurlAngle;
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] != null)
            {
                float jointAngle = (i == 2) ? angle * 0.6f : angle; // Less curl on fingertip
                joints[i].localRotation = inits[i] * Quaternion.Euler(axis * jointAngle);
            }
        }
    }

    void OnDestroy()
    {
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}