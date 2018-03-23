using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GazeBasedButtonProgressor : MonoBehaviour {

    private static bool Verbose = false;

    public delegate void SliderEventHandler();
    public static event SliderEventHandler SliderEvent;

    public Slider Slider;
    public Collider TargetTrigger;

    public Camera TargetCamera;
    public bool SliderProgressActive = false;
    private bool fixated;

	// Use this for initialization
	void Awake () {
        this.fixated = false;
        this.SliderProgressActive = false;

        this.Slider.minValue = 0;
        this.Slider.maxValue = 250;
	}

    public void HideAndResetSlider()
    {
        this.Slider.value = 0;
        this.Slider.transform.root.gameObject.SetActive(false);
        this.SliderProgressActive = false;
    }

    public void ActivateAndResetSlider()
    {
        this.Slider.value = 0;
        this.Slider.transform.root.gameObject.SetActive(true);
        this.SliderProgressActive = true;
        UnityEngine.Debug.Log("slider activated...");
    }
	
	// Update is called once per frame
	void Update () {
        if (!this.SliderProgressActive)
        {
            return;
        }

        Vector3 cameraPosition = this.TargetCamera.transform.position;
		Vector3 cameraForwardOrientation = this.TargetCamera.transform.rotation * Vector3.forward;
		Ray rayFromCamera = new Ray(cameraPosition, cameraForwardOrientation);
		RaycastHit rayHitInfo;
        if (Physics.Raycast(rayFromCamera, out rayHitInfo))
        {
            if (GazeBasedButtonProgressor.Verbose) UnityEngine.Debug.Log("hit something: " + rayHitInfo.collider.transform.gameObject.name + "..." + (rayHitInfo.collider.transform.gameObject == this.TargetTrigger.transform.gameObject));

            if (rayHitInfo.collider.transform.gameObject == this.TargetTrigger.transform.gameObject)
            {
                if (GazeBasedButtonProgressor.Verbose) UnityEngine.Debug.Log("slider progress...");
                this.fixated = true;
                this.Slider.value += 1;
                if (this.Slider.value >= this.Slider.maxValue)
                {
                    this.SliderProgressActive = false;
                    //UnityEngine.Debug.Log("slider done...");
                    GazeBasedButtonProgressor.SliderEvent();
                }
            }
        }
	}
}
