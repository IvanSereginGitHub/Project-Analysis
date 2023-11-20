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

  public static void CloseAfter(this Prompt prompt, float time)
  {
    prompt.closingTime = time;
  }

  public static void OpenAfter(this Prompt prompt, float time)
  {
    prompt.openingTime = time;
  }
  public static Prompt SetToYesOnly(this Prompt prompt)
  {
    prompt.promptType = PromptType.YesOnly;
    prompt.ResetActions();
    return prompt;
  }

  public static void UpdatePromptText(this Prompt prompt, string newText)
  {
    prompt.associatedPrefab.GetComponent<PromptPanel>().promptText.text = newText;
  }

  public static Prompt SetToNormal(this Prompt prompt)
  {
    prompt.promptType = PromptType.Normal;
    prompt.ResetActions();
    return prompt;
  }

  public static Prompt SetToStrictPanel(this Prompt prompt)
  {
    prompt.promptType = PromptType.StrictPanel;
    prompt.ResetActions();
    return prompt;
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

  //public static GameObject AddNewObject(this Prompt prompt,GameObject obj, float width, float height)
  //{
  //    prompt.additionalUIElements.Add(obj);
  //    obj.transform.SetParent(prompt.associatedPrefab.transform);
  //    LayoutElement layEl = obj.AddComponent<LayoutElement>();
  //    layEl.minWidth = width;
  //    layEl.minHeight = height;
  //    return obj;
  //}
  //This should automatically find PromptsManager or completely remove it
  public static void Show(this Prompt prompt)
  {
    Prompts.ShowPrompt(prompt);
  }

  public static void Show(this Prompt prompt, string newTitle)
  {
    prompt.promptText = newTitle;
    prompt.Close();
    Prompts.ShowPrompt(prompt);
  }

  public static void Close(this Prompt prompt)
  {
    Prompts.ClosePrompt(prompt.associatedPrefab);
    prompt.associatedPrefab = null;
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
}
