using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeekObject : MonoBehaviour {

    public string[] targetTransformParentName = { "thumb", "index", "middle", "ring", "pinky" };
    public string targetTransformName = "bone3";
    public int[] feedbackValues;

    public ToSeek ts;
    public GameObject child;

    private void Start()
    {
        ts = GameObject.FindGameObjectWithTag("ToSeek").GetComponent<ToSeek>();
        feedbackValues = ts.feedbackValues;
		this.child = this.transform.GetChild (0).gameObject;
    }


    void OnTriggerStay(Collider other)
    {
		Debug.Log ("stay");
        CheckForFeedback();

    }
    private void CheckForFeedback()
    {
        for (int i = 0; i < this.targetTransformParentName.Length; i++)
        {

            string parentName = this.targetTransformParentName[i];
            GameObject parent = GameObject.Find(parentName);
			if (parent) {
				Transform target = this.targetTransformName == parentName ? parent.transform : parent.transform.Find (this.targetTransformName);
				if (target) {
					bool ininner = child.GetComponent<Renderer> ().bounds.Contains (target.transform.position);

					if (!ininner && this.GetComponent<Renderer> ().bounds.Contains (target.transform.position)) {
						this.feedbackValues [i] = 100;
						continue;
					}
 
				}
			} 
			this.feedbackValues[i] = 0;
			          
        }
    }
    void OnTriggerExit(Collider other)
	{
		
		Debug.Log ("Exit");
		for(int i=0;i<feedbackValues.Length;i++){
			feedbackValues [i] = 0;
		}

    }


}
