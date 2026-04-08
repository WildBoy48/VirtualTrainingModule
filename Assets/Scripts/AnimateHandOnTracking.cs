using UnityEngine;
using UnityEngine.InputSystem;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;



public class AnimateHandOnTracking : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    public int port = 5005;

    public string receivedData;

    public Animator handAnimator_udp;
    private float[] targetFingerValues = new float[5];
    private float[] currentFingerValues = new float[5];
    private float animationSpeed = 15f; // Adjust this for faster/slower animation

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        receiveThread = new Thread(new ThreadStart(Receiver_Data));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("UDP Receiver started on port " + port);
    }


    void Receiver_Data()
    {
        client = new UdpClient(port);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, port);
        while(true)
        {
            try
            {
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                string [] values = text.Split(',');

                // Ensure all 5 pieces of data
                if (values.Length == 5)
                {
                    // Convert to floats and set target finger values
                    for (int i = 0; i < 5; i++)
                    {
                        targetFingerValues[i] = float.Parse(values[i]);
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
        if (handAnimator_udp != null)
        {
            string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
            for (int i = 0; i < 5; i++)
            {
                currentFingerValues[i] = Mathf.Lerp(currentFingerValues[i], targetFingerValues[i], Time.deltaTime * animationSpeed);
                handAnimator_udp.SetFloat(fingerNames[i], currentFingerValues[i]);
            }
            // Debug log to verify values
            Debug.Log("Finger Values: " + string.Join(", ", currentFingerValues));
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
