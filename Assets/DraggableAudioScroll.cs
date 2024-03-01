using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableAudioScroll : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
  public RectTransform draggableAudioParent, spectrum;
  public bool dragToRight;

  Vector2 sizeDif;
  Vector3 mouseDif;
  DraggableAudio drAud;
  Vector3 spectrumPos;

  public float sizeChange;

  void Awake()
  {
    drAud = draggableAudioParent.GetComponent<DraggableAudio>();
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    mouseDif = Input.mousePosition;
    sizeDif = draggableAudioParent.sizeDelta;
    draggableAudioParent.SetPivotRelativePosition(new Vector2(dragToRight.ToZeroOne(), draggableAudioParent.pivot.y));
    spectrumPos = spectrum.position;
  }

  public void OnDrag(PointerEventData eventData)
  {
    Vector2 sizeDelta = new Vector2(Mathf.Clamp(sizeDif.x + (mouseDif - Input.mousePosition).x * -dragToRight.ToNegativePositive(), 0, drAud.defaultSizeDelta.x), sizeDif.y);
    if (dragToRight)
    {
      drAud.cutOut += (mouseDif - Input.mousePosition).x / drAud.pixelsPerSecond;
    }
    else
    {
      drAud.cutIn += -(mouseDif - Input.mousePosition).x / drAud.pixelsPerSecond;
    }
    draggableAudioParent.sizeDelta = sizeDelta;

    mouseDif = Input.mousePosition;
    sizeDif = draggableAudioParent.sizeDelta;
    spectrum.position = spectrumPos;
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    draggableAudioParent.SetPivotRelativePosition(new Vector2(0.5f, draggableAudioParent.pivot.y));
  }

}
