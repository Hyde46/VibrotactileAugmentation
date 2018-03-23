using UnityEngine;
using System.Collections;

public class FixationCrossColorSwitch : MonoBehaviour {

  public Material validMaterial;

  private Renderer[] childRenderer;
  private Material[] backUpMaterials;

	// Use this for initialization
	void Start () {
    MeshRenderer[] obtainedRenderer = this.gameObject.GetComponentsInChildren<MeshRenderer>();
    this.childRenderer = new MeshRenderer[obtainedRenderer.Length - 1];
    int counter = 0;
    for (int i = 0; i < obtainedRenderer.Length; i++) {
      if (!obtainedRenderer[i].Equals(this.gameObject.GetComponent<MeshRenderer>())) {
        this.childRenderer[counter] = obtainedRenderer[i];
        counter++;
      }
    }

    this.backUpMaterials = new Material[this.childRenderer.Length];
    for (int i = 0; i < this.childRenderer.Length; i++) {
      this.backUpMaterials[i] = childRenderer[i].material;
    }
	}
	
  public void switchColor(bool valid)
  {
    for (int i = 0; i < this.childRenderer.Length; i++) {
      childRenderer[i].material = valid ? this.validMaterial : this.backUpMaterials[i];
    }
  }
}
