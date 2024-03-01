using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpectrumBarManager : MonoBehaviour
{
    [SerializeField]
    RectTransform completedMaskTransform, completedSpectrumTransform;
    [SerializeField]
    Slider slider;
    [SerializeField]
    AudioSource audSource;
    // Start is called before the first frame update
    void Start()
    {
        // slider.maxValue = audSource.clip.length;
    }

    public void ChangeMaskWidth(float val)
    {
        SetRight(completedMaskTransform, completedMaskTransform.offsetMin.x * Mathf.Clamp(val / slider.maxValue, 0, slider.maxValue));
        SetRight(completedSpectrumTransform, completedMaskTransform.offsetMax.x + completedMaskTransform.offsetMin.x);
    }

    // Update is called once per frame
    void Update()
    {
        if (audSource.isPlaying)
        {
            slider.SetValueWithoutNotify(audSource.time);
            ChangeMaskWidth(audSource.time);
        }
    }
    public void FindAndPlaySnippet() {
        
    }
    public void SetRight(RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }
}
