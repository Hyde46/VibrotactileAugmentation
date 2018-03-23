using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Leap;

public class SimplifiedHandController : HandController {
    public float GraspDistanceScale = 1.0f;

    // reference for the block controller, if the hand id changes within the trial, the
    // respective hand has been lost and the trial is canceled accordingly
    public int currentRightHandID = -1;

    // with this object, we can replay recorded leap data
    protected LeapRecorder LeapReplay;
    // if true, we will replay the data from the recordingAsset instead of
    // using live data from the sensor
    private bool replay;

    protected Dictionary<int, HandModel> GraphicHandsDictionary;
    protected Dictionary<int, HandModel> PhysicHandsDictionary;

    private long PreviousGraphicsID = 0;
    private long PreviousPhysicsID = 0;
    
    public static string baseRecordPath = Path.Combine(Directory.GetCurrentDirectory(), "Records/");

    /** Creates a new Leap Controller object. */
    void Awake()
    {
        leap_controller_ = new Controller();

        // Optimize for top-down tracking if on head mounted display.
        Controller.PolicyFlag policy_flags = leap_controller_.PolicyFlags;
        if (isHeadMounted)
            policy_flags |= Controller.PolicyFlag.POLICY_OPTIMIZE_HMD;
        else
            policy_flags &= ~Controller.PolicyFlag.POLICY_OPTIMIZE_HMD;

        leap_controller_.SetPolicyFlags(policy_flags);
    }

    /** Initalizes the hand and tool lists and recording, if enabled.*/
    void Start()
    {
        // Initialize hand lookup tables.
        GraphicHandsDictionary = new Dictionary<int, HandModel>();
        PhysicHandsDictionary = new Dictionary<int, HandModel>();

        if (leap_controller_ == null)
        {
            Debug.LogWarning("Cannot connect to controller. Make sure you have Leap Motion v2.0+ installed");
        }

        if (this.recordingAsset != null)
        {
            this.LeapReplay = new LeapRecorder();
            this.LeapReplay.Load(this.recordingAsset);
            this.replay = true;
        }
    }

    new public void IgnoreCollisionsWithHands(GameObject to_ignore, bool ignore = true)
    {
        foreach (HandModel hand in PhysicHandsDictionary.Values)
            Utils.IgnoreCollisions(hand.gameObject, to_ignore, ignore);
    }

    new protected HandModel CreateHand(HandModel model)
    {
        HandModel hand_model = Instantiate(model, transform.position, transform.rotation)
           as HandModel;
        hand_model.gameObject.SetActive(true);

        ParticleSystem[] particleSystems = hand_model.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem system in particleSystems)
        {
            system.Stop();
        }

        Utils.IgnoreCollisions(hand_model.gameObject, gameObject);

        // add scripts...
        foreach (FingerModel finger in hand_model.fingers)
        {
            if (finger.GetType() == typeof(RigidFinger))
            {
                RigidFinger rigidFinger = finger as RigidFinger;
                foreach (Transform bone in rigidFinger.bones)
                {
                    if (bone == null)
                        continue;
                    GameObject bGameObject = bone.gameObject;
                    Rigidbody bRigidBody = bGameObject.gameObject.GetComponent<Rigidbody>() as Rigidbody;
                    bRigidBody.isKinematic = false;
                    bRigidBody.useGravity = false;
                    bRigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }

                rigidFinger.bones[rigidFinger.bones.Length - 1].gameObject.tag = "FingerTip";
            }
        }



        Collider[] allCls = hand_model.GetComponentsInChildren<Collider>();
        foreach (Collider lcl in allCls)
        {
            foreach (Collider refCl in allCls)
            {
                if (!lcl.Equals(refCl))
                {
                    //Physics.IgnoreCollision(lcl, refCl, true);
                }
            }
        }

