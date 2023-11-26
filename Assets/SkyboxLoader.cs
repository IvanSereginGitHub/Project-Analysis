using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SkyboxLoader : MonoBehaviour
{
    public string skyboxName;
    public float skyboxScale;
    public Vector3 cameraPositionOffset;
    public Vector3 cameraRotationOffset;
    public bool affectFOV;
    public float skyboxFOVScale = 1f;

    void Awake()
    {
        SceneManager.LoadScene(skyboxName, LoadSceneMode.Additive);

    }
    void Start()
    {
        FindObjectOfType<SkyboxCamera>().SetSettings(skyboxScale, cameraPositionOffset, cameraRotationOffset, affectFOV, skyboxFOVScale);
    }
}
