using UnityEngine;
using System.Collections;
using Leap;

public class GraspController : MonoBehaviour
{
    private static bool Verbose = false;
    public enum GraspState
    {
        GRASPED,
        RELEASED,
        RELEASING
    }
    // Layers that we can grab.
    public LayerMask GraspableLayers = ~0;
    // Ratio of the length of the proximal bone of the thumb that will trigger a grasp.
    public float GraspTriggerDistance = 0.7f;//0.5f;
                                             // Ratio of the length of the proximal bone of the thumb that will trigger a release.
    public float ReleaseTriggerDistance = 1.2f;
    // Maximum distance of an object that we can grasp.
    // edit JL: i increased it somewhat to reduce accidential warding
    public float GraspObjectDistance = 3.5f;//2.0f;
                                            // If the object gets far from the hand, we release it.
    public float ReleaseBreakDistance = 0.3f;
    // Curve of the trailing off of strength as you release the object.
    public AnimationCurve ReleaseStrengthCurve;
    // Filtering the rotation of grabbed object.
    public float RotationFiltering = 0.4f;//0.05f;//
                                          // Filtering the movement of grabbed object.
    public float PositionFiltering = 0.05f;//0.4f;
                                           // Minimum tracking confidence of the hand that will cause a change of state.
    public float MinConfidence = 0.1f;
    // Clamps the movement of the grabbed object.
    public Vector3 MaxMovement = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
    public Vector3 MinMovement = new Vector3(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);
    // current state, see the enum above
    protected GraspState CurrentGraspState;
    // currently hovered or grasped object
    protected Collider ActiveObject;
    // just a backup in case we change the maximum angular velocity of the object
    protected float ObjectMaxAngularVelocity;
    // initial rotation difference between object and palm
    protected Quaternion RotationFromPalm;
    // current grasping center...
    protected Vector3 CurrentGraspCenter;
    // and the according, filtered value
    protected Vector3 SmoothedGraspPosition;
    // the positional difference between object and grasp center
    protected Vector3 GraspOffset;
    // the current palm rotation
    protected Quaternion PalmRotation;

    private static float MIN_FINGER_TO_PALM_DISTANCE = 0.040f;//0.045f;

    void Start()
    {
        this.CurrentGraspState = GraspState.RELEASED;
        this.ActiveObject = null;
        this.ObjectMaxAngularVelocity = 0.0f;
        this.RotationFromPalm = Quaternion.identity;
        this.CurrentGraspCenter = Vector3.zero;
        this.SmoothedGraspPosition = Vector3.zero;
        this.GraspOffset = Vector3.zero;
        this.PalmRotation = Quaternion.identity;

        this.ReleaseStrengthCurve = new AnimationCurve();
        this.ReleaseStrengthCurve.AddKey(new Keyframe(0.0f, 1.0f));
        this.ReleaseStrengthCurve.AddKey(new Keyframe(1.001355f, -0.0007067919f));

        GameObject container = GameObject.FindGameObjectWithTag("Container");
        if (container != null)
        {
            ContainerController containerController = container.GetComponent<ContainerController>();
            if (containerController != null)
            {
                //containerController.ObjectWasReleased += new ContainerController.ObjectWasReleasedHandler(this.checkRelease);
                containerController.ObjectWasReleased += (this.checkRelease);
            }
        }

    }

    void OnDestroy()
    {
        GameObject container = GameObject.FindGameObjectWithTag("Container");
        if (container != null)
        {
            ContainerController containerController = container.GetComponent<ContainerController>();
            if (containerController != null)
            {
                containerController.ObjectWasReleased -= (this.checkRelease);
            }
        }
        this.OnRelease();
    }

    // Finds the closest graspable object within range of the grasp.
    protected Collider FindClosestGrabbableObject(Vector3 graspPosition)
    {
        Collider closest = null;
        float minGraspDistance = this.GraspObjectDistance * this.GraspObjectDistance;
        Collider[] graspCandidates =
        Physics.OverlapSphere(graspPosition, GraspObjectDistance, GraspableLayers);

        for (int j = 0; j < graspCandidates.Length; ++j)
        {
            float squareDistance = (graspPosition - graspCandidates[j].transform.position).sqrMagnitude;

            if (graspCandidates[j].GetComponent<Rigidbody>() != null && squareDistance < minGraspDistance &&
                !graspCandidates[j].transform.IsChildOf(this.transform) &&
                graspCandidates[j].tag != "NotGrabbable")
            {

                GraspableObject grabbable = graspCandidates[j].GetComponent<GraspableObject>();
                if (grabbable == null || !grabbable.IsGrabbed())
                {
                    closest = graspCandidates[j];
                    minGraspDistance = squareDistance;
                }
            }
        }

        return closest;
    }

