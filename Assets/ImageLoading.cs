using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageLoading : MonoBehaviour
{
    RectTransform rectTransform;
    [SerializeField]
    float rotationSpeed = -180f;
    // Start is called before the first frame update
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void ChangeImage(Texture2D texture, bool setNativeSize = false)
    {
        RawImage rawImage = GetComponent<RawImage>();
        rawImage.texture = texture;
        transform.localEulerAngles = new Vector3(0, 0, 0);
        if (setNativeSize)
            rawImage.SetNativeSize();
        Destroy(this);
    }

    // Update is called once per frame
    void Update()
    {
        rectTransform.Rotate(new Vector3(0, 0, rotationSpeed * Time.deltaTime));
    }
}
