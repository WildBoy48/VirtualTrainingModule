using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// Define the data structure to match the Python JSON
[System.Serializable]
public class HandDataPacket
{
    public Vector3Data wrist;
    public Vector3Data mid;
    public Vector3Data pinky;
    public float[] curls;
}

[System.Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;
}

public class UDPReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 5005;

    [Header("Link Reference")]
    [Tooltip("Drag your MediaPipe Manager object here")]
    public WebcamHandLink handLinkScript;

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    // We store the latest data here so the main Unity thread can read it
    private HandDataPacket latestData;
    private readonly object dataLock = new object();

    void Start()
    {
        isRunning = true;
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("UDP Receiver started on port " + port);
    }

    private void ReceiveData()
    {
        udpClient = new UdpClient(port);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, port);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref anyIP);
                string jsonString = Encoding.UTF8.GetString(data);

                // Debug.Log("RECEIVED DATA: " + jsonString);
                // Parse the JSON
                HandDataPacket parsedData = JsonUtility.FromJson<HandDataPacket>(jsonString);

                // Lock the data so Unity's Update loop doesn't read it while it's being written
                lock (dataLock)
                {
                    latestData = parsedData;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("UDP Receive Error: " + e.Message);
            }
        }
    }

    void Update()
    {
        if (latestData == null || handLinkScript == null) return;

        lock (dataLock)
        {
            // Inject the positional data from Python directly into your Hand Link script
            handLinkScript.wristPos = new Vector3(-latestData.wrist.x, latestData.wrist.y, latestData.wrist.z);
            handLinkScript.middleKnucklePos = new Vector3(-latestData.mid.x, latestData.mid.y, latestData.mid.z);
            handLinkScript.pinkyKnucklePos = new Vector3(-latestData.pinky.x, latestData.pinky.y, latestData.pinky.z);

            // Inject the finger curls
            handLinkScript.fingerCurls = latestData.curls;
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (udpClient != null) udpClient.Close();
        if (receiveThread != null) receiveThread.Abort();
    }
}