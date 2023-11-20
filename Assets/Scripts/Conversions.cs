using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using static ProjectManager;

public class Conversions : MonoBehaviour
{

  public static void ApplyValueToProperty(object comp, ref string temp)
  {
    string fName = NextElementIn(ref temp, ":");
    string val = NextElementIn(ref temp, ";", true);
    PropertyInfo pInfo = comp.GetType().GetProperty(fName);
    if (advancedLogging)
    {
      Debug.Log($"Property name is {fName}, value is {val}");
    }
    if (pInfo == null)
    {
      // loadErrorCount++;
      Debug.LogWarning($"Property {fName} was not found in {comp.GetType()}! Ignoring it...");
      return;
    }
    try
    {
      pInfo.SetValue(comp, FromStringToGenericVar<object>(val, pInfo.PropertyType));
    }
    catch (Exception ex)
    {
      Debug.LogWarning(ex.Message);
    }
  }

  public static string NextElementIn(ref string refString, string breakStr)
  {
    string tmp;
    if (refString.Contains(breakStr))
    {
      tmp = refString.Substring(0, refString.IndexOf(breakStr));
      refString = refString.Substring(refString.IndexOf(breakStr) + breakStr.Length);
    }
    else
    {
      tmp = refString;
    }
    //Debug.Log(tmp);
    return tmp;
  }

  public static string NextElementIn(ref string refString, string breakStr, bool lastIndex)
  {
    string tmp;
    if (refString.Contains(breakStr))
    {
      tmp = refString.Substring(0, lastIndex ? refString.LastIndexOf(breakStr) : refString.IndexOf(breakStr));
      refString = refString.Substring(lastIndex ? refString.LastIndexOf(breakStr) : refString.IndexOf(breakStr) + breakStr.Length);
    }
    else
    {
      tmp = refString;
    }
    //Debug.Log(tmp);
    return tmp;
  }

  public static float NextElementIn_Float(ref string refString, string breakStr)
  {
    return float.Parse(NextElementIn(ref refString, breakStr));
  }

  public static float NextElementIn_Float(ref string refString, string breakStr, string altBreakStr)
  {
    return float.Parse(NextElementIn(ref refString, breakStr, altBreakStr));
  }

  public static int NextElementIn_Int(ref string refString, string breakStr)
  {
    return int.Parse(NextElementIn(ref refString, breakStr));
  }

  public static string NextElementIn(ref string refString, string breakStr, string altBreakStr)
  {
    //Debug.Log(refString);
    string tmp;
    if (refString.Contains(breakStr))
    {
      tmp = refString.Substring(0, refString.IndexOf(breakStr));
      refString = refString.Substring(refString.IndexOf(breakStr) + breakStr.Length);
    }
    else
    {
      if (refString.Contains(altBreakStr))
      {
        tmp = refString.Substring(0, refString.IndexOf(altBreakStr));
        refString = refString.Substring(refString.IndexOf(altBreakStr) + altBreakStr.Length);
      }
      else
      {
        tmp = "";
      }
    }
    //Debug.Log(tmp);
    return tmp;
  }
  public static object FromObjectToAnotherType(object val, Type expectedType)
  {
    object res = null;
    try
    {
      res = Convert.ChangeType(val, expectedType);
    }
    catch
    {
      res = FromStringToObject(val.ToString(), expectedType);
    }
    return res;
  }
  public static T FromStringToGenericVar<T>(string val, Type expectedType)
  {
    return (T)FromStringToObject(val, expectedType);
  }

  public static object FromStringToObject(string val, Type expectedType)
  {
    object final = null;
    try
    {
      final = Convert.ChangeType(val, expectedType);
      if (expectedType == typeof(string))
      {
        //remove quotes
        final = (final as string).Substring(1).RemoveLastCharacters(1);
      }
    }
    catch
    {
      if (expectedType == typeof(bool))
      {
        final = Convert.ToBoolean(val);
      }
      else if (expectedType == typeof(Vector2) || expectedType == typeof(Vector3) || expectedType == typeof(Color) || expectedType == typeof(Vector4))
      {
        final = GetVectorFromString(val, expectedType);
      }
      else if (expectedType.IsEnum)
      {
        final = Enum.Parse(expectedType, val);
      }
      // else if (expectedType == typeof(EventPrefab.IDObjectClass))
      // {
      //   EventPrefab.IDObjectClass tmp = val;
      //   final = tmp;
      // }
      else if (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(TimedVar<>))
      {
        val = val.Substring(1, val.Length - 2);
        string[] properties = val.Split(';');
        Type[] typeArgs = expectedType.GetGenericArguments();
        Type constructed = typeof(TimedVar<>).MakeGenericType(typeArgs);

        var instance = Activator.CreateInstance(constructed);

        for (int i = 0; i < properties.Length; i++)
        {
          try
          {
            ApplyValueToProperty(instance, ref properties[i]);
          }
          catch (Exception ex)
          {
            Debug.Log(ex.Message);
          }
        }
        final = instance;
      }
      else
      {
        Debug.LogError("Could not parse \"" + val + "\" to the " /*+ val*/ + expectedType + " (Source - " + expectedType.Name + ")");
      }
    }
    return final;
  }

  public static T FromStringToVar<T>(string val)
  {
    object final = null;
    try
    {
      final = Convert.ChangeType(val, typeof(T));
    }
    catch
    {
      if (typeof(T) == typeof(bool))
      {
        final = Convert.ToBoolean(val);
      }
      else if (typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector3) || typeof(T) == typeof(Color) || typeof(T) == typeof(Vector4))
      {
        final = GetVectorFromString(val, typeof(T));
      }
      else if (typeof(T).IsEnum)
      {
        final = Enum.Parse(typeof(T), val);
      }
      else
      {
        Debug.LogError("Could not parse \"" + val + "\" to the " /*+ val*/ + typeof(T) + " (Source - " + typeof(T).Name + ")");
      }
    }

    return (T)final;
  }
  public static object GetVectorFromString(string source, Type vectorType)
  {
    source = source.Replace("(", "");

    if (vectorType == typeof(Color))
    {
      source = source.Replace("RGBA", "");
      return new Color(NextElementIn_Float(ref source, ","), NextElementIn_Float(ref source, ","), NextElementIn_Float(ref source, ","), NextElementIn_Float(ref source, ")"));
    }
    else if (vectorType == typeof(Vector4) || vectorType == typeof(Vector2) || vectorType == typeof(Vector3))
    {
      //Now automatically converts unsupported types to the supported ones
      //Cannot "upgrade" vector2 and vector3 to vector4 tho
      float x = NextElementIn_Float(ref source, ",");
      float y = NextElementIn_Float(ref source, ",", ")");
      if (source == "")
        return new Vector2(x, y);
      string z = NextElementIn(ref source, ",", ")");
      if (z == "" || vectorType == typeof(Vector2))
        return new Vector2(x, y);
      string w = NextElementIn(ref source, ",", ")");
      if (w == "" || vectorType == typeof(Vector3))
        return new Vector3(x, y, float.Parse(z));
      return new Vector4(x, y, float.Parse(z), float.Parse(w));
    }
    else
    {
      return null;
    }
  }

  public static T GetVectorFromString<T>(string source)
  {
    return (T)GetVectorFromString(source, typeof(T));
  }

  public static T GetTypeFromGeneric<T>(object generic)
  {
    return (T)GetObjectFromGeneric<T>(generic);
  }

  public static object GetObjectFromGeneric<T>(object generic)
  {
    return Convert.ChangeType(generic, typeof(T));
  }
}
