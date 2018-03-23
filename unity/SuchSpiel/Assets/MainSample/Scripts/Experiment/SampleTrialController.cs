using Leap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.VR;

public class SampleTrialController : MonoBehaviour {

  //reference data
  public static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0);
  //public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }
  public static long Millis { get { return (long)((DateTime.Now - Jan1St1970).TotalMilliseconds); } }

  private static System.Random RandomNumberGenerator = new System.Random();

  private static bool Verbose = false;

  public static int TactileStimulationStrength = 200;

  // instructions

  private String WelcomeInstructionTextPartI = "Herzlich Willkommen und schonmal vielen lieben Dank fürs mitmachen! " +
    "In diesem Experiment wollen wir untersuchen, wie sich das Greifen in einer virtuellen Realität von einer Zweitaufgabe modulieren lässt. " +
    "Deine Aufgabe besteht darin, ein Objekt zu greifen und zu transportieren. Dabei erhältst du eine taktile Stimulation auf deinem " +
    "Zeigefinger, oder deinem Daumen. Gib bitte so schnell wie möglich an, welcher Finger stimuliert wurde. Zu Beginn kannst du " +
    "dich mit beiden Aufgaben vertraut machen. Zentrier das Fadenkreuz auf dem Ladebalken um das Greiftraining zu starten.";

  private String GraspTrainingInstruction = "In der Greifaufgabe müsstest du zunächst deine Hand in die Ausgangsposition bringen. " +
    "Sobald die roten Kugeln grün werden, ist die Ausgangsposition erreicht. Danach erscheint ein Fixationskreuz. Bitte fixiere " +
    "das Fixationskreuz. Das Fixationskreuz wird grün, sobald du ins Zentrum schaust. An der Stelle des Fixationskreuzes erscheint " +
    "eine Flasche. Bitte greife sie und transportiere sie zu der rechten Plattform. Es geht los, sobald du den Ladebalken schaust.";

  private String GraspTrainingInstructionII = "Das Greifen ist am Anfang recht gewöhnungsbedürftig. Ärger dich bitte nicht, wenn es zunächst schwierig ist. " +
      "Der Sensor reagiert leider manchmal seltsam, wenn Teile der Hand verdeckt sind. Übe am besten zunächst das Greifen der aufrechten Flasche und mach " +
      "dich danach mit der umgedrehten Flasche vertraut. In einem Block des Experiments wird deine Hand mehr oder minder stark nach links oder rechts verschoben, " +
      "was das Greifen noch schwieriger machen kann. Wenn du dich mit dem normalen Greifen sicher fühlst, dann probier auch die Variante mit Verschiebung aus. " +
      "Im Training siehst du links von dir eine Kontrolltafel über die du die Flaschenorientierung und die Verschiebung einstellen kannst. " +
      "Wenn du magst, kannst du dir auch noch einmal das Video anschauen. " +
      "Schau auf den Ladebalken, um das Training zu starten. Du kannst das Training beednen, indem du auf den Ladebalken an der Kontrolltafel schaust. ";

  private String SpeechTrainingInstruction = "Im eigentlichen Experiment müsstest du so schnell wie möglich auf die taktile Stimulation " +
    "reagieren, indem du sagst, welcher Finger stimuliert wurde (Zeigefinger oder Daumen). Nimm bitte die Ausgangsposition ein und halte " +
    "sie während des kompletten Durchgangs. Sobald du in der Ausgangsposition bist, kommt der Fixationscheck. Anstelle des Fixationskreuzes " +
    "erscheint eine Flasche. Danach kommt nach einer gewissen Zeit eine visuelle und eine taktile Stimulation, gib dann bitte so schnell wie " +
    "möglich an, welcher Finger stimuliert wurde. Es geht los, sobald du den Ladebalken schaust.";

  private String WelcomeInstructionTextPartII = "Das wärs soweit mit dem Training, das eigentliche Experiment startet, wenn du auf den " +
    "Ladebalken schaust. Die Durchgänge kombinieren die Greifaufgabe und die Detektionsaufgabe. Versuch bitte beide Aufgaben zu lösen. " +
    "Der Versuch besteht aus zwei Blöcken, in einem Block wird deine Hand mehr oder minder stark nach rechts oder links verschoben, während du greifst. " +
    "Die Durchgänge starten, sobald du mit der Hand die Ausgangsposition einnimmst, du kannst also zwischen den Druchgängen eine Pause einlegen, " +
    "wenn du magst. Sag Bescheid, falls du Fragen hast, ansonsten kannst du direkt loslegen.";

  private String FarewellInstructionText = "Das wärs, hab nochmal vielen lieben Dank fürs mitmachen.";

  private enum TrialStates
  {
    WELCOME,

    GRASP_TRAINING_INIT,
    GRASP_TRAINING,
    GRASP_TRAINING_COMMENCING,

    SPEECH_TRAINING,
    SPEECH_TRAINING_COMMENCING,

    TRAINING_DONE,

    STARTUP,
    INIT,
    FIXATION,
    SOA,
    RESPONSE,
    FEEDBACK,
    BREAK,
    PAUSE,
    END
  }

  private class TrialStateObject
  {
    public SampleTrialController.TrialStates TrialState;
    public long getSleepInterval()
    {

      switch (this.TrialState)
      {
        case TrialStates.STARTUP:
          return 2000;
        case TrialStates.PAUSE:
          return 10000;
        case TrialStates.BREAK:
          return 120000;
        case TrialStates.INIT:
          return 0;
        case TrialStates.FIXATION:
          return 1000;
        case TrialStates.SOA:
          return 200;
        case TrialStates.RESPONSE:
          return 40000;
        case TrialStates.FEEDBACK:
          return 1000;
        default:
          return 0;
      }
    }
  }

  // 2 x 4 x 6 = 48 conditions, with 10 repetitions, this equals 480 trials, given each trial takes 10 seconds this sums up to 4800 seconds, or 80 minutes -> quite long...
  // Teamprojekt:
  // 2 x 4 x 3 = 24 conditions, with 10 repetitions, this equals 240 trials per block, given each trial takes 10 seconds this sums up to 4800 seconds, or 80 minutes -> quite long...
  private string[] targetOrientation = new string[] { "upright", "rotated" };
  // private string[] congruencyCondition = new string[] { "both left", "both right", "light left - tactile index", "light right - tactile thumb" };
  // edit 22.02.17: four new conditions for the stimulation on the hand
  /*private string[] congruencyCondition = new string[] { "both left", "both right", "light left - tactile index", "light right - tactile thumb",
      "light index - tactile index", "light thumb - tactile thumb", "light index - tactile thumb", "light thumb - tactile index" };*/
  // edit 01.03.2017: Replication study with visual distractors on the bottle; just added the lift-off SOA	
  private string[] congruencyCondition = new string[] { "both left", "both right", "light left - tactile index", "light right - tactile thumb" };
  /*
  private string[] timingCondition     = new string[] {
    "-150MS", // ms relative to target onset
//    "50MS",
    "250MS",
    "MovementOnset", // at movement start
    "OneHalf"       // trigger collider onsets
    //"TwoThird"
  };
    */
  // edit 22.02.17: dropped early condition, added late condition
  /*
  private string[] timingCondition = new string[] {
    "250MS",
    "MovementOnset", // at movement start
    "OneHalf",       // half distance
    "LiftOff"        // after grasping the bottle
  };
  */
  // edit 31.05.17: team project settings
  private string[] timingCondition = new string[] {
    "250MS",          // stimulus onset
    "MovementOnset", // at movement start
    "OneHalf"        // half distance
  };

  private string[] blockTypes = new string[] {
      "variable",
      "consistent"
  };

  private List<string> blockList;

  private string currentBlock;

  // unstyle, has to match the number of repetitions...
  [HideInInspector]
  public float[] visualOffsets = new float[]
  /*
  {
      -.2f,
      -.166f,
      -.133f,
      -.1f,
      -.066f,
       .066f,
       .1f,
       .133f,
       .166f,
       .2f
  };
  */
  // 120617: six repetitions; smaller offset
  {
      -.175f,
      //-.175f,
      -.125f,
      //-.125f,
      -.075f,
       .075f,
       //.125f,
       .125f,
       //.175f,
       .175f
  };

  private List<TrialCondition> TrialConditionList;


  public class TrialCondition
  {
    private string currentTargetRotation;
    private string currentCongruencyCondition;
    private string currentStimulationTime;
    private float  currentVisualOffset;

    public TrialCondition(string currentTargetRotation, string currentCongruencyCondition, string currentStimulationTime, float currentVisualOffset)
    {
      this.currentTargetRotation      = currentTargetRotation;
      this.currentCongruencyCondition = currentCongruencyCondition;
      this.currentStimulationTime     = currentStimulationTime;
      this.currentVisualOffset        = currentVisualOffset;
    }

    public string CurrentTargetRotation { get { return currentTargetRotation; } }
    public string CurrentCongruencyCondition { get { return currentCongruencyCondition; } }
    public string CurrentStimulationTime { get { return currentStimulationTime; } }
    public float  CurrentVisualOffset { get { return currentVisualOffset; } }
  }

  public bool RecordLeapRawData = true;
  public bool VibroTactileStimulation = true;

  public int NumberOfRepetitions = 10;

  public float StimulationDuration = 2.0f;

  public GameObject Target;
  public GameObject FakeTarget;
  // UI
  public UnityEngine.UI.Text MainInstruction;
  private UnityEngine.UI.Image MainInstructionBackground;
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
  private TrialStateObject CurrentTrialState;

  private EffectorRangeCheck RangeCheck;

  public VisualHandOffsetController OffsetController;

  //Stimulation variables 
  public VibroTactileStimulationInterface vibroTactileStimulationInterface;
  private GameObject leftLight;
  private GameObject rightLight;
  private GameObject leftLightFake;
  private GameObject rightLightFake;

  public bool messageObtained;
  private long answerResponseTime;

  //Scene variables
  private GameObject FixationObject;

  //Trial variables
  private long ResponseIntervalStart;
  public static long ResponseIntervalLength = 10000L;
  public static long VerbalResponseIntervalLength = 3000L;

  public bool Started = false;
  public static bool IsActive = true;//false;
  
  //z.B. auch wenn Vp nicht auf Reize reagieren o.ä. - Trial wiederholung 
  public string LastErrorCode;

  private Action TrialUpdateDelegate;

  //Position where the target will always appear (in foodpuncher this was CurrentTargetPosition)
  public Vector3 InitialTargetPosition;

  private int CurrentHandID;
  private TrialData CurrentTrialData;

  //muss noch implementiert werden, dass es auf unser Experiment passt
  private PoseController HandPoseController;
  private static long InitialPoseDuration = 500L;
  private long PoseCheckTimeStamp;

  // during the response interval the current state of the interaction is checked, the trial might end if:
  // - more than 10 seconds are elapsed (just a failsafe)
  // - the target is out of range
  // - the target has been placed into the container
  private bool CheckInteractionState;

  public InteractionStates InteractionState;

  public enum InteractionStates
  {
    //no interaction occurred
    NONE,
    // position was left before target onset
    LEFT_POSITION_DURING_FIXATION,
    // Target destroyed
    TARGET_DESTROYED,
    // wrong orientation
    WRONG_ORIENTATION,
    // object left bounds
    OUT_OF_BOUNDS,
    //trial end due to time constraint
    TIME_OUT,
    //trial end due to time constraint for vocal response
    VERBAL_TIME_OUT,
    // wrong  response
    VERBAL_WRONG_RESPONSE,
    //object landed in ending position
    IN_BOX
  }

  // if true the script will try to fetch the cmd arg and to initialize the participant date files / directories
  public bool parseCommandLineArguments = true;
  // IO
  public static string defaultDataDirectoryName = "Data";
  private int TrialCounter = 1;

  public GraspTrainingController GraspTrainingController;
  public SpeechTrainingController SpeechTrainingController;

  void Awake()
  {
    // check VR settings and assign viewport anchor accordingly
    if (UnityEngine.VR.VRSettings.enabled)
    {
        UnityEngine.Debug.Log("VR Device detected...");
        UnityEngine.VR.InputTracking.Recenter();
    }

    // hmm, at the moment the seed is the current system time, maybe we want to use somthing participant specific?
    SampleTrialController.RandomNumberGenerator = new System.Random();
    if (this.parseCommandLineArguments)
    {
      // parse command line parameters
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("fetching cmd args...");
      // setup data directory
      string dataDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), SampleTrialController.defaultDataDirectoryName);
      if (!File.Exists(dataDirectoryPath) || !Directory.Exists(dataDirectoryPath))
      {
        Directory.CreateDirectory(dataDirectoryPath);
      }
      // we check for the launcher arguments: RunApplication("-ID " + this.VPID + " -Age " + this.age + " -Sex " + this.sex + " -Handedness " + this.handedness);
      string vpID = "none";
      string vpAge = "none";
      string vpSex = "none";
      string vpHandedness = "none";
      string vpView = "none";
      string VPDirectoryPath = "none";
      string[] args = Environment.GetCommandLineArgs();
      for (int i = 0; i < args.Length; i++)
      {
        if (args[i].Equals("-ID") && i + 1 < args.Length)
        {
          vpID = args[i + 1];
          // try to setup directories and stuff...
          VPDirectoryPath = Path.Combine(dataDirectoryPath, vpID);
          bool foundValidId = false;
          // we do not want to lose data, we iterate until we find an unassigned id...
          int counter = 1;
          string newVpID = vpID;
          while (!foundValidId)
          {
            if (!File.Exists(VPDirectoryPath) && !Directory.Exists(VPDirectoryPath))
            {
              Directory.CreateDirectory(VPDirectoryPath);
              foundValidId = true;
            }
            // path exists, id is not unique...
            else
            {
              string dumppath = Path.Combine(VPDirectoryPath, "triallist.dump");
              // previously cancelled experiment...
              if (File.Exists(dumppath))
              {
                  foundValidId = true;
                  break;
              }

              newVpID = vpID + "_" + counter.ToString();
              VPDirectoryPath = Path.Combine(dataDirectoryPath, newVpID);
              counter++;
            }
          }
          vpID = newVpID;
        }
        else if (args[i].Equals("-Age") && i + 1 < args.Length)
        {
          vpAge = args[i + 1];
        }
        else if (args[i].Equals("-Sex") && i + 1 < args.Length)
        {
          vpSex = args[i + 1];
        }
        else if (args[i].Equals("-Handedness") && i + 1 < args.Length)
        {
          vpHandedness = args[i + 1];
        }
        else if (args[i].Equals("-View") && i + 1 < args.Length)
        {
            vpView = args[i + 1];
        }
      }
      // save data, setup directories...
      if (!VPDirectoryPath.Equals("none"))
      {
        string blockOrder = null;
        string infoPath = Path.Combine(VPDirectoryPath, "participant.info");
        string dumppath = Path.Combine(VPDirectoryPath, "triallist.dump");

        // previously cancelled experiment...
        if (File.Exists(dumppath) && File.Exists(infoPath))
        {
            string[] lines = File.ReadAllLines(infoPath);

            foreach (string line in lines)
            {
                if (line.StartsWith("BlockOrder: "))
                {
                    blockOrder = line.Substring("BlockOrder: ".Length);
                }
            }
        }
        else
        {
            blockOrder = FileWriter.parseAndUpdateBlockFile(vpID);
        }

        // save infos
        
        DateTime cTime = DateTime.Now;
        //using (StreamWriter outfile = new StreamWriter(LeapFileWriter.defaultPath))
        using (StreamWriter outfile = new StreamWriter(new FileStream(infoPath,
                                                                      FileMode.OpenOrCreate,
                                                                      FileAccess.ReadWrite,
                                                                      FileShare.None)))
        {
          outfile.Write("## participant info" + Environment.NewLine
                        + "## created at " + cTime.ToString() + Environment.NewLine
                        + "ID        : " + vpID + Environment.NewLine
                        + "Age       : " + vpAge + Environment.NewLine
                        + "Sex       : " + vpSex + Environment.NewLine
                        + "Handedness: " + vpHandedness + Environment.NewLine
                        + "View      : " + vpView + Environment.NewLine
                        + "BlockOrder: " + blockOrder + Environment.NewLine);
        }

        // assign data path
        string dataPath = Path.Combine(VPDirectoryPath, vpID + ".log");
        FileWriter.defaultPath = dataPath;
        // assign record path
        string recordPath = Path.Combine(VPDirectoryPath, "LeapRawRecords");
        Directory.CreateDirectory(recordPath);
        ExperimentHandController.baseRecordPath = recordPath + "/";

        // setup block order
        string[] blockTokens = blockOrder.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        if (blockTokens[0].Equals("variable"))
        {
            if (blockTokens[1].Equals("consistent"))
            {
                this.blockTypes = new string[] { "variable", "consistent" };
            }
            else
            {
                UnityEngine.Debug.Log("unknown compatibility mapping: " + blockTokens[1] + "...");
            }
        }
        else if (blockTokens[0].Equals("consistent"))
        {
            if (blockTokens[1].Equals("variable"))
            {
                this.blockTypes = new string[] { "consistent", "variable" };
            }
            else
            {
                UnityEngine.Debug.Log("unknown compatibility mapping: " + blockTokens[1] + "...");
            }
        }
        else
        {
            UnityEngine.Debug.Log("unknown visibility mapping: " + blockTokens[0] + "...");
        }
      }
    }

    // blockliste erstellen
    this.blockList = new List<string>();

    for (int i = 0; i < this.blockTypes.Length; i++)
    {
        this.blockList.Add(this.blockTypes[i]);
    }
    // nope, we do a pseudorandomization between participants
    // ExperimentController.Shuffle(this.blockList);
    this.currentBlock = this.blockList[0];
    this.blockList.RemoveAt(0);

    this.HandControllerObject = GameObject.FindWithTag("LeapController");
    this.HandControllerReference = this.HandControllerObject.GetComponent<ExperimentHandController>();
    if (!this.VibroTactileStimulation)
    {
      this.HandControllerReference.enableFingerParticleSystems = true;
    }
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
        containerController.ObjectWasReleased += new ContainerController.ObjectWasReleasedHandler(this.checkTargetPosition);
        containerController.ObjectWasDestroyed += new ContainerController.ObjectWasDestroyedHandler(this.checkTargetDestruction);
      }
    }

    string path = new FileInfo(FileWriter.defaultPath).Directory.FullName;
    path = Path.Combine(path, "triallist.dump");

    if (File.Exists(path))
    {
        this.TrialConditionList = new List<TrialCondition>();
        using (StreamReader streamReader = File.OpenText(path)) {
          string[] lines = streamReader.ReadToEnd().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
          foreach (string line in lines)
          {
            string[] tokens = line.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (tokens[0] == "BlockOrder")
            {
                this.blockList = new List<string>();

                for (int i = 0; i < tokens.Length - 1; i++)
                {
                    this.blockList.Add(tokens[i + 1]);
                }
                // nope, we do a pseudorandomization between participants
                // ExperimentController.Shuffle(this.blockList);
                this.currentBlock = this.blockList[0];
                this.blockList.RemoveAt(0);
            }
            else
            {
                TrialCondition condition = new TrialCondition(tokens[0], tokens[2], tokens[1], float.Parse(tokens[3]));
                this.TrialConditionList.Add(condition);
            }
          }
        }

        if (this.TrialConditionList.Count == 0)
        {
            if (SampleTrialController.Verbose) UnityEngine.Debug.Log("no trial condition list parsed...");
        }
    }
    else
    {
        this.createTrialConditionList();
        // setup trial list
        if (this.TrialConditionList.Count > 0)
        {
            SampleTrialController.ShuffleList(this.TrialConditionList);
        }
        else
        {
            if (SampleTrialController.Verbose) UnityEngine.Debug.Log("no trial condition list created...");
        }
    }
    //UnityEngine.Debug.Log(this.TrialConditionList.Count + " trials created...");
    this.Target.SetActive(true);
    this.Target.transform.position = new Vector3(0, -100, 100);
    this.Target.GetComponent<Rigidbody>().isKinematic = true;


    this.FakeTarget.SetActive(true);
    this.FakeTarget.transform.position = new Vector3(0, -200, 100);
    
    this.leftLight = GameObject.FindGameObjectWithTag("LeftLightStimulus");
    this.rightLight = GameObject.FindGameObjectWithTag("RightLightStimulus");
    this.leftLight.GetComponent<ParticleSystem>().Stop();
    this.leftLight.GetComponent<ParticleSystem>().Clear();
    this.rightLight.GetComponent<ParticleSystem>().Stop();
    this.rightLight.GetComponent<ParticleSystem>().Clear();

    this.leftLightFake = GameObject.FindGameObjectWithTag("LeftLightStimulusFake");
    this.rightLightFake = GameObject.FindGameObjectWithTag("RightLightStimulusFake");
    this.leftLightFake.GetComponent<ParticleSystem>().Stop();
    this.leftLightFake.GetComponent<ParticleSystem>().Clear();
    this.rightLightFake.GetComponent<ParticleSystem>().Stop();
    this.rightLightFake.GetComponent<ParticleSystem>().Clear();
        
    this.messageObtained = false;

    this.OffsetController.ApplyDrift = false;

    //utils
    this.FixationObject = GameObject.FindGameObjectWithTag("FixationObject");
    this.TrialUpdateDelegate = delegate () { this.TrialUpdate(); };
    
    //setup trial control
    this.CurrentTrialState = new TrialStateObject();
    this.CurrentTrialState.TrialState = TrialStates.GRASP_TRAINING;// TrialStates.WELCOME;
    this.LastErrorCode = "none";

    // instruction
    this.MainInstruction.text = "";
    this.MainInstructionBackground = this.MainInstruction.GetComponentInChildren<UnityEngine.UI.Image>();
    this.MainInstructionBackground.enabled = false;
    this.FeedbackDisplay.text = "";
      
    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("done with awaking...");
  }
  
  //takes the three conditions, creates and object from these and adds it to the TrialConditionList
  private void createTrialConditionList()
  {
    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("create condition list...");

    this.TrialConditionList = new List<TrialCondition>();

    for (int i = 0; i < this.targetOrientation.Length; i++)
    {
      for (int j = 0; j < this.congruencyCondition.Length; j++)
      {
        for (int k = 0; k < this.timingCondition.Length; k++)
				{
          for (int l = 0; l < this.NumberOfRepetitions; l++)
          {
            TrialCondition trialCondition = new TrialCondition(this.targetOrientation[i], this.congruencyCondition[j], this.timingCondition[k], this.visualOffsets[l]);
            this.TrialConditionList.Add(trialCondition);
          }
        }
      }
    }
    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("created " + TrialConditionList.Count + " trial templates...");
  }


  private void checkInitialPose()
  {

    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("call to check intial pose...");
    this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.INITIALIZE);
    bool valid = this.HandPoseController.checkInitialPose();
    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("valid pose: " + valid + "...");

    // adapt timer and check hand presence
    if (valid && this.CurrentTrialData != null)
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("pose check time stamp: " + this.PoseCheckTimeStamp);
      if (this.PoseCheckTimeStamp == -1L)
      {
        this.PoseCheckTimeStamp = ((long)(Time.realtimeSinceStartup * 1000.0f));
      }
      else if (((long)(Time.realtimeSinceStartup * 1000.0f)) - this.PoseCheckTimeStamp >= SampleTrialController.InitialPoseDuration)
      {
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("check initial pose...");
        this.CurrentHandID = this.HandControllerReference.currentRightHandID;
        this.CurrentTrialState.TrialState = TrialStates.INIT;
        this.CurrentTrialData.TrialStartTime = SampleTrialController.Millis;
        this.TrialUpdate();
      }
    }
    // reset timer
    else
    {
      this.PoseCheckTimeStamp = -1L;
    }
  }

  private bool checkPalmInInitialPose()
  {

    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("check initial palm position...");
    if (this.CurrentTrialData != null)
    {
      if (this.CurrentTrialData.MovementOnset == -1L)
      {
        bool inInitialPose = this.HandPoseController.checkPalmPosition();
        if (!inInitialPose)
        {
          //Reference for MovementOnset is end of fixation
          this.CurrentTrialData.MovementOnset = SampleTrialController.Millis - this.CurrentTrialData.FixationOffset;
          if (SampleTrialController.Verbose) UnityEngine.Debug.Log("palm left initial position at: " + this.CurrentTrialData.MovementOnset + "...");
        }
        return inInitialPose;
      }
      else
      {
        return true;
      }
    }
    return true;
  }


  // Update is called once per frame
  void Update()
  {
      if (Input.GetKeyUp(KeyCode.Return))
      {
          if (UnityEngine.VR.VRSettings.enabled)
          {
              UnityEngine.Debug.Log("VR Device aligned...");
              UnityEngine.VR.InputTracking.Recenter();
          }
      }
      
    if (!this.Started && SampleTrialController.IsActive)
    {
      this.Started = true;
      this.PoseCheckTimeStamp = -1L;
      FileWriter.checkFile();
      FileWriter.writeData(TrialData.getLogFileHeader());
      this.HandControllerReference.recorder_.Reset();
      this.CurrentTrialState.TrialState = TrialStates.GRASP_TRAINING_COMMENCING;// TrialStates.WELCOME;
      GraspTrainingController.IsActive = true;
      //this.CurrentTrialState.TrialState = TrialStates.STARTUP;
      this.TrialUpdate();
    }
    // check pose
    if (this.Started && SampleTrialController.IsActive && this.CurrentTrialState.TrialState == TrialStates.STARTUP)
    {
      this.checkInitialPose();
    } // check response state

    // fixation check
    if (this.Started && SampleTrialController.IsActive && this.CurrentTrialState.TrialState == TrialStates.FIXATION)
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("fixation check...");
      if (!this.HandPoseController.checkInitialPose())
      {
        this.InteractionState = InteractionStates.LEFT_POSITION_DURING_FIXATION;
        this.cancelAndResetTrial();
      }
      if (this.HandPoseController.checkInitialPose())
      {
        this.TrialUpdate();
      }
      else
      {
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("fixation not yet completed");
      }
    }

    // soa check
    if (this.Started && SampleTrialController.IsActive && this.CurrentTrialState.TrialState == TrialStates.SOA)
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("soa check...");
      if (!this.HandPoseController.checkInitialPose())
      {
        this.InteractionState = InteractionStates.LEFT_POSITION_DURING_FIXATION;
        this.cancelAndResetTrial();
      }
    }

    //check hand trajectory
    if (this.Started && SampleTrialController.IsActive && this.CurrentTrialState.TrialState != TrialStates.STARTUP && this.CurrentTrialState.TrialState != TrialStates.FIXATION && this.CurrentTrialState.TrialState != TrialStates.FEEDBACK && this.CurrentTrialState.TrialState != TrialStates.END)
    {
      if (this.CurrentTrialData != null)
      {
        if (this.CurrentTrialData.MovementOnset == -1L) this.checkPalmInInitialPose();

        Vector3 palmPosition = this.HandControllerReference.getCurrentPalmPosition();
        Vector3 indexPosition = this.HandControllerReference.getCurrentIndexPosition();
        Vector3 thumbPosition = this.HandControllerReference.getCurrentThumbPosition();
        float timeStamp = Time.realtimeSinceStartup;
        this.CurrentTrialData.PalmTrajectory.Add(new Vector4(palmPosition.x, palmPosition.y, palmPosition.z, timeStamp));
        this.CurrentTrialData.IndexTrajectory.Add(new Vector4(indexPosition.x, indexPosition.y, indexPosition.z, timeStamp));
        this.CurrentTrialData.ThumbTrajectory.Add(new Vector4(thumbPosition.x, thumbPosition.y, thumbPosition.z, timeStamp));
      }
      // cancel trial
      if (this.CurrentHandID != this.HandControllerReference.currentRightHandID && this.CurrentTrialState.TrialState == TrialStates.RESPONSE)
      {
        this.cancelAndResetTrial();
      }
    }

    //checkInteractionState = true after fixation
    if (this.Started && SampleTrialController.IsActive && this.CheckInteractionState)
    {

      //checks if no time out for vocal response and object interaction and if vocal response has been obtained
      this.checkResponseIntervalTime();
      this.checkWordRecognition();
      this.checkVerbalResponseIntervalTime();
      
      if (this.InteractionState != InteractionStates.NONE)
      {
        this.CheckInteractionState = false;
        // bottle placed, but no speech respone, while there is still time -> wait
        if (this.CurrentTrialData.VerbalResponseTime == -1L && this.InteractionState == InteractionStates.IN_BOX)
        {
          this.StartCoroutine(this.waitForVerbalResponse());
        }
        else
        {
          this.TrialUpdate();
        }
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
    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("trial canceled");
    // this.CurrentTrialState.TrialState = TrialStates.STARTUP;
    this.CurrentTrialState.TrialState = TrialStates.FEEDBACK;
    //this.TrialUpdate();
    StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
  }

  public void TrialUpdate()
  {

    if (!SampleTrialController.IsActive)
    {
      this.Started = false;
      return;
    }

    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("timer called, in state = " + CurrentTrialState.TrialState.ToString() + "...");
    switch (this.CurrentTrialState.TrialState)
    {
      case TrialStates.WELCOME:
        this.MainInstruction.text = this.WelcomeInstructionTextPartI;
        this.MainInstructionBackground.enabled = true;
        break;
        case TrialStates.GRASP_TRAINING_INIT:
        this.MainInstruction.text = this.GraspTrainingInstruction;
        this.MainInstructionBackground.enabled = true;
        break;
      case TrialStates.GRASP_TRAINING:
        this.MainInstruction.text = this.GraspTrainingInstructionII;
        this.MainInstructionBackground.enabled = true;
        break;
      case TrialStates.SPEECH_TRAINING:
        this.MainInstruction.text = this.SpeechTrainingInstruction;
        this.MainInstructionBackground.enabled = true;
        break;
      case TrialStates.TRAINING_DONE:
        this.MainInstruction.text = this.WelcomeInstructionTextPartII;
        this.MainInstructionBackground.enabled = true;
        break;
      case TrialStates.FEEDBACK:
        this.FeedbackDisplay.text = "";
        this.CurrentTrialState.TrialState = TrialStates.STARTUP;
        this.TrialUpdate();
        break;
      case TrialStates.STARTUP:
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("startup...");

        //Last Trial of the block / experiment
        if (this.TrialConditionList.Count == 0)
        {
            this.OffsetController.ApplyDrift = false;

            if (this.blockList.Count == 0)
            {
                string path = new FileInfo(FileWriter.defaultPath).Directory.FullName;
                path = Path.Combine(path, "triallist.dump");
                if (File.Exists(path))
                {
                    // remvoe old dump
                    File.Delete(path);
                }

                this.MainInstruction.text = this.FarewellInstructionText;
                this.MainInstructionBackground.enabled = true;
                this.FeedbackDisplay.text = "";
                this.HandControllerReference.recorder_.Stop();
                this.HandControllerReference.recorder_.Reset();
                this.HandPoseController.resetInitialPoseChecks();
                this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.IDLE);
                this.CurrentTrialState.TrialState = TrialStates.END;
                StartCoroutine(this.shutdownExperiment(10.0f));
                break;
            }
            else
            {
                this.currentBlock = this.blockList[0];
                this.blockList.RemoveAt(0);
                this.OffsetController.ApplyDrift = false;
                /*
                // set only to true when the trial starts, otherwise its possible to
                // explore the offset before going into the pose check
                if (this.currentBlock.EndsWith("consistent"))
                {
                    this.OffsetController.ApplyDrift = false;
                }
                else
                {
                    this.OffsetController.ApplyDrift = true;
                }
                */
                this.createTrialConditionList();
                // setup trial list
                if (this.TrialConditionList.Count > 0)
                {
                    SampleTrialController.ShuffleList(this.TrialConditionList);
                }
                else
                {
                    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("no trial condition list created...");
                }

                this.FeedbackDisplay.text = "Jetzt kommt der nächste Block, " + (this.currentBlock.EndsWith("consistent") ? "diesmal mit konsistentem Mapping." : "diesmal mit variablem Mapping.");

                this.CurrentTrialState.TrialState = TrialStates.STARTUP;
                StartCoroutine(this.trialUpdateTimeout(30.0f));
                break;
            }
        }
        else
        {
          this.OffsetController.ApplyDrift = false;
          // just paranoia...
          TrialCondition currentCondition = this.TrialConditionList[0];
          this.CurrentTrialData = new TrialData(currentCondition.CurrentTargetRotation, currentCondition.CurrentStimulationTime, currentCondition.CurrentCongruencyCondition, this.currentBlock);
          this.LastErrorCode = "none";
          this.CurrentTrialData.ErrorCode = this.LastErrorCode;
          if (this.CurrentTrialData.BlockType == "consistent")
          {
              this.CurrentTrialData.VisualOffset = 0.0f;
          }
          else if (this.CurrentTrialData.BlockType == "variable")
          {
              this.CurrentTrialData.VisualOffset = currentCondition.CurrentVisualOffset;
              this.OffsetController.DriftFactor = this.CurrentTrialData.VisualOffset;
          }
          else
          {
              if (SampleTrialController.Verbose) UnityEngine.Debug.Log("unknown block type: " + this.CurrentTrialData.BlockType + "...");
          }
          this.HandControllerReference.recorder_.Stop();
          this.HandControllerReference.recorder_.Reset();
          this.HandPoseController.resetInitialPoseChecks();
          this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.CHECK);
          this.FeedbackDisplay.text = "Noch " + this.TrialConditionList.Count.ToString() + " Durchgänge...";
          if (SampleTrialController.Verbose) UnityEngine.Debug.Log("startup done...");
          break;
        }
      case TrialStates.PAUSE:
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("pause...");
        this.RangeCheck.clearMonitor();
        this.CurrentTrialState.TrialState = TrialStates.STARTUP;
        StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        break;
      case TrialStates.BREAK:
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("break...");
        this.RangeCheck.clearMonitor();
        this.CurrentTrialState.TrialState = TrialStates.STARTUP;

        StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        break;
      case TrialStates.INIT:
        this.FeedbackDisplay.text = "";
        this.HandControllerReference.ExperimentMode = this.RecordLeapRawData;
        if (this.RecordLeapRawData) this.HandControllerReference.recorder_.Record();

        if (SampleTrialController.Verbose) Debug.Log("Stimulation Time: " + this.CurrentTrialData.StimulationCondition + "...");
        
        this.CurrentTrialState.TrialState = TrialStates.FIXATION;
        this.FixationObject.transform.position = this.InitialTargetPosition;

        this.CurrentTrialData.FixationOnset = SampleTrialController.Millis;

        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("dummy fixation interval");
        StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        break;
      case TrialStates.FIXATION:
        if (this.currentBlock.EndsWith("consistent"))
        {
            this.OffsetController.ApplyDrift = false;
        }
        else
        {
            this.OffsetController.ApplyDrift = true;
        }
        this.CurrentTrialData.FixationOffset = SampleTrialController.Millis;
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("fixation cross was displayed for " + (this.CurrentTrialData.FixationOffset - this.CurrentTrialData.FixationOnset) + "ms...");
        this.CurrentTrialState.TrialState = TrialStates.SOA;
        this.FixationObject.transform.position = new Vector3(0, -10, 0);
        StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        // time based stimulation starts here...
        if (this.CurrentTrialData.StimulationCondition == "-150MS")
        {
          this.FakeTarget.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
          this.FakeTarget.transform.position = this.InitialTargetPosition;
          float timeout = (CurrentTrialState.getSleepInterval() - 150) * .001f;
          StartCoroutine(this.TimeBasedStimulation(timeout, true));
        }
        else if (this.CurrentTrialData.StimulationCondition == "50MS")
        {
          float timeout = (CurrentTrialState.getSleepInterval() + 50) * .001f;
          StartCoroutine(this.TimeBasedStimulation(timeout));
        }
        else if (this.CurrentTrialData.StimulationCondition == "250MS")
        {
          float timeout = (CurrentTrialState.getSleepInterval() + 250) * .001f;
          StartCoroutine(this.TimeBasedStimulation(timeout));
        }
        else if (this.CurrentTrialData.StimulationCondition == "MovementOnset")
        {
          StartCoroutine(this.MovementOnsetStimulation());
        }
        else if (this.CurrentTrialData.StimulationCondition == "OneHalf")
        {
          StartCoroutine(this.MovementStimulation(.50f));
        }
        else if (this.CurrentTrialData.StimulationCondition == "OneThird")
        {
          StartCoroutine(this.MovementStimulation(.33f));
        }
        else if (this.CurrentTrialData.StimulationCondition == "TwoThird")
        {
          StartCoroutine(this.MovementStimulation(.66f));
        }
        break;
      case TrialStates.SOA:
        // we reset again, this is not just paranoia, sometimes, the response arrives after the cancelation and can interfere with the
        // actual response time measurement
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
        graspable.ResetPositionAndOrientation(this.CurrentTrialData.TargetOrientation == "rotated" ? 180.0f : 0.0f, this.InitialTargetPosition);
        if (this.CurrentTrialData.TargetOrientation == "rotated")
        {
          if (SampleTrialController.Verbose) Debug.Log("rotated target...");
         // this.Target.transform.Rotate(new Vector3(180.0f, 0.0f, 0.0f));
         // this.Target.transform.position = this.InitialTargetPosition;
        }
        else if (this.CurrentTrialData.TargetOrientation == "upright")
        {
          if (SampleTrialController.Verbose) Debug.Log("upright target...");
        //  this.Target.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
        //  this.Target.transform.position = this.InitialTargetPosition;
        }

        this.CurrentTrialData.InitialTargetLocation = this.Target.transform.position;
        this.CurrentTrialData.TargetOnset = SampleTrialController.Millis;
        this.Target.GetComponent<Rigidbody>().isKinematic = false;
        // edit JL
        this.Target.GetComponent<Rigidbody>().useGravity = true;

        this.CurrentTrialState.TrialState = TrialStates.RESPONSE;
        // enable range check
        this.InteractionState = InteractionStates.NONE;
        this.CheckInteractionState = true;
        this.RangeCheck.clearMonitor();
        this.RangeCheck.monitorObject(this.Target);
        this.ResponseIntervalStart = SampleTrialController.Millis;
        if (Verbose) Debug.Log("Current Trial State: " + CurrentTrialState.TrialState);
        // start this one only after target activation
        if (this.CurrentTrialData.StimulationCondition == "LiftOff")
        {
			// 100 ms after grasp; edit 01.03.2017: no timeout, stimulation halfway between start and end position
            StartCoroutine(this.StimulateOnGrasp(100 * .001f));
        }

        break;

      case TrialStates.RESPONSE:
        if (SampleTrialController.Verbose) Debug.Log("response state...");
        this.OffsetController.ApplyDrift = false;
        this.CheckInteractionState = false;
        if (this.CurrentTrialData.VerbalResponseTime == -1L && this.InteractionState == InteractionStates.IN_BOX)
        {
          this.InteractionState = InteractionStates.VERBAL_TIME_OUT;
        }

        if (this.CurrentTrialData.VerbalResponseTime != -1L && this.InteractionState == InteractionStates.IN_BOX && !this.CurrentTrialData.CorrectResponse)
        {
          this.InteractionState = InteractionStates.VERBAL_WRONG_RESPONSE;
        }

        this.checkTargetState();
        if (this.InteractionState != InteractionStates.IN_BOX)
        {
          this.cancelTrial();
          if (this.CurrentTrialData != null)
          {
            StartCoroutine(this.printAsyncDataCoroutine());
            //FileWriter.writeData(this.CurrentTrialData.getOutputLine());
            //this.GazeCollector.print(this.CurrentTrialData.TrialStartTime, this.CurrentTrialData.info);
            //this.GazeCollector.printAsync(this.CurrentTrialData.TrialStartTime, this.CurrentTrialData.info);
          }
          TrialCondition condition = this.TrialConditionList[0];
          this.TrialConditionList.RemoveAt(0);
          // only repeat if there was no verbal response
          if (this.CurrentTrialData.VerbalResponseTime == -1L) this.TrialConditionList.Add(condition);
          // reset trial state...
          //this.CurrentTrialState.TrialState = TrialStates.STARTUP;
          this.PoseCheckTimeStamp = -1L;
          this.CurrentTrialState.TrialState = TrialStates.FEEDBACK;
          //this.TrialUpdate();

          StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        }
        else
        {
          this.FeedbackDisplay.text = this.positiveFeedbackTemplates[UnityEngine.Random.Range(0, this.positiveFeedbackTemplates.Length - 1)];

          this.deactivateTarget();
          this.InteractionState = InteractionStates.NONE;
          // clear monitor
          this.RangeCheck.clearMonitor();
          this.messageObtained = false;

          if (this.CurrentTrialData != null)
          {
            StartCoroutine(this.printAsyncDataCoroutine());
            //FileWriter.writeData(this.CurrentTrialData.getOutputLine());
            //this.GazeCollector.print(this.CurrentTrialData.TrialStartTime, this.CurrentTrialData.info);
            //this.GazeCollector.printAsync(this.CurrentTrialData.TrialStartTime, this.CurrentTrialData.info);
            this.LastErrorCode = "none";
            this.TrialConditionList.RemoveAt(0);
          }
          this.HandControllerReference.recorder_.Stop();

          if (this.RecordLeapRawData)
          {
            string trialId = ExperimentHandController.baseRecordPath + "TrialNo_" + this.TrialCounter.ToString() +  "_StartedAt_" + this.CurrentTrialData.TrialStartTime.ToString() + "_LeapRecording.bytes";
            string output = this.HandControllerReference.recorder_.SaveToNewFile(trialId);
            this.TrialCounter++;
            if (SampleTrialController.Verbose) UnityEngine.Debug.Log("saved leap data to: " + output + "...");
          }
          this.HandControllerReference.recorder_.Reset();

          this.PoseCheckTimeStamp = -1L;
          //this.CurrentTrialState.TrialState = TrialStates.STARTUP;
          this.CurrentTrialState.TrialState = TrialStates.FEEDBACK;

          StartCoroutine(CoroutineTimer.Start(CurrentTrialState.getSleepInterval() * .001f, this.TrialUpdateDelegate));
        }
        break;
    }
  }

  private IEnumerator shutdownExperiment(float timeout)
  {
      yield return new WaitForSeconds(timeout);
      Application.Quit();
      yield return null;
  }

  private IEnumerator trialUpdateTimeout(float timeout)
  {
      this.HandPoseController.toogleVisibility(false);
      yield return new WaitForSeconds(timeout);
      this.HandPoseController.toogleVisibility(true);
      this.TrialUpdate();
      yield return null;
  }
  /* change 080317
  private IEnumerator printAsyncDataCoroutine()
  {
      FileWriter.writeData(this.CurrentTrialData.getOutputLine());
      yield return null;
  }*/

  private IEnumerator printAsyncDataCoroutine()
  {
      Thread trialDataThread = new Thread(this.CurrentTrialData.printDataAsync);
      this.CurrentTrialData.isWriting = true;
      trialDataThread.Start();

      while (this.CurrentTrialData.isWriting)
      {
          yield return 0;
      }

      trialDataThread.Join();

      yield return null;
  }

  private IEnumerator ResponseTimeout(float timeout)
  {
      float baseTime = Time.realtimeSinceStartup;
      float time = Time.realtimeSinceStartup;
      while (time - baseTime < timeout)
      {
          time = Time.realtimeSinceStartup;
          yield return 0;
      }

      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("timeout done...");

      this.TrialUpdate();

      yield return null;
  }

  private IEnumerator TimeBasedStimulation(float timeout, bool visualStimulation = true)
  {
    float baseTime = Time.realtimeSinceStartup;
    float time = Time.realtimeSinceStartup;
    while (time - baseTime < timeout && this.LastErrorCode == "none")
    {
      time = Time.realtimeSinceStartup;
      yield return 0;
    }

    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("timeout done, start stimulation");

    if (this.LastErrorCode == "none")
    {
      this.CurrentTrialData.StimulationOnset = SampleTrialController.Millis - this.CurrentTrialData.FixationOffset;
      StartCoroutine(this.Stimulate(visualStimulation));
    }

    yield return null;
  }

  private IEnumerator MovementOnsetStimulation()
  {
    while (this.CurrentTrialData.MovementOnset == -1L && this.LastErrorCode == "none")
    {
      yield return 0;
    }

    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("movement onset, start stimulation");

    if (this.LastErrorCode == "none")
    {
      this.CurrentTrialData.StimulationOnset = SampleTrialController.Millis - this.CurrentTrialData.FixationOffset;
      StartCoroutine(this.Stimulate());
    }

    yield return null;
  }
  // we only consider the depth, i.e. the z-distance
  private IEnumerator MovementStimulation(float distanceRatio)
  {
    float targetZ = this.InitialTargetPosition.z;//this.Target.transform.position.z;
    float startZ  = this.HandPoseController.getPalmStartPosition().z;
    // minus distance ratio, otherwise the <= equal comparison does not work
    float distance = (targetZ - startZ) * (1.0f - distanceRatio);
    bool inRange = false;

    while (!inRange && this.LastErrorCode == "none")
    {
      float currentZ = this.HandControllerReference.getCurrentPalmPosition().z;
      float cDistance = targetZ - currentZ;
      //UnityEngine.Debug.Log("distance info: " + cDistance + " vs. " + distance);
      if (cDistance <= distance)
      {
        inRange = true;
      }
      yield return 0;
    }

    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("distance reached, start stimulation: " + this.LastErrorCode);

    if (this.LastErrorCode == "none")
    {
      this.CurrentTrialData.StimulationOnset = SampleTrialController.Millis - this.CurrentTrialData.FixationOffset;
      StartCoroutine(this.Stimulate());
    }

    yield return null;
  }
	
  // edit 01.03.2017: Graps stimulation when we are closer to the end- than to the start position instead of time based stimulation
  private IEnumerator StimulateOnGrasp(float timeout)
  {
	GraspableObject graspable = this.Target.GetComponent<GraspableObject>();
	Vector3 endPosition = GameObject.FindGameObjectWithTag ("Container").transform.position;
	endPosition.y = this.InitialTargetPosition.y;
	// for the visual distractor, we use the fake bottle
	this.FakeTarget.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
	this.FakeTarget.transform.position = endPosition;

	while (!graspable.HasBeenGrasped() && this.LastErrorCode == "none")
	{
		yield return 0;
	}

	float positionDistance = Vector3.Distance (endPosition, this.InitialTargetPosition);
	float objectDistance = Vector3.Distance (endPosition, graspable.transform.position);
	while (objectDistance > positionDistance * .66f && this.LastErrorCode == "none")
	{
		objectDistance = Vector3.Distance (endPosition, graspable.transform.position);
		yield return 0;
	}

	if (SampleTrialController.Verbose) UnityEngine.Debug.Log("object grasped, distance threshold met, start stimulation: " + this.LastErrorCode);

	if (this.LastErrorCode == "none")
	{
		this.CurrentTrialData.StimulationOnset = SampleTrialController.Millis - this.CurrentTrialData.FixationOffset;
		StartCoroutine(this.Stimulate());
	}

	yield return null;
  }

  private IEnumerator Stimulate(bool visualStimulation = true)
  {
    if (this.CurrentTrialData.Congruency == "both left")
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("congruency condition: both left...");
      if (visualStimulation)
      {
		  if (this.CurrentTrialData.StimulationCondition == "-150MS" || this.CurrentTrialData.StimulationCondition == "LiftOff")
          {
            this.leftLightFake.GetComponent<ParticleSystem>().Play();
          }
          else
          {
            this.leftLight.GetComponent<ParticleSystem>().Play();
          }
      }
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
    else if (this.CurrentTrialData.Congruency == "both right")
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("congruency condition: both right");
      if (visualStimulation)
      {
		  if (this.CurrentTrialData.StimulationCondition == "-150MS" || this.CurrentTrialData.StimulationCondition == "LiftOff")
          {
              this.rightLightFake.GetComponent<ParticleSystem>().Play();
          }
          else
          {
              this.rightLight.GetComponent<ParticleSystem>().Play();
          }
      }
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
    else if (this.CurrentTrialData.Congruency == "light left - tactile index")
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("congruency condition: light left - tactile index");
      if (visualStimulation)
      {
		  if (this.CurrentTrialData.StimulationCondition == "-150MS" || this.CurrentTrialData.StimulationCondition == "LiftOff")
          {
              this.leftLightFake.GetComponent<ParticleSystem>().Play(); 
          }
          else
          {
              this.leftLight.GetComponent<ParticleSystem>().Play(); 
          }
      }
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
    else if (this.CurrentTrialData.Congruency == "light right - tactile thumb")
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("congruency condition: light right - tactile thumb");
      if (visualStimulation)
      {
		  if (this.CurrentTrialData.StimulationCondition == "-150MS" || this.CurrentTrialData.StimulationCondition == "LiftOff")
          {
              this.rightLightFake.GetComponent<ParticleSystem>().Play();
          }
          else
          {
              this.rightLight.GetComponent<ParticleSystem>().Play();
          }
      }
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
    else if (this.CurrentTrialData.Congruency == "light index - tactile index")
    {
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("congruency condition: light index - tactile index");
        if (visualStimulation)
        {
            this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateIndexFinger();
        }
        if (VibroTactileStimulation)
        {
            // index
            this.vibroTactileStimulationInterface.sendData(0, SampleTrialController.TactileStimulationStrength, 0, 0, 0);
        }
        StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentTrialData.Congruency == "light thumb - tactile thumb")
    {
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("congruency condition: light thumb - tactile thumb");
        if (visualStimulation)
        {
            this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateThumb();
        }
        if (VibroTactileStimulation)
        {
            // thumb
            this.vibroTactileStimulationInterface.sendData(0, 0, SampleTrialController.TactileStimulationStrength, 0, 0);
        }
        StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentTrialData.Congruency == "light index - tactile thumb")
    {
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("congruency condition: light index - tactile thumb");
        if (visualStimulation)
        {
            this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateIndexFinger();
        }
        if (VibroTactileStimulation)
        {
            // thumb
            this.vibroTactileStimulationInterface.sendData(0, 0, SampleTrialController.TactileStimulationStrength, 0, 0);
        }
        StartCoroutine(this.StopStimulation());
    }
    else if (this.CurrentTrialData.Congruency == "light thumb - tactile index")
    {
        if (SampleTrialController.Verbose) UnityEngine.Debug.Log("congruency condition: light thumb - tactile index");
        if (visualStimulation)
        {
            this.HandControllerReference.getCurrentHandModel().GetComponent<StimulationProxy>().stimulateThumb();
        }
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

    if (SampleTrialController.Verbose) UnityEngine.Debug.Log("timeout done, stop stimulation");

    this.leftLight.GetComponent<ParticleSystem>().Stop();
    this.leftLight.GetComponent<ParticleSystem>().Clear();
    this.rightLight.GetComponent<ParticleSystem>().Stop();
    this.rightLight.GetComponent<ParticleSystem>().Clear();

    this.leftLightFake.GetComponent<ParticleSystem>().Stop();
    this.leftLightFake.GetComponent<ParticleSystem>().Clear();
    this.rightLightFake.GetComponent<ParticleSystem>().Stop();
    this.rightLightFake.GetComponent<ParticleSystem>().Clear();

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
    // nope, we use this variable for the relative verbal response time...
    //this.CurrentTrialData.StimulusResponseIntervalTime = SampleTrialController.Millis - this.CurrentTrialData.FixationOffset;

    yield return null;
  }

  private void cancelTrial()
  {
    this.RangeCheck.clearMonitor();
    this.deactivateTarget();
    this.Target.transform.position = new Vector3(0, -100, 0);
    this.Target.transform.Rotate(new Vector3(0f, 0f, 0f));
    this.FixationObject.transform.position = new Vector3(0, -10, 0);

    switch (this.InteractionState)
    {
      case InteractionStates.NONE:
        this.FeedbackDisplay.text = "Die Hand muss im Sensorbereich bleiben.";
        this.LastErrorCode = "error";
        break;
      case SampleTrialController.InteractionStates.LEFT_POSITION_DURING_FIXATION:
        this.FeedbackDisplay.text = "Bitte bewege deine Hand erst, wenn das Objekt erscheint.";
        this.LastErrorCode = "early_start";
        break;
      case SampleTrialController.InteractionStates.TARGET_DESTROYED:
        this.FeedbackDisplay.text = "Bitte mach die Flasche nicht kaputt.";
        this.LastErrorCode = "target_destroyed";
        break;
      case SampleTrialController.InteractionStates.WRONG_ORIENTATION:
        this.FeedbackDisplay.text = "Bitte stell die Flasche richtig herum ab.";
        this.LastErrorCode = "target_wrong_orientation";
        break;
      case InteractionStates.OUT_OF_BOUNDS:
        this.FeedbackDisplay.text = "Objekt außer Reichweite.";
        this.LastErrorCode = "out_of_bound";
        break;
      case InteractionStates.TIME_OUT:
        this.FeedbackDisplay.text = "Bitte greife schneller nach der Flasche.";
        this.LastErrorCode = "time_out_grasp";
        break;
      case InteractionStates.VERBAL_TIME_OUT:
        this.FeedbackDisplay.text = "Bitte antworte schneller.";
        this.LastErrorCode = "time_out_response";
        break;
      case InteractionStates.VERBAL_WRONG_RESPONSE:
        this.FeedbackDisplay.text = "Es war leider der andere Finger.";
        this.LastErrorCode = "wrong_verbal_response";
        break;
    }

    this.CurrentTrialData.ErrorCode = this.LastErrorCode;
    this.HandControllerReference.recorder_.Stop();
    this.HandControllerReference.recorder_.Reset();
    this.messageObtained = false;
  }

  private void rangeCheckHandler(GameObject checkedObject)
  {
    if (this.CurrentTrialData == null)
    {
      return;
    }

    if (SampleTrialController.Verbose) UnityEngine.Debug.Log(checkedObject.name + " left bounds...");
    this.InteractionState = InteractionStates.OUT_OF_BOUNDS;
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
    // just paranoia
    this.leftLight.GetComponent<ParticleSystem>().Stop();
    this.leftLight.GetComponent<ParticleSystem>().Clear();
    this.rightLight.GetComponent<ParticleSystem>().Stop();
    this.rightLight.GetComponent<ParticleSystem>().Clear();

    this.FakeTarget.transform.position = new Vector3(0, -200, 100);
    this.leftLightFake.GetComponent<ParticleSystem>().Stop();
    this.leftLightFake.GetComponent<ParticleSystem>().Clear();
    this.rightLightFake.GetComponent<ParticleSystem>().Stop();
    this.rightLightFake.GetComponent<ParticleSystem>().Clear();
  }

  private void checkTargetState()
  {
    GraspableObject graspable = this.Target.GetComponent<GraspableObject>();
    // update trial data container
    if (this.CurrentTrialData != null)
    {
      if (graspable.HasBeenGrasped())
      {
        this.CurrentTrialData.ObjectContact = graspable.GetResponseTime() - this.CurrentTrialData.FixationOffset;
        this.CurrentTrialData.graspDirection = graspable.getGraspDirection();
      }
    }
  }

  private void checkResponseIntervalTime()
  {
    long deltaTime = SampleTrialController.Millis - this.ResponseIntervalStart;
    //if possible interaction time has passed and there was no interaction or if possible speech response time is over, set InteractionState to TIME_OUT
    if (deltaTime >= SampleTrialController.ResponseIntervalLength && (!this.Target.GetComponent<GraspableObject>().HasBeenGrasped()))
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("trial time out...");
      this.InteractionState = InteractionStates.TIME_OUT;
    }
  }

  private void checkVerbalResponseIntervalTime()
  {
    if (this.CurrentTrialData.StimulationOnset == -1L)
    {
      return;
    }

    long deltaTime = SampleTrialController.Millis - (this.CurrentTrialData.FixationOffset + this.CurrentTrialData.StimulationOnset);
    // allow grasping before speech...
    if ((deltaTime >= SampleTrialController.VerbalResponseIntervalLength && this.messageObtained == false))
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("verbal trial time out: " + deltaTime + " (" + (this.CurrentTrialData.FixationOffset + this.CurrentTrialData.StimulationOnset) + ") " + "...");
      this.InteractionState = InteractionStates.VERBAL_TIME_OUT;
    }
    /*
    // TODO: shall we add the hard check again?
    else if (this.CurrentTrialData.ObjectContact != -1L)
    {
      if (deltaTime >= this.CurrentTrialData.ObjectContact + 500L)
      {
        if (SampleTrialController.Verbose) UnityEngine.Debug.LogError("vocal reponse time out after object contact...");
        this.InteractionState = InteractionStates.VERBAL_TIME_OUT;
      }
    }
    */
  }

  private IEnumerator waitForVerbalResponse()
  {
      bool done = false;
      // happens only if object interaction is done, so lets remove the target
      this.deactivateTarget();
      // clear monitor
      this.RangeCheck.clearMonitor();

      while(!done)
      {
          this.checkWordRecognition();
          if (this.messageObtained)
          {
              break;
          }

          if (this.CurrentTrialState.TrialState != TrialStates.RESPONSE)
          {
              break;
          }

          long deltaTime = SampleTrialController.Millis - (this.CurrentTrialData.FixationOffset + this.CurrentTrialData.StimulationOnset);
          // allow grasping before speech...
          if ((deltaTime >= SampleTrialController.VerbalResponseIntervalLength && this.messageObtained == false))
          {
              done = true;
          }
          yield return 0;
      }
      // hand might be lost, or something like this
      if (this.CurrentTrialState.TrialState == TrialStates.RESPONSE) this.TrialUpdate();
      yield return null;
  }

  private void checkWordRecognition()
  {
  }

  private void checkTargetPosition(GameObject gameObject, bool validOrientation)
  {
    if (this.InteractionState == InteractionStates.NONE && this.CurrentTrialState.TrialState == TrialStates.RESPONSE)
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("target in position...");
      this.InteractionState = validOrientation ? InteractionStates.IN_BOX : InteractionStates.WRONG_ORIENTATION;
      this.CurrentTrialData.ReleaseTime = SampleTrialController.Millis - this.CurrentTrialData.FixationOffset;
    }
  }

  private void checkTargetDestruction(GameObject gameObject)
  {
    if (this.InteractionState == InteractionStates.NONE && this.CurrentTrialState.TrialState == TrialStates.RESPONSE)
    {
      if (SampleTrialController.Verbose) UnityEngine.Debug.Log("target destroyed...");
      this.InteractionState = InteractionStates.TARGET_DESTROYED;
      this.CurrentTrialData.ReleaseTime = SampleTrialController.Millis - this.CurrentTrialData.FixationOffset;
    }
  }

  public void HandleSlider()
  {
      if (this.CurrentTrialState.TrialState == TrialStates.WELCOME)
      {
          this.CurrentTrialState.TrialState = TrialStates.GRASP_TRAINING_INIT;
          //this.CurrentTrialState.TrialState = TrialStates.SPEECH_TRAINING;
          this.TrialUpdate();
      }
      else if (this.CurrentTrialState.TrialState == TrialStates.GRASP_TRAINING_INIT)
      {
          this.CurrentTrialState.TrialState = TrialStates.GRASP_TRAINING;
          //this.CurrentTrialState.TrialState = TrialStates.SPEECH_TRAINING;
          this.TrialUpdate();
      }
      else if (this.CurrentTrialState.TrialState == TrialStates.GRASP_TRAINING)
      {
          this.MainInstruction.text = "";
          this.MainInstructionBackground.enabled = false;
          this.CurrentTrialState.TrialState = TrialStates.GRASP_TRAINING_COMMENCING;
          GraspTrainingController.IsActive = true;
      }
      else if (this.CurrentTrialState.TrialState == TrialStates.GRASP_TRAINING_COMMENCING)
      {
          GraspTrainingController.IsActive = false;
          GameObject.Destroy(this.GraspTrainingController);
          this.CurrentTrialState.TrialState = TrialStates.SPEECH_TRAINING;
          this.TrialUpdate();
      }
      else if (this.CurrentTrialState.TrialState == TrialStates.SPEECH_TRAINING)
      {
          this.MainInstruction.text = "";
          this.MainInstructionBackground.enabled = false;
          this.CurrentTrialState.TrialState = TrialStates.SPEECH_TRAINING_COMMENCING;
          SpeechTrainingController.IsActive = true;
      }
      else if (this.CurrentTrialState.TrialState == TrialStates.SPEECH_TRAINING_COMMENCING)
      {
          SpeechTrainingController.IsActive = false;
          GameObject.Destroy(this.SpeechTrainingController);
          this.CurrentTrialState.TrialState = TrialStates.TRAINING_DONE;
          this.TrialUpdate();
      }
      else if (this.CurrentTrialState.TrialState == TrialStates.TRAINING_DONE)
      {
          //this.CurrentTrialState.TrialState = TrialStates.STARTUP;
          this.MainInstruction.text = "";
          //this.FeedbackDisplay.text = "";
          this.MainInstructionBackground.enabled = false;
          //this.TrialUpdate();

          this.FeedbackDisplay.text = (this.blockList.Count > 0 ? "Jetzt kommt der erste Block. " : "Jetzt kommt der nächste Block. ") + (this.currentBlock.EndsWith("consistent") ? "Das Mapping ist konsistent." : "Das Mapping ist variabel.");

          this.CurrentTrialState.TrialState = TrialStates.STARTUP;
          StartCoroutine(this.trialUpdateTimeout(30.0f));
      }
  }

  public void GraspTrainingTrialDone()
  {
    //this.ContinueButton.SetActive(true);
    //this.RepeatButton.SetActive(true);
    this.GraspTrainingController.ExternalTrialStart();
  }

  public void GraspTrainingTrialStart()
  {
      //this.ContinueButton.SetActive(false);
  }
   
  public static void ShuffleList<T>(List<T> list)
  {
    int n = list.Count;
    while (n > 1)
    {
      n--;
      int k = SampleTrialController.RandomNumberGenerator.Next(n + 1);
      T value = list[k];
      list[k] = list[n];
      list[n] = value;
    }
  }
}
