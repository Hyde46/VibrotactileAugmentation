using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class ToSeek : MonoBehaviour
{

    public int[] feedbackValues;
    public Text feedbackText;

    public PortInterface pInterface;


    private void Start()
    {
        feedbackValues = new int[] { 0, 0, 0, 0, 0 };
        pInterface = gameObject.GetComponent<PortInterface>();
        pInterface.OpenPort();
    }

    private void Update()
    {
		if (null == GameObject.Find ("RigidRoundHand(Clone)")) {
			for(int i=0;i<feedbackValues.Length;i++){
				feedbackValues [i] = 0;
			}
		
		}
			

        feedbackText.text = "(" + feedbackValues[0] + "," + feedbackValues[1] + "," + feedbackValues[2] + "," + feedbackValues[3] + "," + feedbackValues[4] + ")";

		pInterface.SendData(feedbackValues[0], feedbackValues[1], feedbackValues[2], feedbackValues[3], feedbackValues[4]);

    }

}