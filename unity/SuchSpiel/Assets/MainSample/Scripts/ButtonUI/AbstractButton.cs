using UnityEngine;
using System.Collections;

// required for physical interaction
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public abstract class AbstractButton : MonoBehaviour {

  public float spring = 50.0f;
  public float triggerDistance = 0.025f;
  public float cushionThickness = 0.005f;
  
  protected bool is_pressed_;
  protected float min_distance_;
  protected float max_distance_;

  protected float initialYPosition;
  protected float initialXPosition;
  
  public abstract void ButtonReleased();
  public abstract void ButtonPressed();
  
  public float GetPercent()
  {
    return Mathf.Clamp(transform.localPosition.z / triggerDistance, 0.0f, 1.0f);
  }
  
  public Vector3 GetPosition()
  {
    if (triggerDistance == 0.0f)
      return Vector3.zero;
    
    Vector3 position = transform.localPosition;
    position.z = GetPercent() * triggerDistance;
    return position;
  }
  
  protected void SetMinDistance(float distance)
  {
    min_distance_ = distance;
  }
  
  protected void SetMaxDistance(float distance)
  {
    max_distance_ = distance;
  }
  // buttons can't move from their initial position, except in the z-dimension when pressed
  protected virtual void ApplyConstraints()
  {
    Vector3 local_position = transform.localPosition;
    local_position.x = this.initialXPosition;
    local_position.y = this.initialYPosition;
    local_position.z = Mathf.Clamp(local_position.z, min_distance_, max_distance_);
    transform.localPosition = local_position;
  }
  // apply spring force to return into initial position
  protected void ApplySpring()
  {
    GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0.0f, 0.0f, -spring * (transform.localPosition.z)));
  }

  private int triggerCounter = 0;

  // check for button presses / releases
  protected void CheckTrigger()
  {
    if (is_pressed_ == false)
    {
      if (transform.localPosition.z > triggerDistance)
      {
        //is_pressed_ = true;
        this.triggerCounter += 1;
          if (this.triggerCounter > 10)
          {
              is_pressed_ = true;
              ButtonPressed();
              this.triggerCounter = 0;
          }
        //  ButtonPressed();
      }
    }
    else if (is_pressed_ == true)
    {
      if (transform.localPosition.z < (triggerDistance - cushionThickness))
      {
        is_pressed_ = false;
        this.triggerCounter = 0;
        ButtonReleased();
      }
    }
  }
  // initialization, might be overwritten by heirs
  public virtual void Awake()
  {
    this.initialXPosition = this.transform.localPosition.x;
    this.initialYPosition = this.transform.localPosition.y;
    is_pressed_ = false;
    cushionThickness = Mathf.Clamp(cushionThickness, 0.0f, triggerDistance - 0.001f);
    min_distance_ = 0.0f;
    max_distance_ = float.MaxValue;
    /*
    Collider[] allColliders = this.gameObject.transform.root.gameObject.GetComponentsInChildren<Collider>();
    for(int i = 0; i < allColliders.Length; i++)
    {
      for(int j = 0; j < allColliders.Length; j++)
      {
        if (i != j)
        {
          Physics.IgnoreCollision(allColliders[i], allColliders[j], true);
        }
      }
    }
    */
  }
  
  public virtual void Update()
  {
    ApplySpring();
    ApplyConstraints();
    CheckTrigger();
  }
}