using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class KeysShortcut : MonoBehaviour
{
    public KeyCode keyCode;
    public UnityEvent ev = new UnityEvent();
    EventSystem system;

    void Start()
    {
        system = FindObjectOfType<EventSystem>();
    }
    void Update()
    {
        if (Input.GetKeyUp(keyCode) && system.currentSelectedGameObject == null)
        {
            ev.Invoke();
        }
    }
}
