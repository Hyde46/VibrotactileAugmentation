using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusController : MonoBehaviour {

    public bool FocusControlActive = true;
    public Camera TargetCamera;
	private BasicFocusHandler previousSelection;

    void Update()
    {
        if (this.FocusControlActive) 
        {
            this.performFocusUpdate();
        }
    }

    private void performFocusUpdate()
    {
        Vector3 cameraPosition = this.TargetCamera.transform.position;
        Vector3 cameraForwardOrientation = this.TargetCamera.transform.rotation * Vector3.forward;
        Ray rayFromCamera = new Ray(cameraPosition, cameraForwardOrientation);
        RaycastHit rayHitInfo;

        if (Physics.Raycast(rayFromCamera, out rayHitInfo))
        {
            GameObject objectInFocus = rayHitInfo.collider.gameObject;

            BasicFocusHandler selection = objectInFocus.GetComponent<BasicFocusHandler>();

            if (selection != null)
            {
                if (previousSelection == null)
                {
                    previousSelection = selection;
                }

                else if (selection != previousSelection)
                {
                    previousSelection.OnObjectExit();
                    previousSelection = selection;
                }
                selection.OnElementHit(rayHitInfo);
            }

            else if (previousSelection != null)
            {
                previousSelection.OnObjectExit();
                previousSelection = null;
            }
        }

        else if (previousSelection != null)
        {
            previousSelection.OnObjectExit();
            previousSelection = null;
        }
    }
}