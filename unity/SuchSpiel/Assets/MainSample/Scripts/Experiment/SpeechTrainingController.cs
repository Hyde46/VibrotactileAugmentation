using UnityEngine;
using System;
using System.Collections;

public class SpeechTrainingController : MonoBehaviour {

  private static bool Verbose = false;

  private enum TrainingStates
  {
    STARTUP,
    INIT,
    FIXATION,
    SOA,
    RESPONSE,
    WAITING
  }

  private class TrainingStateObject
  {
    public SpeechTrainingController.TrainingStates TrialState;
    public long getSleepInterval()
    {

      switch (this.TrialState)
      {
        case TrainingStates.STARTUP:
          return 2000;
        case TrainingStates.INIT:
          return 0;
        case TrainingStates.FIXATION:
          return 1000;
        case TrainingStates.SOA:
          return 200;
        case TrainingStates.RESPONSE:
          return 40000;
        default:
          return 0;
      }
    }
  }
    
  public float FeedbackIntervalInSeconds = 1.0f;
  public bool VibroTactileStimulation = true;

  public float StimulationDuration = 2.0f;

  public GameObject Target;

  public UnityEngine.UI.Text FeedbackDisplay;
  
  private string[] positiveFeedbackTemplates = new string[]
  {
    "Sehr gut",
    "Super",
    "Gut gemacht"
  };

  //will contain the HandController script
  private GameObject HandControllerObject;
  //actual hand controller that handles the Leap data;
  private ExperimentHandController HandControllerReference;
  private TrainingStateObject CurrentTrialState;

  //Stimulation variables 
  public VibroTactileStimulationInterface vibroTactileStimulationInterface;
  private GameObject leftLight;
  private GameObject rightLight;
  //speech recognition variables
  public bool messageObtained;
  private long answerResponseTime;

  //Scene variables
  private GameObject FixationObject;
  
  public static long VerbalResponseIntervalLength = 3000L;

  public bool Started = false;
  public static bool IsActive = false;

  //z.B. auch wenn Vp nicht auf Reize reagieren o.ä. - Trial wiederholung 
  public string LastErrorCode;
  private long TrialStartTime;

  private Action TrialUpdateDelegate;

  //Position where the target will always appear (in foodpuncher this was CurrentTargetPosition)
  public Vector3 InitialTargetPosition;

  private int CurrentHandID;

  //muss noch implementiert werden, dass es auf unser Experiment passt
  private PoseController HandPoseController;
  private static long InitialPoseDuration = 500L;
  private long PoseCheckTimeStamp;

  // during the response interval the current state of the interaction is checked, the trial might end if:
  // - more than 10 seconds are elapsed (just a failsafe)
  // - the target is out of range
  // - the target has been placed into the container
  private bool CheckInteractionState;

  public SampleTrialController.InteractionStates InteractionState;

  public SampleTrialController master;

  // private string[] congruencyCondition = new string[] { "both left", "both right", "light left - tactile index", "light right - tactile thumb" };
  // edit 22.02.17: four new conditions for the stimulation on the hand
  //private string[] congruencyCondition = new string[] { "both left", "both right", "light left - tactile index", "light right - tactile thumb",
  //    "light index - tactile index", "light thumb - tactile thumb", "light index - tactile thumb", "light thumb - tactile index" };
  private string[] congruencyCondition = new string[] { "both left", "both right", "light left - tactile index", "light right - tactile thumb" };
  private string CurrentCongruencyCondition;
  private bool CorrectResponse;
  private long StimulationOnset;
  private long VerbalResponseTime;

  void Awake()
  {
    this.HandControllerObject = GameObject.FindWithTag("LeapController");
    this.HandControllerReference = this.HandControllerObject.GetComponent<ExperimentHandController>();
    if (!this.VibroTactileStimulation)
    {
      this.HandControllerReference.enableFingerParticleSystems = true;
    }
    GameObject posecontrollerContainer = GameObject.FindGameObjectWithTag("PoseController");
    this.HandPoseController = posecontrollerContainer.GetComponent<PoseController>();

    this.Target.SetActive(true);
    this.Target.transform.position = new Vector3(0, -100, 100);
    this.Target.GetComponent<Rigidbody>().isKinematic = true;

    this.leftLight = GameObject.FindGameObjectWithTag("LeftLightStimulus");
    this.rightLight = GameObject.FindGameObjectWithTag("RightLightStimulus");
    this.leftLight.GetComponent<ParticleSystem>().Stop();
    this.leftLight.GetComponent<ParticleSystem>().Clear();
    this.rightLight.GetComponent<ParticleSystem>().Stop();
    this.rightLight.GetComponent<ParticleSystem>().Clear();

    this.messageObtained = false;


    //utils
    this.FixationObject = GameObject.FindGameObjectWithTag("FixationObject");
    this.TrialUpdateDelegate = delegate () { this.TrialUpdate(); };

    //setup trial control
    this.CurrentTrialState = new TrainingStateObject();
    this.CurrentTrialState.TrialState = TrainingStates.STARTUP;
    this.LastErrorCode = "none";

    // instruction
    this.FeedbackDisplay.text = "";

    if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("done with awaking...");
  }

