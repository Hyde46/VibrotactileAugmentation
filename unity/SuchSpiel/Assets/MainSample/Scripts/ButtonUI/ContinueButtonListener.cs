using UnityEngine;
using System.Collections;

public class ContinueButtonListener : ButtonListener
{

    public SampleTrialController master;

    public override void init() { }
    
    public override void LocalUpdate() { }
    // command is passed on to expcontroller, 
    // depending on the actually pressed button, other functions are called
    public override void ProcessButtonEvent(ButtonEvent buttonEvent)
    {
    }
}
