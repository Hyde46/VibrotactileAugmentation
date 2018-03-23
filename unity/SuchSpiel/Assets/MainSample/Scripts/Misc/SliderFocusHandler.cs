using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderFocusHandler : BasicFocusHandler {

    public string command;

    private Slider control;

    private Color normalColor;
    private Color highlightColor;
    private Color destinationColor;

    public delegate void FocusSliderEventHandler(string slidercommand);
    public static event FocusSliderEventHandler SliderEvent;

	// Use this for initialization
	void Start () {
        this.control = this.GetComponentInParent<Slider>();
        this.normalColor = this.GetComponent<Renderer>().material.color;
        this.highlightColor = new Color(this.normalColor.r + .75f, this.normalColor.g, this.normalColor.b, .5f);
        this.destinationColor = this.normalColor;

        this.control.value = 0;
	}
	
	// Update is called once per frame
	void Update () {
        this.GetComponent<Renderer>().material.color = Color.Lerp(GetComponent<Renderer>().material.color, this.destinationColor, 0.05f);
        if (!base.isSelected && this.control.value > 0)
        {
            this.control.value--;
        }
	}

    public override void OnFocusStay(RaycastHit hitInformation)
    {
        base.OnFocusStay(hitInformation);
        this.destinationColor = this.highlightColor;
        if (this.control.value >= this.control.maxValue)
        {
            return;
        }
        this.control.value++;

        if (this.control.value >= this.control.maxValue)
        {
            if (SliderFocusHandler.SliderEvent != null) SliderFocusHandler.SliderEvent(this.command);
        }
    }

    public override void OnFocusExit()
    {
        base.OnFocusExit();
        this.destinationColor = this.normalColor;
    }
}
