using UnityEngine;
using System.Collections;
using Leap;
[RequireComponent (typeof(SkeletalHand))]
public class StimulationProxy : MonoBehaviour {

  private ParticleSystem indexFingerProxy;
  private ParticleSystem thumbProxy;

	// Use this for initialization
	void Start () {
    SkeletalHand hand = this.gameObject.GetComponent<SkeletalHand>();
    foreach (FingerModel finger in hand.fingers)
    {
      if (finger.GetType() == typeof(SkeletalFinger))
      {
        SkeletalFinger skeletalFinger = finger as SkeletalFinger;

        if (skeletalFinger.fingerType == Finger.FingerType.TYPE_INDEX)
        {
          foreach (Transform bone in skeletalFinger.bones)
          {
            if (bone == null)
              continue;
            ParticleSystem ps = bone.gameObject.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
              this.indexFingerProxy = ps;
              break;
            }
          }
        }
        else if (skeletalFinger.fingerType == Finger.FingerType.TYPE_THUMB)
        {
          foreach (Transform bone in skeletalFinger.bones)
          {
            if (bone == null)
              continue;
            ParticleSystem ps = bone.gameObject.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
              this.thumbProxy = ps;
              break;
            }
          }
        }
      }
    }

    if (this.indexFingerProxy == null)
    {
      Debug.LogError("index finger stimulation proxy is missing...");
    }
    if (this.thumbProxy == null)
    {
      Debug.LogError("thumb stimulation proxy is missing...");
    }

    this.stopParticleSystems();
  }
    /*
  void Update()
  {
    // quite unstyle, but sice scaling affects the local positioning...
    this.thumbProxy.transform.parent.transform.localPosition = Vector3.zero;
    this.thumbProxy.transform.parent.transform.localRotation = Quaternion.identity;

    this.indexFingerProxy.transform.parent.transform.localPosition = Vector3.zero;
    this.indexFingerProxy.transform.parent.transform.localRotation = Quaternion.identity;
  }
    */
	
	public void stopParticleSystems()
  {
    this.indexFingerProxy.Stop();
    this.thumbProxy.Stop();
  }

  public void stimulateIndexFinger()
  {
    this.indexFingerProxy.Play();
  }

  public void stimulateThumb()
  {
    this.thumbProxy.Play();
  }
}