        return hand_model;
    }

    new protected void DestroyHand(HandModel hand_model)
    {
        if (destroyHands)
            Destroy(hand_model.gameObject);
        else
            hand_model.SetLeapHand(null);
    }

    new protected void UpdateHandModels(Dictionary<int, HandModel> all_hands,
                          HandList leap_hands,
                          HandModel left_model, HandModel right_model)
    {
        List<int> ids_to_check = new List<int>(all_hands.Keys);

        // Go through all the active hands and update them.
        int num_hands = leap_hands.Count;
        for (int h = 0; h < num_hands; ++h)
        {
            Hand leap_hand = leap_hands[h];

            HandModel model = (mirrorZAxis != leap_hand.IsLeft) ? left_model : right_model;

            // hmm, quite stupid way to keep track of the right hand ID...
            if (leap_hand.IsRight && !(right_model.GetType() == typeof(RigidHand)))
                this.currentRightHandID = leap_hand.Id;

            // If we've mirrored since this hand was updated, destroy it.
            if (all_hands.ContainsKey(leap_hand.Id) &&
                all_hands[leap_hand.Id].IsMirrored() != mirrorZAxis)
            {
                DestroyHand(all_hands[leap_hand.Id]);
                all_hands.Remove(leap_hand.Id);
            }

            // Only create or update if the hand is enabled.
            if (model != null)
            {
                ids_to_check.Remove(leap_hand.Id);

                // Create the hand and initialized it if it doesn't exist yet.
                if (!all_hands.ContainsKey(leap_hand.Id))
                {
                    HandModel new_hand = CreateHand(model);
                    new_hand.SetLeapHand(leap_hand);
                    new_hand.MirrorZAxis(mirrorZAxis);
                    new_hand.SetController(this);

                    // Set scaling based on reference hand.
                    float hand_scale = MM_TO_M * leap_hand.PalmWidth / new_hand.handModelPalmWidth;
                    new_hand.transform.localScale = hand_scale * transform.lossyScale;

                    new_hand.InitHand();
                    new_hand.UpdateHand();
                    all_hands[leap_hand.Id] = new_hand;
                }
                else
                {
                    // Make sure we update the Leap Hand reference.
                    HandModel hand_model = all_hands[leap_hand.Id];
                    hand_model.SetLeapHand(leap_hand);
                    hand_model.MirrorZAxis(mirrorZAxis);

                    // Set scaling based on reference hand.
                    float hand_scale = MM_TO_M * leap_hand.PalmWidth / hand_model.handModelPalmWidth;
                    hand_model.transform.localScale = hand_scale * transform.lossyScale;
                    hand_model.UpdateHand();
                }
                //Debug.Log(all_hands[leap_hand.Id].fingers[0].GetTipPosition());
            }
        }

        // Destroy all hands with defunct IDs.
        for (int i = 0; i < ids_to_check.Count; ++i)
        {
            DestroyHand(all_hands[ids_to_check[i]]);
            all_hands.Remove(ids_to_check[i]);
            if (ids_to_check[i] == this.currentRightHandID)
            {
                this.currentRightHandID = -1;
            }
        }
    }

    public virtual Frame GetNextFrame()
    {
        if (this.replay && this.LeapReplay.state == RecorderState.Playing)
        {
            return this.LeapReplay.GetCurrentFrame();
        }
        else
        {
            return leap_controller_.Frame();
        }
    }

    void Update()
    {
        if (leap_controller_ == null)
            return;

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (this.replay)
            {
                if (this.LeapReplay.state == RecorderState.Playing)
                {
                    this.LeapReplay.Pause();
                }
                else
                {
                    this.LeapReplay.Play();
                }
            }
        }
        
        Frame frame = this.GetNextFrame();
        if (GraphicHandsDictionary == null)
        {
            Debug.Log("no hand graphics lookup table...");
        }
        else if (leftGraphicsModel == null)
        {
            Debug.Log("no left hand graphics...");
        }
        else if (rightGraphicsModel == null)
        {
            Debug.Log("no tight hand graphics...");
        }
        else if (frame == null)
        {
            Debug.Log("no frame...");
        }

        if (frame.Id != PreviousGraphicsID)
        {
            UpdateHandModels(GraphicHandsDictionary, frame.Hands, leftGraphicsModel, rightGraphicsModel);
            PreviousGraphicsID = frame.Id;
        }

        if (frame.Id != PreviousPhysicsID)
        {
            UpdateHandModels(PhysicHandsDictionary, frame.Hands, leftPhysicsModel, rightPhysicsModel);
            PreviousPhysicsID = frame.Id;
        }

        if (this.replay)
        {
            this.LeapReplay.NextFrame();
        }
    }

    /** Updates the physics objects */

    void FixedUpdate()
    {
        if (leap_controller_ == null)
            return;

        Frame frame = this.GetNextFrame();
        if (frame.Id != PreviousPhysicsID)
        {
            UpdateHandModels(PhysicHandsDictionary, frame.Hands, leftPhysicsModel, rightPhysicsModel);
            PreviousPhysicsID = frame.Id;

        }

    }

    public HandModel getCurrentHandModel()
    {
        if (this.GraphicHandsDictionary.ContainsKey(this.currentRightHandID))
        {
            return this.GraphicHandsDictionary[this.currentRightHandID];
        }
        else
        {
            return null;
        }
    }

    public HandModel getCurrentHandPhysicsModel()
    {
        if (this.PhysicHandsDictionary.ContainsKey(this.currentRightHandID))
        {
            return this.PhysicHandsDictionary[this.currentRightHandID];
        }
        else
        {
            return null;
        }
    }
}
