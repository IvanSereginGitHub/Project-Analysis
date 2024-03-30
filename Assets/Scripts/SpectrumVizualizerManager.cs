using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SpectrumVizualizerManager : MonoBehaviour
{
    public GameObject prefab;
    public Transform prefParent, prefParent2;
    public AudioSource audSource;
    public int beatCount = 64;
    public int changeIndexBy = 0;
    public float changeIndexTime = 0;
    public float sizeMultiplier = 1, defaultWidthMultiplier = 1, endPosition, startPosition;
    public float distance;
    float[] samples = new float[1024];
    public bool activateByStart = false;
    int indexChangeCount = 0;
    float time = 0;
    public enum spectrumType
    {
        Standard,
        Circle,
        Corners
    }
    public spectrumType SpectrumType;
    GameObject[] objects = new GameObject[0];

    private void Start()
    {
        if (activateByStart)
        {
            distance = Math.Abs(startPosition - endPosition) / beatCount;
            ClearArray();
            FillArray();
            DrawSpectrum();
        }
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
        switch (SpectrumType)
        {
            case spectrumType.Standard:
                for (int i = 0; i < objects.Length; i++)
                {
                    objects[i] = Instantiate(prefab, prefParent);
                    objects[i].transform.localPosition = new Vector3(startPosition + distance * i, 0, 0);
                }
                break;
            case spectrumType.Circle:
                //for (int i = 0; i < objects.Length; i++)
                //{
                //    objects[i] = Instantiate(prefab, prefParent);
                //    objects[i].transform.localPosition = new Vector3(distance * i, 0, 0);
                //}

                float radius = Mathf.Abs(endPosition - startPosition) / 2;
                Vector3 point = new Vector3(endPosition - radius, 0, 0);
                for (int i = 0; i < objects.Length; i++)
                {
                    var radians = 2 * Mathf.PI / objects.Length * i;

                    var spawnDir = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0);

                    var spawnPos = point + spawnDir * radius;

                    objects[i] = Instantiate(prefab, prefParent);
                    objects[i].transform.localPosition = spawnPos;
                    objects[i].transform.localEulerAngles = new Vector3(0, 0, 90f + 360f * ((float)i / (float)objects.Length));
                }
                break;

        }

    }

    private void Update()
    {
        time += Time.deltaTime;
        if (beatCount == 0)
            return;
        if (time > changeIndexTime)
        {
            indexChangeCount += changeIndexBy;
            if (indexChangeCount > 1024 || indexChangeCount < 0)
            {
                indexChangeCount = 0;
            }
            time = 0;
        }
        audSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
        distance = Math.Abs(startPosition - endPosition) / beatCount;
        DrawSpectrum();
    }
    float AverageFromSamplesRange(int start, int length) {
        return samples.Skip(start).Take(length).Average();
    }
    void DrawSpectrum()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            int countMultiplier = 1024 / beatCount;
            //objects[i].transform.localPosition = new Vector3(distance * i, 0, 0);
            int index = i * countMultiplier;
            objects[i].transform.localScale = new Vector3(0.1f * defaultWidthMultiplier * countMultiplier, sizeMultiplier * AverageFromSamplesRange(countMultiplier * i,countMultiplier), 0);
        }
    }

    public void ChangeSamplesCount(int val)
    {
        if (val == 0)
        {
            ClearArray();
            return;
        }
        beatCount = (int)Mathf.Pow(2, 5 + val);
        ClearArray();
        FillArray();
        DrawSpectrum();
    }
}
