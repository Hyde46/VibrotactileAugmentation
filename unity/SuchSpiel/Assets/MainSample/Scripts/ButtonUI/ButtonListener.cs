using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
// this abstract class provides can be considered as a listener.
// it sets up an own event queue which is forwarded to the actual
// listener imlementation
public abstract class ButtonListener : MonoBehaviour
{

    public ToggleButton[] relevantButtons;

    public class ButtonEvent
    {
        public string name = "";
        public bool state = false;
        public ButtonEvent(string name, bool state)
        {
            this.name = name;
            this.state = state;
        }
    }

    protected ArrayList eventQueue;

    // Use this for initialization
    void Start()
    {
        this.eventQueue = new ArrayList();
        foreach (ToggleButton button in this.relevantButtons)
        {
            button.ButtonWasPressed += new ToggleButton.ButtonWasPressedHandler(this.OnButtonPressed);
        }
        this.init();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (ButtonEvent buttoneEvent in this.eventQueue)
        {
            this.ProcessButtonEvent(buttoneEvent);
        }
        this.eventQueue.Clear();
        this.LocalUpdate();
    }
    // fill the event queue
    void OnButtonPressed(string name, bool state)
    {
        this.eventQueue.Add(new ButtonEvent(name, state));
    }

    public abstract void init();

    public abstract void LocalUpdate();

    public abstract void ProcessButtonEvent(ButtonEvent buttonEvent);
}