    // Notify graspable objects when they are ready to grasp
    protected void Hover()
    {
        Collider hover = this.FindClosestGrabbableObject(CurrentGraspCenter);

        if (hover != this.ActiveObject && this.ActiveObject != null)
        {
            GraspableObject oldGraspable = ActiveObject.GetComponent<GraspableObject>();

            if (oldGraspable != null)
                oldGraspable.OnStopHover();
        }

        if (hover != null)
        {
            GraspableObject newGraspable = hover.GetComponent<GraspableObject>();

            if (newGraspable != null)
            {
                newGraspable.OnStartHover();
            }
        }

        this.ActiveObject = hover;
    }

    protected void StartGrasp()
    {
        // Only grasp if we're hovering over an object.
        if (this.ActiveObject == null)
            return;

        HandModel handModel = this.GetComponent<HandModel>();
        GraspableObject graspable = this.ActiveObject.GetComponent<GraspableObject>();
        Leap.Utils.IgnoreCollisions(this.gameObject, this.ActiveObject.gameObject, true);

        // Setup initial position and rotation conditions.
        this.PalmRotation = handModel.GetPalmRotation();
        this.GraspOffset = Vector3.zero;

        // If we don't center the object, find the closest point in the collider for our grab point.
        if (graspable == null || !graspable.CenterGraspedObject)
        {
            Vector3 deltaPosition = ActiveObject.transform.position - this.CurrentGraspCenter;

            Ray graspRay = new Ray(this.CurrentGraspCenter, deltaPosition);
            RaycastHit graspHit;

            // If the raycast hits the object, we are outside the collider so grab the hit point.
            // If not, we are inside the collider so just use the grasp position.
            if (ActiveObject.Raycast(graspRay, out graspHit, GraspObjectDistance))
            {
                this.GraspOffset = this.ActiveObject.transform.position - graspHit.point;
            }
            else
            {
                this.GraspOffset = this.ActiveObject.transform.position - CurrentGraspCenter;
            }
        }

        this.SmoothedGraspPosition = this.ActiveObject.transform.position - this.GraspOffset;
        this.GraspOffset = Quaternion.Inverse(this.ActiveObject.transform.rotation) * GraspOffset;
        this.RotationFromPalm = Quaternion.Inverse(this.PalmRotation) * this.ActiveObject.transform.rotation;

        // If we can rotate the object quickly, increase max angular velocity for now.
        if (graspable == null || graspable.RotateQuickly)
        {
            this.ObjectMaxAngularVelocity = this.ActiveObject.GetComponent<Rigidbody>().maxAngularVelocity;
            this.ActiveObject.GetComponent<Rigidbody>().maxAngularVelocity = Mathf.Infinity;
        }

        if (graspable != null)
        {
            // Notify grabbable object that it was grabbed.
            if (graspable.GetResponseTime() < 0L)
            {
                if (handModel != null)
                {
                    Vector3 normal = handModel.GetPalmNormal();
                    float angleLeft = Vector3.Angle(normal, Vector3.left);
                    float angleRight = Vector3.Angle(normal, Vector3.right);
                    if (GraspController.Verbose) Debug.LogWarning("hand normal: " + normal + ", to right: " + angleRight + ", to left: " + angleLeft);
                    if (angleLeft < angleRight)
                    {
                        graspable.setGraspDirection(GraspableObject.GraspDirection.UPRIGHT);
                    }
                    else
                    {
                        graspable.setGraspDirection(GraspableObject.GraspDirection.ROTATED);
                    }
                }
                else
                {
                    if (GraspController.Verbose) Debug.LogWarning("no hand found...");
                }
            }
            graspable.OnGrasp(this.gameObject);

            if (graspable.UseAxisAlignment)
            {
                // If this option is enabled we only want to align the object axis with the palm axis,
                // hence we cancel out any rotation about the aligned axis.
                Vector3 palmVector = graspable.RightHandAxis;
                if (handModel.GetLeapHand().IsLeft)
                {
                    palmVector = Vector3.Scale(palmVector, new Vector3(-1, 1, 1));
                }
                Vector3 axisInPalm = this.RotationFromPalm * graspable.ObjectAxis;
                Quaternion axisCorrection = Quaternion.FromToRotation(axisInPalm, palmVector);
                if (Vector3.Dot(axisInPalm, palmVector) < 0)
                {
                    axisCorrection = Quaternion.FromToRotation(axisInPalm, -palmVector);
                }
                this.RotationFromPalm = axisCorrection * this.RotationFromPalm;
            }
        }
    }

