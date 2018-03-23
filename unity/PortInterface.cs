using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Threading;



public delegate void dataReceived(object sender, SerialPortEventArgs arg);
public class SerialPortEventArgs
{
    public string ReceivedData { get; private set; }
    public SerialPortEventArgs(string data)
    {
        ReceivedData = data;
    }
}

public class PortInterface : MonoBehaviour {

    private SerialPort serialPort = new SerialPort();

    private int baudRate = 9600;

    private int dataBits = 8;

    private Handshake handshake = Handshake.None;

    private Parity parity = Parity.None;

    private StopBits stopBits = StopBits.One;

    private string tString = string.Empty;

    private byte terminator = 0x4;

    public int BaudRate { get { return this.baudRate; } set { this.baudRate = value; } }

    public int DataBits { get { return this.dataBits; } set { this.dataBits = value; } }

    public Handshake Handshake { get { return this.handshake; } set { this.handshake = value; } }

    public Parity Parity { get { return this.parity; } set { this.parity = value; } }

    public string PortName { get { return this.portName; } set { this.portName = value; } }

    public StopBits StopBits { get { return this.stopBits; } set { this.stopBits = value; } }

    public string portName = "COM3";

    public bool stopSending;

    private string sendData;

    public bool OpenPort()
    {
        try
        {
            this.serialPort.BaudRate = this.baudRate;
            this.serialPort.DataBits = this.dataBits;
            this.serialPort.Handshake = this.Handshake;
            this.serialPort.Parity = this.parity;
            this.serialPort.PortName = this.portName;
            this.serialPort.StopBits = this.stopBits;
        }
        catch
        {
            return false;
        }
        try { serialPort.DtrEnable = true; }
        catch { }
        try { serialPort.RtsEnable = true; }
        catch { }

        try
        {
            serialPort.Open();
        }catch
        {
            Debug.Log("Could not open port");
            return false;
        }
        Thread sendThread = new Thread(new ThreadStart(SendDataT));
        sendThread.Start();

        return true;
    }

    public  void SendDataT()
    {
        try
        {
            while (!stopSending)
            {
                if (sendData != "")
                {
                    serialPort.Write(sendData);
                    sendData = "";
                }
            }
        }
        catch {
            Debug.Log("Could not write to serial port.");
            Close();
        }
    }
    public void Close()
    {
        try
        {
            serialPort.Close();
        }
        catch { }
    }

    public void HaltSending()
    {
        stopSending = true;
    }

    public void SendData(int v1, int v2, int v3, int v4, int v5)
    {
        sendData = "("+v1+","+v2+","+v3+","+v4+","+v5+")";
    }
    //muss noch getest Werden
    public string FindPort()
    {
        string request = "Side?";
        string[] portList = SerialPort.GetPortNames();
        foreach (string port in portList)
        {
            Debug.Log("Trying open port> " + port);
            if (port != "COM1")
            {
                try
                {
                    SerialPort currentPort = new SerialPort(port, baudRate);
                    if (!currentPort.IsOpen)
                    {
                        currentPort.Open();
                        Debug.Log("Opened port> " + port);
                        currentPort.Write(request);
                        string received = currentPort.ReadLine();
                        Debug.Log("Opened port> " + port + " and received= " + received);
                        currentPort.Close();
                        if (received.Equals("Left"))
                        {
                            return port;
                        }
                    }
                }
                catch { }
            }

        }

        return null;
    }

    void Start () {
        stopSending = false;
        sendData = "";
        OpenPort();
	}

	void Update () {
        if (Input.GetKeyDown(KeyCode.S))
        {
            HaltSending();
            Close();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            SendData(1, 2, 3, 4, 5);
        }
    }

}
