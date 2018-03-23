using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrosshairRay : MonoBehaviour {

    public Camera RelevantCamera;
    public bool active = true;

    private static bool Verbose = true;
    // in seconds...
    private static float timeThreshold = 1.0f;

    private bool fixated;
    private bool fixationDone;
    private float timeStamp;
    private GameObject fixationLeft;
    private GameObject fixationRight;
    private FixationCrossColorSwitch colorSwitchLeft;
    private FixationCrossColorSwitch colorSwitchRight;
    private GameLogic logic;

    void Start()
    {

        this.fixated = false;
        this.fixationDone = false;
        this.timeStamp = 0f;
        this.fixationLeft = GameObject.FindWithTag("FixationLeft");
        
        this.fixationRight = GameObject.FindWithTag("FixationRight");
        this.colorSwitchLeft = this.fixationLeft.GetComponent<FixationCrossColorSwitch>();
        this.colorSwitchRight = this.fixationRight.GetComponent<FixationCrossColorSwitch>();
        this.logic = GameObject.FindWithTag("GameLogic").GetComponent<GameLogic>();
        this.active = true;
    }

    public void reset()
    {
        this.fixated = false;
        this.fixationDone = false;
        this.timeStamp = 0f;
        this.active = true;
        this.colorSwitchLeft.switchColor(false);
    }

    void Update()
    {
        if (this.fixationLeft == null || !this.active)
        {
            if (this.fixationLeft == null) Debug.Log("no Left");
            return;
        }
        Vector3 cameraPosition; // Current camera Position
        Vector3 cameraForwardOrientation; // Current camera Orientation
        Ray rayFromCamera; // Ray originated from Left Camera
        RaycastHit rayHitInfo; // Info of the ray hitting

        // get camera position & orientation
        cameraPosition = this.RelevantCamera.transform.position;
        cameraForwardOrientation = this.RelevantCamera.transform.rotation * Vector3.forward;
        // Ray from Camera to facing direction has to build new every Frame
        rayFromCamera = new Ray(cameraPosition, cameraForwardOrientation);
        // Check if ray hits something
        if (Physics.Raycast(rayFromCamera, out rayHitInfo))
        {

            if (rayHitInfo.collider != null && rayHitInfo.collider.transform.gameObject == fixationRight)
            {

                if (!fixated)
                {
                    this.timeStamp = 0f;
                }
                this.fixated = true;
                this.colorSwitchRight.switchColor(true);
                this.timeStamp += Time.deltaTime;
                if (this.timeStamp >= CrosshairRay.timeThreshold)
                {
                    this.fixationDone = true;
                    this.timeStamp = 0f;
                    fixationRight = rayHitInfo.collider.transform.gameObject;
                    logic.GiveAnswer(false);
                }
            } else if (rayHitInfo.collider != null && rayHitInfo.collider.transform.gameObject == fixationLeft)
            {
                if (!fixated)
                {
                    this.timeStamp = 0f;
                }
                this.fixated = true;
                this.colorSwitchLeft.switchColor(true);
                this.timeStamp += Time.deltaTime;
                if (this.timeStamp >= CrosshairRay.timeThreshold)
                {
                    this.fixationDone = true;
                    this.timeStamp = 0f;
                    fixationLeft = rayHitInfo.collider.transform.gameObject;
                    logic.GiveAnswer(true);
                }
            }
            else
            {
                if(fixationDone) logic.ChangeObjects();
                this.colorSwitchRight.switchColor(false);
                this.colorSwitchLeft.switchColor(false);
                this.fixated = false;
                this.fixationDone = false;

            }
        }
        else
        {
            this.fixated = false;
            this.fixationDone = false;
            this.colorSwitchLeft.switchColor(false);
            this.colorSwitchRight.switchColor(false);
        }
    }

    public bool FixationCompleted()
    {
        return this.fixationDone;
    }
}
