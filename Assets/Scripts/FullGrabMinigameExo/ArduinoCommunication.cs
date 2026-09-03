using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Samples.Hands;
using System.Collections.Concurrent;

/// <summary>
/// Manages a background thread for safe, non-blocking serial communication with the Arduino.
/// Handles sending Grab/Release commands and maintaining a heartbeat to keep the exoskeleton active.
/// </summary>
public class ArduinoCommunication : MonoBehaviour
{
    [Header("Serial Port Settings")]
    [Tooltip("The name of the serial port to connect to (e.g., COM3).")]
    [SerializeField] private string portName = "COM3";

    [Tooltip("The baud rate for the serial communication.")]
    [SerializeField] private int baudRate = 115200;
    
    private SerialPort serialPort;
    private Thread serialThread;
    private bool isRunning = false;
    private bool isReady = false;

    // Thread-safe queue for commands to be sent to the Arduino
    private ConcurrentQueue<string> commandQueue = new ConcurrentQueue<string>();

    private void Start()
    {
        StartCoroutine(InitializeConnection());
    }
    private void Update()
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

    /// <summary>
    /// Coroutine to initialize the serial connection with the Arduino.
    /// </summary>
    /// <returns></returns>
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
        serialThread = new Thread(SerialWriterLoop);
        serialThread.IsBackground = true;
        serialThread.Start();
    }

    /// <summary>
    /// Background thread loop that handles sending commands and heartbeats to the Arduino. Exclusively handles writing commands.
    /// Runs independently of the main Unity thread to prevent blocking and ensure timely communication.
    /// </summary>
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
                        //Debug.Log("Sent Command: " + command);
                        lastHeartbeatTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                    }

                    long currentTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                    if(currentTime - lastHeartbeatTime >= 500)
                    {
                        serialPort.Write("H");
                        //Debug.Log("Sent Heartbeat");
                        lastHeartbeatTime = currentTime;
                    }


                    while (serialPort.BytesToRead > 0)
                    {
                        string incomingMessage = serialPort.ReadLine().TrimEnd();

                        if (!incomingMessage.StartsWith("S"))
                        {
                            Debug.Log("<color=cyan>[ARDUINO]</color> " + incomingMessage);
                        }
                        // Print the Arduino's message to the Unity Console
                        //Debug.Log("<color=cyan>[ARDUINO]</color> " + incomingMessage);
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

    /// <summary>
    /// Queues a Grab command to be sent to the Arduino.
    /// Thread-safe and non-blocking, can be called from the main Unity thread without causing delays.
    /// </summary>
    public void Grab()
    {
        if(!isReady || !serialPort.IsOpen) return;
        commandQueue.Enqueue("G");
        Debug.Log("Sent Grab Command");
    }

    /// <summary>
    /// Queues a Release command to be sent to the Arduino.
    /// Thread-safe and non-blocking, can be called from the main Unity thread without causing delays.
    /// </summary>
    public void Release()
    {
        if(!isReady || !serialPort.IsOpen) return;
        commandQueue.Enqueue("R");
        Debug.Log("Sent Release Command");
    }
    //Clenaup
    private void OnDestroy()
    {
        isRunning = false;

        if(serialThread != null && serialThread.IsAlive)
        {
            serialThread.Join(500);
        }

        if(serialPort != null && serialPort.IsOpen)
        {
            try {serialPort.Write("R"); } catch { }
            serialPort.Close();
            Debug.Log("<color=cyan>[ARDUINO]</color> Closed Serial Port safely.");
        }
    }
}
