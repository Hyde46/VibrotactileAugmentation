using UnityEngine;
using Leap;
using System.Collections;

public class PoseController : MonoBehaviour
{
    private static bool verbose = false;

    public SimplifiedHandController handControllerReference;

    private GameObject[] InitialPoseObjects;
    private InitialPoseScript[] InitialPoseScripts;

    public float scaleFactor = 3.0f;
    public Vector3 Offset = new Vector3(0.0f, 10.0f, 0.0f);
    public Material TransparentMaterial;

    // initial pose stuff
    private Color InitialPositionNoneMatchColor = Color.red;
    private Color InitialPositionMatchColor = Color.green;


    public enum PoseControllerMode
    {
        INITIALIZE,
        IDLE,
        CHECK
    }

    public PoseControllerMode mode = PoseControllerMode.INITIALIZE;

    // quite unstyle: outside the editor some shader options are not available, leading to errors in case you want to instantiate for instant transparent diffuse
    // materials on the fly, one option is to assign some kind of template which is done here...
    // this material is used for displaying the initial hand reference points which indicate the rest position which has to be maintained for some time before a
    // trial starts
    public Material materialRef;

    // Use this for initialization
    void Start()
    {
        this.mode = PoseControllerMode.INITIALIZE;
        this.initializeInitialPoseCheckObjects();
        // alpha
        this.InitialPositionNoneMatchColor.a = 0.4f;
        this.InitialPositionMatchColor.a = 0.4f;
    }

    void Update()
    {
        // we have a hand, so we have data...
        if (this.handControllerReference.currentRightHandID != -1 && this.handControllerReference.GetNextFrame() != null && this.mode == PoseControllerMode.INITIALIZE)
        {
            this.calculateInitialPose();
            this.checkInitialPose();
        }
    }

    private void calculateInitialPose()
    {
        Hand leapHand = null;
        for (int i = 0; i < this.handControllerReference.GetNextFrame().Hands.Count; i++)
        {
            if (this.handControllerReference.GetNextFrame().Hands[i].Id == this.handControllerReference.currentRightHandID)
            {
                leapHand = this.handControllerReference.GetNextFrame().Hands[i];
            }
        }
        if (leapHand != null)
        {
            if (PoseController.verbose) Debug.Log("found hand...");
            foreach (Finger finger in leapHand.Fingers)
            {
                if (PoseController.verbose) Debug.Log(finger.Type() + ": " + finger.Length + ", " + Leap.UnityVectorExtension.INPUT_SCALE * finger.Length);
            }
            if (PoseController.verbose) Debug.Log("palm width: " + leapHand.PalmWidth * Leap.UnityVectorExtension.INPUT_SCALE);
            this.InitialPoseObjects[0].transform.position = this.handControllerReference.transform.position
              + new Vector3(
                 1.0f * leapHand.PalmWidth * Leap.UnityVectorExtension.INPUT_SCALE * this.scaleFactor,
                3.5f * leapHand.PalmWidth * Leap.UnityVectorExtension.INPUT_SCALE * this.scaleFactor,
                 -1.0f * leapHand.PalmWidth * Leap.UnityVectorExtension.INPUT_SCALE * this.scaleFactor
                )
              + this.Offset;
            ;
            this.InitialPoseObjects[0].SetActive(true);
            this.InitialPoseObjects[6].transform.position = this.InitialPoseObjects[0].transform.position;
            Vector3 palmPosition = this.InitialPoseObjects[0].transform.position;
            // ignore first and last one (the palms)
            for (int i = 1; i < this.InitialPoseObjects.Length - 1; i++)
            {
                Finger finger = null;
                switch (i)
                {
                    case 1:
                        finger = this.getFingerByType(leapHand.Fingers, Finger.FingerType.TYPE_THUMB);
                        break;
                    case 2:
                        finger = this.getFingerByType(leapHand.Fingers, Finger.FingerType.TYPE_INDEX);
                        break;
                    case 3:
                        finger = this.getFingerByType(leapHand.Fingers, Finger.FingerType.TYPE_MIDDLE);
                        break;
                    case 4:
                        finger = this.getFingerByType(leapHand.Fingers, Finger.FingerType.TYPE_RING);
                        break;
                    case 5:
                        finger = this.getFingerByType(leapHand.Fingers, Finger.FingerType.TYPE_PINKY);
                        break;
                }
                if (finger != null)
                {
                    // rather stupid heuristic, but seems to work...
                    float fingerLength = Leap.UnityVectorExtension.INPUT_SCALE * finger.Length;
                    float distance = Vector3.Distance(leapHand.PalmPosition.ToUnityScaled(), finger.Bone(Bone.BoneType.TYPE_METACARPAL).PrevJoint.ToUnityScaled());
                    this.InitialPoseObjects[i].transform.position = palmPosition
                      + new Vector3(
                        (i - 3) * this.scaleFactor * .35f,
                        0.0f,
                        fingerLength + (i != 1 ? distance : 0.0f)
                        );
                    this.InitialPoseObjects[i].SetActive(true);
                }
            }
        }
    }

