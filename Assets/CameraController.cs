using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public Camera cam;
    public Vector3 defaultPos;

    public float minCamSize = 0.1f, maxCamSize = 5f, defaultSize = 5f;

    public GameObject followObject;
    public Vector3 followOffset;

    public void ToggleFollowingTheObject()
    {
        isFollowingObject = !isFollowingObject;
    }

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
    bool pointerOverObject = false, isFollowingObject = false;
    float previousDistance = 0;
    // Update is called once per frame
    void LateUpdate()
    {
        if (Input.GetKeyUp(KeyCode.F))
        {
            isFollowingObject = !isFollowingObject;
        }

        if (isFollowingObject && followObject != null)
        {
            cam.transform.position = followObject.transform.position + followOffset;
        }

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
        if (Input.GetMouseButton(0) && Input.touchCount < 2)
        {
            cam.transform.position += middleClickPos - cam.ScreenToWorldPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(1))
            {
                cam.transform.position = defaultPos;
                cam.orthographicSize = defaultSize;
                middleClickPos = cam.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else if (Input.touchCount == 2)
        {
            Vector2 touch0, touch1;
            float distance;
            touch0 = cam.ScreenToViewportPoint(Input.GetTouch(0).position);
            touch1 = cam.ScreenToViewportPoint(Input.GetTouch(1).position);
            distance = Vector2.Distance(touch0, touch1);

            float diff = Mathf.Abs(distance - previousDistance);
            if (distance - previousDistance > 0)
            {
                ChangeCameraSize(-1);
            }
            else if (distance - previousDistance < 0)
            {
                ChangeCameraSize(1);
            }

            previousDistance = distance;
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

