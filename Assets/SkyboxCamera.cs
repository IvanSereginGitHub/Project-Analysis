using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
[System.Serializable]
public class SkyboxSettings
{
    public string skyboxName;
    public float skyboxScale;
    public Vector3 cameraPositionOffset;
    public Vector3 cameraRotationOffset;
    public bool moveByRotation;
    public bool affectFOV;
    public float skyboxFOVScale = 1f;
}

public class SkyboxCamera : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;

    public SkyboxSettings skyboxSettings;

    Camera _cam;

    Vector3? lastPos = null;


    void Start()
    {
        _cam = gameObject.GetComponent<Camera>();
    }

    public void SetSettings(SkyboxSettings settings)
    {
        GameObject sky = Instantiate(Resources.Load<GameObject>(Path.Combine("Skyboxes", settings.skyboxName)));
        skyboxSettings = settings;
    }
    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        }
        transform.localEulerAngles = skyboxSettings.cameraRotationOffset + playerCamera.transform.localEulerAngles;

        transform.localPosition = skyboxSettings.cameraPositionOffset + playerCamera.transform.position / skyboxSettings.skyboxScale;
        if (skyboxSettings.affectFOV)
        {
            _cam.fieldOfView = playerCamera.fieldOfView * skyboxSettings.skyboxFOVScale;
        }

    }

}