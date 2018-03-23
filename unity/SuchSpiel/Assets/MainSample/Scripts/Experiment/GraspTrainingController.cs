using UnityEngine;
using System;
using System.Collections;

public class GraspTrainingController : MonoBehaviour {

  private static bool Verbose = true;

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
    public GraspTrainingController.TrainingStates TrialState;
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

  public UnityEngine.UI.Text FeedbackDisplay;
  public GameObject Target;

  public GameObject TrainingUI;
  public VisualHandOffsetController OffsetController;

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

  private bool variableMapping = false;
  private string bottleOrientationMode = "upright";

  private EffectorRangeCheck RangeCheck;

  //Scene variables
  private GameObject FixationObject;

  private long TargetOnsetTime;

  public bool Started = false;
  public static bool IsActive = false;

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

  public SampleTrialController master;

  public SampleTrialController.InteractionStates InteractionState;

  private Action TrialUpdateDelegate;

  void Awake()
  { 
    this.HandControllerObject = GameObject.FindWithTag("LeapController");
    this.HandControllerReference = this.HandControllerObject.GetComponent<ExperimentHandController>();
    
    GameObject rangeCheckContainer = GameObject.FindGameObjectWithTag("RangeCheck");
    this.RangeCheck = rangeCheckContainer.GetComponent<EffectorRangeCheck>();
    this.RangeCheck.ObjectLeftBounds += this.rangeCheckHandler;
    GameObject posecontrollerContainer = GameObject.FindGameObjectWithTag("PoseController");
    this.HandPoseController = posecontrollerContainer.GetComponent<PoseController>();

    // register container controller
    GameObject container = GameObject.FindGameObjectWithTag("Container");
    if (container != null)
    {
      ContainerController containerController = container.GetComponent<ContainerController>();
      if (containerController != null)
      {
        //containerController.ObjectWasReleased += new ContainerController.ObjectWasReleasedHandler(this.checkTargetPosition);
        containerController.ObjectWasReleased  += this.checkTargetPosition;
        containerController.ObjectWasDestroyed += this.checkTargetDestruction;
      }
    }

    this.Target.SetActive(true);
    this.Target.transform.position = new Vector3(0, -100, 100);
    this.Target.GetComponent<Rigidbody>().isKinematic = true;

    this.FixationObject = GameObject.FindGameObjectWithTag("FixationObject");
    this.TrialUpdateDelegate = delegate () { this.TrialUpdate(); };

    //setup trial control
    this.CurrentTrialState = new TrainingStateObject();
    this.CurrentTrialState.TrialState = TrainingStates.STARTUP;

    // register listeners
    SliderFocusHandler.SliderEvent += this.handleSlider;
    CheckMarkFocusHandler.ToggleEvent += this.handleToggle;

    if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("done with awaking...");

    this.OffsetController.ApplyDrift = false;
    this.TrainingUI.SetActive(false);
  }

  void OnDestroy()
  {
    // unregister container controller
    GameObject container = GameObject.FindGameObjectWithTag("Container");
    if (container != null)
    {
      ContainerController containerController = container.GetComponent<ContainerController>();
      if (containerController != null)
      {
        containerController.ObjectWasReleased  -= this.checkTargetPosition;
        containerController.ObjectWasDestroyed -= this.checkTargetDestruction;
      }
    }

    this.RangeCheck.ObjectLeftBounds -= this.rangeCheckHandler;

    SliderFocusHandler.SliderEvent -= this.handleSlider;
    CheckMarkFocusHandler.ToggleEvent -= this.handleToggle;

    this.OffsetController.DriftFactor = 0.0f;
    this.OffsetController.ApplyDrift = false;
    this.TrainingUI.SetActive(false);
  }

  private void handleSlider(string command)
  {
      this.master.HandleSlider();
  }

