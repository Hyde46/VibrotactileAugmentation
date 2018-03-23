using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InnerObject : MonoBehaviour {

    public GameObject parentO;
    private SeekObject so;
 

	void Start () {
        this.so = parentO.GetComponent<SeekObject>();

	}
    
    private void OnTriggerStay(Collision collision)
    {
        SetInnerCollision();
    }
    private void OnTriggerExit(Collision collision)
    {
        SetInnerCollision();
    }

    private void SetInnerCollision()
    {
        Debug.Log("function");
        for (int i = 0; i < so.targetTransformParentName.Length; i++)
        {
            

            string parentName = so.targetTransformParentName[i];
            GameObject parent = GameObject.Find(parentName);
            if (parent)
            {
                Transform target = so.targetTransformName == parentName ? parent.transform : parent.transform.Find(so.targetTransformName);
                if (target)
                {
                    if (this.GetComponent<Renderer>().bounds.Contains(target.transform.position))
                    {
                        so.innerActive[i]= true;
                        Debug.Log("true");
                    }
                    else
                    {
                        so.innerActive[i] = false;
                    }
                    continue;
                }
            }
            
        }
    }

}
