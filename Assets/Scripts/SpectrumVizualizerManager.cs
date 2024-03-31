using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SpectrumVizualizerManager : MonoBehaviour
{
    public GameObject prefab;
    public Transform prefParent;
    public AudioSource audSource;
    public int beatCount = 64;
    public int changeIndexBy = 0;
    public float changeIndexTime = 0;
    public float sizeMultiplier = 1, defaultWidthMultiplier = 1, endPosition, startPosition;
    public int countMultiplier = 0;
    float distance;
    float[] samples = new float[1024];
    GameObject[] objects = new GameObject[0];

    private void Start()
    {
        if (countMultiplier == 0)
        {
            countMultiplier = 1024 / beatCount;
        }
        distance = Math.Abs(startPosition - endPosition) / beatCount;
        ClearArray();
        FillArray();
        VizualizeSpectrum();
    }
    void ClearArray()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            Destroy(objects[i]);
        }
        objects = new GameObject[0];
    }

    void FillArray()
    {
        objects = new GameObject[beatCount];
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i] = Instantiate(prefab, prefParent);
            objects[i].transform.localPosition = new Vector3(startPosition + distance * i, 0, 0);
        }
    }

    private void Update()
    {
        audSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
        VizualizeSpectrum();
    }
    float AverageFromSamplesRange(int start, int length)
    {
        return samples.Skip(start).Take(length).Average();
    }
    void VizualizeSpectrum()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].transform.localScale = new Vector3(defaultWidthMultiplier, sizeMultiplier * AverageFromSamplesRange(countMultiplier * i, countMultiplier), 0);
        }
    }
}
