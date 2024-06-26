using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Runtime.InteropServices;
using System.IO.Compression;

using static Conversions;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ObjectGroups
{
  public string groupName;
  public List<GameObject> objects;
}

public class ProjectManager : MonoBehaviour
{
  public string objectPath;
  public List<string> allowedTypes;
  public List<GameObject> allCustomObjects = new List<GameObject>();
  public bool spawnAllObjectsOnLoad;
  public Transform customObjectsParent;
  public Transform levelsParent;
  public GameObject updatePanel;
  public TextMeshProUGUI versionText, versionDesc;

  public GameObject levelPrefab, player, pausePanel, pauseButton;
  public static readonly long maxFileSize = 20971520;
  [HideInInspector]
  public static bool advancedLogging = false;
  public static bool isPlayingLoadedLevel = false;

  public static string customObjectsPath = "";

  public static string levelsPath = "";
  public static bool preferMinFilesOverRegular = true;
  public static bool generateMinFilesWhenSaving = true;

  public static bool compressAndUseZip = false;


  //pair of (origin,field/property, value)
  public static object GetFieldValue(object src, string fieldName)
  {
    return src.GetType().GetField(fieldName).GetValue(src);
  }

  public static void SetFieldValue(object src, string fieldName, object val)
  {
    src.GetType().GetField(fieldName).SetValue(src, val);
  }

  public static object GetPropertyValue(object src, string propName, object[] indexes = null)
  {
    return src.GetType().GetProperty(propName).GetValue(src, indexes);
  }

  public static void SetPropertyValue(object src, string fieldName, object val)
  {
    src.GetType().GetProperty(fieldName).SetValue(src, val);
  }

  public static bool TryGetFieldValue(object src, string fieldName, out object val, out FieldInfo field)
  {
    try
    {
      field = src.GetType().GetField(fieldName);
      val = field.GetValue(src);
    }
    catch (Exception ex)
    {
      // Debug.LogObjects(src, fieldName);
      if (advancedLogging)
        Debug.Log($"Something went wrong ({ex})");
      field = null;
      val = null;
      return false;
    }
    return true;
  }

  public static bool TryGetPropertyValue(object src, string propertyName, out object val, out PropertyInfo prop, object[] indexes = null)
  {
    try
    {
      prop = src.GetType().GetProperty(propertyName);
      val = prop.GetValue(src);
    }
    catch (Exception ex)
    {
      // Debug.LogObjects(src, fieldName);
      if (advancedLogging)
      {
        Debug.Log($"Something went wrong ({ex})");
      }

      prop = null;
      val = null;
      return false;
    }
    return true;
  }
  //should only be executed in Initialize method, since it uses a lot of 
  public static (object, dynamic, object) FromStringToOrigin(object src, string path, out dynamic newSource)
  {
    dynamic bracketsComponent = null;
    newSource = null;
    dynamic element = null;
    if (path.Contains("[") && path.Contains("]"))
    {
      var tmp = FromStringToOrigin(src, path.Substring(path.IndexOf("[") + 1, path.IndexOf("]") - 1 - path.IndexOf("[")), out bracketsComponent);
      src = tmp.Item1;
      path = path.Substring(path.IndexOf("]") + 1);
      newSource = tmp.Item1;
    }
    string[] subpath = path.Split('.');
    object origin = src;



    for (int i = 0; i < subpath.Length; i++)
    {
      if (TryGetPropertyValue(origin, subpath[i], out object val_prop, out PropertyInfo prop))
      {
        if (ProjectManager.advancedLogging)
          Debug.Log($"Found property {subpath[i]} in {origin}");
        origin = val_prop;
        element = prop;
      }
      else if (TryGetFieldValue(origin, subpath[i], out object val_field, out FieldInfo field))
      {
        if (ProjectManager.advancedLogging)
          Debug.Log($"Found field {subpath[i]} in {origin}");
        origin = val_field;
        element = field;
      }
      else if ((origin as GameObject).GetComponent(subpath[i]) != null)
      {
        var val_comp = (origin as GameObject).GetComponent(subpath[i]);
        if (val_comp != null)
        {
          if (ProjectManager.advancedLogging)
            Debug.Log($"Found component {val_comp} in {origin}");
          origin = val_comp;
          newSource = val_comp;
        }
      }
    }
    // Debug.LogObjects(origin, element, src);
    return (origin, element, src);
  }

  public static void FromStringToSetValue(object src, dynamic entryToApply, object value, Type expectedType = null, bool addValue = false)
  {
    try
    {
      entryToApply.SetValue(src, Convert.ChangeType(value, expectedType));
    }
    catch (Exception ex)
    {
      if (ProjectManager.advancedLogging)
      {
        //Debug.LogObjects("Couldn't convert object of type,", expectedType, "trying manually changing type");

        //Debug.LogObjects(src, entryToApply, value, expectedType);
      }

      if (expectedType == typeof(Vector3) || expectedType == typeof(Color))
      {
        var res = GetVectorFromString(value.ToString(), expectedType);
        entryToApply.SetValue(src, res);
      }
    }
  }

  public static string GetProperPath(params string[] pathes)
  {
    return Path.GetFullPath(Path.Combine(pathes));
  }
}
