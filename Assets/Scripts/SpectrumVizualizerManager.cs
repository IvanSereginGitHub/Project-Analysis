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
    Transform[] objects = new Transform[0];

    public bool useInverseSamples = false;


    public void Regenerate()
    {
        if (countMultiplier == 0)
        {
            countMultiplier = 1024 / beatCount;
        }
        distance = Math.Abs(startPosition - endPosition) / beatCount;
        ClearArray();
        FillArray();
        if (!useInverseSamples)
            VizualizeSpectrum();
        else
            VizualizeSamples();
    }

    private void Start()
    {
        Regenerate();
    }
    void ClearArray()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            Destroy(objects[i]);
        }
        objects = new Transform[0];
    }

    void FillArray()
    {
        objects = new Transform[beatCount];
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i] = Instantiate(prefab, prefParent).transform;
            objects[i].localPosition = new Vector3(startPosition + distance * i, 0, 0);
            objects[i].localScale = new Vector2(5, 5);
        }
    }
    double[] reversedSamples;
    private void Update()
    {
        audSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
        if (!useInverseSamples)
            VizualizeSpectrum();
        else
            VizualizeSamples();
    }
    float AverageFromSamplesRange(int start, int length)
    {
        float avg = 0;
        for (int i = start; i < start + length; i++)
        {
            avg += samples[i];
        }
        return avg / length;
    }
    void VizualizeSpectrum()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].localScale = new Vector3(defaultWidthMultiplier, sizeMultiplier * AverageFromSamplesRange(countMultiplier * i, countMultiplier), 0);
        }
    }

    void VizualizeSamples()
    {
        reversedSamples = AnalysisFunctions.PerformInverseFFT(FastFourierTransform.ConvertFloatToComplex(samples));
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].localPosition = new Vector2(objects[i].localPosition.x, Convert.ToSingle(reversedSamples[i] * sizeMultiplier));
            //objects[i].localScale = new Vector3(defaultWidthMultiplier, sizeMultiplier * AverageFromSamplesRange(countMultiplier * i, countMultiplier), 0);
        }
    }
}
