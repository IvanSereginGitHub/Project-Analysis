using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Popups : MonoBehaviour
{
  public static TextMeshProUGUI popupTMP;
  public static void Popup(string text)
  {
    popupTMP.text = text;
    popupTMP.transform.parent.gameObject.GetComponent<Animator>().Play(0, 0, 0);
    popupTMP.transform.parent.gameObject.GetComponent<Animator>().speed = 1;
  }
  public static void Popup(string text, bool generateDebugLog)
  {
    Popup(text);
    if (generateDebugLog)
      Debug.Log(text);
  }

  public static void FixedPopup(string text)
  {
    popupTMP.text = text;
    popupTMP.transform.parent.gameObject.GetComponent<Animator>().Play(0, 0, 0.2f);
    popupTMP.transform.parent.gameObject.GetComponent<Animator>().speed = 0;
  }

  public static void FixedPopup(string text, bool generateDebugLog)
  {
    FixedPopup(text);
    if (generateDebugLog)
      Debug.Log(text);
  }

  public static void StopPopup()
  {
    popupTMP.transform.parent.gameObject.GetComponent<Animator>().Play(0, 0, 0.6f);
    popupTMP.transform.parent.gameObject.GetComponent<Animator>().speed = 1;
  }
}
