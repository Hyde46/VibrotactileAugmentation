using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheckMarkFocusHandler : BasicFocusHandler {

    public string command;

    private int counter = 0;

    private Toggle control;

    private Color normalColor;
    private Color highlightColor;
    private Color destinationColor;

    public delegate void FocusToggleEventHandler(string togglecommand);
    public static event FocusToggleEventHandler ToggleEvent;

    void Start()
    {
        this.control = this.GetComponentInParent<Toggle>();
        this.normalColor = this.GetComponent<Renderer>().material.color;
        this.highlightColor = new Color(this.normalColor.r + .75f, this.normalColor.g, this.normalColor.b, .5f);
        this.destinationColor = this.normalColor;
    }

    void Update()
    {
        this.GetComponent<Renderer>().material.color = Color.Lerp(GetComponent<Renderer>().material.color, this.destinationColor, 0.05f);
    }

    public override void OnFocusStay(RaycastHit hitInformation)
    {
        base.OnFocusStay(hitInformation);
        this.destinationColor = this.highlightColor;
        if (this.control.isOn)
        {
            return;
        }
        this.counter++;
        if (this.counter >= 20 && !this.control.isOn)
        {
            this.control.isOn = true;
            if (CheckMarkFocusHandler.ToggleEvent != null) CheckMarkFocusHandler.ToggleEvent(this.command);
        }
    }

    public override void OnFocusExit()
    {
        base.OnFocusExit();
        this.destinationColor = this.normalColor;
        this.counter = 0;
    }
}
