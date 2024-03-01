using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ButtonExtension : MonoBehaviour
{
  [Serializable]
  public class CustomEvent : UnityEvent { }
  [SerializeField]
  List<CustomEvent> Events = new List<CustomEvent>();
  int _count = 0;
  public void ExtendButtonFunctionality()
  {
    Events[_count].Invoke();
    _count++;
    if (_count >= Events.Count)
    {
      _count = 0;
    }
  }
  public void CallFunctionWithIndex(int index)
  {
    Events[index].Invoke();
    _count = index + 1;
  }
  public void ResetCount()
  {
    _count = 0;
  }
}
