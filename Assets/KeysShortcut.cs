using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class KeysShortcut : MonoBehaviour
{
    public KeyCode keyCode;
    public UnityEvent ev = new UnityEvent();
    void Update()
    {
        if(Input.GetKeyUp(keyCode)) {
            ev.Invoke();
        }
    }
}