    private void checkRelease(GameObject gameObject, bool validOrientation)
    {
        if (GraspController.Verbose) UnityEngine.Debug.Log("release check by event...");
        if (this.ActiveObject != null)
        {
            if (GraspController.Verbose) UnityEngine.Debug.Log("active object exists...");
            if (this.ActiveObject.gameObject == gameObject)
            {
                if (GraspController.Verbose) UnityEngine.Debug.Log("release request by event...");
                this.requestRelease();
            }
        }
    }

    public void requestRelease()
    {
        this.CurrentGraspState = GraspState.RELEASED;
        this.OnRelease();
    }

    public void OnRelease()
    {
        if (this.ActiveObject != null)
        {
            // Notify the grabbable object that is was released.
            GraspableObject graspable = this.ActiveObject.GetComponent<GraspableObject>();
            if (graspable != null)
            {
                graspable.OnRelease(this.gameObject);
            }
            if (graspable == null || graspable.RotateQuickly)
            {
                ActiveObject.GetComponent<Rigidbody>().maxAngularVelocity = ObjectMaxAngularVelocity;
            }
            Leap.Utils.IgnoreCollisions(gameObject, ActiveObject.gameObject, false);
        }

        this.ActiveObject = null;
        this.Hover();
    }

    protected GraspState GetNewGraspState()
    {
        HandModel handModel = this.GetComponent<HandModel>();
        Hand leapHand = handModel.GetLeapHand();

        // check destruction
        if (this.CurrentGraspState == GraspState.GRASPED && this.ActiveObject != null)
        {
            // TODO: implement some kind of check for the overall grip aperture, if it falls
            // below a certain threshold, the object should be destroyed and the returned
            // state should be 'RELEASED'. Please stick to the initiateDestructionByGrabbing
            // method from the ContainerController. To search within the scene you can
            // use for instance the FindGameObjectWithTag method.
            Vector3 centroid = leapHand.Fingers[1].TipPosition.ToUnityScaled() * .25f + leapHand.Fingers[2].TipPosition.ToUnityScaled() * .25f + leapHand.Fingers[3].TipPosition.ToUnityScaled() * .25f + leapHand.Fingers[4].TipPosition.ToUnityScaled() * .25f;
            float distance = Vector3.Distance(leapHand.PalmPosition.ToUnityScaled(), centroid);
            if (distance <= GraspController.MIN_FINGER_TO_PALM_DISTANCE)
            {
                ContainerController containerController = GameObject.FindGameObjectWithTag("Container").GetComponent<ContainerController>();
                containerController.initiateDestructionByGrabbing(this.ActiveObject.gameObject);
                return GraspState.RELEASED;
            }
        }

        // power grasp conditions:
        // - grabstrength > .5 -> difficult for rotated hands otherwise
        if (leapHand.GrabStrength > .35f)
        {
            return GraspState.GRASPED;
        }

        // Scale trigger distance by thumb proximal bone length.
        float proximalLength = leapHand.Fingers[0].Bone(Bone.BoneType.TYPE_PROXIMAL).Length;

        if (this.CurrentGraspState == GraspState.GRASPED && this.ActiveObject != null)
        {
            //return GraspState.GRASPED;
            // check if fingers point away from palm
            Vector3 axis = handModel.GetPalmDirection();
            int stretchedFingers = 0;
            for (int i = 1; i < 5; i++)
            {
                Vector3 fingerAxis = handModel.fingers[i].GetRay().direction;
                float angle = Vector3.Angle(axis, fingerAxis);
                //UnityEngine.Debug.Log(i + ": " + angle);
                if (angle > 0 && angle < 40)
                {
                    stretchedFingers++;
                }
            }

            if (stretchedFingers >= 3)
            {
                return GraspState.RELEASED;
            }
            else
            {
                return GraspState.GRASPED;
            }
        }

        return GraspState.RELEASED;
    }

    protected void UpdateGraspPosition()
    {
        HandModel handModel = this.GetComponent<HandModel>();
        // our grasp center only depends on thumb and index finger for stability reasons
        this.CurrentGraspCenter = 0.5f * (handModel.fingers[0].GetTipPosition() +
                                          handModel.fingers[1].GetTipPosition());

        if (this.CurrentGraspState == GraspState.GRASPED && this.ActiveObject != null)
        {
            this.CurrentGraspCenter = handModel.GetPalmPosition() + handModel.GetPalmNormal() * .025f;
        }

        Vector3 graspDelta = this.CurrentGraspCenter - this.SmoothedGraspPosition;
        this.SmoothedGraspPosition += (1.0f - this.PositionFiltering) * graspDelta;
    }

