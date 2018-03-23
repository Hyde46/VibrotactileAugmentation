/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Leap;

// Overall Controller object that will instantiate hands when they appear.
public class ExperimentHandController : HandController
{
  public bool ExperimentMode = false;
  [HideInInspector]
  public bool enableFingerParticleSystems = true; // true in dummy mode to fake the stimulation

  public float GraspDistanceScale = 1.0f;

  public VisualHandOffsetController OffsetController;

  // reference for the block controller, if the hand id changes within the trial, the
  // respective hand has been lost and the trial is canceled accordingly
  public int currentRightHandID = -1;
  private Vector3 currentRelevantPalmPosition;
  private Vector3 currentRelevantIndexPosition;
  private Vector3 currentRelevantThumbPosition;

  public GameObject IndexStimulationProxyPrefab;
  public GameObject ThumbStimulationProxyPrefab;

  protected Dictionary<int, HandModel> GraphicHandsDictionary;
  protected Dictionary<int, HandModel> PhysicHandsDictionary;

  private long PreviousGraphicsID = 0;
  private long PreviousPhysicsID = 0;
  // these parameters control the distance dependent z - Drift
  /*
  public bool ApplyZOffset = false;
  public float StartZOffset;
  public float EndZOffset;
  public float MaxZOffset;
  [HideInInspector]
  public float xOffset;
    */

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

    this.enableFingerParticleSystems = true;

