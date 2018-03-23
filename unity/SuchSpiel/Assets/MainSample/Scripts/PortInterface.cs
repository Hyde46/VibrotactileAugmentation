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

public class PortInterface : MonoBehaviour
{

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
    public bool readNext;
    public string readData = "";
    private bool connectionClose = false;

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

        }
        catch
        {
            Debug.Log("Could not open port");
            return false;
        }
        sendData = "";
        Thread sendThread = new Thread(new ThreadStart(SendDataT));
        sendThread.Start();

        return true;
    }

    public void SendDataT()
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
                if (connectionClose)
                {
					serialPort.Write("(0,0,0,0,0)");
					Thread.Sleep(System.TimeSpan.FromSeconds(1.0));
                    serialPort.Close();
                    return;
                }
            }
        }
        catch
        {
            Debug.Log("Could not write to serial port.");
            serialPort.Close();

        }
    }
    public void Close()
    {
        try
        {
            //SendData(0, 0, 0, 0, 0);
            serialPort.Close();
            Debug.Log("close");
        }
        catch { }
    }

    public void HaltSending()
    {
        stopSending = true;
    }

    public void SendData(int v1, int v2, int v3, int v4, int v5)
    {
        sendData = "(" + v1 + "," + v2 + "," + v3 + "," + v4 + "," + v5 + ")";
    }
    public void SendData(string s)
    {
        sendData = s;
    }

    public string ReadFromArduino(int timeout = 100)
    {
        serialPort.ReadTimeout = timeout;
        try
        {
            return serialPort.ReadLine();
        }
        catch (System.TimeoutException e)
        {
            return e.ToString();
        }
    }
    void OnApplicationQuit()
    {
        connectionClose = true;
        //SendData(0, 0, 0, 0, 0);
        Debug.Log("OnApplicationQuit");
        serialPort.Close();
    }







}
