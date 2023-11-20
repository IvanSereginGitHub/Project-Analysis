using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PromptPanel : MonoBehaviour
{
    public TextMeshProUGUI promptText;
    public Button ok_button, cancel_button, close_button;
    public TextResizeBehaviour textBehaviour;
    public void DestroyThis()
    {
        Destroy(gameObject);
    }
}
