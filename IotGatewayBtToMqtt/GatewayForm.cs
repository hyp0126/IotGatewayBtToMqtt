using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

// References:
// https://docs.microsoft.com/en-us/dotnet/api/system.io.ports.serialport?view=dotnet-plat-ext-5.0
// https://www.nuget.org/packages/M2Mqtt/
// https://m2mqtt.wordpress.com/m2mqtt_doc/

namespace IotGatewayBtToMqtt
{
    public partial class GatewayForm : Form
    {
        static SerialPort serialPort;
        MqttClient client;
        string clientId;
        const string MQTT_BROKER_ADDRESS = "192.168.2.62";

        public GatewayForm()
        {
            InitializeComponent();
        }

        private void GatewayForm_Load(object sender, EventArgs e)
        {
            serialPort = new SerialPort();
            serialPort.DataReceived += new SerialDataReceivedEventHandler(BtDataReceivedHandler);

            foreach (string s in SerialPort.GetPortNames())
            {
                cbxCommPort.Items.Add(s);
            }

            // create client instance
            client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));
            clientId = Guid.NewGuid().ToString();
        }

        private void BtDataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            string message = serialPort.ReadLine();
            txtInput.Invoke(new MethodInvoker(delegate ()
            {
                txtInput.Text += message;
                // set the current caret position to the end
                txtInput.SelectionStart = txtInput.Text.Length;
                // scroll it automatically
                txtInput.ScrollToCaret();
            }));

            //string strValue = Convert.ToString(value);
            // publish a message on "/home/temperature" topic with QoS 2
            //client.Publish("/home/temperature", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE);

            string[] strValues = message.Split('/');
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < strValues.Length - 1; i++)
            {
                builder.Append(strValues[i]);
                if (i != strValues.Length - 2)
                {
                    builder.Append("/");
                }
            }
            string topic = builder.ToString();
            client.Publish(topic, Encoding.UTF8.GetBytes(strValues[strValues.Length - 1]));

            txtOutput.Invoke(new MethodInvoker(delegate ()
            {
                txtOutput.Text += topic + "/" + strValues[strValues.Length - 1];
                // set the current caret position to the end
                txtOutput.SelectionStart = txtInput.Text.Length;
                // scroll it automatically
                txtOutput.ScrollToCaret();
            }));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (serialPort.IsOpen)
            {
                MessageBox.Show("Port already opened");
            }
            else
            {
                // Allow the user to set the appropriate properties.
                serialPort.BaudRate = 9600;
                serialPort.Parity = Parity.None;
                serialPort.DataBits = 8;
                serialPort.StopBits = StopBits.One;
                serialPort.Handshake = Handshake.None;

                // Set the read/write timeouts
                serialPort.ReadTimeout = 500;
                serialPort.WriteTimeout = 500;

                serialPort.Open();

                client.Connect(clientId);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                client.Disconnect();
            }
        }

        private void cbxCommPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            serialPort.PortName = cbxCommPort.SelectedItem.ToString();
        }
    }
}
