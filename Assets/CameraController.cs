using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public Camera cam;
    public Vector3 defaultPos;

    public float minCamSize = 0.1f, maxCamSize = 5f, defaultSize = 5f;

    void ChangeCameraSize(float multiplier)
    {
        if (cam.orthographicSize <= maxCamSize && cam.orthographicSize >= minCamSize)
        {
            float res = (cam.orthographicSize + 0.1f * multiplier).Round(1);
            if (res < minCamSize)
                cam.orthographicSize = minCamSize;
            else if (res > maxCamSize)
                cam.orthographicSize = maxCamSize;
            else
                cam.orthographicSize = res;
        }
    }

    bool IsPointerOverObject()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            foreach (Touch touch in Input.touches)
            {
                int id = touch.fingerId;
                if (EventSystem.current.IsPointerOverGameObject(id))
                    return true;
            }
            return false;
        }
        return EventSystem.current.IsPointerOverGameObject();
    }
    Vector3 middleClickPos, currentMousePos;
    bool pointerOverObject = false;
    // Update is called once per frame
    void LateUpdate()
    {

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverObject())
            {
                pointerOverObject = true;
            }
            middleClickPos = cam.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Input.GetMouseButtonUp(0))
        {
            pointerOverObject = false;
            middleClickPos = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            cam.transform.position = defaultPos;
            cam.orthographicSize = defaultSize;
            middleClickPos = cam.ScreenToWorldPoint(Input.mousePosition);
        }
        if (pointerOverObject == true)
        {
            return;
        }
        if (Input.GetMouseButton(0))
        {
            cam.transform.position += middleClickPos - cam.ScreenToWorldPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(1))
            {
                cam.transform.position = defaultPos;
                cam.orthographicSize = defaultSize;
                middleClickPos = cam.ScreenToWorldPoint(Input.mousePosition);
            }
        }

        if (Input.mouseScrollDelta.y > 0)
        {
            ChangeCameraSize(-1);
        }
        else if (Input.mouseScrollDelta.y < 0)
        {
            ChangeCameraSize(1);
        }
    }
}
