using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxCamera : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float skyboxScale;

    Camera _cam;
    public Vector3 cameraPositionOffset;
    public Vector3 cameraRotationOffset;
    public bool affectFOV;
    public float skyboxFOVScale = 1f;


    void Start()
    {
        _cam = gameObject.GetComponent<Camera>();
    }
    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        }
        transform.localEulerAngles = cameraRotationOffset + playerCamera.transform.localEulerAngles;
        transform.localPosition = cameraPositionOffset + playerCamera.transform.position / skyboxScale;
        if (affectFOV)
        {
            _cam.fieldOfView = playerCamera.fieldOfView * skyboxFOVScale;
        }

    }

}