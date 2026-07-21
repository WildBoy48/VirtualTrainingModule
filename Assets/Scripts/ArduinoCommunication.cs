using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Samples.Hands;
using System.Collections.Concurrent;

public class ArduinoCommunication : MonoBehaviour
{
    [Header("Serial Port Settings")]
    public string portName = "COM3";
    public int baudRate = 115200;
    
    private SerialPort serialPort;
    private Thread heartbeatThread;
    private bool isRunning = false;

    private bool isReady = false;

    private ConcurrentQueue<string> commandQueue = new ConcurrentQueue<string>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(InitializeConnection());
    }

    IEnumerator InitializeConnection()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 50; // Set a read timeout of 1 second

        try
        {
            serialPort.Open();
            Debug.Log($"Opened {portName}. Waiting 2 seconds for arduino.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to open serial port: " + e.Message);
            yield break;
        }
        yield return new WaitForSeconds(2f); // Wait for Arduino to reset
        isReady = true;
        Debug.Log("Arduino is ready. Starting.");

        // Safety Concerns
        Release();

        isRunning = true;
        //heartbeatThread = new Thread(HeartbeatLoop);
        heartbeatThread = new Thread(SerialWriterLoop);
        heartbeatThread.IsBackground = true;
        heartbeatThread.Start();
    }

    private void HeartbeatLoop()
    {
        while (isRunning)
        {
            if(serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    serialPort.WriteLine("H");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to send heartbeat: " + e.Message);
                }
            }
        }
        // Send Heartbeat every 500ms
        Thread.Sleep(500);
    }

    // Only Thread that writes to the serial port, to avoid conflicts with Unity's main thread
    private void SerialWriterLoop()
    {
        long lastHeartbeatTime = 0;

        while (isRunning)
        {
            if(serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    while(commandQueue.TryDequeue(out string command))
                    {
                        serialPort.WriteLine(command);
                        Debug.Log("Sent Command: " + command);
                        lastHeartbeatTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                    }

                    long currentTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                    if(currentTime - lastHeartbeatTime >= 500)
                    {
                        serialPort.Write("H");
                        Debug.Log("Sent Heartbeat");
                        lastHeartbeatTime = currentTime;
                    }


                    while (serialPort.BytesToRead > 0)
                    {
                        string incomingMessage = serialPort.ReadLine();
                        // Print the Arduino's message to the Unity Console
                        Debug.Log("<color=cyan>[ARDUINO]</color> " + incomingMessage);
                    }
                }
                catch (System.TimeoutException)
                {
                    // Timeout is expected on empty reads, safely ignore
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to send heartbeat: " + e.Message);
                }
            }
            // Sleep for 10ms 
            Thread.Sleep(10);
        }
    }


    // Public Control Methods
    public void Grab()
    {
        if(!isReady || !serialPort.IsOpen) return;
        commandQueue.Enqueue("G");
        Debug.Log("Sent Grab Command");
    }

    public void Release()
    {
        if(!isReady || !serialPort.IsOpen) return;
        commandQueue.Enqueue("R");
        Debug.Log("Sent Release Command");
    }
    //Clenaup
    void OnDestroy()
    {
        isRunning = false;

        if(heartbeatThread != null && heartbeatThread.IsAlive)
        {
            heartbeatThread.Join(500);
        }

        if(serialPort != null && serialPort.IsOpen)
        {
            try {serialPort.Write("R"); } catch { }
            serialPort.Close();
            Debug.Log("Closed Serial Port");
        }
    }

    void Update()
    {
        if (!isReady) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Grab();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Release();
        }
    }
}
