using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SerializableDictionary<T1, T2> where T1 : class
{
  public List<T1> _keys = new List<T1>();
  public List<T2> _values = new List<T2>();
  public SerializableDictionary()
  {

  }
  public SerializableDictionary(List<T1> keys, List<T2> values)
  {
    // TODO: check for duplicates
    if (keys.Count != values.Count)
    {
      throw new ArgumentException("keys and values must be same size!");
    }
    _keys = keys;
    _values = values;
  }

  public T2 GetValue(T1 key)
  {
    int ind = _keys.FindIndex((x) => x.Equals(key));
    return _values[ind];
  }
}

public class PromptsVariableParser : MonoBehaviour
{
  public SerializableDictionary<string, GameObject> typesDictionary = new SerializableDictionary<string, GameObject>();
  public string variables_prefix, prompt_title;
  public Component objectToScanFields;

  public GameObject tooltipObj;
  //public bool includeProperties;
  Prompt _prompt;
  void Start()
  {
    GenerateSettingsPrompt();
  }

  void ConfigureCorrespondingObject(GameObject obj, FieldInfo field_info)
  {
    var value = field_info.GetValue(objectToScanFields).ToString();
    switch (field_info.FieldType.Name)
    {
      //add more in the future
      case "Single":
        if (Attribute.IsDefined(field_info, typeof(UnityEngine.RangeAttribute)))
        {
          Destroy(obj);
          obj = Instantiate(FindCorrespondingObject("Single-range"));
          _prompt.PlaceGameobjectInside(obj);
          RangeAttribute attr = field_info.GetCustomAttribute<UnityEngine.RangeAttribute>();
          obj.GetComponent<Slider>().minValue = attr.min;
          obj.GetComponent<Slider>().maxValue = attr.max;
          obj.GetComponent<Slider>().value = Convert.ToSingle(value);
          obj.GetComponent<Slider>().onValueChanged.AddListener((val) =>
          {
            field_info.SetValue(objectToScanFields, val);
          });
        }
        else
        {
          obj.GetComponent<TMP_InputField>().text = value;
          obj.GetComponent<TMP_InputField>().onEndEdit.AddListener((str) =>
          {
            field_info.SetValue(objectToScanFields, Convert.ToSingle(str));
          });
        }
        break;
      case "Int32":
        if (Attribute.IsDefined(field_info, typeof(UnityEngine.RangeAttribute)))
        {
          Destroy(obj);
          obj = Instantiate(FindCorrespondingObject("Int32-range"));
          _prompt.PlaceGameobjectInside(obj);
          RangeAttribute attr = field_info.GetCustomAttribute<UnityEngine.RangeAttribute>();
          obj.GetComponent<Slider>().minValue = attr.min;
          obj.GetComponent<Slider>().maxValue = attr.max;
          obj.GetComponent<Slider>().wholeNumbers = true;
          obj.GetComponent<Slider>().value = Convert.ToSingle(value);
          obj.GetComponent<Slider>().onValueChanged.AddListener((val) =>
          {
            field_info.SetValue(objectToScanFields, Convert.ToInt32(val));
          });
        }
        else
        {
          obj.GetComponent<TMP_InputField>().text = value;
          obj.GetComponent<TMP_InputField>().onEndEdit.AddListener((str) =>
          {
            field_info.SetValue(objectToScanFields, Convert.ToInt32(str));
          });
        }
        break;
      case "Boolean":
        obj.GetComponent<Toggle>().isOn = Convert.ToBoolean(value);
        if (Attribute.IsDefined(field_info, typeof(FieldInformationAttribute)))
        {
          obj.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = field_info.GetCustomAttribute<FieldInformationAttribute>().Name;
        }
        obj.GetComponent<Toggle>().onValueChanged.AddListener((val) =>
        {
          field_info.SetValue(objectToScanFields, val);
        });
        break;
      default:
        break;
    }
    // add object with description if tooltip exists
    if (Attribute.IsDefined(field_info, typeof(FieldInformationAttribute)))
    {
      GameObject new_tooltip = Instantiate(tooltipObj, obj.transform);
      new_tooltip.GetComponent<Button>().onClick.AddListener(delegate { Prompts.ShowQuickExitOnlyPrompt("<color=yellow><b>" + field_info.GetCustomAttribute<FieldInformationAttribute>().Name + "</b></color>\n\n" + field_info.GetCustomAttribute<FieldInformationAttribute>().Description); });
      if (field_info.GetCustomAttribute<FieldInformationAttribute>().CustomWidth != 0 && field_info.GetCustomAttribute<FieldInformationAttribute>().CustomHeight != 0)
      {
        obj.GetComponent<LayoutElement>().minWidth = field_info.GetCustomAttribute<FieldInformationAttribute>().CustomWidth;
        obj.GetComponent<LayoutElement>().minHeight = field_info.GetCustomAttribute<FieldInformationAttribute>().CustomHeight;
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(field_info.GetCustomAttribute<FieldInformationAttribute>().CustomWidth, field_info.GetCustomAttribute<FieldInformationAttribute>().CustomHeight);
      }
    }
  }

  GameObject FindCorrespondingObject(Type varType)
  {
    return typesDictionary.GetValue(varType.Name);
  }
  GameObject FindCorrespondingObject(string type_name)
  {
    return typesDictionary.GetValue(type_name);
  }

  List<FieldInfo> GetVariables(object obj)
  {
    List<FieldInfo> var = new List<FieldInfo>();
    var field_infos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
    foreach (var field_info in field_infos)
    {
      if (field_info.Name.StartsWith(variables_prefix))
        var.Add(field_info);
    }
    return var;
  }

  Prompt GenerateSettingsPrompt()
  {
    var vars = GetVariables(objectToScanFields);
    _prompt = new Prompt(PromptType.ExitOnly);
    _prompt.promptText = prompt_title;
    Prompts.PreparePrompt(_prompt, true);
    foreach (var item in vars)
    {
      GameObject obj = FindCorrespondingObject(item.FieldType);
      obj = Instantiate(obj);
      ConfigureCorrespondingObject(obj, item);
      _prompt.PlaceGameobjectInside(obj);
    }
    return _prompt;
  }

  public void ShowSettingsPrompt()
  {
    _prompt.Show();
  }
}
