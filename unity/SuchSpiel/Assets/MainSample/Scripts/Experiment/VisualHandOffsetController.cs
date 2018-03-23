using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualHandOffsetController : MonoBehaviour {

    /// <summary>
    /// Aus dem HandController ziehen wir uns die Position der Haende
    /// </summary>
    //public ExperimentHandController handController;

    public GameObject InnerBound;
    public GameObject OuterBound;
    /// <summary>
    /// Der maximale Drift, nehmt keine allzu hohen Werte, die Szene ist sehr klein skaliert
    /// </summary>
    public float DriftFactor = 0.0f;
    public bool ApplyDrift = false;
    /// <summary>
    /// Startpunkt auf der Teifenachse ab dem der Drift angewendet werden soll, verschiebt das Objekt um den Startpunkt zu aendern.
    /// </summary>
    private float startZ;
    /// <summary>
    /// Endpunkt auf der Teifenachse ab dem der Drift maximal sein soll, verschiebt das Objekt um den Endpunkt zu aendern.
    /// </summary>
    private float endZ;

    void Start()
    {
        // zuweisen der z-Koordinaten
        this.startZ = this.InnerBound.transform.position.z;
        this.endZ = this.OuterBound.transform.position.z;
    }
    /*
    // wir nehmen hier das LateUpdate um sicherzugehen, dass wir den Drift erst andwenden, wenn das Update der Handmodelle gemacht wurde
    void LateUpdate()
    {
        if (!this.ApplyDrift)
        {
            return;
        }
        // Schaut mal in den HandController, dort gibt es drei Methoden, die Handmodelle abzufragen
        // zwei liefern Kopien, eine liefert Referenzen, geht die Werte durch und
        // 1. findet raus, wo die Handflaeche auf der z-Achse liegt
        // 2. berechnet die Staerke des Drifts
        // 3. wendet den Drift an
        // Wenn ihr ein Handmodel habt, dass z.B. model heisst, dann kommt ihr ueber model.gameObject an das GameObject ran; dessen Transform
        // kann man nach child Objekten durchsuchen, z.B. einem das 'Palm' heisst.
        if (this.handController.getCurrentHandModel() == null)
        {
            return;
        }
        HandModel model = this.handController.getCurrentHandModel();
        GameObject palm = model.transform.FindChild("palm").gameObject;

        if (palm.transform.position.z > this.startZ)
        {
            float relativeZ = (palm.transform.position.z < this.endZ ? palm.transform.position.z : this.endZ) - this.startZ;

            float drift = this.DriftFactor * relativeZ / (this.endZ - this.startZ);

            model.gameObject.transform.Translate(new Vector3(drift, 0.0f, 0.0f));
        }

        model = this.handController.getCurrentHandPhysicsModel();

        palm = model.transform.FindChild("palm").gameObject;

        if (palm.transform.position.z > this.startZ)
        {
            float relativeZ = (palm.transform.position.z < this.endZ ? palm.transform.position.z : this.endZ) - this.startZ;

            float drift = this.DriftFactor * relativeZ / (this.endZ - this.startZ);

            model.gameObject.transform.Translate(new Vector3(drift, 0.0f, 0.0f));
        }
    }
    */
    public Vector3 getOffset(HandModel model)
    {
        if (model == null)
        {
            return Vector3.zero;
        }

        if (!this.ApplyDrift)
        {
            return Vector3.zero;
        }

        GameObject palm = model.transform.Find("palm").gameObject;

        if (palm.transform.position.z > this.startZ)
        {
            float relativeZ = (palm.transform.position.z < this.endZ ? palm.transform.position.z : this.endZ) - this.startZ;

            float drift = this.DriftFactor * relativeZ / (this.endZ - this.startZ);

            return new Vector3(drift, 0.0f, 0.0f);
        }
        else
        {
            return Vector3.zero;
        }
    }
}
