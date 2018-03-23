using UnityEngine;
using System.Collections;

public class GlowControl : MonoBehaviour
{

  public Material glowMaterial;
  private Material backupMaterial;
  private float baseGlow;
  private float maxGlow = 10.0f;

  private float minLerp;
  private float maxLerp;
  private float cLerp;

  private bool active;

  // Use this for initialization
  void Awake()
  {
    this.backupMaterial = this.GetComponent<Renderer>().material;
    this.baseGlow = this.glowMaterial.GetFloat("_MKGlowTexStrength");

    this.cLerp = 0.0f;
    this.minLerp = 1.0f;
    this.maxLerp = 3.0f;

    this.active = false;
  }

  public void ToggleActive(bool active)
  {
    if (active == this.active)
    {
      return;
    }

    if (active)
    {
      this.GetComponent<Renderer>().material = this.glowMaterial;
    } else
    {
      this.GetComponent<Renderer>().material = this.backupMaterial;
    }

    this.active = active;
  }

  // Update is called once per frame
  void Update()
  {
    if (this.active)
      {
        this.glowMaterial.SetFloat("_MKGlowTexStrength", Mathf.Lerp(this.minLerp, this.maxLerp, this.cLerp));

        this.cLerp += 0.5f * Time.deltaTime;

        if (this.cLerp > 1.0f)
          {
            float temp = this.maxLerp;
            this.maxLerp = this.minLerp;
            this.minLerp = temp;
            this.cLerp = 0.0f;
          }
      }

    
  }
}
