using UnityEngine;
using UnityEngine.InputSystem;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Globalization;

public class CustomHandVisualizer : MonoBehaviour
{
    [Header("Network Settings")]
    Thread receiveThread;
    UdpClient client;
    public int port = 5005;

    public string receivedData;

    [Header("Finger Joints")]
    public Transform[] thumbJoints = new Transform[3]; // MCP, PIP, DIP
    public Transform[] indexJoints = new Transform[3];
    public Transform[] middleJoints = new Transform[3];
    public Transform[] ringJoints = new Transform[3];
    public Transform[] pinkyJoints = new Transform[3];

    [Header("Rotation Settings")]
    private float[] targetFingerValues = new float[5];
    private float[] currentFingerValues = new float[5];

    public Vector3 curlAxis = new Vector3(1, 0, 0); // Usually X axis for bending inward
    public float maxCurlAngle = 70f;
    private float animationSpeed = 15f;
    private Quaternion[] thumbInit, indexInit, middleInit, ringInit, pinkyInit;

    [Header("Tracking Settings")]
    public float movementScale = 2.0f; // Multiplier to make hand move further in Unity
    public float zDepthScale = 1.0f;   // Multiplier for forward/back movement

    private Vector3 targetWristPos;
    private Vector3 currentWristPos;
    private Vector3 wristPos, middlePos, pinkyPos;
    private Quaternion targetRotation;
    private Quaternion currentRotation = Quaternion.identity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Save the natural resting pose of the hand before we start moving it
        thumbInit = SaveInitialPose(thumbJoints);
        indexInit = SaveInitialPose(indexJoints);
        middleInit = SaveInitialPose(middleJoints);
        ringInit = SaveInitialPose(ringJoints);
        pinkyInit = SaveInitialPose(pinkyJoints);

        receiveThread = new Thread(new ThreadStart(Receiver_Data));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("UDP Receiver started on port " + port);
    }
    Quaternion[] SaveInitialPose(Transform[] joints)
    {
        Quaternion[] inits = new Quaternion[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] != null) inits[i] = joints[i].localRotation;
        }
        return inits;
    }

    void Receiver_Data()
    {
        client = new UdpClient(port);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, port);
        while (true)
        {
            try
            {
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                string[] values = text.Split(',');

                // Ensure all 8 pieces of data, 3 for wrist + 5 for fingers
                if (values.Length == 14)
                {
                    // 1. Parse the 3 anchor points
                    wristPos = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
                    middlePos = new Vector3(float.Parse(values[3]), float.Parse(values[4]), float.Parse(values[5]));
                    pinkyPos = new Vector3(float.Parse(values[6]), float.Parse(values[7]), float.Parse(values[8]));

                    // Convert to floats and set target finger values
                    for (int i = 0; i < 5; i++)
                    {
                        targetFingerValues[i] = float.Parse(values[i + 3]);
                    }
                }
                Debug.Log("Received: " + text);
                receivedData = text;
            }
            catch (System.Exception err)
            {
                Debug.LogError(err.ToString());
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        // 1. Position: Center the wrist and scale the movement
        Vector3 rawPos = new Vector3((wristPos.x - 0.5f), (wristPos.y + 0.5f), wristPos.z);
        Vector3 targetPos = rawPos * movementScale;
        currentWristPos = Vector3.Lerp(currentWristPos, targetPos, Time.deltaTime * animationSpeed);
        transform.localPosition = currentWristPos;

        // 2. Rotation: Calculate Forward and Up based on your knuckles
        // Forward is from wrist to middle knuckle
        Vector3 forward = middlePos - wristPos;

        // Right is from index to pinky (roughly), we use Cross product to find true UP
        Vector3 right = pinkyPos - middlePos;
        Vector3 up = Vector3.Cross(forward, right);

        // Prevent Unity from throwing an error if tracking blips to 0
        if (forward != Vector3.zero && up != Vector3.zero)
        {
            // Calculate the target rotation and smooth it out
            targetRotation = Quaternion.LookRotation(forward, up);
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * animationSpeed);

            // Apply it to the main hand object
            transform.localRotation = currentRotation;
        }
        // Lerp the values
        for (int i = 0; i < 5; i++)
        {
            // --- THE DEADZONE FILTER ---

            // If it's mostly closed, snap to 1.0 (stops fist jitter)
            if (targetFingerValues[i] > 0.85f) targetFingerValues[i] = 1.0f;

            // If it's mostly open, snap to 0.0 (stops flat-hand jitter)
            if (targetFingerValues[i] < 0.15f) targetFingerValues[i] = 0.0f;

            currentFingerValues[i] = Mathf.Lerp(currentFingerValues[i], targetFingerValues[i], Time.deltaTime * animationSpeed);
        }

        // Set bone rotations based on curl (0 = extended, 1 = curled)
        SetFingerJoints(thumbJoints, thumbInit, currentFingerValues[0]);
        SetFingerJoints(indexJoints, indexInit, currentFingerValues[1]);
        SetFingerJoints(middleJoints,middleInit, currentFingerValues[2]);
        SetFingerJoints(ringJoints, ringInit, currentFingerValues[3]);
        SetFingerJoints(pinkyJoints, pinkyInit, currentFingerValues[4]);


        // Debug log
        Debug.Log("Finger Values: " + string.Join(", ", currentFingerValues));
    }

    void SetFingerJoints(Transform[] joints, Quaternion[] initRots, float curl)
    {
        float baseAngle = curl * maxCurlAngle;

        for (int i = 0; i < joints.Length; i++)
        {   
            if (joints[i] != null)
            {
                // Multiply the base angle by 0.6 for the Distal joint (Index 2) so the fingertip curls naturally
                float jointAngle = (i == 2) ? baseAngle * 0.6f : baseAngle;

                // Multiply the original saved pose by the new rotation to keep the natural finger splay
                joints[i].localRotation = initRots[i] * Quaternion.Euler(curlAxis * jointAngle);
            }
        }

    }

    // Clean up the thread and UDP client when the object is destroyed
    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        if (client != null)
        {
            client.Close();
        }
    }
}