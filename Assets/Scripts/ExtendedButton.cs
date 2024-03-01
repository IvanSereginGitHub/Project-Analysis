using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[System.Serializable]
public class ExtendedButtonGraphics
{
  public Graphic targetGraphic;
  public Color normalColor;
  public Color highlightedColor;
  public Color pressedColor;
  public Color selectedColor;
  public Color disabledColor;
  public float transitionTime;
}

public class ExtendedButton : MonoBehaviour
{
  public bool Interactable;
  public Graphic mainHitboxGraphic;
  public List<ExtendedButtonGraphics> allGraphics = new List<ExtendedButtonGraphics>();
  [System.Serializable]
  public class ExtendedButtonEvent : UnityEvent { }

  public ExtendedButtonEvent onClick = new ExtendedButtonEvent();

  public List<ExtendedButtonEvent> perClick = new List<ExtendedButtonEvent>();
  int clickCount;

  void Awake()
  {
    SelectableGraphic temp = mainHitboxGraphic.gameObject.AddComponent<SelectableGraphic>();
    temp.buttonRef = this;
    temp.isMain = true;
    foreach (ExtendedButtonGraphics t in allGraphics)
    {
      t.targetGraphic.gameObject.AddComponent<SelectableGraphic>().buttonRef = this;
    }
  }

  public void Press()
  {
    if (!Interactable)
      return;

    onClick.Invoke();

    if (perClick.Count == 0)
      return;

    perClick[clickCount].Invoke();

    if (clickCount >= perClick.Count)
    {
      clickCount = 0;
      return;
    }

    clickCount++;
  }

  public static IEnumerator TransitionColor(Graphic applyTo, Color from, Color to, float time)
  {
    if (time == 0)
    {
      applyTo.color = to;
      yield break;
    }

    float timer = 0;
    while (timer <= time)
    {
      applyTo.color = EasingFunction.ConvertToColor(EasingFunction.Ease.None, from, to, timer / time);
      timer += Time.unscaledDeltaTime;
      yield return null;
    }
    applyTo.color = EasingFunction.ConvertToColor(EasingFunction.Ease.None, from, to, 1);
  }
}
