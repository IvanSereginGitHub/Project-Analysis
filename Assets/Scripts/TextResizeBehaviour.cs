using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
[ExecuteAlways]
public class TextResizeBehaviour : MonoBehaviour
{
    public RectTransform textController;
    public Vector2 maxSize;
    public Vector2 minSize;
    public RectOffset textOnlyOffset;
    public TextMeshProUGUI tmPro;
    string prevText = "";
    private void Update()
    {
        string newText = tmPro.text;
        if (newText != prevText)
        {
            ResetSize();
            prevText = newText;
        }
        Vector2 newSize = new Vector2(minSize.x, minSize.y);
        if (textController.sizeDelta.x > maxSize.x)
        {
            textController.gameObject.GetComponent<VerticalLayoutGroup>().childControlWidth = false;
            textController.GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(maxSize.x, textController.GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta.y);
        }

        if (textController.sizeDelta.y > maxSize.y)
        {
            textController.gameObject.GetComponent<VerticalLayoutGroup>().childControlHeight = false;
            textController.GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(textController.GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta.x, maxSize.y);
        }
        gameObject.GetComponent<RectTransform>().sizeDelta = newSize;
    }

    public void ResetSize()
    {
        textController.gameObject.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
        textController.gameObject.GetComponent<VerticalLayoutGroup>().childControlWidth = true;
    }
}
