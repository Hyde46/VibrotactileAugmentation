using UnityEngine;
using System;
using System.Collections;
using System.IO.Ports;

public class VibroTactileStimulationInterface : MonoBehaviour
{
  private static bool Verbose = false;
  public static SerialPort ArduinoPort = new SerialPort("COM3", 9600);
  // may correspond to the fingers, depends on the hardware setup
  private int ChannelOne = -1;
  private int ChannelTwo = -1;
  private int ChannelThree = -1;
  private int ChannelFour = -1;
  private int ChannelFive = -1;
  // if you want to reuse the port between scenes, you should keep it open
  public bool PreservePort = false;
  // if true, the serial port connection will not be used
  public bool DummyMode = false;
  
  void Start()
  {
    if (!this.DummyMode) OpenConnection();
  }

  public void OpenConnection()
  {
    if (VibroTactileStimulationInterface.ArduinoPort != null)
    {
      if (VibroTactileStimulationInterface.ArduinoPort.IsOpen)
      {
        if (VibroTactileStimulationInterface.Verbose) Debug.Log("port is already open...");
      }
      else
      {
        if (VibroTactileStimulationInterface.Verbose) Debug.Log("try to fetch com port...");
        string comPort = null;

        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
          if (args[i].Equals("-ComPort") && i + 1 < args.Length)
          {
            comPort = args[i + 1];
          }
        }

        if (comPort != null)
        {
          if (VibroTactileStimulationInterface.Verbose) Debug.Log("using port: " + comPort + " from cmd-args...");
          VibroTactileStimulationInterface.ArduinoPort.PortName = comPort;
        }
        else
        {
          if (VibroTactileStimulationInterface.Verbose) Debug.Log("no com port assigned, using default...");
        }

        VibroTactileStimulationInterface.ArduinoPort.Open();  // opens the connection
        VibroTactileStimulationInterface.ArduinoPort.ReadTimeout = 16;  // sets the timeout value before reporting error
        if (VibroTactileStimulationInterface.Verbose) Debug.Log("Port Opened...");
      }
    }
    else
    {
      if (VibroTactileStimulationInterface.ArduinoPort.IsOpen)
      {
        if (VibroTactileStimulationInterface.Verbose) Debug.Log("Port is already open...");
      }
      else
      {
        if (VibroTactileStimulationInterface.Verbose) Debug.Log("Port is null...");
      }
    }
  }

  void OnApplicationQuit()
  {
    if (this.DummyMode) return;

    if (!this.PreservePort)
    {
      string output = "0,0,0,0,0\n";
      VibroTactileStimulationInterface.ArduinoPort.Write(output);
      VibroTactileStimulationInterface.ArduinoPort.Close();
    }
  }

  public void sendData(int stimulationValueOne, int stimulationValueTwo, int stimulationValueThree, int stimulationValueFour, int stimulationValueFive)
  {
    if (this.DummyMode) return;

    int nChannelOne   = stimulationValueOne;
    int nChannelTwo   = stimulationValueTwo;
    int nChannelThree = stimulationValueThree;
    int nChannelFour  = stimulationValueFour;
    int nChannelFive  = stimulationValueFive;

    if (this.ChannelOne != nChannelOne || this.ChannelTwo != nChannelTwo || this.ChannelThree != nChannelThree || this.ChannelFour != nChannelFour || this.ChannelFive != nChannelFive)
    {
      this.ChannelOne   = nChannelOne;
      this.ChannelTwo   = nChannelTwo;
      this.ChannelThree = nChannelThree;
      this.ChannelFour  = nChannelFour;
      this.ChannelFive  = nChannelFive;
      string output     = this.ChannelOne.ToString() + "," + this.ChannelTwo.ToString() + "," + this.ChannelThree.ToString() + "," + this.ChannelFour.ToString() + "," + this.ChannelFive.ToString() + "\n";
      if (VibroTactileStimulationInterface.Verbose) Debug.Log("change tactile stimulation pattern to: " + output + "...");
      VibroTactileStimulationInterface.ArduinoPort.Write(output);
    }

  }

}