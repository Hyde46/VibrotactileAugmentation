using UnityEngine;
using System;
using System.Collections;
/// <summary>
/// This script handles the grasp and carry interaction; it runs the checks for the initial hand position,
/// displays the bottle and monitors the interaction
/// </summary>
public class GraspStateController : MonoBehaviour {
    // set to true to obtain some diagnostic command line output
    private static bool Verbose = true;
    // the controller is some kind of state-machine that uses these
    // values to control the interaction accordingly
    private enum InteractionStage
    {
        STARTUP,
        TARGET_PRESENTATION,
        GRASPING_INTERACTION,
        WAITING
    }

    private class InteractionStateObject
    {
        // this class can be used as data container to store time-stamps and
        // stuff like this, here, only the state is relevant
        public GraspStateController.InteractionStage InteractionStage;
    }
    // how long the feedback will be shown
    public float FeedbackIntervalInSeconds = 1.0f;
    // text element to present feedback
    public UnityEngine.UI.Text FeedbackDisplay;
    // the to-be-grasped object
    public GameObject Target;
    // jsut some generic response text (this interaction was used in one experiment)
    private string[] positiveFeedbackTemplates = new string[]
    {
    "Sehr gut",
    "Super",
    "Gut gemacht"
    };
    
    //actual hand controller that handles the Leap data;
    public SimplifiedHandController HandControllerReference;
    // current state in the sequence
    private InteractionStateObject CurrentInteractionState;
    // you can change this to 'upsidedown'
    private string bottleOrientationMode = "upright";
    // the range monitor, used to detect whether the object left the interaction space
    private EffectorRangeCheck RangeCheck;
    
    // becomes true in the first update, allows a 'late' start so to say
    public bool Started = false;
    public static bool IsActive = false;

    // position where the target will always appear
    public Vector3 InitialTargetPosition;
    // id of the interacting hand, in case of online data this can be used to check whether the
    // sensor lost the hand in between
    private int CurrentHandID;

    // reference to the pose controller (displays the red spheres) and some control variables
    private PoseController HandPoseController;
    private static long InitialPoseDuration = 500L;
    private long PoseCheckTimeStamp;
    // just to control if and when the interaction state is checked (see the Update method)
    private bool CheckInteractionState;
    // the meta-controller, it is notified when the interaction is finished
    public Interactioncontroller master;
    // current state within the sequence
    public Interactioncontroller.InteractionStates InteractionState;
    // delegate to invoke the InteractionStateUpdate method
    private Action InteractionStateUpdateDelegate;

