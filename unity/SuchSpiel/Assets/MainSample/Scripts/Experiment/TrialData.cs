using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TrialData {

  private static bool UseTimeStampInNumericalDifferentiation = true;

  // timing variables
  public long TrialStartTime;
  public long FixationOnset;
  public long FixationOffset;
  public long StimulationOnset;
  public long StimulusResponseIntervalTime;
  public long TargetOnset;
  public long MovementOnset;
  public long ObjectContact;
  public long ReleaseTime;

  public long VerbalResponseTime;

  public Vector3 Targetlocation;
  public List<Vector4> PalmTrajectory;
  public List<Vector4> IndexTrajectory;
  public List<Vector4> ThumbTrajectory;
  public Vector3 InitialTargetLocation;

  public string BlockType;
  public float VisualOffset;

  //starting when object is presented
  //we can have trial types: upright position of the bottle and turned position, time of stimulation and left right congruent or incongruent
  public string TargetOrientation;
  public string StimulationCondition;
  public string Congruency;
  
  public string ErrorCode;
  public bool CorrectResponse;
  public GraspableObject.GraspDirection graspDirection;

  public string info;
  // serialization of data can cause lags, hence we put the writing process in a separate thread, this boolean is
  // used to check whether writing is done
  public volatile bool isWriting;

  //Default constructor 
  public TrialData(string TargetOrientation, string StimulationCondition, string Congruency, string BlockType)
  {
    this.PalmTrajectory = new List<Vector4>();
    this.IndexTrajectory = new List<Vector4>();
    this.ThumbTrajectory = new List<Vector4>();
    this.InitialTargetLocation = new Vector3();

    this.BlockType = BlockType;
    this.VisualOffset = 0.0f;

    this.TrialStartTime = -1L;
    this.FixationOnset = -1L;
    this.FixationOffset = -1L;
    this.StimulationOnset = -1L;
    this.StimulusResponseIntervalTime = -1L;
    this.TargetOnset = -1L;
    this.MovementOnset = -1L;
    this.ObjectContact = -1L;
    this.ReleaseTime = -1L;
    this.VerbalResponseTime = -1L;
    
    this.TargetOrientation    = TargetOrientation;
    this.StimulationCondition = StimulationCondition;
    this.Congruency           = Congruency;
 
    this.ErrorCode       = "none";
    this.CorrectResponse = false;
    this.graspDirection  = GraspableObject.GraspDirection.NONE;
    this.info = "";
    if (this.Congruency == "both left")
    {
        info = "tact_thumb:locallight_left:globallight_" + (this.TargetOrientation == "upright" ? "left" : "right");
    }
    else if (this.Congruency == "both right")
    {
        info = "tact_index:locallight_right:globallight_" + (this.TargetOrientation == "upright" ? "right" : "left");
    }
    else if (this.Congruency == "light left - tactile index")
    {
        info = "tact_index:locallight_left:globallight_" + (this.TargetOrientation == "upright" ? "left" : "right");
    }
    else if (this.Congruency == "light right - tactile thumb")
    {
        info = "tact_thumb:locallight_right:globallight_" + (this.TargetOrientation == "upright" ? "right" : "left");
    }
    else if (this.Congruency == "light index - tactile index")
    {
        info = "tact_index:locallight_index:globallight_none";
    }
    else if (this.Congruency == "light thumb - tactile thumb")
    {
        info = "tact_thumb:locallight_thumb:globallight_none";
    }
    else if (this.Congruency == "light index - tactile thumb")
    {
        info = "tact_thumb:locallight_index:globallight_none";
    }
    else if (this.Congruency == "light thumb - tactile index")
    {
        info = "tact_index:locallight_thumb:globallight_none";
    }
    else
    {
        info = "unknown";
    }
  }

  public void resetDependentMeasures()
  {
    this.PalmTrajectory = new List<Vector4>();
    this.IndexTrajectory = new List<Vector4>();
    this.ThumbTrajectory = new List<Vector4>();

    this.TrialStartTime = -1L;
    this.FixationOnset = -1L;
    this.FixationOffset = -1L;
    this.StimulationOnset = -1L;
    this.StimulusResponseIntervalTime = -1L;
    this.TargetOnset = -1L;
    this.MovementOnset = -1L;
    this.ObjectContact = -1L;
    this.ReleaseTime = -1L;
    this.VerbalResponseTime = -1L;

    this.ErrorCode = "none";
    this.CorrectResponse = false;
    this.graspDirection = GraspableObject.GraspDirection.NONE;
  }


  //to obtain velocity and acceleration profiles, compuation of a five point stencil over positional data 
  //numerical analysis to approximate derivatives
  public List<Vector4> calculateFivePointStencil(List<Vector4> inputData)
  {
    List<Vector4> derivation = new List<Vector4>();

    if (TrialData.UseTimeStampInNumericalDifferentiation)
    {
      // simple, time-weighted delta
      for (int i = 1; i < inputData.Count; i++)
      {
        Vector4 prevPoint = inputData[i - 1];
        Vector4 currentPoint = inputData[i];
        float deltaT = currentPoint.w - prevPoint.w;
        float x = (currentPoint.x - prevPoint.x) / deltaT;
        float y = (currentPoint.y - prevPoint.y) / deltaT;
        float z = (currentPoint.z - prevPoint.z) / deltaT;
        derivation.Add(new Vector4(x, y, z, currentPoint.w));
      }
    }
    else
    {
      // five point stencil
      for (int i = 2; i < inputData.Count - 2; i++)
      {
        // helper variables
        Vector4 xMinusTwo = inputData[i - 2];
        Vector4 xMinusOne = inputData[i - 1];
        Vector4 xPlusOne = inputData[i + 1];
        Vector4 xPlusTwo = inputData[i + 2];
        // the actual stencil
        float x = (-xPlusTwo.x + 8.0f * xPlusOne.x - 8.0f * xMinusOne.x + xMinusTwo.x) / 12.0f;
        float y = (-xPlusTwo.y + 8.0f * xPlusOne.y - 8.0f * xMinusOne.y + xMinusTwo.y) / 12.0f;
        float z = (-xPlusTwo.z + 8.0f * xPlusOne.z - 8.0f * xMinusOne.z + xMinusTwo.z) / 12.0f;

        derivation.Add(new Vector4(x, y, z, inputData[i].w));
      }
    }

    return derivation;
  }


  // utility for formatting a single vector, used by getOutputLine
  public string formatSingleVector(Vector3 vector)
  {
    string vectorString = vector.x.ToString() + "," + vector.y.ToString() + "," + vector.z.ToString();
    return vectorString;
  }
  public string formatSingleVector(Vector4 vector)
  {
    string vectorString = vector.x.ToString() + "," + vector.y.ToString() + "," + vector.z.ToString() + "," + vector.w.ToString();
    return vectorString;
  }
  // utility for formatting a list of vectors, used by getOutputLine
  public string formatVectorList(List<Vector4> vectorList)
  {
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    for (int i = 0; i < vectorList.Count; i++)
    {
      sb.Append(this.formatSingleVector(vectorList[i]));
      if (i < vectorList.Count - 1)
      {
        sb.Append(";");
      }
    }
    return sb.ToString();
  }

  // generates a single output line for the log file
  public string getOutputLine()
  {
    string format = "{0:0000.00}";
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.Append(String.Format(format, this.TrialStartTime) + "\t");
    sb.Append(this.BlockType + "\t");
    sb.Append(String.Format(format, this.VisualOffset) + "\t");
    sb.Append(this.info + "\t");
    sb.Append(this.TargetOrientation + "\t");
    sb.Append(this.StimulationCondition + "\t");
    sb.Append(this.Congruency + "\t");
    sb.Append(String.Format(format, this.FixationOnset) + "\t");
    sb.Append(String.Format(format, this.FixationOffset) + "\t");
    sb.Append(String.Format(format, this.StimulationOnset) + "\t");
    sb.Append(String.Format(format, this.StimulusResponseIntervalTime) + "\t");
    sb.Append(String.Format(format, this.TargetOnset) + "\t");
    sb.Append(String.Format(format, this.MovementOnset) + "\t");
    sb.Append(String.Format(format, this.ObjectContact) + "\t");
    sb.Append(String.Format(format, this.ReleaseTime) + "\t");
    sb.Append(String.Format(format, this.VerbalResponseTime) + "\t");
    sb.Append(this.CorrectResponse.ToString() + "\t");
    sb.Append(this.ErrorCode + "\t");
    sb.Append(this.graspDirection.ToString() + "\t");
    sb.Append(this.formatSingleVector(this.Targetlocation) + "\t");
    // here go the profiles: position vector, velocity and acceleration patterns
    sb.Append(this.formatVectorList(this.PalmTrajectory) + "\t");
    List<Vector4> velocityVector = this.calculateFivePointStencil(this.PalmTrajectory);
    sb.Append(this.formatVectorList(velocityVector) + "\t");
    List<Vector4> accelerationVector = this.calculateFivePointStencil(velocityVector);
    sb.Append(this.formatVectorList(accelerationVector) + "\t");

    sb.Append(this.formatVectorList(this.IndexTrajectory) + "\t");
    velocityVector = this.calculateFivePointStencil(this.IndexTrajectory);
    sb.Append(this.formatVectorList(velocityVector) + "\t");
    accelerationVector = this.calculateFivePointStencil(velocityVector);
    sb.Append(this.formatVectorList(accelerationVector) + "\t");

    sb.Append(this.formatVectorList(this.ThumbTrajectory) + "\t");
    velocityVector = this.calculateFivePointStencil(this.ThumbTrajectory);
    sb.Append(this.formatVectorList(velocityVector) + "\t");
    accelerationVector = this.calculateFivePointStencil(velocityVector);
    sb.Append(this.formatVectorList(accelerationVector));

    return sb.ToString();
  }

  public static string getLogFileHeader()
  {
    string header =
    "TrialStartTime" + "\t" +
    "BlockType" + "\t" +
    "VisualOffset" + "\t" +
    "TrialInfo" + "\t" +

    "TargetOrientation" + "\t" +
    "StimulationCondition" + "\t" +
    "Congruency" + "\t" +

    "FixationOnset" + "\t" +
    "FixationOffset" + "\t" +
    "StimulationOnset" + "\t" +
    "StimulusResponseIntervalTime" + "\t" +
    "TargetOnset" + "\t" +
    "MovementOnset" + "\t" +
    "ObjectContact" + "\t" +
    "ReleaseTime" + "\t" +
    "VerbalResponseTime" + "\t" +
    "CorrectResponse" + "\t" +
    "ErrorCode" + "\t" +
    "GraspDirection" + "\t" +
    "Targetlocation" + "\t" +

    "PalmTrajectory" + "\t" +
    "PalmVelocityProfile" + "\t" +
    "PalmAccelerationProfile" + "\t" +

    "IndexTrajectory" + "\t" +
    "IndexVelocityProfile" + "\t" +
    "IndexAccelerationProfile" + "\t" +

    "ThumbTrajectory" + "\t" +
    "ThumbVelocityProfile" + "\t" +
    "ThumbAccelerationProfile"
    ;

    return header;
  }

  public void printDataAsync()
  {
      this.isWriting = true;
      FileWriter.writeData(this.getOutputLine());
      this.isWriting = false;
  }
}
