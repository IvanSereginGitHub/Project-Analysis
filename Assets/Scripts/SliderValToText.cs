using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SliderValToText : MonoBehaviour
{
    public Slider slider;
    float prevVal;
    //public TextMesh
    public TMP_Text textObject;
    public TMP_InputField inputField;
    
    public string loadStoredValue;
    public string valFormat;
    public string textObjInput = "{value}";
    public string textInput = "{value}";
    public bool currentValAsDefault;
    public bool executeOnAwake;

    private void Awake()
    {
        LoadValue();
        slider.onValueChanged.Invoke(slider.value);
    }
    private void Start()
    {
        if(!executeOnAwake)
            LoadValue();
    }
    void LoadValue()
    {
        if (!string.IsNullOrEmpty(loadStoredValue))
        {
            if (currentValAsDefault && !PlayerPrefs.HasKey(loadStoredValue))
                PlayerPrefs.SetFloat(loadStoredValue, slider.value);
            slider.value = PlayerPrefs.GetFloat(loadStoredValue);
        }
        UpdateObjectText();
    }
    void Update()
    {
        if(prevVal != slider.value)
        {
            UpdateObjectText();
        }
    }

    void UpdateObjectText()
    {
        if (textObject != null)
            textObject.text = textObjInput.Replace("{value}", slider.value.ToString(valFormat));

        if (inputField != null)
            inputField.text = textInput.Replace("{value}", slider.value.ToString(valFormat));

        prevVal = slider.value;
    }

    public void SetTextAsValue()
    {
        string template = textInput.Replace("value", "");
        string rawValue = inputField.text;
        foreach(char t in template)
        {
            rawValue = rawValue.Replace(t.ToString(), "");
        }
        slider.value = float.Parse(rawValue);
    }
}
