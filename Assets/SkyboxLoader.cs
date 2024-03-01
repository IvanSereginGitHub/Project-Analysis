using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SkyboxLoader : MonoBehaviour
{
    public SkyboxSettings skyboxSettings;

    void Awake()
    {
        SceneManager.LoadScene(1, LoadSceneMode.Additive);
    }
    void Start()
    {
        FindObjectOfType<SkyboxCamera>().SetSettings(skyboxSettings);
    }
}
