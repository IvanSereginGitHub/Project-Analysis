using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class Extensions
{
  public static Vector3 Round(this Vector3 vector3, int decimalPlaces = 2)
  {
    float multiplier = 1;
    for (int i = 0; i < decimalPlaces; i++)
    {
      multiplier *= 10f;
    }
    return new Vector3(
      Mathf.Round(vector3.x * multiplier) / multiplier,
      Mathf.Round(vector3.y * multiplier) / multiplier,
      Mathf.Round(vector3.z * multiplier) / multiplier);
  }

  public static float Round(this float value, int decimalPlaces = 2)
  {
    float multiplier = 1;
    for (int i = 0; i < decimalPlaces; i++)
    {
      multiplier *= 10f;
    }
    return Mathf.Round(value * multiplier) / multiplier;
  }
  public static int ToInt(this bool value)
  {
    return value ? 1 : 0;
  }
  public static string ToNonCamelName(this string source)
  {
    string[] s = Regex.Matches(source, @"([A-Z]+(?![a-z])|[A-Z][a-z]+|[0-9]+|[a-z]+)").OfType<Match>().Select(m => m.Value).ToArray();
    return new CultureInfo("en-US").TextInfo.ToTitleCase(string.Join(" ", s));
  }

  public static float SnapValueTo(this float input, float snapValue)
  {
    return snapValue * Mathf.Round((input / snapValue));
  }


  public static string RemoveLastCharacters(this string str, int count)
  {
    if (count <= 0)
      return str;
    return str.Remove(str.Length - count);
  }

  public static List<string> CleanSplit(this string str, bool removeTabs = true, char customSplitChar = '\n')
  {
    // Basically split and remove "empty" lines (with \n and \t symbols + spaces)

    List<string> list = str.Split(customSplitChar).ToList();

    List<string> list_copy = new List<string>();

    foreach (string t in list)
    {
      if (t.Replace(" ", "").Replace("\t", "").Replace("\n", "").Length != 0)
      {
        list_copy.Add(removeTabs ? t.Replace("\t", "") : t);
      }
    }

    return list_copy;
  }

  public static List<string> CleanSplit(this string str, bool removeTabs = true, Regex regexRule = null)
  {
    // Basically split and remove "empty" lines (with \n and \t symbols + spaces)

    List<string> list = regexRule.Split(str).ToList();

    List<string> list_copy = new List<string>();

    foreach (string t in list)
    {
      if (t.Replace(" ", "").Replace("\t", "").Replace("\n", "").Length != 0)
      {
        list_copy.Add(removeTabs ? t.Replace("\t", "") : t);
      }
    }

    return list_copy;
  }

  public static bool IsNearlyEqualTo(this float a, float b, float margin)
  {
    return Math.Abs(a - b) < margin;
  }

  public static Color OverwriteOpacity(this Color col, float val)
  {
    return new Color(col.r, col.g, col.b, val);
  }

  public static Color ChangeOpacity(this Color col, float val)
  {
    return new Color(col.r, col.g, col.b, col.a + val);
  }

  public static void ReplaceElements<T>(this List<T> list, T element1, T element2)
  {
    int ind1 = list.IndexOf(element1);
    int ind2 = list.IndexOf(element2);

    list[ind1] = element2;
    list[ind2] = element1;
  }

  public static void ReplaceElements<T>(this List<T> list, T element1, int ind2)
  {
    int ind1 = list.IndexOf(element1);
    T element2 = list[ind2];

    list[ind1] = element2;
    list[ind2] = element1;
  }

  public static void ReplaceElements<T>(this List<T> list, int ind1, T element2)
  {
    T element1 = list[ind1];
    int ind2 = list.IndexOf(element2);

    list[ind1] = element2;
    list[ind2] = element1;
  }

  public static void ReplaceElements<T>(this List<T> list, int ind1, int ind2)
  {
    T element1 = list[ind1];
    T element2 = list[ind2];

    list[ind1] = element2;
    list[ind2] = element1;
  }

  public static void MoveTo<T>(this List<T> list, int startIndex, int endIndex)
  {
    T obj = list[startIndex];
    list.RemoveAt(startIndex);
    list.Insert(endIndex, obj);
  }

  public static Bounds CalculateRendererBounds(this Renderer rend)
  {
    Bounds bounds = rend.bounds;
    foreach (Renderer t in rend.gameObject.GetComponentsInChildren<Renderer>())
    {
      bounds.Encapsulate(t.bounds);
    }
    return bounds;
  }

  public static void SetMaterialColorToAllChildren(this Renderer rend, Color color)
  {
    rend.material.color = color;
    foreach (Renderer t in rend.gameObject.GetComponentsInChildren<Renderer>())
    {
      t.material.color = color;
    }
  }

  public static void SetMaterialColorToAllChildren(this GameObject obj, Color color)
  {
    if (obj.TryGetComponent<Renderer>(out Renderer rend))
    {
      rend.SetMaterialColorToAllChildren(color);
      return;
    }
    foreach (Renderer t in obj.GetComponentsInChildren<Renderer>())
    {
      t.material.color = color;
    }
  }

  public static void MoveTransform(this Transform target, Vector3 moveTo)
  {
    // Debug.Log($"moving {target} to {moveTo}");
    target.localPosition = moveTo;
  }

  public static void ScaleTransform(this Transform target, Vector3 scaleTo)
  {
    // Debug.Log($"scaling {target} to {scaleTo}");
    target.localScale = scaleTo;
  }

  public static void RotateTransform(this Transform target, Vector3 rotateTo)
  {
    // Debug.Log($"rotating {target} to {rotateTo}");
    target.localEulerAngles = rotateTo;
  }

  public static void SetPivotRelativePosition(this RectTransform rectTransform, Vector2 pivot)
  {
    if (rectTransform == null) return;

    Vector2 size = rectTransform.rect.size;
    Vector2 deltaPivot = rectTransform.pivot - pivot;
    Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
    rectTransform.pivot = pivot;
    rectTransform.localPosition -= deltaPosition;
  }
  // BAD CODE PRACTICE TERRITORY, BE WARNED
  public static float ToNegativePositive(this bool boolean)
  {
    return boolean ? 1f : -1f;
  }
  public static float ToOneZero(this bool boolean)
  {
    return boolean ? 1f : 0f;
  }
  public static float ToZeroOne(this bool boolean)
  {
    return boolean ? 0f : 1f;
  }

  public static T GetAddComponent<T>(this GameObject obj) where T : UnityEngine.Component
  {
    obj.TryGetComponent(out T comp);
    if (comp == null)
    {
      comp = obj.AddComponent<T>();
    }
    return comp;
  }
}
