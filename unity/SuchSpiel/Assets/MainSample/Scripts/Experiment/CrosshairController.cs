using UnityEngine;
using System.Collections;

public class CrosshairController : MonoBehaviour
{

  public Camera RelevantCamera;
  public bool active = true;

  private static bool Verbose = true;
  // in seconds...
  private static float timeThreshold = 1.0f;

  private bool fixated;
  private bool fixationDone;
  private float timeStamp;
  private GameObject fixCrossRef;
  private FixationCrossColorSwitch colorSwitch;

  void Start()
  {
    if (CrosshairController.Verbose) Debug.Log("Crosshair is alive...");
    this.fixated = false;
    this.fixationDone = false;
    this.timeStamp = 0f;
    this.fixCrossRef = GameObject.FindWithTag("FixationCrossCenter");
    this.colorSwitch = this.fixCrossRef.GetComponent<FixationCrossColorSwitch>();
    this.active = true;
  }

  public void reset()
  {
    this.fixated = false;
    this.fixationDone = false;
    this.timeStamp = 0f;
    this.active = false;
    this.colorSwitch.switchColor(false);
  }

  void Update()
  {
    if (this.fixCrossRef == null || !this.active)
    {
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
      if (CrosshairController.Verbose) Debug.Log("Hit! Collider is " + rayHitInfo.collider);
      if (rayHitInfo.collider != null && rayHitInfo.collider.transform.gameObject == fixCrossRef)
      {
        if (CrosshairController.Verbose) Debug.Log("Fixated.");
        if (!fixated)
        {
          this.timeStamp = 0f;
        }
        this.fixated = true;
        this.colorSwitch.switchColor(true);
        this.timeStamp += Time.deltaTime;
        if (this.timeStamp >= CrosshairController.timeThreshold)
        {
          if (CrosshairController.Verbose) Debug.Log("fixation lasted " + CrosshairController.timeThreshold + "sec (" + timeStamp + ")...");
          this.fixationDone = true;
          this.timeStamp = 0f;
          fixCrossRef = rayHitInfo.collider.transform.gameObject;
        }
      }
      else
      {
        if (CrosshairController.Verbose) Debug.Log("Far away");
        this.colorSwitch.switchColor(false);
        this.fixated = false;
        this.fixationDone = false;
      }
    }
    else
    {
      this.fixated = false;
      this.fixationDone = false;
      this.colorSwitch.switchColor(false);
    }
  }

  public bool FixationCompleted()
  {
    return this.fixationDone;
  }
}