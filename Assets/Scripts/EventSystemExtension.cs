using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemExtension : MonoBehaviour
{
    bool wasOnObject = false;

    public bool WasPointerOnObject(KeyCode key)
    {
        if (Input.GetKeyDown(key))
        {
            wasOnObject = EventSystem.current.IsPointerOverGameObject();
        }
        else if (Input.GetKeyUp(key))
        {
            wasOnObject = EventSystem.current.IsPointerOverGameObject();
        }
        return wasOnObject;
    }
}
