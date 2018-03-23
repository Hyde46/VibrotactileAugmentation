/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using System;
using UnityEngine;
using System.Collections;

public class GraspableObject : MonoBehaviour
{
    private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0);
    public static long Millis { get { return (long)((DateTime.Now - Jan1St1970).TotalMilliseconds); } }

    // if true some diagnostic information is printed to the console
    private static bool Verbose = false;
    // if true, the object is not rotated but only aligned with
    // the palm
    public bool UseAxisAlignment = false;
    // base vector for the object alignment, only used when
    // useAxisAlignment is true
    public Vector3 RightHandAxis;
    // if you want to, you can specify a rotation axis for the object.
    // this value is only used if you set useAxisAlignment to true, in
    // this case the object is aligned with the hand palm via this axis
    public Vector3 ObjectAxis;
    // if true the angular velocity will be unconstraint during interaction, if false, the
    // maximum velocity is determined by the respective rigidbody poperty
    public bool RotateQuickly = true;
    // if true, the object is moved to the grasp center upon grasping, this
    // results in some kind of magnetic effect
    public bool CenterGraspedObject = false;
    // to allow force-based culling, you can assign a joint to this object which might
    // be broken by forces exerted during grasping
    public Rigidbody BreakableJoint;
    public float BreakForce;
    public float BreakTorque;
    // is the objects grasped?
    protected bool Grasped = false;
    // is a hand hovering above the object?
    protected bool Hovered = false;
    // initial position
    private Vector3 InitialPosition;
    // initial rotation
    private Quaternion InitialRotation;
    // offset between object's position and grasp center
    private Vector3 TransformPositionOffset;
    // the time between the call to startClock and the actual grasp
    private long ResponseTime;
    // grasped?
    private bool WasGrasped;
    // touched?
    private bool MadeHandContact;
    // most recent release time in terms of Application time
    public float ReleaseTime;
    // the target container, where the objects should be stored, i.e. the box
    private GameObject CollectionContainer;
    // to make grasping easier the object is held in its initial position by a joint, which is part of this object
    private GameObject InitialPositionAnchor;
    // collision timeout to avoid unwanted collisions, do we still need this?
    public float CollisionTimeOutInSeconds = 2.0f;
    // these two are used to perform a quick and dirty position check upon landing:
    // if the bottom reference is closer to the collision center than the top, everything is fine,
    // otherwise the bottle was placed up-side down
    public GameObject TopReference;
    public GameObject BottomReference;

    public GameObject HighlightComponent;
    public Material HighlightMaterial;
    private Material backupMaterial;

    public enum GraspDirection
    {
        NONE,
        UPRIGHT,
        ROTATED
    }

    private GraspDirection graspDirection;

    public void Awake()
    {
        this.InitialPosition = this.transform.position;
        this.InitialRotation = this.transform.rotation;
        this.CollectionContainer = GameObject.FindWithTag("Container");
        this.InitialPositionAnchor = GameObject.FindWithTag("PositionAnchor");

        if (this.HighlightComponent != null)
        {
            this.backupMaterial = this.HighlightComponent.GetComponent<Renderer>().material;
        }
    }

    public bool IsHovered()
    {
        return this.Hovered;
    }

    public bool IsGrabbed()
    {
        return this.Grasped;
    }

    public virtual void OnStartHover()
    {
        this.Hovered = true;
    }

    public virtual void OnStopHover()
    {
        this.Hovered = false;
    }

    public virtual void OnGrasp(GameObject hand)
    {
        this.Grasped = true;
        this.Hovered = false;
        this.WasGrasped = true;
        if (this.BreakableJoint != null)
        {
            // TODO: disconnect the joint
            Joint breakJoint = this.BreakableJoint.GetComponent<Joint>();
            if (breakJoint != null)
            {
                // do not destroy it, we will reuse it
                breakJoint.connectedBody = null;
            }
        }

        if (this.ResponseTime == -1L) this.ResponseTime = GraspableObject.Millis;
        this.MadeHandContact = true;

        Collider[] cls = hand.GetComponentsInChildren<Collider>();
        foreach (Collider cl in cls)
        {
            Physics.IgnoreCollision(this.GetComponent<Collider>(), cl, true);
        }
        this.GetComponent<Rigidbody>().useGravity = false;

        if (this.HighlightComponent != null) this.HighlightComponent.GetComponent<Renderer>().material = this.HighlightMaterial;
    }

    public virtual bool OnRelease(GameObject hand)
    {
        this.Grasped = false;
        this.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        this.GetComponent<Rigidbody>().useGravity = true;

        // if we are above the container, it shall just fall down...
        if (this.AboveContainer())
        {
            if (GraspableObject.Verbose) Debug.Log("released above container...");
            this.GetComponent<Rigidbody>().velocity = new Vector3(0.0f, -10.0f, 0.0f);
            this.GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);
            this.ReleaseTime = Time.time;
            return true;
        }

        Collider[] cls = hand.GetComponentsInChildren<Collider>();

        foreach (Collider cl in cls)
        {
            if (cl != null) Physics.IgnoreCollision(this.GetComponent<Collider>(), cl, false);
        }

        if (this.HighlightComponent != null) this.HighlightComponent.GetComponent<Renderer>().material = this.backupMaterial;

        return false;
    }
    void OnCollisionEnter(Collision other)
    {
        //Debug.LogWarning("obect collision");
        //  if (other.gameObject.name == "capsule" || other.gameObject.name == "torus") {
        if (other.gameObject.transform.root.gameObject.GetComponent<RigidHand>() != null)
        {
            //Debug.LogWarning("obect grasped");
            if (this.ResponseTime == -1L)
            {
                this.ResponseTime = GraspableObject.Millis;
                this.MadeHandContact = true;
            }
        }
        else
        {
            if (GraspableObject.Verbose) Debug.Log("collision with " + other.gameObject.name + "...");
        }
    }
    IEnumerator CollisionDispatcher(GameObject hand)
    {
        if (GraspableObject.Verbose) Debug.Log("try to disable collisions between " + this.gameObject.name + " and " + hand.name);
        Collider[] cls = hand.GetComponentsInChildren<Collider>();
        foreach (Collider cl in cls)
        {
            Physics.IgnoreCollision(this.GetComponent<Collider>(), cl, true);
        }
        if (GraspableObject.Verbose) Debug.Log("disabled " + cls.Length + "collisions");
        yield return new WaitForSeconds(this.CollisionTimeOutInSeconds);

        foreach (Collider cl in cls)
        {
            if (cl != null) Physics.IgnoreCollision(this.GetComponent<Collider>(), cl, false);
        }
        if (GraspableObject.Verbose) Debug.Log("enabled " + cls.Length + "collisions");
    }
    public void setGraspDirection(GraspDirection graspDirection)
    {
        this.graspDirection = graspDirection;
    }
    public GraspDirection getGraspDirection()
    {
        return this.graspDirection;
    }
    // check if we are above the target container, i.e. the box
    public bool AboveContainer()
    {
        if (this.CollectionContainer == null)
        {
            return false;
        }

        Vector3 checkPoint = new Vector3(
          this.transform.position.x,
          this.CollectionContainer.transform.position.y,
          this.transform.position.z
        );


        return this.CollectionContainer.GetComponent<Renderer>().bounds.Contains(checkPoint);
    }
    // reinit the globals
    public void ResetVariables()
    {
        this.ResponseTime = -1L;
        this.WasGrasped = false;
        this.MadeHandContact = false;
        this.Grasped = false;
        this.Hovered = false;
        this.ReleaseTime = -1.0f;
        this.graspDirection = GraspDirection.NONE;
    }
    public void ResetPositionAndOrientation(float angleX, Vector3 position)
    {
        this.transform.position = position; // = this.InitialPosition
        this.transform.rotation = Quaternion.Euler(-angleX, 0.0f, 0.0f);//Quaternion.identity; //this.InitialRotation;
        this.GetComponent<Rigidbody>().velocity = new Vector3(0.0f, 0.0f, 0.0f);
        this.GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);
        // TODO: use the HingeJoint from the InitialPositionAnchor to realize a coil spring,
        // make sure to assigne the BreakableJoint of this script variable accordingly.

        this.InitialPositionAnchor.GetComponent<HingeJoint>().connectedAnchor = angleX == 0.0f ? new Vector3(0.0f, -.8f, 0.0f) : new Vector3(0.0f, .8f, 0.0f);
        this.InitialPositionAnchor.GetComponent<HingeJoint>().connectedBody = this.gameObject.GetComponent<Rigidbody>();

        this.BreakableJoint = this.InitialPositionAnchor.GetComponent<Rigidbody>();

        if (this.HighlightComponent != null) this.HighlightComponent.GetComponent<Renderer>().material = this.backupMaterial;
    }

    public bool HasBeenTouched()
    {
        return this.MadeHandContact;
    }
    public bool HasBeenGrasped()
    {
        return this.WasGrasped;
    }
    public long GetResponseTime()
    {
        return this.ResponseTime;
    }
}