    private Finger getFingerByType(FingerList fingers, Finger.FingerType type)
    {
        foreach (Finger finger in fingers)
        {
            if (finger.Type().Equals(type))
            {
                return finger;
            }
        }
        return null;
    }

    private void initializeInitialPoseCheckObjects()
    {
        this.InitialPoseObjects = new GameObject[7];
        this.InitialPoseScripts = new InitialPoseScript[7];

        // palm
        this.createInitialPoseObject(0, new Vector3(0.0f, 0.0f, 0.0f), "palm", "palm");
        // thumb
        this.createInitialPoseObject(1, new Vector3(0.0f, 0.0f, 0.0f), "bone3", "thumb");
        // index
        this.createInitialPoseObject(2, new Vector3(0.0f, 0.0f, 0.0f), "bone3", "index");
        // middle
        this.createInitialPoseObject(3, new Vector3(0.0f, 0.0f, 0.0f), "bone3", "middle");
        // ring
        this.createInitialPoseObject(4, new Vector3(0.0f, 0.0f, 0.0f), "bone3", "ring");
        // pinky
        this.createInitialPoseObject(5, new Vector3(0.0f, 0.0f, 0.0f), "bone3", "pinky");
        // invisible palm
        this.createInitialPoseObject(6, new Vector3(0.0f, 0.0f, 0.0f), "palm", "palm");

        this.InitialPoseObjects[6].GetComponent<Renderer>().material = this.TransparentMaterial;
        this.InitialPoseObjects[6].SetActive(true);
    }


    private void createInitialPoseObject(int index, Vector3 position, string targetTransformName, string targetTransformParentName)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = targetTransformParentName + "_initial_pose_reference";

        if (index == 6) obj.tag = "PalmInitialPoseReference";
        Destroy(obj.GetComponent<Collider>());
        obj.GetComponent<Renderer>().material = this.materialRef;
        obj.GetComponent<Renderer>().material.color = Color.gray;
        obj.transform.position = new Vector3(position.x / Leap.UnityVectorExtension.INPUT_SCALE, position.y / Leap.UnityVectorExtension.INPUT_SCALE, position.z / Leap.UnityVectorExtension.INPUT_SCALE);//* 1.0f / Leap.UnityVectorExtension.INPUT_SCALE;
        Vector3 scale = obj.transform.localScale * this.scaleFactor * .75f;
        obj.transform.localScale = scale;
        InitialPoseScript script = obj.AddComponent<InitialPoseScript>() as InitialPoseScript;
        script.targetTransformName = targetTransformName;
        script.targetTransformParentName = targetTransformParentName;

        obj.SetActive(false);

        this.InitialPoseObjects[index] = obj;
        this.InitialPoseScripts[index] = script;
    }

    public bool checkInitialPose()
    {
        // TODO: check whether all fingers and the palm are in position; you can use the
        // created InitialPoseScript instances, stored within the InitialPoseScripts array,
        // the checkContact() method should return true, if the target finger is in position

        //return false;

        bool valid = true;

        for (int i = 0; i < this.InitialPoseObjects.Length - 1; i++)
        {
            bool contact = this.InitialPoseScripts[i].checkContact();
            if (contact)
            {
                this.InitialPoseObjects[i].GetComponent<Renderer>().material.color = this.InitialPositionMatchColor;
            }
            else
            {
                valid = false;
                this.InitialPoseObjects[i].GetComponent<Renderer>().material.color = this.InitialPositionNoneMatchColor;
            }
        }

        return valid;
    }

    public bool checkPalmPosition()
    {
        return this.InitialPoseScripts[this.InitialPoseObjects.Length - 1].checkContact();
    }

    public void resetInitialPoseChecks()
    {
        for (int i = 0; i < this.InitialPoseObjects.Length - 1; i++)
        {
            this.InitialPoseObjects[i].GetComponent<Renderer>().material.color = this.InitialPositionNoneMatchColor;
            this.InitialPoseScripts[i].targetObject = null;
        }
    }

    public void setControllerMode(PoseControllerMode mode)
    {
        this.mode = mode;
        if (this.mode == PoseControllerMode.IDLE)
        {
            for (int i = 0; i < this.InitialPoseObjects.Length - 1; i++)
            {
                this.InitialPoseObjects[i].SetActive(false);
            }
        }
        else if (this.mode == PoseControllerMode.CHECK || this.mode == PoseControllerMode.INITIALIZE)
        {
            for (int i = 0; i < this.InitialPoseObjects.Length - 1; i++)
            {
                this.InitialPoseObjects[i].SetActive(true);
            }
        }
    }

    public bool initialPositionsDefined()
    {
        foreach (GameObject positionMarker in this.InitialPoseObjects)
        {
            if (positionMarker.transform.position.x == 0.0f && positionMarker.transform.position.y == 0.0f && positionMarker.transform.position.z == 0.0f)
            {
                return false;
            }
        }

        return true;
    }

    public void toogleVisibility(bool visible)
    {
        foreach (GameObject obj in this.InitialPoseObjects)
        {
            obj.GetComponent<Renderer>().enabled = visible;
        }
    }

    public Vector3 getPalmStartPosition()
    {
        return this.InitialPoseObjects[0].transform.position;
    }
}