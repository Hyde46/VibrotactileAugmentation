using UnityEngine;
using System.Collections;

public class ToggleButton : AbstractButton {

  public delegate void ButtonWasPressedHandler( string name, bool state );
  // causes null pointer exception in case no listener was assigned...
  public event ButtonWasPressedHandler ButtonWasPressed;

  public bool checkButtonGroup;
  public ButtonListener listener;

  public float onDistance = 0.0f;
  public float offDistance = 0.0f;

  public ButtonGraphics onGraphics;
  public ButtonGraphics offGraphics;
  public ButtonGraphics fillGraphics;
  public ButtonGraphics strokeGraphics;

  private bool toggle_state_;

  private ArrayList otherButtons;

  public void ButtonTurnsOn()
  {
    TurnsOnGraphics();
  }
  
  public void ButtonTurnsOff()
  {
    TurnsOffGraphics();
  }
  
  private void TurnsOnGraphics()
  {
    onGraphics.SetActive(true);
    offGraphics.SetActive(false);
  }
  
  private void TurnsOffGraphics()
  {
    onGraphics.SetActive(false);
    offGraphics.SetActive(true);
  }
  
  private void UpdateGraphics()
  {
    Vector3 position = GetPosition();
    onGraphics.transform.localPosition = position;
    offGraphics.transform.localPosition = position;
    Vector3 bot_position = position;
    bot_position.z = Mathf.Max(bot_position.z, onDistance);
    strokeGraphics.transform.localPosition = bot_position;
    Vector3 mid_position = position;
    mid_position.z = (position.z + bot_position.z) / 2.0f;
    fillGraphics.transform.localPosition = mid_position;
  }
  
  public override void Awake()
  {
    base.Awake();
    onDistance = Mathf.Min(onDistance, triggerDistance - cushionThickness - 0.001f);
    offDistance = Mathf.Min(offDistance, triggerDistance - cushionThickness - 0.001f);
    TurnsOffGraphics();

    ArrayList buttons = new ArrayList();
    for (int i = 0; i < this.transform.parent.childCount; i++)
    {
      ToggleButton button = this.transform.parent.GetChild(i).GetComponent("ToggleButton") as ToggleButton;
      if (button != null)
      {
        if (button != this)
        {
          buttons.Add(button);
        }
      }
    }

    this.otherButtons = buttons;
  }
  
  public override void Update()
  {
    base.Update();
    UpdateGraphics();
  }

  public override void ButtonReleased()
  {
  }
  
  public override void ButtonPressed()
  {
    if (toggle_state_ == false)
    {
      ButtonWasPressed(this.name, true);
      SetMinDistance(onDistance);
      toggle_state_ = !toggle_state_;
      if (this.checkButtonGroup)
      {
        foreach (ToggleButton otherButton in this.otherButtons)
        {
          otherButton.silentTurnOff();
        }
      }
    } 
    else
    {
      if (!this.checkButtonGroup)
      {
        ButtonWasPressed(this.name, false);
        ButtonTurnsOff();
        SetMinDistance(offDistance);
        toggle_state_ = !toggle_state_;
      }
      else
      {
        // make sure one remains active
        bool lastActive = true;
        foreach (ToggleButton otherButton in this.otherButtons)
        {
          if (otherButton.isOn())
          {
            lastActive = false;
            break;
          }
        }

        if (!lastActive)
        {
          ButtonWasPressed(this.name, false);
          ButtonTurnsOff();
          SetMinDistance(offDistance);
          toggle_state_ = !toggle_state_;
        }
      }
    }
  }

  public void silentTurnOff()
  {
    ButtonWasPressed(this.name, false);
    ButtonTurnsOff();
    SetMinDistance(offDistance);
    toggle_state_ = false;
  }

  public bool isOn()
  {
    return this.toggle_state_;
  }
}