using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Delays : MonoBehaviour
{
  public static IEnumerator DelayAction(float time, Action act)
  {
    yield return new WaitForSeconds(time);
    act();
  }
}
