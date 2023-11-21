using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SkyboxLoader : MonoBehaviour
{
    public string skyboxName;

    void Awake()
    {
        SceneManager.LoadScene(skyboxName, LoadSceneMode.Additive);
    }
}
