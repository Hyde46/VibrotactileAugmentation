using UnityEngine;
using System.Collections;

public class ContainerController : MonoBehaviour
{

    private static bool Verbose = false;

    public delegate void ObjectWasReleasedHandler(GameObject gameObject, bool validOrientation);
    public event ObjectWasReleasedHandler ObjectWasReleased;

    public delegate void ObjectWasDestroyedHandler(GameObject gameObject);
    public event ObjectWasDestroyedHandler ObjectWasDestroyed;

    public GameObject ExplosionTemplate;
    public GameObject InvalidExplosionTemplate;
    public GameObject DebrisTemplate;

    private void OnCollisionEnter(Collision other)
    {
        if (ContainerController.Verbose) UnityEngine.Debug.Log("collision with: " + other.gameObject.name + "...");
        // TODO: if the other gameobject has 1. a certain tag ("Target") and 2. has a GraspableObject script
        // attached to it and 3. is grasped, then the ObjectWasReleased event should be invoked, further,
        // the initiateDestruction method should be called, please set the validOrientation flag to
        // true in both cases

        if (ContainerController.Verbose) UnityEngine.Debug.Log("collision with: " + other.gameObject.name + "...");

        if (other.gameObject.tag == "Target")
        {
            GraspableObject graspable = other.gameObject.GetComponent<GraspableObject>();
            if (graspable != null)
            {
                if (graspable.IsGrabbed())
                {
                    bool validOrientation = true;
                    
                    this.initiateDestruction(other.gameObject, validOrientation);
                    this.ObjectWasReleased(other.gameObject, validOrientation);
                }
                else
                {
                    if (ContainerController.Verbose) UnityEngine.Debug.Log("target object was not grapsed: " + other.gameObject.name + "...");
                }
            }
            else
            {
                if (ContainerController.Verbose) UnityEngine.Debug.Log("target object is no graspable: " + other.gameObject.name + "...");
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (ContainerController.Verbose) UnityEngine.Debug.Log("collision with: " + other.name + "...");
        // TODO: if the other gameobject has 1. a certain tag ("Target") and 2. has a GraspableObject script
        // attached to it and 3. is grasped, then the ObjectWasReleased event should be invoked, further,
        // the initiateDestruction method should be called, please set the validOrientation flag to
        // true in both cases

        if (ContainerController.Verbose) UnityEngine.Debug.Log("collision with: " + other.gameObject.name + "...");

        if (other.gameObject.tag == "Target")
        {
            GraspableObject graspable = other.gameObject.GetComponent<GraspableObject>();
            if (graspable != null)
            {
                if (graspable.IsGrabbed())
                {
                    bool validOrientation = true;

                    this.initiateDestruction(other.gameObject, validOrientation);
                    this.ObjectWasReleased(other.gameObject, validOrientation);
                }
                else
                {
                    if (ContainerController.Verbose) UnityEngine.Debug.Log("target object was not grapsed: " + other.gameObject.name + "...");
                }
            }
            else
            {
                if (ContainerController.Verbose) UnityEngine.Debug.Log("target object is no graspable: " + other.gameObject.name + "...");
            }
        }
    }

    public void initiateDestruction(GameObject bottle, bool validOrientation)
    {
        GameObject explosion = GameObject.Instantiate(validOrientation ? this.ExplosionTemplate : this.InvalidExplosionTemplate, bottle.transform.position, bottle.transform.rotation) as GameObject;
        GameObject.Destroy(explosion, 1.0f);
        int numberOfDebris = Random.Range(15, 50);
        for (int i = 0; i < numberOfDebris; i++)
        {
            GameObject debris = GameObject.Instantiate(this.DebrisTemplate, bottle.transform.position, bottle.transform.rotation) as GameObject;
            Rigidbody debrisBody = debris.GetComponent<Rigidbody>();
            debrisBody.useGravity = false;

            GameObject.Destroy(debris, 1.0f);
        }
    }

    public void initiateDestructionByGrabbing(GameObject bottle)
    {
        int numberOfDebris = Random.Range(15, 50);
        for (int i = 0; i < numberOfDebris; i++)
        {
            GameObject debris = GameObject.Instantiate(this.DebrisTemplate, bottle.transform.position, bottle.transform.rotation) as GameObject;
            Rigidbody debrisBody = debris.GetComponent<Rigidbody>();
            debrisBody.useGravity = false;

            GameObject.Destroy(debris, 1.0f);
        }

        this.ObjectWasDestroyed(bottle);
    }
}
