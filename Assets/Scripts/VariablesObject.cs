using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VariablesObject : MonoBehaviour
{
  public enum varType
  {
    InputField,
    Dropdown,
    Slider,
    Color,
    Button,
    Toggle,
    Vector2,
    Vector3,
    IDObject,
    TimedVar,
    TimedColor
  }
  public enum TimedSubtype
  {
    None,
    Float,
    Integer,
    Vector2,
    Vector3,
    Color
  }
  public varType variableType;
  public TimedSubtype timedSubtype;
  public GameObject variableCaller;
  public TextMeshProUGUI variableName;
  public List<GameObject> variableCallers = new List<GameObject>();

  public List<GameObject> additionalObjects;

  public void DisableObject(int index)
  {
    variableCallers[index].SetActive(false);
  }

  public void EnableObject(int index)
  {
    variableCallers[index].SetActive(true);
  }

  public void DisableAllObjects()
  {
    foreach (GameObject t in variableCallers)
    {
      t.SetActive(false);
    }
  }
  //Questionable to do this, but it is needed to decrease code size
  public TMP_InputField GetInputField(int index)
  {
    return variableCallers[index].GetComponent<TMP_InputField>();
  }

  public string GetInputFieldText(int index)
  {
    return variableCallers[index].GetComponent<TMP_InputField>().text;
  }

  public void SetInputFieldText(int index, string text)
  {
    variableCallers[index].GetComponent<TMP_InputField>().text = text;
  }
}
