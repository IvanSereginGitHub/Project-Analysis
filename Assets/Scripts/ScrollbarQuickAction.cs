using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollbarQuickAction : MonoBehaviour, IPointerClickHandler
{
  public float secondClickDelay = 0.2f, timeMultiplier = 1;
  float first_time = 0f;
  bool openPanel = true;
  public void OnPointerClick(PointerEventData eventData)
  {
    if (first_time + secondClickDelay >= Time.unscaledTime)
    {
      StartCoroutine(ChangeScrollbarValue());
    }
    first_time = Time.unscaledTime;
  }

  IEnumerator ChangeScrollbarValue(bool checkAndReverseState = true)
  {
    float timer = 0;
    float startVal = gameObject.GetComponent<Scrollbar>().value;
    // if(checkAndReverseState)
    //   openPanel = gameObject.GetComponent<Scrollbar>().value == 0;
    float endVal = openPanel ? 1 : 0;
    while (timer < 1)
    {
      timer += Time.unscaledDeltaTime * timeMultiplier;
      float val = EasingFunction.ConvertToFloat(EasingFunction.Ease.EaseInOutQuad, startVal, endVal, timer);
      gameObject.GetComponent<Scrollbar>().value = val;
      yield return null;
    }
    if(checkAndReverseState)
      openPanel = !openPanel;
  }

  public void MoveToMinValue()
  {
    openPanel = false;
    StartCoroutine(ChangeScrollbarValue(false));
  }

  public void MoveToMaxValue()
  {
    openPanel = true;
    StartCoroutine(ChangeScrollbarValue(false));
  }
}