    if (/*this.enableRecordPlayback &&*/ this.recordingAsset != null)
    {
        this.enableRecordPlayback = true;
        this.recorder_.Load(this.recordingAsset);
        this.recorder_.state = RecorderState.Playing;
        this.recorder_.SetDefault();
        Debug.Log("obtained: " + this.recorder_.GetFrames().Count + " frames...");
    }
  }

  new public void IgnoreCollisionsWithHands(GameObject to_ignore, bool ignore = true)
  {
    foreach (HandModel hand in PhysicHandsDictionary.Values)
      Utils.IgnoreCollisions(hand.gameObject, to_ignore, ignore);
  }

  new protected HandModel CreateHand(HandModel model)
  {
        Debug.Log("creating hands...");
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

    // add grasp script
    if (hand_model.GetType() == typeof(RigidHand))
    {
      hand_model.gameObject.AddComponent<GraspController>();
      GraspController graspController = hand_model.gameObject.GetComponent<GraspController>();
      //graspController.GraspTriggerDistance = 1.0f * this.GraspDistanceScale;
      //graspController.ReleaseTriggerDistance = 2.5f * this.GraspDistanceScale;
      graspController.GraspObjectDistance = 3.5f * this.GraspDistanceScale;
    }
    
    Collider[] allCls = hand_model.GetComponentsInChildren<Collider>();
    foreach (Collider lcl in allCls)
    {
      foreach (Collider refCl in allCls)
      {
        if (!lcl.Equals(refCl))
        {
          Physics.IgnoreCollision(lcl, refCl, true);
        }
      }
    }

    if (this.enableFingerParticleSystems && hand_model.GetType() == typeof(SkeletalHand))
    {
      SkeletalHand skeletalHand = hand_model as SkeletalHand;
      foreach (FingerModel finger in skeletalHand.fingers)
      {
        if (finger.GetType() == typeof(SkeletalFinger))
        {
          SkeletalFinger skeletalFinger = finger as SkeletalFinger;

          if (skeletalFinger.fingerType == Finger.FingerType.TYPE_INDEX)
          {
            Transform bone = skeletalFinger.bones[skeletalFinger.bones.Length - 1];
            GameObject indexProxy = GameObject.Instantiate(this.IndexStimulationProxyPrefab, Vector3.zero, Quaternion.identity) as GameObject;
            indexProxy.transform.parent = bone;
            indexProxy.transform.localPosition = Vector3.zero;
          }
          else if (skeletalFinger.fingerType == Finger.FingerType.TYPE_THUMB)
          {
            Transform bone = skeletalFinger.bones[skeletalFinger.bones.Length - 1];
            GameObject thumbProxy = GameObject.Instantiate(this.ThumbStimulationProxyPrefab, Vector3.zero, Quaternion.identity) as GameObject;
            thumbProxy.transform.parent = bone;
            thumbProxy.transform.localPosition = Vector3.zero;
          }
        }
      }
      skeletalHand.gameObject.AddComponent<StimulationProxy>();
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
        Debug.Log("updating " + leap_hands.Count + " hands...");
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
          new_hand.ExternalOffset = this.OffsetController.getOffset(new_hand);
          new_hand.UpdateHand();
          all_hands[leap_hand.Id] = new_hand;
          if (leap_hand.Id == this.currentRightHandID)
          {
            this.currentRelevantPalmPosition = all_hands[leap_hand.Id].GetPalmPosition();
            foreach (FingerModel finger in all_hands[leap_hand.Id].fingers)
            {
              if (finger.fingerType == Finger.FingerType.TYPE_INDEX)
              {
                this.currentRelevantIndexPosition = finger.GetTipPosition();
              }
              else if (finger.fingerType == Finger.FingerType.TYPE_THUMB)
              {
                this.currentRelevantThumbPosition = finger.GetTipPosition();
              }
            }
          }
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
          hand_model.ExternalOffset = this.OffsetController.getOffset(hand_model);
          hand_model.UpdateHand();
          if (leap_hand.Id == this.currentRightHandID)
          {
            this.currentRelevantPalmPosition = all_hands[leap_hand.Id].GetPalmPosition();
            foreach (FingerModel finger in all_hands[leap_hand.Id].fingers)
            {
              if (finger.fingerType == Finger.FingerType.TYPE_INDEX)
              {
                this.currentRelevantIndexPosition = finger.GetTipPosition();
              }
              else if (finger.fingerType == Finger.FingerType.TYPE_THUMB)
              {
                this.currentRelevantThumbPosition = finger.GetTipPosition();
              }
            }
          }
        }
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

  public Vector3 getCurrentPalmPosition()
  {
      return this.currentRelevantPalmPosition;// -new Vector3(this.xOffset, 0, 0);
  }

  public Vector3 getCurrentIndexPosition()
  {
      return this.currentRelevantIndexPosition;// -new Vector3(this.xOffset, 0, 0); ;
  }

  public Vector3 getCurrentThumbPosition()
  {
      return this.currentRelevantThumbPosition;// -new Vector3(this.xOffset, 0, 0); ;
  }

  void Update()
  {
    if (leap_controller_ == null)
      return;
    
    if (Input.GetKeyUp(KeyCode.Space))
    {
            if (!this.ExperimentMode)
            {
                this.ResetRecording();
                this.Record();
                this.ExperimentMode = true;
            }
            else
            {
                UnityEngine.Debug.Log(this.FinishAndSaveRecording());
                this.ExperimentMode = false;
            }
    }

    // record data if requested...
    if (this.ExperimentMode)
      this.UpdateRecorder();

    // apply offset if necessary
    /*
    if (this.ApplyZOffset)
    {
        if (this.getCurrentHandModel() != null)
        {
            if (this.currentRelevantPalmPosition.z > this.StartZOffset)
            {
                float maxDist = this.EndZOffset - this.StartZOffset;
                float dist = this.currentRelevantPalmPosition.z - this.StartZOffset;
                float fraction = Mathf.Min(1.0f, dist / maxDist);
                this.xOffset = this.MaxZOffset * fraction;
            }
            else
            {
                this.xOffset = 0.0f;
            }
        }
    }
    else
    {
        this.xOffset = 0.0f;
    }
    */
    Frame frame = GetFrame();
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

    //if (frame.Id != PreviousGraphicsID)
    {
      UpdateHandModels(GraphicHandsDictionary, frame.Hands, leftGraphicsModel, rightGraphicsModel);
      PreviousGraphicsID = frame.Id;
    }

    //if (frame.Id != PreviousPhysicsID)
    {
        UpdateHandModels(PhysicHandsDictionary, frame.Hands, leftPhysicsModel, rightPhysicsModel);
        PreviousPhysicsID = frame.Id;
    }

    if (this.enableRecordPlayback)
    {
      if (this.recorder_.GetFrames().Count == 0)
      {
        this.recorder_.Load(this.recordingAsset);
        this.recorder_.state = RecorderState.Playing;
        this.recorder_.SetDefault();
      }

      this.recorder_.NextFrame();
      //Debug.Log("still have: " + this.recorder_.GetFrames().Count + " frames...");
      //Debug.Log("current frame: " + this.recorder_.GetIndex() + " / " + this.recorder_.GetProgress() + " ...");
    } 
  }

  /** Updates the physics objects */
  
  void FixedUpdate()
  {
    if (leap_controller_ == null)
      return;
      
    Frame frame = GetFrame();
    //if (frame.Id != PreviousPhysicsID)
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


  new public void UpdateRecorder()
  {
    if (recorder_.state == RecorderState.Recording)
    {
      recorder_.AddFrame(leap_controller_.Frame());
    }
  }
}
