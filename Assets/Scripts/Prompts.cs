using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum PromptType
{
    Normal,
    YesOnly,
    ExitOnly,
    StrictPanel
}

public class Prompt
{
    public Action ok_action, cancel_action, close_action = null;

    public string okName = "OK", cancelName = "Cancel";
    public string promptText = "";
    /// <summary>
    /// A specific structure for instantiating and configuring prepared objects.
    /// </summary>
    public List<IEventInterface> additionalObjects = new List<IEventInterface>();
    public PromptType promptType = PromptType.Normal;
    public float openingTime = 0, closingTime = 0;
    public bool closeOnBgClick = true;
    /// <summary>
    /// Keeps prompt panel alive for later usage. 
    /// </summary>
    public bool keepAlive = false;
    /// <summary>
    /// Contains generated panel.
    /// </summary>
    public GameObject associatedPrefab;
    public PromptPanel promptPanel;
    public void ResetActions()
    {
        ok_action = null;
        cancel_action = null;
        close_action = null;
    }

    public Prompt()
    {

    }
    public Prompt(string text)
    {
        promptText = text;
    }
    public Prompt(PromptType type)
    {
        promptType = type;
    }
}


public class Prompts : MonoBehaviour
{
    public static void UnselectDropdown(TMP_Dropdown dropdown)
    {
        dropdown.GetType().GetField("m_Value", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(dropdown, -1);
    }
    public static List<string> GetStringsFromList<T>(List<T> list, Func<T, string> condition)
    {
        List<string> newList = new List<string>();
        foreach (var t in list)
        {
            newList.Add(condition(t));
        }
        return newList;
    }
    public static void PopulateUnselectedDropdown(TMP_Dropdown dropdown, List<string> options)
    {
        dropdown.options.Clear();
        foreach (string t in options)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(t));
        }
        UnselectDropdown(dropdown);
    }
    public static Prompts instance;
    public static Canvas canvasParent;
    private void Awake()
    {
        instance = this;
    }
    public static Prompt CreateNewPrompt(string text)
    {
        Prompt newPrompt = new Prompt(text);
        return newPrompt;
    }

    static IEnumerator OpenPromptAfter(GameObject promptPanel, float time)
    {
        yield return new WaitForSecondsRealtime(time);
        promptPanel.SetActive(true);
    }

    static IEnumerator ClosePromptAfter(GameObject promptPanel, float time)
    {
        yield return new WaitForSecondsRealtime(time);
        ClosePrompt(promptPanel);
    }

    public static void ClosePrompt(GameObject promptPanel)
    {
        // This is a temporary solution, must find a root of problem
        if (promptPanel)
            promptPanel.GetComponent<Animator>().Play("ClosePromptAnim");
    }

    public static void DestroyPrompt(GameObject promptPanel)
    {
        Destroy(promptPanel);
    }

    public static Prompt QuickPrompt(string text)
    {
        Prompt prompt = new Prompt
        {
            promptText = text,
            promptType = PromptType.Normal
        };
        prompt.Show();
        return prompt;
    }

    public static void ShowQuickPrompt(string text)
    {
        Prompt prompt = new Prompt
        {
            promptText = text,
            promptType = PromptType.Normal
        };
        prompt.Show();
    }

    public static void ShowQuickExitOnlyPrompt(string text)
    {
        Prompt prompt = new Prompt
        {
            promptText = text,
            promptType = PromptType.ExitOnly
        };
        prompt.Show();
    }

    public static void ShowQuickExitOnlyPrompt(TextAsset text)
    {
        Prompt prompt = new Prompt
        {
            promptText = text.text,
            promptType = PromptType.ExitOnly
        };
        prompt.Show();
    }

    public static void ShowQuickExitOnlyPrompt(string text, out Prompt prompt)
    {
        prompt = new Prompt
        {
            promptText = text,
            promptType = PromptType.ExitOnly
        };
        prompt.Show();
    }
    /// <summary>
    /// Generates a prompt panel with several custom objects, specified in events
    /// </summary>
    /// <param name="events"></param>
    /// <returns></returns>
    public static Prompt ShowQuickAltSettingsPrompt(List<IEventInterface> events)
    {
        Prompt prompt = new Prompt
        {
            promptText = "Choose an action:",
            promptType = PromptType.ExitOnly
        };
        prompt.additionalObjects.AddRange(events);
        prompt.Show();
        return prompt;
    }
    /// <summary>
    /// Generates a prompt panel with several custom objects, specified in events
    /// </summary>
    /// <param name="headerText"></param>
    /// <param name="events"></param>
    /// <returns></returns>
    public static Prompt ShowQuickAltSettingsPrompt(string headerText, List<IEventInterface> events)
    {
        Prompt prompt = new Prompt
        {
            promptText = string.IsNullOrWhiteSpace(headerText) ? "Выберите действие:" : headerText,
            promptType = PromptType.ExitOnly
        };
        prompt.additionalObjects.AddRange(events);
        prompt.Show();
        return prompt;
    }

    public static Prompt ShowQuickStrictPrompt(string headerText, float closingTime = 1f)
    {
        Prompt prompt = new Prompt(PromptType.StrictPanel);
        prompt.promptText = headerText;
        prompt.closingTime = closingTime;
        prompt.Show();
        return prompt;
    }

    public static Prompt ShowQuickStrictPrompt(string headerText)
    {
        Prompt prompt = new Prompt(PromptType.StrictPanel);
        prompt.promptText = headerText;
        prompt.closingTime = 1f;
        prompt.Show();
        return prompt;
    }
    /// <summary>
    /// Updates some fields in prompt.<br/>Cannot modify type-specific properties and cannot recreate/remove additional objects. Destroy prompt and recreate it instead.
    /// </summary>
    /// <param name="prompt"></param>
    public static void UpdatePrompt(Prompt prompt)
    {
        if (prompt.associatedPrefab == null)
        {
            Prompts.ShowQuickStrictPrompt("Prompts error: You are trying to update uninitialized prompt. Call PreparePrompt first.");
            return;
        }
        PromptPanel promptPanel = prompt.promptPanel;
        Button _ok = promptPanel.ok_button, _cancel = promptPanel.cancel_button, _exit = promptPanel.close_button;
        TextMeshProUGUI _promptText = promptPanel.promptText;

        promptPanel.keepAlive = prompt.keepAlive;
        promptPanel.transform.GetChild(0).gameObject.GetComponent<Button>().enabled = prompt.closeOnBgClick;

        if (prompt.ok_action != null)
        {
            _ok.onClick.RemoveAllListeners();
            _ok.onClick.AddListener(delegate { prompt.ok_action(); });
        }
        if (prompt.cancel_action != null)
        {
            _cancel.onClick.RemoveAllListeners();
            _cancel.onClick.AddListener(delegate { prompt.cancel_action(); });
        }

        if (prompt.close_action != null)
        {
            _exit.onClick.RemoveAllListeners();
            _exit.onClick.AddListener(delegate { prompt.close_action(); });
            promptPanel.transform.GetChild(0).gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
            promptPanel.transform.GetChild(0).gameObject.GetComponent<Button>().onClick.AddListener(delegate { prompt.close_action(); });
        }

        _promptText.text = prompt.promptText;

        _ok.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = prompt.okName;
        _cancel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = prompt.cancelName;
    }
    /// <summary>
    /// Loads panel and any other parameters, specified in prompt.<br/>Triggered by default when calling prompt.Show() if prompt.associatedPrefab is null.<br/><br/>
    /// Hint: use this method with variable prompt.keepAlive to add custom objects for panel and keep them until panel is destroyed.
    /// </summary>
    /// <param name="prompt"></param>
    public static void PreparePrompt(Prompt prompt, bool keepAlive = false)
    {
        if (canvasParent == null)
        {
            canvasParent = new GameObject("PromptCanvas").AddComponent<Canvas>();
            canvasParent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasParent.sortingOrder = 10000;

            CanvasScaler scaler = canvasParent.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366, 768);

            canvasParent.gameObject.AddComponent<GraphicRaycaster>();
        }
        GameObject panel = Instantiate((GameObject)Resources.Load("PromptPanel"), canvasParent.transform);
        panel.SetActive(false);
        PromptPanel promptPanel = panel.GetComponent<PromptPanel>();

        Button _ok = promptPanel.ok_button, _cancel = promptPanel.cancel_button, _exit = promptPanel.close_button;
        TextMeshProUGUI _promptText = promptPanel.promptText;

        TextResizeBehaviour _promptResizeBehaviour = promptPanel.textBehaviour;
        // promptPanel.keepAlive = prompt.keepAlive;

        switch (prompt.promptType)
        {
            case PromptType.Normal:
                _ok.gameObject.SetActive(true);
                _cancel.gameObject.SetActive(true);
                _exit.gameObject.SetActive(true);
                break;
            case PromptType.YesOnly:
                _ok.transform.localPosition = new Vector3(0, _ok.transform.localPosition.y, 0);
                _cancel.gameObject.SetActive(false);
                _exit.gameObject.SetActive(true);
                break;
            case PromptType.StrictPanel:
                _promptResizeBehaviour.gameObject.GetComponent<VerticalLayoutGroup>().padding = _promptResizeBehaviour.textOnlyOffset;
                _ok.gameObject.SetActive(false);
                _cancel.gameObject.SetActive(false);
                _exit.gameObject.SetActive(false);
                prompt.closeOnBgClick = false;
                break;
            case PromptType.ExitOnly:
                _promptResizeBehaviour.gameObject.GetComponent<VerticalLayoutGroup>().padding = _promptResizeBehaviour.textOnlyOffset;
                _ok.gameObject.SetActive(false);
                _cancel.gameObject.SetActive(false);
                break;
        }
        if (prompt.additionalObjects.Count != 0)
        {
            foreach (IEventInterface ob in prompt.additionalObjects)
            {
                switch (ob.Type)
                {
                    case EventType.button:
                        EventClass btn = (EventClass)ob;
                        Button button = Instantiate((GameObject)Resources.Load("PromptButton"), _promptResizeBehaviour.transform).GetComponent<Button>();
                        button.onClick.AddListener(delegate { btn.InvokeAll(); button.transform.parent.parent.gameObject.GetComponent<Animator>().Play("ClosePromptAnim"); });
                        button.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = btn.name;
                        break;
                    case EventType.inputField:
                        EventClass<string> input = (EventClass<string>)ob;
                        TMP_InputField inputField = Instantiate((GameObject)Resources.Load("PromptInputField"), _promptResizeBehaviour.transform).GetComponent<TMP_InputField>();
                        inputField.onEndEdit.AddListener((string n) => { input.InvokeAll(n);/* inputField.transform.parent.parent.gameObject.GetComponent<Animator>().Play("ClosePromptAnim"); */});
                        inputField.placeholder.GetComponent<TextMeshProUGUI>().text = input.name;
                        inputField.text = input.defaultValue;
                        EventSystem.current.SetSelectedGameObject(inputField.gameObject, null);
                        break;
                    case EventType.dropdown:
                        EventClass drop = (EventClass)ob;
                        TMP_Dropdown dropdown = Instantiate((GameObject)Resources.Load("PromptDropdown"), _promptResizeBehaviour.transform).GetComponent<TMP_Dropdown>();
                        PopulateUnselectedDropdown(dropdown, GetStringsFromList<EventMethodsClass>(drop.methods, x => x.name));
                        dropdown.onValueChanged.AddListener((int n) => { drop.InvokeAtIndex(dropdown.value); });
                        break;
                }
            }
        }
        prompt.associatedPrefab = panel;
        prompt.promptPanel = promptPanel;
        prompt.keepAlive = keepAlive;
        UpdatePrompt(prompt);
        // promptPanel.transform.GetChild(0).gameObject.GetComponent<Button>().enabled = prompt.closeOnBgClick;

        // if (prompt.ok_action != null)
        //     _ok.onClick.AddListener(delegate { prompt.ok_action(); });
        // if (prompt.cancel_action != null)
        //     _cancel.onClick.AddListener(delegate { prompt.cancel_action(); });
        // if (prompt.close_action != null)
        //     _exit.onClick.AddListener(delegate { prompt.close_action(); });

        // _promptText.text = prompt.promptText;

        // _ok.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = prompt.okName;
        // _cancel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = prompt.cancelName;
    }

    public static void ShowPrompt(Prompt prompt)
    {
        if (prompt.associatedPrefab == null)
        {
            PreparePrompt(prompt);
        }

        if (prompt.openingTime != 0f)
        {
            instance.StartCoroutine(OpenPromptAfter(prompt.associatedPrefab, prompt.openingTime));
        }
        else
        {
            prompt.associatedPrefab.SetActive(true);
        }

        if (prompt.closingTime != 0f)
        {
            instance.StartCoroutine(ClosePromptAfter(prompt.associatedPrefab, prompt.closingTime));
        }
    }
}
