using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// At the moment, this class handles only a single interaction that is realized by the GraspStateController,
/// in more complex scenarios, this setup allows a meta-level control, where this class handles interaction
/// sequences, especially the transitions.
/// </summary>
public class Interactioncontroller : MonoBehaviour
{
    // set to true to obtain some diagnostic command line output; not use at the moment
    private static bool Verbose = false;
    // becomes true in the first update, allows a 'late' start so to say
    public bool Started = false;
    // the main controller for the grasping interaction
    public GraspStateController GraspStateController;
    // states that are used by the GraspStateController and similar controllers
    public enum InteractionStates
    {
        //no interaction occurred
        NONE,
        // Target destroyed
        TARGET_DESTROYED,
        // wrong orientation
        WRONG_ORIENTATION,
        // object left bounds
        OUT_OF_BOUNDS,
        //object landed in ending position
        IN_BOX
    }

    void Update()
    {
        if (!this.Started)
        {
            this.Started = true;
            GraspStateController.IsActive = true;
        }
    }
    // just restarts the grasping
    public void GraspInteractionDone()
    {
        this.GraspStateController.ExternalInteractionStart();
    }
}
