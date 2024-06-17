using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableAudio : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler
{
  public AudioClip audClip;
  [SerializeField]
  float _cutIn, _cutOut;
  public float cutIn
  {
    get { return _cutIn; }
    set { _cutIn = value < 0 ? _cutIn : value; }
  }
  public float cutOut
  {
    get { return _cutOut; }
    set { _cutOut = value < 0 ? _cutOut : value; }
  }
  //music cutin, cutout
  // public float cutIn, cutOut;
  public RawImage audioSpectrum;
  public Vector2 scaleVector = new Vector2(1, 1);
  public Vector2 defaultSizeDelta;
  public GameObject dragIndicator;
  public float pixelsPerSecond;
  RectTransform rectTr;
  [SerializeField]
  RectTransform spectrumTransform;

  Vector3 mouseDif, objDif;

  float[] songSpectrum;

  void Awake()
  {
    rectTr = gameObject.GetComponent<RectTransform>();
    rectTr.sizeDelta = new Vector2(audClip.length * pixelsPerSecond, rectTr.sizeDelta.y);
    spectrumTransform.sizeDelta = new Vector2(rectTr.sizeDelta.x, spectrumTransform.sizeDelta.y);
    defaultSizeDelta = rectTr.sizeDelta;
    songSpectrum = new float[audClip.samples * audClip.channels];
    audClip.GetData(songSpectrum, 0);
    UpdateSpectrum();
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    mouseDif = Input.mousePosition;
    objDif = transform.position;
    transform.localScale = scaleVector;
    dragIndicator.SetActive(true);
  }
  public void OnPointerUp(PointerEventData eventData)
  {
    transform.localScale = Vector2.one;
    dragIndicator.SetActive(false);
  }

  public void OnDrag(PointerEventData eventData)
  {
    transform.position = objDif - (mouseDif - Input.mousePosition);
  }

  void UpdateSpectrum()
  {
    audioSpectrum.texture = MusicSelection.GenerateSpectrum(audClip, new Vector2(rectTr.sizeDelta.x, 185), songSpectrum, cutIn, cutOut);
  }

  public void OnEndDrag(PointerEventData eventData)
  {

  }
}
