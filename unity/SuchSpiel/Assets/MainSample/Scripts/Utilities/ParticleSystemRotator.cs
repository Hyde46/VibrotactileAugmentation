using UnityEngine;
using System.Collections;

public class ParticleSystemRotator : MonoBehaviour {

  public float RotationSpeedScale = 20.0f;

  private ParticleSystem[] particleSystems;

	// Use this for initialization
	void Start () {
    this.particleSystems = this.GetComponentsInChildren<ParticleSystem>();
	}
	
	// Update is called once per frame
	void Update () {
	  foreach (ParticleSystem ps in this.particleSystems)
    {
      ps.transform.Rotate(Vector3.up * Time.deltaTime * this.RotationSpeedScale, Space.World);
    }
	}
}