    protected void UpdatePalmRotation()
    {
        HandModel handModel = this.GetComponent<HandModel>();
        // apply some smoothing to avoid abrupt changes
        this.PalmRotation = Quaternion.Slerp(this.PalmRotation, handModel.GetPalmRotation(),
                                             1.0f - this.RotationFiltering);
    }

    protected bool ObjectReleaseBreak(Vector3 graspPosition)
    {
        if (this.ActiveObject == null)
            return true;

        Vector3 deltaPosition = graspPosition - this.ActiveObject.transform.position;
        return deltaPosition.magnitude > this.ReleaseBreakDistance;
    }

    protected void ContinueHardGrasp()
    {
        // default mode
        Quaternion targetRotation = this.PalmRotation * this.RotationFromPalm;

        GraspableObject graspable = ActiveObject.gameObject.GetComponent<GraspableObject>();

        if (graspable != null)
        {
            Vector3 targetPosition = this.SmoothedGraspPosition + targetRotation * this.GraspOffset;
            targetPosition.x = Mathf.Clamp(targetPosition.x, MinMovement.x, MaxMovement.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, MinMovement.y, MaxMovement.y);
            targetPosition.z = Mathf.Clamp(targetPosition.z, MinMovement.z, MaxMovement.z);
            Vector3 velocity = (targetPosition - ActiveObject.transform.position) / Time.deltaTime;
            this.ActiveObject.GetComponent<Rigidbody>().velocity = velocity;
        }

        Quaternion deltaRotation = targetRotation *
          Quaternion.Inverse(this.ActiveObject.transform.rotation);

        if (graspable != null)
        {
            float angle = 0.0f;
            Vector3 axis = Vector3.zero;
            deltaRotation.ToAngleAxis(out angle, out axis);

            if (angle >= 180)
            {
                angle = 360 - angle;
                axis = -axis;
            }
            if (angle != 0)
            {
                this.ActiveObject.GetComponent<Rigidbody>().angularVelocity = angle * axis;
            }
        }
    }

    // this happens on release
    protected void ContinueSoftGrasp()
    {
        Quaternion targetRotation = this.PalmRotation * this.RotationFromPalm;

        Vector3 targetPosition = this.SmoothedGraspPosition + targetRotation * this.GraspOffset;
        Vector3 deltaPosition = targetPosition - this.ActiveObject.transform.position;

        float strength = (this.ReleaseBreakDistance - deltaPosition.magnitude) / this.ReleaseBreakDistance;
        strength = this.ReleaseStrengthCurve.Evaluate(strength);

        GraspableObject graspable = this.ActiveObject.gameObject.GetComponent<GraspableObject>();

        if (graspable != null)
        {
            this.ActiveObject.GetComponent<Rigidbody>().AddForce(deltaPosition.normalized * strength * this.PositionFiltering,
                                                                 ForceMode.Acceleration);
        }

        Quaternion deltaRotation = targetRotation *
          Quaternion.Inverse(this.ActiveObject.transform.rotation);

        float angle = 0.0f;
        Vector3 axis = Vector3.zero;
        deltaRotation.ToAngleAxis(out angle, out axis);

        if (graspable != null)
        {
            this.ActiveObject.GetComponent<Rigidbody>().AddTorque(strength * RotationFiltering * angle * axis,
                                                                  ForceMode.Acceleration);
        }
    }

    void FixedUpdate()
    {
        this.UpdatePalmRotation();
        this.UpdateGraspPosition();
        HandModel handModel = this.GetComponent<HandModel>();
        Hand leapHand = handModel.GetLeapHand();

        if (leapHand == null)
            return;

        GraspState newGraspState = this.GetNewGraspState();
        if (this.CurrentGraspState == GraspState.GRASPED)
        {
            if (newGraspState == GraspState.RELEASED)
            {
                this.OnRelease();
            }
            else if (this.ActiveObject != null)
            {
                this.ContinueHardGrasp();
            }
        }
        else if (this.CurrentGraspState == GraspState.RELEASING)
        {
            if (newGraspState == GraspState.RELEASED)
            {
                this.OnRelease();
            }
            else if (newGraspState == GraspState.GRASPED)
            {
                this.StartGrasp();
            }
            else if (this.ActiveObject != null)
            {
                this.ContinueSoftGrasp();
            }
        }
        else
        {
            if (newGraspState == GraspState.GRASPED)
            {
                this.StartGrasp();
            }
            else
            {
                this.Hover();
            }
        }
        this.CurrentGraspState = newGraspState;
    }
}