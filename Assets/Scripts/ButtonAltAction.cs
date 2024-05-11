using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public enum EventType
{
  button,
  inputField,
  dropdown
}

public interface IEventInterface
{
  public EventType Type { get; set; }
}
//generic version
[Serializable]
public class EventClass : IEventInterface
{
  public string name;
  public List<EventMethodsClass> methods;
  public EventType Type { get { return evType; } set { evType = value; } }
  [SerializeField]
  private EventType evType;
  public EventClass(string n, List<EventMethodsClass> e, EventType type)
  {
    name = n;
    methods = e;
    evType = type;
  }

  public void InvokeAll()
  {
    methods.ForEach((x) => x.ev.Invoke());
  }

  public void InvokeAtIndex(int index)
  {
    methods[index].ev.Invoke();
  }
}
[Serializable]
public class EventMethodsClass<T>
{
  public string name;
  public UnityEvent<T> ev = new UnityEvent<T>();

  public static implicit operator List<EventMethodsClass<T>>(EventMethodsClass<T> method)
  {
    return new List<EventMethodsClass<T>> { method };
  }
}
[Serializable]
public class EventMethodsClass
{
  public string name;
  public UnityEvent ev = new UnityEvent();

  public static implicit operator List<EventMethodsClass>(EventMethodsClass method)
  {
    return new List<EventMethodsClass> { method };
  }
}
[Serializable]
public class EventClass<T> : IEventInterface
{
  public string name;
  public List<EventMethodsClass<T>> methods;
  public EventType Type { get { return evType; } set { evType = value; } }
  [SerializeField]
  private EventType evType;
  public EventClass(string n, List<EventMethodsClass<T>> e, EventType type)
  {
    name = n;
    methods = e;
    evType = type;
  }

  public void InvokeAll(T param)
  {
    methods.ForEach((x) => x.ev.Invoke(param));
  }

  public void InvokeAtIndex(int index, T param)
  {
    methods[index].ev.Invoke(param);
  }
}
public class ButtonAltAction : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
  [TextArea(10, 20)]
  public string title;
  static float secondsToHold = 0.3f;
  [HideInInspector]
  public int dropdownIndex = -1;
  public List<EventClass> eventClasses = new List<EventClass>();
  float timer = 0;
  bool isHolding;

  public void OnPointerClick(PointerEventData eventData)
  {
    if (eventData.button == PointerEventData.InputButton.Right)
    {
      Prompts.QuickAltSettingsPrompt(title, eventClasses.ToList<IEventInterface>());
    }
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    isHolding = true;
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    isHolding = false;
    timer = 0;
  }

  void Update()
  {
    if (isHolding)
    {
      timer += Time.unscaledDeltaTime;
    }
    if (timer > secondsToHold)
    {
      Prompts.QuickAltSettingsPrompt(title, eventClasses.ToList<IEventInterface>());
      isHolding = false;
      timer = 0;
    }
  }
}