  private void handleToggle(string command)
  {
      switch (command) 
      {
          case "fixation_on":
              break;
          case "fixation_off":
              break;
          case "mapping_consistent":
              this.variableMapping = false;
              break;
          case "mapping_variable":
              this.variableMapping = true;
              break;
          case "orientation_upright":
              this.bottleOrientationMode = "upright";
              break;
          case "orientation_upsidedown":
              this.bottleOrientationMode = "upsidedown";
              break;
          case "orientation_random":
              this.bottleOrientationMode = "random";
              break;
          default:
              UnityEngine.Debug.Log("unknown comannd: " + command + "...");
              break;
      }
  }

  private void checkInitialPose()
  {

    if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("call to check intial pose...");
    this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.INITIALIZE);
    bool valid = this.HandPoseController.checkInitialPose();
    if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("valid pose: " + valid + "...");

    // adapt timer and check hand presence
    if (valid)
    {
      if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("pose check time stamp: " + this.PoseCheckTimeStamp);
      if (this.PoseCheckTimeStamp == -1L)
      {
        this.PoseCheckTimeStamp = ((long)(Time.realtimeSinceStartup * 1000.0f));
      }
      else if (((long)(Time.realtimeSinceStartup * 1000.0f)) - this.PoseCheckTimeStamp >= GraspTrainingController.InitialPoseDuration)
      {
        if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("check initial pose...");
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
    if (!this.Started && GraspTrainingController.IsActive)
    {
      this.Started = true;
      this.TrainingUI.SetActive(true);
      this.PoseCheckTimeStamp = -1L;
      this.CurrentTrialState.TrialState = TrainingStates.STARTUP;
      this.TrialUpdate();
    }
    // check pose
    if (this.Started && GraspTrainingController.IsActive && this.CurrentTrialState.TrialState == TrainingStates.STARTUP)
    {
      this.checkInitialPose();
    } // check response state

    // fixation check
    if (this.Started && GraspTrainingController.IsActive && this.CurrentTrialState.TrialState == TrainingStates.FIXATION)
    {
      if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("fixation check...");
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
        if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("fixation not yet completed");
      }
    }

    // soa check
    if (this.Started && GraspTrainingController.IsActive && this.CurrentTrialState.TrialState == TrainingStates.SOA)
    {
      if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("soa check...");
      if (!this.HandPoseController.checkInitialPose())
      {
        this.InteractionState = SampleTrialController.InteractionStates.LEFT_POSITION_DURING_FIXATION;
        this.cancelAndResetTrial();
      }
    }

    //check hand trajectory
    if (this.Started && GraspTrainingController.IsActive && this.CurrentTrialState.TrialState != TrainingStates.STARTUP && this.CurrentTrialState.TrialState != TrainingStates.FIXATION)
    {
      // cancel trial
      if (this.CurrentHandID != this.HandControllerReference.currentRightHandID && this.CurrentTrialState.TrialState == TrainingStates.RESPONSE)
      {
        this.cancelAndResetTrial();
      }
    }

    //checkInteractionState = true after fixation
    if (this.Started && GraspTrainingController.IsActive && this.CheckInteractionState)
    {

      //checks if no time out for vocal response and object interaction and if vocal response has been obtained
      this.checkResponseIntervalTime();

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
    // reset trial state...
    if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("trial canceled");
    //this.CurrentTrialState.TrialState = TrainingStates.STARTUP;
    //this.TrialUpdate();
    //StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
    this.CurrentTrialState.TrialState = TrainingStates.WAITING;
    StartCoroutine(this.FeedbackInterval());
  }

  private IEnumerator FeedbackInterval()
  {
    yield return new WaitForSeconds(this.FeedbackIntervalInSeconds);
    this.FeedbackDisplay.text = "";
    this.master.GraspTrainingTrialDone();

    yield return null;
  }

  public void ExternalTrialStart()
  {
    this.CurrentTrialState.TrialState = TrainingStates.STARTUP;
    this.TrialUpdate();
  }

  public void TrialUpdate()
  {

    if (!GraspTrainingController.IsActive)
    {
      this.Started = false;
      return;
    }

    if (this.CurrentTrialState.TrialState == TrainingStates.RESPONSE && SampleTrialController.Millis - this.TargetOnsetTime < 1000L)
    {
        return;
    }

    if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("timer called, in state = " + CurrentTrialState.TrialState.ToString() + "...");
    switch (this.CurrentTrialState.TrialState)
    {
      case TrainingStates.STARTUP:
        if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("startup...");

        this.HandPoseController.resetInitialPoseChecks();
        this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.CHECK);
        this.FeedbackDisplay.text = "";
        
        if (this.variableMapping)
        {
            this.OffsetController.DriftFactor = this.master.visualOffsets[UnityEngine.Random.Range(0, this.master.visualOffsets.Length)];
        }
        else
        {
            this.OffsetController.DriftFactor = 0.0f;
        }

        if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("startup done...");
        break;
      case TrainingStates.INIT:
        this.CurrentTrialState.TrialState = TrainingStates.FIXATION;
        this.FixationObject.transform.position = this.InitialTargetPosition;
        this.master.GraspTrainingTrialStart();

        if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("dummy fixation interval");
        StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        break;
      case TrainingStates.FIXATION:
        this.CurrentTrialState.TrialState = TrainingStates.SOA;
        this.FixationObject.transform.position = new Vector3(0, -10, 0);
        if (this.variableMapping)
        {
            this.OffsetController.ApplyDrift = true;
        }
        else
        {
            this.OffsetController.ApplyDrift = false;
        }
        StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        break;
      case TrainingStates.SOA:
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

        if (this.bottleOrientationMode == "upright")
        {
            graspable.ResetPositionAndOrientation(0.0f, this.InitialTargetPosition);
            if (GraspTrainingController.Verbose) Debug.Log("upright target...");
          //  this.Target.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
          //  this.Target.transform.position = this.InitialTargetPosition;
        }
        else if (this.bottleOrientationMode == "upsidedown")
        {
            graspable.ResetPositionAndOrientation(180.0f, this.InitialTargetPosition);
            if (GraspTrainingController.Verbose) Debug.Log("rotated target...");
           // this.Target.transform.Rotate(new Vector3(180.0f, 0.0f, 0.0f));
          //  this.Target.transform.position = this.InitialTargetPosition;
        }
        else if (this.bottleOrientationMode == "random")
        {
            if (UnityEngine.Random.value > .5f)
            {
                graspable.ResetPositionAndOrientation(180.0f, this.InitialTargetPosition);
                if (GraspTrainingController.Verbose) Debug.Log("rotated target...");
              //  this.Target.transform.Rotate(new Vector3(180.0f, 0.0f, 0.0f));
              //  this.Target.transform.position = this.InitialTargetPosition;
            }
            else
            {
                graspable.ResetPositionAndOrientation(0.0f, this.InitialTargetPosition);
                if (GraspTrainingController.Verbose) Debug.Log("upright target...");
             //   this.Target.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
              //  this.Target.transform.position = this.InitialTargetPosition;
            }
        }
        else
        {
            UnityEngine.Debug.Log("unknown orientation mode: " + this.bottleOrientationMode + "...");
        }
        
        this.Target.GetComponent<Rigidbody>().isKinematic = false;
        // edit JL
        this.Target.GetComponent<Rigidbody>().useGravity = true;

        this.CurrentTrialState.TrialState = TrainingStates.RESPONSE;
        // enable range check
        this.InteractionState = SampleTrialController.InteractionStates.NONE;
        this.CheckInteractionState = true;
        this.RangeCheck.clearMonitor();
        this.RangeCheck.monitorObject(this.Target);
        this.TargetOnsetTime = SampleTrialController.Millis;
        if (Verbose) Debug.Log("Current Trial State: " + CurrentTrialState.TrialState);
        break;

      case TrainingStates.RESPONSE:
        if (GraspTrainingController.Verbose) Debug.Log("response state...");
        this.CheckInteractionState = false;
        this.OffsetController.ApplyDrift = false;

        if (this.InteractionState != SampleTrialController.InteractionStates.IN_BOX)
        {
          this.cancelTrial();
          // reset trial state...
          //this.CurrentTrialState.TrialState = TrainingStates.STARTUP;
          //StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        }
        else
        {
          this.FeedbackDisplay.text = this.positiveFeedbackTemplates[UnityEngine.Random.Range(0, this.positiveFeedbackTemplates.Length - 1)];

          this.deactivateTarget();
          this.InteractionState = SampleTrialController.InteractionStates.NONE;
          // clear monitor
          this.RangeCheck.clearMonitor();

          this.PoseCheckTimeStamp = -1L;

          //this.CurrentTrialState.TrialState = TrainingStates.STARTUP;
          //StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        }
        this.CurrentTrialState.TrialState = TrainingStates.WAITING;
        StartCoroutine(this.FeedbackInterval());

        break;
    }
  }

  private void cancelTrial()
  {
    this.RangeCheck.clearMonitor();
    this.deactivateTarget();
    this.Target.transform.position = new Vector3(0, -100, 100);
    this.Target.transform.rotation = Quaternion.identity; 
    this.FixationObject.transform.position = new Vector3(0, -10, 0);


    switch (this.InteractionState)
    {
      case SampleTrialController.InteractionStates.NONE:
        this.FeedbackDisplay.text = "Die Hand muss im Sensorbereich bleiben.";
        break;
      case SampleTrialController.InteractionStates.LEFT_POSITION_DURING_FIXATION:
        this.FeedbackDisplay.text = "Bitte bewege deine Hand erst, wenn das Objekt erscheint.";
        break;
      case SampleTrialController.InteractionStates.TARGET_DESTROYED:
        this.FeedbackDisplay.text = "Bitte mach die Flasche nicht kaputt.";
        break;
      case SampleTrialController.InteractionStates.WRONG_ORIENTATION:
        this.FeedbackDisplay.text = "Bitte stell die Flasche richtig herum ab.";
        break;
      case SampleTrialController.InteractionStates.OUT_OF_BOUNDS:
        this.FeedbackDisplay.text = "Objekt außer Reichweite.";
        break;
      case SampleTrialController.InteractionStates.TIME_OUT:
        this.FeedbackDisplay.text = "Bitte greife schneller nach der Flasche.";
        break;
    }
  }

  private void rangeCheckHandler(GameObject checkedObject)
  {
    if (GraspTrainingController.Verbose) UnityEngine.Debug.Log(checkedObject.name + " left bounds...");
    this.InteractionState = SampleTrialController.InteractionStates.OUT_OF_BOUNDS;
    this.cancelAndResetTrial();
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
  }

  private void checkResponseIntervalTime()
  {
    long deltaTime = SampleTrialController.Millis - this.TargetOnsetTime;
    //if possible interaction time has passed and there was no interaction or if possible speech response time is over, set InteractionState to TIME_OUT
    if (deltaTime >= SampleTrialController.ResponseIntervalLength && (!this.Target.GetComponent<GraspableObject>().HasBeenGrasped()))
    {
      if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("trial time out...");
      this.InteractionState = SampleTrialController.InteractionStates.TIME_OUT;
    }
  }

  private void checkTargetPosition(GameObject gameObject, bool validOrientation)
  {
    if (!GraspTrainingController.IsActive)
    {
      return;
    }

    if (this.InteractionState == SampleTrialController.InteractionStates.NONE && this.CurrentTrialState.TrialState == TrainingStates.RESPONSE)
    {
      if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("target in position...");
      this.InteractionState = validOrientation ? SampleTrialController.InteractionStates.IN_BOX : SampleTrialController.InteractionStates.WRONG_ORIENTATION;
    }
  }

  private void checkTargetDestruction(GameObject gameObject)
  {
    if (!GraspTrainingController.IsActive)
    {
      return;
    }

    if (this.InteractionState == SampleTrialController.InteractionStates.NONE && this.CurrentTrialState.TrialState == TrainingStates.RESPONSE)
    {
      if (GraspTrainingController.Verbose) UnityEngine.Debug.Log("target destroyed...");
      this.InteractionState = SampleTrialController.InteractionStates.TARGET_DESTROYED;
    }
  }
}
