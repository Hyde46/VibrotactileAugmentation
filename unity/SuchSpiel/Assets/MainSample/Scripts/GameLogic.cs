using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class GameLogic : MonoBehaviour
{
    //Array with the Objects
    public GameObject[] objectsToFind;

    //Current used GameObjects and their places
    public GameObject placeLeft;
    public GameObject placeRight;
    public GameObject placeShowObject;
    private GameObject leftObject;
    private GameObject rightObject;
    private GameObject leftInnerObject;
    private GameObject rightInnerObject;
    private GameObject showObject;
    private bool shownObjectPlacedLeft;
    public GameObject leftAnswer;
    public GameObject rightAnswer;
    private bool answerGiven = false;

    private System.Random rng = new System.Random();

    //Strings for the Fingertips
    private string[] targetTransformParentName = { "thumb", "index", "middle", "ring", "pinky" };
    private string targetTransformName = "bone3";


    //Testing only
    public Text feedbackText;
    private void Start()
    {
        ChangeObjects();
    }
    public void ChangeObjects()
    {
        Destroy(leftObject);
        Destroy(rightObject);
        Destroy(showObject);
        //Destroy(rightInnerObject);
        //Destroy(leftInnerObject);
        var numberLeft = rng.Next(0, objectsToFind.Length);
        var alreadyUsed = numberLeft;
        leftObject = MakeObject(objectsToFind[numberLeft], placeLeft.transform.position);
        //leftInnerObject = MakeObject(innerObjects[numberLeft], placeLeft.transform.position);
        //leftObject.transform.parent = placeShowObject.transform;
        leftObject.GetComponent<MeshRenderer>().enabled = false;
        var numberRight = rng.Next(0, objectsToFind.Length - 1);
        if (numberRight == alreadyUsed)
            numberRight += 1;

        rightObject = MakeObject(objectsToFind[numberRight], placeRight.transform.position);
        //rightInnerObject = MakeObject(innerObjects[numberRight], placeRight.transform.position);
        //rightObject.transform.parent = placeShowObject.transform;
        rightObject.GetComponent<MeshRenderer>().enabled = false;
        shownObjectPlacedLeft = rng.NextDouble() >= 0.5;
        if (shownObjectPlacedLeft)
        {
            showObject = MakeObject(objectsToFind[numberLeft], placeShowObject.transform.position);
        }
        else
        {
            showObject = MakeObject(objectsToFind[numberRight], placeShowObject.transform.position);
        }

        //var test=showObject.GetComponent<ToSeek>().feedbackText;
        //showObject.GetComponent<ToSeek>().object2 = rightObject;

    }

    private GameObject MakeObject(GameObject go, Vector3 p)
    {
        GameObject o = Instantiate(go);
        o.transform.position = p + new Vector3(0, 0.5F * o.transform.localScale.y, 0);

        //ToSeek seekSkript = o.AddComponent<ToSeek>();
        //seekSkript.feedbackText = feedbackText;
        return o;

    }

    private bool GiveAnswer(GameObject answerBlock)
    {
        GameObject targetObject;
        for (int i = 0; i < targetTransformParentName.Length; i++)
        {
            string parentName = targetTransformParentName[i];
            GameObject parent = GameObject.Find(parentName);
            if (parent)
            {
                Transform target = this.targetTransformName == parentName ? parent.transform : parent.transform.Find(this.targetTransformName);
                if (target)
                {
                    targetObject = target.gameObject;

                    if (answerBlock.gameObject.GetComponent<Renderer>().bounds.Contains(targetObject.transform.position))
                    {
                        continue;
                    }
                }
            }
            return false;
        }

        return true;
    }
    public void GiveAnswer(bool answerLeft)
    {
        if (answerLeft==shownObjectPlacedLeft)
        {
            Debug.Log("Richtig!");
            showObject.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
        }
        else
        {
            Debug.Log("Falsch!");
            showObject.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
        }
        leftObject.GetComponent<MeshRenderer>().enabled = true;
        rightObject.GetComponent<MeshRenderer>().enabled = true;

    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ChangeObjects();
        }
    }
        
}
