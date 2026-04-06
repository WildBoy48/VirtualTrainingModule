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
    private float targetgripValue = 0f;
    private float currentgripValue = 0f;
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
                    // Convert to floats and set animator parameters
                    //handAnimator_udp.SetFloat("Thumb", float.Parse(values[0]));
                    //handAnimator_udp.SetFloat("Index", float.Parse(values[1]));
                    //handAnimator_udp.SetFloat("Middle", float.Parse(values[2]));
                    //handAnimator_udp.SetFloat("Ring", float.Parse(values[3]));
                    //handAnimator_udp.SetFloat("Pinky", float.Parse(values[4]));

                    //handAnimator_udp.SetFloat("Grip", float.Parse(values[1])); 

                    // Using Index for Grip
                    targetgripValue = float.Parse(values[1]);
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
            currentgripValue = Mathf.Lerp(currentgripValue, targetgripValue, Time.deltaTime * animationSpeed);
            handAnimator_udp.SetFloat("Grip", currentgripValue );
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
