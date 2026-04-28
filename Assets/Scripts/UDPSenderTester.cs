using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

public class UDPSenderAndTester : MonoBehaviour
{
    [Header("Network Settings")]
    [Tooltip("Must match your Python machine's IP (use 127.0.0.1 if on the same computer)")]
    public string pythonIP = "192.168.1.234";
    [Tooltip("Matches the LISTEN_PORT in your Python script")]
    public int sendPort = 5006;

    [Header("References")]
    public WebcamHandLink handLink;

    private UdpClient udpClient;
    private bool isHolding = false;

    void Start()
    {
        udpClient = new UdpClient();
        Debug.Log("UDP Sender ready to broadcast on port " + sendPort);
    }

    void Update()
    {
        if (handLink == null || handLink.fingerCurls.Length < 5) return;

        // Simple grasp heuristic: If the Index finger is heavily curled, we assume a grasp
        if (handLink.fingerCurls[1] > 0.7f && !isHolding)
        {
            isHolding = true;
            StartCoroutine(ReleaseAfterDelay(2.0f));
        }
        // Reset the toggle if the hand naturally opens (or gets forced open by the release state)
        else if (handLink.fingerCurls[1] < 0.3f)
        {
            isHolding = false;
        }
    }

    IEnumerator ReleaseAfterDelay(float delay)
    {
        Debug.Log($"Grasp detected! Waiting {delay} seconds to trigger release...");

        yield return new WaitForSeconds(delay);

        // Send the specific release strings your Python FSM is looking for
        string[] fingers = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
        foreach (string f in fingers)
        {
            SendString("RELEASE_" + f);
        }

        Debug.Log("Release messages fired back to Python!");
    }

    private void SendString(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, pythonIP, sendPort);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("UDP Send Error: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (udpClient != null) udpClient.Close();
    }
}