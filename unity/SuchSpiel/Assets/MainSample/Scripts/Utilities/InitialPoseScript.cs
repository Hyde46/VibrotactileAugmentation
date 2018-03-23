using System;
using UnityEngine;

public class InitialPoseScript : MonoBehaviour
{
    public string targetTransformParentName;
    public string targetTransformName;
    public GameObject targetObject;

    public bool checkContact()
    {
        if (this.targetObject != null)
        {
            if (this.gameObject.GetComponent<Renderer>().bounds.Contains(this.targetObject.transform.position))
            {
                return true;
            }
        }
        else
        {
            if (this.targetTransformName != null && this.targetTransformParentName != null)
            {
                GameObject parent = GameObject.Find(this.targetTransformParentName);
                if (parent)
                {
                    Transform target = this.targetTransformName == this.targetTransformParentName ? parent.transform : parent.transform.Find(this.targetTransformName);
                    if (target)
                    {
                        this.targetObject = target.gameObject;

                        if (this.gameObject.GetComponent<Renderer>().bounds.Contains(this.targetObject.transform.position))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}