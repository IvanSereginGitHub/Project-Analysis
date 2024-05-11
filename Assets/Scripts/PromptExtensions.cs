using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class PromptExtensions
{
  public static void CloseAfter(this Prompt prompt, float time)
  {
    prompt.closingTime = time;
  }

  public static void OpenAfter(this Prompt prompt, float time)
  {
    prompt.openingTime = time;
  }
  public static void PlaceGameobjectInside(this Prompt prompt, GameObject obj, bool addIgnoreLayout = true, Vector2? ignoreLayoutCustomSize = null)
  {
    obj.transform.SetParent(prompt.promptPanel.textBehaviour.transform);
    //fix localscale incase world one is bigger/smaller
    obj.transform.localScale = Vector3.one;
    if (addIgnoreLayout)
    {
      var elem = obj.GetAddComponent<LayoutElement>();
      var width = obj.GetComponent<RectTransform>().rect.width;
      var height = obj.GetComponent<RectTransform>().rect.height;
      if (ignoreLayoutCustomSize != null)
      {
        width = ignoreLayoutCustomSize.Value.x;
        height = ignoreLayoutCustomSize.Value.y;
      }
      elem.minWidth = width;
      elem.minHeight = height;
    }
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
    if (!prompt.keepAlive)
      prompt.associatedPrefab = null;
  }
}