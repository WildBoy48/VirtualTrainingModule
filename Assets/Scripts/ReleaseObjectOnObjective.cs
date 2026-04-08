using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ReleaseObjectOnObjective : MonoBehaviour
{
 
    private string pythonIP = "192.168.0.101";
    private int pythonPort = 5006; // Must match LISTEN_PORT in Python
    private UdpClient udpClient;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        udpClient = new UdpClient();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Mug")) // Make sure the object corresponds to the tag used in Unity
        {
            Debug.Log("Task Complete! Telling Python to release the hand.");

            // Send the release command for all fingers involved in the task
            SendToPython("RELEASE_THUMB");
            SendToPython("RELEASE_INDEX");
            SendToPython("RELEASE_MIDDLE");
            SendToPython("RELEASE_RING");
            SendToPython("RELEASE_PINKY");
        }
    }

    void SendToPython(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, pythonIP, pythonPort);
        }
        catch (System.Exception e) { Debug.LogError("Failed to send: " + e.Message); }
    }

    void OnApplicationQuit()
    {
        if (udpClient != null) udpClient.Close();
    }
}

   