using System.IO.Ports;
using UnityEngine;

public partial class PortViewer
{
    public struct Sensor
    {
        public SerialPort serialPort;
        public Transform transform;
        public float sensitivity;

        public Sensor(SerialPort serialPort, Transform transform, float time)
        {
            this.serialPort = serialPort;
            serialPort.BaudRate = 115200;
            serialPort.DataBits = 8;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            this.sensitivity = time;

            this.transform = transform;
        }
    }

}