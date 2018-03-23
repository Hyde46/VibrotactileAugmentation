using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasicFocusHandler : MonoBehaviour {

    protected bool isSelected = false;

    public void OnElementHit(RaycastHit hitInformation)
    {
        if (this.isSelected)
        {
            this.OnFocusStay(hitInformation);
        }

        else
        {
            this.OnFocusEnter(hitInformation);
            this.isSelected = true;
        }
    }

    public void OnObjectExit()
    {
        this.isSelected = false;
        this.OnFocusExit();
    }

    public virtual void OnFocusEnter(RaycastHit hitInformation){}

    public virtual void OnFocusStay(RaycastHit hitInformation) { }

    public virtual void OnFocusExit() { }

}