    void Awake()
    {
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
                containerController.ObjectWasReleased += this.checkTargetPosition;
                containerController.ObjectWasDestroyed += this.checkTargetDestruction;
            }
        }

        this.Target.SetActive(true);
        this.Target.transform.position = new Vector3(0, -100, 100);
        this.Target.GetComponent<Rigidbody>().isKinematic = true;
        
        this.InteractionStateUpdateDelegate = delegate () { this.InteractionStateUpdate(); };

        //setup interaction control
        this.CurrentInteractionState = new InteractionStateObject();
        this.CurrentInteractionState.InteractionStage = InteractionStage.STARTUP;

        if (GraspStateController.Verbose) UnityEngine.Debug.Log("done with awaking...");
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
                containerController.ObjectWasReleased -= this.checkTargetPosition;
                containerController.ObjectWasDestroyed -= this.checkTargetDestruction;
            }
        }

        this.RangeCheck.ObjectLeftBounds -= this.rangeCheckHandler;
    }

    private void checkInitialPose()
    {

        if (GraspStateController.Verbose) UnityEngine.Debug.Log("call to check intial pose...");
        this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.INITIALIZE);
        bool valid = this.HandPoseController.checkInitialPose();
        if (GraspStateController.Verbose) UnityEngine.Debug.Log("valid pose: " + valid + "...");
        
        if (valid)
        {
            if (GraspStateController.Verbose) UnityEngine.Debug.Log("pose check time stamp: " + this.PoseCheckTimeStamp);
            if (this.PoseCheckTimeStamp == -1L)
            {
                this.PoseCheckTimeStamp = ((long)(Time.realtimeSinceStartup * 1000.0f));
            }
            else if (((long)(Time.realtimeSinceStartup * 1000.0f)) - this.PoseCheckTimeStamp >= GraspStateController.InitialPoseDuration)
            {
                if (GraspStateController.Verbose) UnityEngine.Debug.Log("check initial pose...");
                this.CurrentHandID = this.HandControllerReference.currentRightHandID;
                this.CurrentInteractionState.InteractionStage = InteractionStage.TARGET_PRESENTATION;
                this.InteractionStateUpdate();
            }
        }
        else
        {
            this.PoseCheckTimeStamp = -1L;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!this.Started && GraspStateController.IsActive)
        {
            this.Started = true;
            this.PoseCheckTimeStamp = -1L;
            this.CurrentInteractionState.InteractionStage = InteractionStage.STARTUP;
            this.InteractionStateUpdate();
        }
        // check pose
        if (this.Started && GraspStateController.IsActive && this.CurrentInteractionState.InteractionStage == InteractionStage.STARTUP)
        {
            this.checkInitialPose();
        }

        // target presentation check
        if (this.Started && GraspStateController.IsActive && this.CurrentInteractionState.InteractionStage == InteractionStage.TARGET_PRESENTATION)
        {
            if (GraspStateController.Verbose) UnityEngine.Debug.Log("target presentation check...");
            if (!this.HandPoseController.checkInitialPose())
            {
                this.cancelAndResetInteractionSequence();
            }
        }

        //check hand during trajectory
        if (this.Started && GraspStateController.IsActive && this.CurrentInteractionState.InteractionStage != InteractionStage.STARTUP)
        {
            // cancel interaction
            if (this.CurrentHandID != this.HandControllerReference.currentRightHandID && this.CurrentInteractionState.InteractionStage == InteractionStage.GRASPING_INTERACTION)
            {
                this.cancelAndResetInteractionSequence();
            }
        }

        //checkInteractionState = true after fixation
        if (this.Started && GraspStateController.IsActive && this.CheckInteractionState)
        {

            if (this.InteractionState != Interactioncontroller.InteractionStates.NONE)
            {
                this.InteractionStateUpdate();
                // just paranoia
                this.CheckInteractionState = false;
            }
        }
    }

    public void cancelAndResetInteractionSequence()
    {
        // try to stop coroutine
        StopCoroutine("CoroutineTimer.Start");
        // if target has been assigned, reset it
        this.cancelInteractionSequence();
        // reset interaction state...
        if (GraspStateController.Verbose) UnityEngine.Debug.Log("interaction canceled");
        this.CurrentInteractionState.InteractionStage = InteractionStage.WAITING;
        StartCoroutine(this.FeedbackInterval());
    }

    private IEnumerator FeedbackInterval()
    {
        yield return new WaitForSeconds(this.FeedbackIntervalInSeconds);
        this.FeedbackDisplay.text = "";
        this.master.GraspInteractionDone();

        yield return null;
    }

    public void ExternalInteractionStart()
    {
        this.CurrentInteractionState.InteractionStage = InteractionStage.STARTUP;
        this.InteractionStateUpdate();
    }
    /// <summary>
    /// this is the state machine that controls the interaction
    /// </summary>
    public void InteractionStateUpdate()
    {

        if (!GraspStateController.IsActive)
        {
            this.Started = false;
            return;
        }

        if (GraspStateController.Verbose) UnityEngine.Debug.Log("timer called, in state = " + CurrentInteractionState.InteractionStage.ToString() + "...");
        switch (this.CurrentInteractionState.InteractionStage)
        {
            case InteractionStage.STARTUP:
                if (GraspStateController.Verbose) UnityEngine.Debug.Log("startup...");

                this.HandPoseController.resetInitialPoseChecks();
                this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.CHECK);
                this.FeedbackDisplay.text = "";
                
                if (GraspStateController.Verbose) UnityEngine.Debug.Log("startup done...");
                break;
            case InteractionStage.TARGET_PRESENTATION:
                // just paranoia, it is highly unlikely that something like this happens
                if (this.Target.GetComponent<GraspableObject>().IsGrabbed())
                {
                    GraspController[] hands = GameObject.FindObjectsOfType<GraspController>();

                    foreach (GraspController hand in hands)
                    {
                        hand.requestRelease();
                    }
                }
                // disable the pose check
                this.HandPoseController.setControllerMode(PoseController.PoseControllerMode.IDLE);
                GraspableObject graspable = this.Target.GetComponent<GraspableObject>();
                graspable.ResetVariables();
                // TODO: display the bottle either upright or upside down, depending on the
                // bottleOrientationMode string, you can use the InitialTargetPosition variable
                // and the ResetPositionAndOrientation of the Graspable script

                // TODO: enable the physics of the bottle

                if (this.bottleOrientationMode == "upright")
                {
                    graspable.ResetPositionAndOrientation(0.0f, this.InitialTargetPosition);
                    if (GraspStateController.Verbose) Debug.Log("upright target...");
                }
                else if (this.bottleOrientationMode == "upsidedown")
                {
                    graspable.ResetPositionAndOrientation(180.0f, this.InitialTargetPosition);
                    if (GraspStateController.Verbose) Debug.Log("rotated target...");
                }

                this.Target.GetComponent<Rigidbody>().isKinematic = false;
                this.Target.GetComponent<Rigidbody>().useGravity = true;

                this.CurrentInteractionState.InteractionStage = InteractionStage.GRASPING_INTERACTION;
                // enable range check
                this.InteractionState = Interactioncontroller.InteractionStates.NONE;
                this.CheckInteractionState = true;
                this.RangeCheck.clearMonitor();
                // TODO: add the target to the monitor, have a look at the EffectorRangeCheck script
                this.RangeCheck.monitorObject(this.Target);


                if (Verbose) Debug.Log("current interaction state: " + CurrentInteractionState.InteractionStage);
                break;

            case InteractionStage.GRASPING_INTERACTION:
                if (GraspStateController.Verbose) Debug.Log("response state...");
                this.CheckInteractionState = false;

                if (this.InteractionState != Interactioncontroller.InteractionStates.IN_BOX)
                {
                    this.cancelInteractionSequence();
                }
                else
                {
                    this.FeedbackDisplay.text = this.positiveFeedbackTemplates[UnityEngine.Random.Range(0, this.positiveFeedbackTemplates.Length - 1)];

                    this.deactivateTarget();
                    this.InteractionState = Interactioncontroller.InteractionStates.NONE;
                    // clear monitor
                    this.RangeCheck.clearMonitor();

                    this.PoseCheckTimeStamp = -1L;
                }
                this.CurrentInteractionState.InteractionStage = InteractionStage.WAITING;
                StartCoroutine(this.FeedbackInterval());

                break;
        }
    }

    private void cancelInteractionSequence()
    {
        // TODO: this method cancels the interaction if something strage happens.
        // please:
        // 1. clear the range check
        // 2. deactivate and reset the target
        // 3. display a message depending on the InteractionState variable; you can
        // use the FeedbackDisplay.text textfield

        this.RangeCheck.clearMonitor();
        this.deactivateTarget();
        this.Target.transform.position = new Vector3(0, -100, 100);
        this.Target.transform.rotation = Quaternion.identity;

        switch (this.InteractionState)
        {
            case Interactioncontroller.InteractionStates.NONE:
                this.FeedbackDisplay.text = "Die Hand muss im Sensorbereich bleiben.";
                break;
            case Interactioncontroller.InteractionStates.TARGET_DESTROYED:
                this.FeedbackDisplay.text = "Bitte mach die Flasche nicht kaputt.";
                break;
            case Interactioncontroller.InteractionStates.WRONG_ORIENTATION:
                this.FeedbackDisplay.text = "Bitte stell die Flasche richtig herum ab.";
                break;
            case Interactioncontroller.InteractionStates.OUT_OF_BOUNDS:
                this.FeedbackDisplay.text = "Objekt außer Reichweite.";
                break;
        }
    }

    private void rangeCheckHandler(GameObject checkedObject)
    {
        if (GraspStateController.Verbose) UnityEngine.Debug.Log(checkedObject.name + " left bounds...");
        this.InteractionState = Interactioncontroller.InteractionStates.OUT_OF_BOUNDS;
        this.cancelAndResetInteractionSequence();
    }

    private void deactivateTarget()
    {
        this.Target.GetComponent<Rigidbody>().useGravity = false;
        this.Target.GetComponent<Rigidbody>().isKinematic = true;
        this.Target.transform.position = new Vector3(0, -100, 100);
        this.Target.transform.rotation = Quaternion.identity;
        if (this.Target.GetComponent<GraspableObject>().BreakableJoint != null)
        {
            Joint joint = this.Target.GetComponent<GraspableObject>().BreakableJoint.GetComponent<Joint>();
            if (joint != null)
            {
                joint.connectedBody = null;
            }
        }
    }

    private void checkTargetPosition(GameObject gameObject, bool validOrientation)
    {
        if (!GraspStateController.IsActive)
        {
            return;
        }

        if (this.InteractionState == Interactioncontroller.InteractionStates.NONE && this.CurrentInteractionState.InteractionStage == InteractionStage.GRASPING_INTERACTION)
        {
            if (GraspStateController.Verbose) UnityEngine.Debug.Log("target in position...");
            this.InteractionState = validOrientation ? Interactioncontroller.InteractionStates.IN_BOX : Interactioncontroller.InteractionStates.WRONG_ORIENTATION;
        }
    }

    private void checkTargetDestruction(GameObject gameObject)
    {
        if (!GraspStateController.IsActive)
        {
            return;
        }

        if (this.InteractionState == Interactioncontroller.InteractionStates.NONE && this.CurrentInteractionState.InteractionStage == InteractionStage.GRASPING_INTERACTION)
        {
            if (GraspStateController.Verbose) UnityEngine.Debug.Log("target destroyed...");
            this.InteractionState = Interactioncontroller.InteractionStates.TARGET_DESTROYED;
        }
    }
}