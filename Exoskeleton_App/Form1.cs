using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace ExoskeletonApp
{
    public partial class App : Form
    {
        // Array to loop through elements
        private ProgressBar[] progressBars;
        private Label[] sensorLabels;

        private SerialPort serialPort;
        private Timer heartbeatTimer;

        private System.Diagnostics.Stopwatch latencyTimer = new System.Diagnostics.Stopwatch();

        public App()
        {
            InitializeComponent();

            // Get Avaliable Serial Ports and populate the comboBox
            comboBoxPorts.Items.AddRange(SerialPort.GetPortNames());

            // Send Heartbeat every 300ms just like the Arduino code does
            heartbeatTimer = new Timer();
            heartbeatTimer.Interval = 300;
            heartbeatTimer.Tick += HeartbeatTimer_Tick;

            progressBars = new ProgressBar[] {progressBarSensor0, progressBarSensor1 , progressBarSensor2, progressBarSensor3,
            progressBarSensor4};
            sensorLabels = new Label[] {lblSensorValue0, lblSensorValue1, lblSensorValue2, lblSensorValue3,
                lblSensorValue4};

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                // Disconnect
                try { serialPort.Write("R"); } catch { }
                serialPort.Close();
                btnConnect.Text = "Connect";
                heartbeatTimer.Stop();

                panelStatusLight.BackColor = System.Drawing.Color.DarkRed;

                Log("Disconnected.");

            }
            else
            {
                // Connect
                if (comboBoxPorts.SelectedItem == null) return;
                serialPort = new SerialPort(comboBoxPorts.SelectedItem.ToString(), 115200);
                serialPort.ReadTimeout = 50;
                serialPort.WriteTimeout = 100;

                try
                {
                    serialPort.Open();
                    btnConnect.Text = "Disconnect";

                    panelStatusLight.BackColor = System.Drawing.Color.DarkGreen;

                    Log($"Connected to {serialPort.PortName}.");

                    // Route incoming Arduino messages to a custom event handler
                    serialPort.DataReceived += SerialPort_DataReceived;
                    heartbeatTimer.Start();
                }
                catch (Exception ex)
                {
                    Log($"Error connecting to {serialPort.PortName}: {ex.Message}");
                }
            }
        }

        private void HeartbeatTimer_Tick(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    serialPort.Write("H"); // Send heartbeat
                }
                catch (TimeoutException)
                {
                    Log("Heartbeat timeout.");
                }
                catch (System.IO.IOException)
                {
                    Log("Serial port disconnected.");
                    btnConnect.Text = "Connect";
                    panelStatusLight.BackColor = System.Drawing.Color.DarkRed;
                    heartbeatTimer.Stop();
                }
            }
        }

        // Interaction Logic
        private void btnGrab_Click(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    latencyTimer.Restart(); // Start measuring latency
                    serialPort.Write("G"); // Send Grab command
                    Log("Sent Grab command.");
                }
                catch (Exception ex)
                {
                    Log($"Error sending Grab command: {ex.Message}");
                }
            }
        }

        private void btnRelease_Click(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    latencyTimer.Restart(); // Start measuring latency
                    serialPort.Write("R"); // Send Release command
                    Log("Sent Release command.");
                }
                catch (Exception ex)
                {
                    Log($"Error sending Release command: {ex.Message}");
                }
            }
        }

        // Serial Monitor Logic
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (serialPort != null && serialPort.IsOpen && serialPort.BytesToRead > 0 )
            {
                try
                {
                    string message = serialPort.ReadLine().TrimEnd();

                    // Update to the UI thread once per line
                    this.Invoke(new Action(() => ParseArduinoMessage(message)));
                }
                catch (TimeoutException)
                {
                    // Ignore timeout exceptions
                    break;
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() => Log($"Error reading from serial port: {ex.Message}")));
                }
                
            }
            
        }

        /// <summary>
        /// Dynamically parses incoming messages from the Arduino and updates the UI accordingly.
        /// </summary>
        private void ParseArduinoMessage(string message)
        {
            
            // Handle FSM State Notifications & Warnings
            if (message.StartsWith(">>>") || message.StartsWith("!!!") || message.Contains("FSM"))
            {
                if(latencyTimer.IsRunning)
                {
                    latencyTimer.Stop();
                    long rtt = latencyTimer.ElapsedMilliseconds;
                    long onewayLatency = rtt / 2;
                    Log($"Round-trip time: {rtt} ms, Estimated one-way latency: {onewayLatency} ms");
                }
                Log(message);
                return;
            }

            // Check if the message is a sensor reading
            if (message.StartsWith("S") && message.Contains(":"))
            {
                try
                {
                    string[] parts = message.Split(':');

                    // Extract integer from prefix (e.g., "S0:") and use it as an index for the progress bar
                    int sensorIndex = int.Parse(parts[0].Substring(1));

                    if (double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pressure))
                    {
                        // Ensure the index matches our UI array bounds
                        if (sensorIndex >= 0 && sensorIndex < progressBars.Length)
                        {
                            // Cap progress bar at 125 (MAX_SAFE_KPA from Arduino)
                            progressBars[sensorIndex].Value = (int)Math.Max(0, Math.Min(125, pressure));
                            sensorLabels[sensorIndex].Text = $"Pressure: {pressure:F1} kPa";
                        }
                    }
                }
                catch (Exception ex) 
                {
                    // Silently ignore parsing errors for sensor readings
                    Log($"Parsing Crash: {ex.Message} | Raw String: {message}");
                }
                return;
            }
            Log($"Unknown Message: {message}");
        }

        private void Log(string message)
        {
            // Appends all messages to the Text Box with a timestamp
            richTextBoxConsole.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            richTextBoxConsole.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try { serialPort.Write("R"); } catch { }
                serialPort.Close();
            }
            base.OnFormClosing(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        
    }
}
