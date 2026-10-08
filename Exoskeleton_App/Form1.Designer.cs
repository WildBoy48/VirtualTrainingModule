namespace ExoskeletonApp
{
    partial class App
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.comboBoxPorts = new System.Windows.Forms.ComboBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.richTextBoxConsole = new System.Windows.Forms.RichTextBox();
            this.btnGrab = new System.Windows.Forms.Button();
            this.btnRelease = new System.Windows.Forms.Button();
            this.panelStatusLight = new System.Windows.Forms.Panel();
            this.lblPneumaticControl = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lblSerialPort = new System.Windows.Forms.Label();
            this.lblSerialMonitor = new System.Windows.Forms.Label();
            this.lblSensorValue0 = new System.Windows.Forms.Label();
            this.progressBarSensor0 = new System.Windows.Forms.ProgressBar();
            this.lblSensorValue1 = new System.Windows.Forms.Label();
            this.progressBarSensor1 = new System.Windows.Forms.ProgressBar();
            this.lblSensorValue2 = new System.Windows.Forms.Label();
            this.progressBarSensor2 = new System.Windows.Forms.ProgressBar();
            this.lblSensorValue3 = new System.Windows.Forms.Label();
            this.progressBarSensor3 = new System.Windows.Forms.ProgressBar();
            this.lblSensorValue4 = new System.Windows.Forms.Label();
            this.progressBarSensor4 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // comboBoxPorts
            // 
            this.comboBoxPorts.FormattingEnabled = true;
            this.comboBoxPorts.Location = new System.Drawing.Point(10, 30);
            this.comboBoxPorts.Name = "comboBoxPorts";
            this.comboBoxPorts.Size = new System.Drawing.Size(120, 21);
            this.comboBoxPorts.TabIndex = 0;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(29, 60);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 30);
            this.btnConnect.TabIndex = 1;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // richTextBoxConsole
            // 
            this.richTextBoxConsole.Location = new System.Drawing.Point(12, 317);
            this.richTextBoxConsole.Name = "richTextBoxConsole";
            this.richTextBoxConsole.Size = new System.Drawing.Size(776, 121);
            this.richTextBoxConsole.TabIndex = 2;
            this.richTextBoxConsole.Text = "";
            // 
            // btnGrab
            // 
            this.btnGrab.Location = new System.Drawing.Point(12, 249);
            this.btnGrab.Name = "btnGrab";
            this.btnGrab.Size = new System.Drawing.Size(75, 40);
            this.btnGrab.TabIndex = 3;
            this.btnGrab.Text = "Grab (Inflate)";
            this.toolTip1.SetToolTip(this.btnGrab, "Drops pumps and valves to LOW to curl the fingers");
            this.btnGrab.UseVisualStyleBackColor = true;
            this.btnGrab.Click += new System.EventHandler(this.btnGrab_Click);
            // 
            // btnRelease
            // 
            this.btnRelease.Location = new System.Drawing.Point(116, 249);
            this.btnRelease.Name = "btnRelease";
            this.btnRelease.Size = new System.Drawing.Size(75, 40);
            this.btnRelease.TabIndex = 4;
            this.btnRelease.Text = "Release (Deflate)";
            this.btnRelease.UseVisualStyleBackColor = true;
            this.btnRelease.Click += new System.EventHandler(this.btnRelease_Click);
            // 
            // panelStatusLight
            // 
            this.panelStatusLight.BackColor = System.Drawing.Color.DarkRed;
            this.panelStatusLight.Location = new System.Drawing.Point(110, 70);
            this.panelStatusLight.Name = "panelStatusLight";
            this.panelStatusLight.Size = new System.Drawing.Size(10, 10);
            this.panelStatusLight.TabIndex = 5;
            // 
            // lblPneumaticControl
            // 
            this.lblPneumaticControl.AutoSize = true;
            this.lblPneumaticControl.Location = new System.Drawing.Point(9, 218);
            this.lblPneumaticControl.Name = "lblPneumaticControl";
            this.lblPneumaticControl.Size = new System.Drawing.Size(83, 13);
            this.lblPneumaticControl.TabIndex = 6;
            this.lblPneumaticControl.Text = "Manual Controls";
            // 
            // lblSerialPort
            // 
            this.lblSerialPort.AutoSize = true;
            this.lblSerialPort.Location = new System.Drawing.Point(10, 10);
            this.lblSerialPort.Name = "lblSerialPort";
            this.lblSerialPort.Size = new System.Drawing.Size(55, 13);
            this.lblSerialPort.TabIndex = 7;
            this.lblSerialPort.Text = "Serial Port";
            // 
            // lblSerialMonitor
            // 
            this.lblSerialMonitor.AutoSize = true;
            this.lblSerialMonitor.Location = new System.Drawing.Point(12, 301);
            this.lblSerialMonitor.Name = "lblSerialMonitor";
            this.lblSerialMonitor.Size = new System.Drawing.Size(71, 13);
            this.lblSerialMonitor.TabIndex = 13;
            this.lblSerialMonitor.Text = "Serial Monitor";
            // 
            // lblSensorValue0
            // 
            this.lblSensorValue0.AutoSize = true;
            this.lblSensorValue0.Location = new System.Drawing.Point(678, 34);
            this.lblSensorValue0.Name = "lblSensorValue0";
            this.lblSensorValue0.Size = new System.Drawing.Size(91, 13);
            this.lblSensorValue0.TabIndex = 17;
            this.lblSensorValue0.Text = "Pressure: 0.0 kPa";
            // 
            // progressBarSensor0
            // 
            this.progressBarSensor0.Location = new System.Drawing.Point(572, 37);
            this.progressBarSensor0.Maximum = 220;
            this.progressBarSensor0.Name = "progressBarSensor0";
            this.progressBarSensor0.Size = new System.Drawing.Size(100, 10);
            this.progressBarSensor0.TabIndex = 16;
            // 
            // lblSensorValue1
            // 
            this.lblSensorValue1.AutoSize = true;
            this.lblSensorValue1.Location = new System.Drawing.Point(678, 64);
            this.lblSensorValue1.Name = "lblSensorValue1";
            this.lblSensorValue1.Size = new System.Drawing.Size(91, 13);
            this.lblSensorValue1.TabIndex = 19;
            this.lblSensorValue1.Text = "Pressure: 0.0 kPa";
            // 
            // progressBarSensor1
            // 
            this.progressBarSensor1.Location = new System.Drawing.Point(572, 67);
            this.progressBarSensor1.Maximum = 220;
            this.progressBarSensor1.Name = "progressBarSensor1";
            this.progressBarSensor1.Size = new System.Drawing.Size(100, 10);
            this.progressBarSensor1.TabIndex = 18;
            // 
            // lblSensorValue2
            // 
            this.lblSensorValue2.AutoSize = true;
            this.lblSensorValue2.Location = new System.Drawing.Point(678, 94);
            this.lblSensorValue2.Name = "lblSensorValue2";
            this.lblSensorValue2.Size = new System.Drawing.Size(91, 13);
            this.lblSensorValue2.TabIndex = 21;
            this.lblSensorValue2.Text = "Pressure: 0.0 kPa";
            // 
            // progressBarSensor2
            // 
            this.progressBarSensor2.Location = new System.Drawing.Point(572, 97);
            this.progressBarSensor2.Maximum = 220;
            this.progressBarSensor2.Name = "progressBarSensor2";
            this.progressBarSensor2.Size = new System.Drawing.Size(100, 10);
            this.progressBarSensor2.TabIndex = 20;
            // 
            // lblSensorValue3
            // 
            this.lblSensorValue3.AutoSize = true;
            this.lblSensorValue3.Location = new System.Drawing.Point(678, 124);
            this.lblSensorValue3.Name = "lblSensorValue3";
            this.lblSensorValue3.Size = new System.Drawing.Size(91, 13);
            this.lblSensorValue3.TabIndex = 23;
            this.lblSensorValue3.Text = "Pressure: 0.0 kPa";
            // 
            // progressBarSensor3
            // 
            this.progressBarSensor3.Location = new System.Drawing.Point(572, 127);
            this.progressBarSensor3.Maximum = 220;
            this.progressBarSensor3.Name = "progressBarSensor3";
            this.progressBarSensor3.Size = new System.Drawing.Size(100, 10);
            this.progressBarSensor3.TabIndex = 22;
            // 
            // lblSensorValue4
            // 
            this.lblSensorValue4.AutoSize = true;
            this.lblSensorValue4.Location = new System.Drawing.Point(678, 154);
            this.lblSensorValue4.Name = "lblSensorValue4";
            this.lblSensorValue4.Size = new System.Drawing.Size(91, 13);
            this.lblSensorValue4.TabIndex = 25;
            this.lblSensorValue4.Text = "Pressure: 0.0 kPa";
            // 
            // progressBarSensor4
            // 
            this.progressBarSensor4.Location = new System.Drawing.Point(572, 157);
            this.progressBarSensor4.Maximum = 220;
            this.progressBarSensor4.Name = "progressBarSensor4";
            this.progressBarSensor4.Size = new System.Drawing.Size(100, 10);
            this.progressBarSensor4.TabIndex = 24;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(475, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 26;
            this.label1.Text = "Thumb Finger";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(475, 157);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 27;
            this.label2.Text = "Index Finger";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(475, 127);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 13);
            this.label3.TabIndex = 28;
            this.label3.Text = "Middle Finger";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(475, 97);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 13);
            this.label4.TabIndex = 29;
            this.label4.Text = "Ring Finger";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(475, 67);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 13);
            this.label5.TabIndex = 30;
            this.label5.Text = "Pinky Finger";
            // 
            // App
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(816, 456);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblSensorValue4);
            this.Controls.Add(this.progressBarSensor4);
            this.Controls.Add(this.lblSensorValue3);
            this.Controls.Add(this.progressBarSensor3);
            this.Controls.Add(this.lblSensorValue2);
            this.Controls.Add(this.progressBarSensor2);
            this.Controls.Add(this.lblSensorValue1);
            this.Controls.Add(this.progressBarSensor1);
            this.Controls.Add(this.lblSensorValue0);
            this.Controls.Add(this.progressBarSensor0);
            this.Controls.Add(this.lblSerialMonitor);
            this.Controls.Add(this.lblSerialPort);
            this.Controls.Add(this.lblPneumaticControl);
            this.Controls.Add(this.panelStatusLight);
            this.Controls.Add(this.btnRelease);
            this.Controls.Add(this.btnGrab);
            this.Controls.Add(this.richTextBoxConsole);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.comboBoxPorts);
            this.Name = "App";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxPorts;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.RichTextBox richTextBoxConsole;
        private System.Windows.Forms.Button btnGrab;
        private System.Windows.Forms.Button btnRelease;
        private System.Windows.Forms.Panel panelStatusLight;
        private System.Windows.Forms.Label lblPneumaticControl;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblSerialPort;
        private System.Windows.Forms.Label lblSerialMonitor;
        private System.Windows.Forms.Label lblSensorValue0;
        private System.Windows.Forms.ProgressBar progressBarSensor0;
        private System.Windows.Forms.Label lblSensorValue1;
        private System.Windows.Forms.ProgressBar progressBarSensor1;
        private System.Windows.Forms.Label lblSensorValue2;
        private System.Windows.Forms.ProgressBar progressBarSensor2;
        private System.Windows.Forms.Label lblSensorValue3;
        private System.Windows.Forms.ProgressBar progressBarSensor3;
        private System.Windows.Forms.Label lblSensorValue4;
        private System.Windows.Forms.ProgressBar progressBarSensor4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
    }
}

