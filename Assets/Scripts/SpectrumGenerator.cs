using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SpectrumGenerator : MonoBehaviour
{
    [SerializeField]
    RawImage[] imgs_to_apply;
    [SerializeField]
    AudioSource audSource;

    void Start()
    {
        // foreach (var item in imgs_to_apply)
        // {
        //     ApplySpectrumTextureToImage(item, audSource.clip);
        // }
    }
    public void ApplySpectrumTextureToImage(RawImage img, AudioClip clip)
    {
        img.texture = GenerateSpectrumTexture((int)img.GetComponent<RectTransform>().rect.width, (int)img.GetComponent<RectTransform>().rect.height, clip);
    }
    public Texture2D GenerateSpectrumTexture(int width, int height, AudioClip clip)
    {
        float[] samples = new float[0];

        if (samples.Length < 1 || clip.samples * clip.channels != samples.Length)
        {
            samples = new float[clip.samples * clip.channels];
        }
        clip.GetData(samples, 0);
        Texture2D finalSpectrum = new Texture2D(width, height, TextureFormat.RGBA32, false);

        float[] widthRange = new float[width];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                finalSpectrum.SetPixel(x, (height / 2) + y, new Color(0, 0, 0, 0));
                finalSpectrum.SetPixel(x, (height / 2) - y, new Color(0, 0, 0, 0));
            }
        }

        int deltaValue = samples.Length / width;
        float maxValue = 0;
        int value = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = value; j < value + deltaValue; j++)
            {
                if (maxValue < samples[j])
                {
                    maxValue = samples[j];
                }
            }
            widthRange[i] = maxValue;
            value += deltaValue;
            maxValue = 0;
        }


        for (int x = 0; x < widthRange.Length; x++)
        {
            finalSpectrum.SetPixel(x, height / 2, Color.white);
            for (int y = 0; y < Mathf.Abs(widthRange[x] * (height / 2)); y++)
            {
                finalSpectrum.SetPixel(x, (height / 2) + y, Color.white);
                finalSpectrum.SetPixel(x, (height / 2) - y, Color.white);
            }
        }

        finalSpectrum.Apply();
        return finalSpectrum;
    }
}
