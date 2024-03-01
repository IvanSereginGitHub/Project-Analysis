using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleExtension : MonoBehaviour
{
    Toggle toggle;
    public string playerPrefs_Name;
    void Start()
    {
        toggle = GetComponent<Toggle>();
        if(playerPrefs_Name != "")
            toggle.isOn = Convert.ToBoolean(PlayerPrefs.GetInt(playerPrefs_Name,0));
    }
}
