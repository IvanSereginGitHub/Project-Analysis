using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[HideInInspector]
public class SelectableGraphic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler, IPointerClickHandler
{
  public bool isMain;
  public ExtendedButton buttonRef;
  Graphic graphic;
  ExtendedButtonGraphics exButtonRef;
  bool startedHolding;

  public enum TransitionStatus
  {
    Click,
    Highlight,
    Normal
  }
  public TransitionStatus transitionStatus = TransitionStatus.Normal;
  void Start()
  {
    graphic = gameObject.GetComponent<Graphic>();
    exButtonRef = buttonRef.allGraphics.Find(x => x.targetGraphic == graphic);

    MakeTransition(transitionStatus);
  }
  public void OnPointerUp(PointerEventData eventData)
  {
    if (!isMain)
      return;
    transitionStatus = TransitionStatus.Highlight;
    MakeTransition(transitionStatus);
    startedHolding = false;
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (!isMain)
      return;
    transitionStatus = TransitionStatus.Click;
    MakeTransition(transitionStatus);
    startedHolding = true;
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    if (!isMain)
      return;
    transitionStatus = TransitionStatus.Highlight;
    MakeTransition(transitionStatus);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    if (!isMain || startedHolding)
        return;
    transitionStatus = TransitionStatus.Normal;
    MakeTransition(transitionStatus);
  }

  void MakeTransition(TransitionStatus status)
  {
    foreach (ExtendedButtonGraphics t in buttonRef.allGraphics)
    {
      SelectableGraphic sel = t.targetGraphic.gameObject.GetComponent<SelectableGraphic>();
      sel.StopAllCoroutines();
      Color currentColor = t.targetGraphic.color;
      Color finalColor = currentColor;
      switch (status)
      {
        case TransitionStatus.Click:
          finalColor = t.pressedColor;
          break;
        case TransitionStatus.Highlight:
          finalColor = t.highlightedColor;
          break;
        case TransitionStatus.Normal:
          finalColor = t.normalColor;
          break;
      }
      sel.StartCoroutine(ExtendedButton.TransitionColor(t.targetGraphic, currentColor, finalColor, t.transitionTime));
    }
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    if (!isMain)
      return;
    transitionStatus = TransitionStatus.Highlight;
    MakeTransition(transitionStatus);
    buttonRef.Press();
  }
}