  private void checkInitialPose()
  {

    if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("call to check intial pose...");
    this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.INITIALIZE);
    bool valid = this.HandPoseController.checkInitialPose();
    if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("valid pose: " + valid + "...");

    // adapt timer and check hand presence
    if (valid)
    {
      if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("pose check time stamp: " + this.PoseCheckTimeStamp);
      if (this.PoseCheckTimeStamp == -1L)
      {
        this.PoseCheckTimeStamp = ((long)(Time.realtimeSinceStartup * 1000.0f));
      }
      else if (((long)(Time.realtimeSinceStartup * 1000.0f)) - this.PoseCheckTimeStamp >= SpeechTrainingController.InitialPoseDuration)
      {
        if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("check initial pose...");
        this.CurrentHandID = this.HandControllerReference.currentRightHandID;
        this.CurrentTrialState.TrialState = TrainingStates.INIT;
        this.TrialUpdate();
      }
    }
    // reset timer
    else
    {
      this.PoseCheckTimeStamp = -1L;
    }
  }

  // Update is called once per frame
  void Update()
  {
    if (!this.Started && SpeechTrainingController.IsActive)
    {
      this.Started = true;
      this.PoseCheckTimeStamp = -1L;
      this.CurrentTrialState.TrialState = TrainingStates.STARTUP;
      this.TrialUpdate();
    }
    // check pose
    if (this.Started && SpeechTrainingController.IsActive && this.CurrentTrialState.TrialState == TrainingStates.STARTUP)
    {
      this.checkInitialPose();
    } // check response state

    // fixation check
    if (this.Started && SpeechTrainingController.IsActive && this.CurrentTrialState.TrialState == TrainingStates.FIXATION)
    {
      if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("fixation check...");
      if (!this.HandPoseController.checkInitialPose())
      {
        this.InteractionState = SampleTrialController.InteractionStates.LEFT_POSITION_DURING_FIXATION;
        this.cancelAndResetTrial();
      }
      if (this.HandPoseController.checkInitialPose())
      {
        this.TrialUpdate();
      }
      else
      {
        if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("fixation not yet completed");
      }
    }

    // soa + response check
    if (this.Started && SpeechTrainingController.IsActive && (this.CurrentTrialState.TrialState == TrainingStates.SOA || this.CurrentTrialState.TrialState == TrainingStates.RESPONSE))
    {
      if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("soa check...");
      if (!this.HandPoseController.checkInitialPose())
      {
        this.InteractionState = SampleTrialController.InteractionStates.LEFT_POSITION_DURING_FIXATION;
        this.cancelAndResetTrial();
      }
    }

    //check hand trajectory
    if (this.Started && SpeechTrainingController.IsActive && this.CurrentTrialState.TrialState != TrainingStates.STARTUP && this.CurrentTrialState.TrialState != TrainingStates.FIXATION)
    {
      // cancel trial
      if (this.CurrentHandID != this.HandControllerReference.currentRightHandID && this.CurrentTrialState.TrialState == TrainingStates.RESPONSE)
      {
        this.cancelAndResetTrial();
      }
    }

    //checkInteractionState = true after fixation
    if (this.Started && SpeechTrainingController.IsActive && this.CheckInteractionState)
    {
      this.checkWordRecognition();
      this.checkVerbalResponseIntervalTime();

      if (this.InteractionState != SampleTrialController.InteractionStates.NONE)
      {
        this.TrialUpdate();
        // just paranoia
        this.CheckInteractionState = false;
      }
    }
  }

  public void cancelAndResetTrial()
  {
    // try to stop coroutine
    StopCoroutine("CoroutineTimer.Start");
    // if target has been assigned, reset it
    this.cancelTrial();

    this.CurrentCongruencyCondition = "";
    this.CorrectResponse = false;
    this.StimulationOnset = -1L;
    this.VerbalResponseTime = -1L;
    this.LastErrorCode = "none";

    // reset trial state...
    if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("trial canceled");
    this.CurrentTrialState.TrialState = TrainingStates.WAITING;
    StartCoroutine(this.FeedbackInterval());
  }

  private IEnumerator FeedbackInterval()
  {
    yield return new WaitForSeconds(this.FeedbackIntervalInSeconds);
    this.FeedbackDisplay.text = "";

    yield return null;
  }

  public void ExternalTrialStart()
  {
    this.CurrentTrialState.TrialState = TrainingStates.STARTUP;
    this.TrialUpdate();
  }

  public void TrialUpdate()
  {

    if (!SpeechTrainingController.IsActive)
    {
      this.Started = false;
      return;
    }

    if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("timer called, in state = " + CurrentTrialState.TrialState.ToString() + "...");
    switch (this.CurrentTrialState.TrialState)
    {
      case TrainingStates.STARTUP:
        if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("startup...");

        this.HandPoseController.resetInitialPoseChecks();
        this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.CHECK);
        this.FeedbackDisplay.text = "";
        if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("startup done...");
        break;
      case TrainingStates.INIT:
                
        this.CurrentCongruencyCondition = "";
        this.CorrectResponse = false;
        this.StimulationOnset = -1L;
        this.VerbalResponseTime = -1L;

        this.CurrentTrialState.TrialState = TrainingStates.FIXATION;
        this.FixationObject.transform.position = this.InitialTargetPosition;

        if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("dummy fixation interval");
        StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        break;
      case TrainingStates.FIXATION:
        this.CurrentTrialState.TrialState = TrainingStates.SOA;
        this.FixationObject.transform.position = new Vector3(0, -10, 0);
        StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));

        // stimulation
        int stimulationTime = UnityEngine.Random.Range(-150, 150);
        float timeout = (CurrentTrialState.getSleepInterval() + stimulationTime) * .001f;
        StartCoroutine(this.TimeBasedStimulation(timeout));
        break;
      case TrainingStates.SOA:
        this.messageObtained = false;
        if (this.Target.GetComponent<GraspableObject>().IsGrabbed())
        {
          GraspController[] hands = GameObject.FindObjectsOfType<GraspController>();

          foreach (GraspController hand in hands)
          {
            hand.requestRelease();
          }
        }

        this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.IDLE);
        GraspableObject graspable = this.Target.GetComponent<GraspableObject>();
        graspable.ResetVariables();

        bool rotate = UnityEngine.Random.value > .5f;

        graspable.ResetPositionAndOrientation(rotate ? 180.0f : 0.0f, this.InitialTargetPosition);
        if (rotate)
        {
          if (SpeechTrainingController.Verbose) Debug.Log("rotated target...");
        //  this.Target.transform.Rotate(new Vector3(180.0f, 0.0f, 0.0f));
        //  this.Target.transform.position = this.InitialTargetPosition;
        }
        else
        {
          if (SpeechTrainingController.Verbose) Debug.Log("upright target...");
         // this.Target.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
         // this.Target.transform.position = this.InitialTargetPosition;
        }

        this.Target.GetComponent<Rigidbody>().isKinematic = false;
        // edit JL
        this.Target.GetComponent<Rigidbody>().useGravity = true;

        this.CurrentTrialState.TrialState = TrainingStates.RESPONSE;
        // enable range check
        this.InteractionState = SampleTrialController.InteractionStates.NONE;
        this.CheckInteractionState = true;
        if (SpeechTrainingController.Verbose) Debug.Log("Current Trial State: " + CurrentTrialState.TrialState);
        break;
      case TrainingStates.RESPONSE:
        if (SpeechTrainingController.Verbose) Debug.Log("response state...");
        this.CheckInteractionState = false;
        //method that saves the interaction in the TrialData
        // no verbal reponse...
        if (this.VerbalResponseTime == -1L)
        {
          this.InteractionState = SampleTrialController.InteractionStates.VERBAL_TIME_OUT;
        }

        if (this.VerbalResponseTime != -1L && !this.CorrectResponse)
        {
          this.InteractionState = SampleTrialController.InteractionStates.VERBAL_WRONG_RESPONSE;
        }

        if (this.InteractionState != SampleTrialController.InteractionStates.IN_BOX)
        {
          this.cancelTrial();
        }
        else
        {
          this.FeedbackDisplay.text = this.positiveFeedbackTemplates[UnityEngine.Random.Range(0, this.positiveFeedbackTemplates.Length - 1)];

          this.deactivateTarget();
          this.InteractionState = SampleTrialController.InteractionStates.NONE;
          // clear monitor
          this.messageObtained = false;

          this.PoseCheckTimeStamp = -1L;
        }

        this.CurrentCongruencyCondition = "";
        this.CorrectResponse = false;
        this.StimulationOnset = -1L;
        this.VerbalResponseTime = -1L;
        this.LastErrorCode = "none";

        this.CurrentTrialState.TrialState = TrainingStates.WAITING;
        StartCoroutine(this.FeedbackInterval());

        break;
    }
  }

  private IEnumerator TimeBasedStimulation(float timeout)
  {
    // define congruency
    this.CurrentCongruencyCondition = this.congruencyCondition[UnityEngine.Random.Range(0, this.congruencyCondition.Length - 1)];

    float baseTime = Time.realtimeSinceStartup;
    float time = Time.realtimeSinceStartup;
    while (time - baseTime < timeout && this.LastErrorCode == "none")
    {
      time = Time.realtimeSinceStartup;
      yield return 0;
    }

    if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("timeout done, start stimulation");

    if (this.LastErrorCode == "none")
    {
       StartCoroutine(this.Stimulate());
    }

    yield return null;
  }

  private IEnumerator Stimulate()
  {
    this.StimulationOnset = SampleTrialController.Millis;

    if (this.CurrentCongruencyCondition == "both left")
    {
      if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("congruency condition: both left...");
      this.leftLight.GetComponent<ParticleSystem>().Play();
      if (!VibroTactileStimulation)
      {
        this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateThumb();
      }
      else
      {
        // thumb
        this.vibroTactileStimulationInterface.sendData(0, 0, SampleTrialController.TactileStimulationStrength, 0, 0);
      }
      StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentCongruencyCondition == "both right")
    {
      if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("congruency condition: both right");
      this.rightLight.GetComponent<ParticleSystem>().Play();
      if (!VibroTactileStimulation)
      {
        this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateIndexFinger();
      }
      else
      {
        // index
        this.vibroTactileStimulationInterface.sendData(0, SampleTrialController.TactileStimulationStrength, 0, 0, 0);
      }
      StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentCongruencyCondition == "light left - tactile index")
    {
      if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("congruency condition: light left - tactile index");
      this.leftLight.GetComponent<ParticleSystem>().Play();
      if (!VibroTactileStimulation)
      {
        this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateIndexFinger();
      }
      else if (VibroTactileStimulation)
      {
        // index
        this.vibroTactileStimulationInterface.sendData(0, SampleTrialController.TactileStimulationStrength, 0, 0, 0);
      }
      StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentCongruencyCondition == "light right - tactile thumb")
    {
      if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("congruency condition: light right - tactile thumb");
      this.rightLight.GetComponent<ParticleSystem>().Play();
      if (!VibroTactileStimulation)
      {
        this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateThumb();
      }
      else
      {
        // thumb
        this.vibroTactileStimulationInterface.sendData(0, 0, SampleTrialController.TactileStimulationStrength, 0, 0);
      }
      StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentCongruencyCondition == "light index - tactile index")
    {
        if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("congruency condition: light index - tactile index");
        this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateIndexFinger();
        if (VibroTactileStimulation)
        {
            // index
            this.vibroTactileStimulationInterface.sendData(0, SampleTrialController.TactileStimulationStrength, 0, 0, 0);
        }
        StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentCongruencyCondition == "light thumb - tactile thumb")
    {
        if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("congruency condition: light thumb - tactile thumb");
        this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateThumb();
        if (VibroTactileStimulation)
        {
            // thumb
            this.vibroTactileStimulationInterface.sendData(0, 0, SampleTrialController.TactileStimulationStrength, 0, 0);
        }
        StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentCongruencyCondition == "light index - tactile thumb")
    {
        if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("congruency condition: light index - tactile thumb");
        this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateIndexFinger();
        if (VibroTactileStimulation)
        {
            // thumb
            this.vibroTactileStimulationInterface.sendData(0, 0, SampleTrialController.TactileStimulationStrength, 0, 0);
        }
        StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentCongruencyCondition == "light thumb - tactile index")
    {
        if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("congruency condition: light thumb - tactile index");
        this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateThumb();
        if (VibroTactileStimulation)
        {
            // index
            this.vibroTactileStimulationInterface.sendData(0, SampleTrialController.TactileStimulationStrength, 0, 0, 0);
        }
        StartCoroutine(this.StopStimulation());
    }

    yield return null;
  }

  private IEnumerator StopStimulation()
  {
    float baseTime = Time.realtimeSinceStartup;
    float time = Time.realtimeSinceStartup;
    while (time - baseTime < this.StimulationDuration && this.LastErrorCode == "none")
    {
      time = Time.realtimeSinceStartup;
      yield return 0;
    }

    if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("timeout done, stop stimulation");

    this.leftLight.GetComponent<ParticleSystem>().Stop();
    this.leftLight.GetComponent<ParticleSystem>().Clear();
    this.rightLight.GetComponent<ParticleSystem>().Stop();
    this.rightLight.GetComponent<ParticleSystem>().Clear();

    if (!VibroTactileStimulation)
    {
      if (this.HandControllerReference.getCurrentHandModel() != null)
      {
        this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stopParticleSystems();
      }
    }
    else
    {
      this.vibroTactileStimulationInterface.sendData(0, 0, 0, 0, 0);
    }
   
    yield return null;
  }

  private void cancelTrial()
  {
    this.deactivateTarget();
    this.Target.transform.position = new Vector3(0, -100, 0);
    this.Target.transform.Rotate(new Vector3(0f, 0f, 0f));
    this.FixationObject.transform.position = new Vector3(0, -10, 0);


    switch (this.InteractionState)
    {
      case SampleTrialController.InteractionStates.NONE:
        this.FeedbackDisplay.text = "Die Hand muss im Sensorbereich bleiben.";
        this.LastErrorCode = "error";
        break;
      case SampleTrialController.InteractionStates.LEFT_POSITION_DURING_FIXATION:
        this.FeedbackDisplay.text = "Bitte halte deine Hand in der Ausgangsposition.";
        this.LastErrorCode = "early_start";
        break;
      case SampleTrialController.InteractionStates.OUT_OF_BOUNDS:
        this.FeedbackDisplay.text = "Objekt außer Reichweite.";
        this.LastErrorCode = "out_of_bound";
        break;
      case SampleTrialController.InteractionStates.TIME_OUT:
        this.FeedbackDisplay.text = "Bitte greife schneller nach der Flasche.";
        this.LastErrorCode = "time_out_grasp";
        break;
      case SampleTrialController.InteractionStates.VERBAL_TIME_OUT:
        this.FeedbackDisplay.text = "Bitte antworte schneller.";
        this.LastErrorCode = "time_out_response";
        break;
      case SampleTrialController.InteractionStates.VERBAL_WRONG_RESPONSE:
        this.FeedbackDisplay.text = "Es war leider der andere Finger.";
        this.LastErrorCode = "wrong_verbal_response";
        break;
    }
    
    this.messageObtained = false;
        
    this.vibroTactileStimulationInterface.sendData(0, 0, 0, 0, 0);
  }

  private void deactivateTarget()
  {
    this.Target.GetComponent<Rigidbody>().useGravity = false;
    this.Target.GetComponent<Rigidbody>().isKinematic = true;
    this.Target.transform.position = new Vector3(0, -100, 100);
    this.Target.transform.rotation = Quaternion.identity;
    Joint joint = this.Target.GetComponent<GraspableObject>().BreakableJoint.GetComponent<Joint>();
    if (joint != null)
    {
        joint.connectedBody = null;
    }
    // just paranoia
    this.leftLight.GetComponent<ParticleSystem>().Stop();
    this.leftLight.GetComponent<ParticleSystem>().Clear();
    this.rightLight.GetComponent<ParticleSystem>().Stop();
    this.rightLight.GetComponent<ParticleSystem>().Clear();
  }

  private void checkVerbalResponseIntervalTime()
  {
    if (this.StimulationOnset == -1L)
    {
      return;
    }

    long deltaTime = SampleTrialController.Millis - (this.StimulationOnset);
    // allow grasping before speech...
    if ((deltaTime >= SpeechTrainingController.VerbalResponseIntervalLength && this.messageObtained == false))
    {
      if (SpeechTrainingController.Verbose) UnityEngine.Debug.Log("verbal trial time out: " + deltaTime + " (" + (this.StimulationOnset) + ") " + "...");
      this.InteractionState = SampleTrialController.InteractionStates.VERBAL_TIME_OUT;
    }
  }

  private void checkWordRecognition()
  {
  }
}
