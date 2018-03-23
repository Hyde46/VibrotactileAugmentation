using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EffectorRangeCheck : MonoBehaviour
{

    public delegate void ObjectLeftBoundsHandler(GameObject lostObject);

    public event ObjectLeftBoundsHandler ObjectLeftBounds;

    public List<GameObject> monitoredObjects;

    void Awake()
    {
        this.monitoredObjects = new List<GameObject>();
    }

    void Update()
    {

        if (this.monitoredObjects.Count != 0 && this.ObjectLeftBounds != null)
        {
            // hmm, i admit that this might not be the best design pattern, the initial idea was to
            // modify the list asynchroneously from the block script that receives the signal, which
            // is not a good idea; dynamic modification of collections via an iterator is possible
            // with lambdas, but also not very elegant, hence we take the stupid approach: iterate backwards
            // and remove stuff that is out of bounds...
            for (int i = this.monitoredObjects.Count - 1; i >= 0; i--)
            {
                // TODO: Check whether a monitored object left the range, you might use the
                // render-bounds for this

                if (!this.GetComponent<Renderer>().bounds.Contains(this.monitoredObjects[i].transform.position))
                {
                    GameObject lostObject = this.monitoredObjects[i];
                    this.monitoredObjects.RemoveAt(i);
                    this.ObjectLeftBounds(lostObject);
                }
            }
        }
    }

    public void monitorObject(GameObject monitoredObject)
    {
        this.monitoredObjects.Add(monitoredObject);
    }

    public void clearMonitor()
    {
        this.monitoredObjects.Clear();
    }
}