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
    [FieldInformation("Количество сэмплов*", "Изменение количества сэмплов для графика по-умолчанию. Чем меньше значение - тем крупнее элементы графика.<br><br>* - Требуется пересоздание для применения изменений.")]
    public int spectrum_beatCount = 64;
    public int changeIndexBy = 0;
    public float changeIndexTime = 0;
    [FieldInformation("Множитель размера", "Изменение множителя чувствительности значений для графика.")]
    public float spectrum_sizeMultiplier = 1;
    public float defaultWidthMultiplier = 1, endPosition, startPosition;
    public int countMultiplier = 0;
    [FieldInformation("Дистанция между элементами*", "Количество условных единиц мира, между которыми будут находится два соседних элемента.<br><br>* - Требуется пересоздание для применения изменений, значение по умолчанию: |startPosition - endPosition| / spectrum_beatCount.")]
    public float spectrum_distance = 0;
    float[] samples = new float[1024];
    Transform[] objects = new Transform[0];

    public bool useInverseSamples = false;
    [FieldInformation("Постоянно обновлять", "Переключение обновления графика. Если опция включена - при остановке музыки график будет сбрасываться до стандартных значений.")]
    public bool spectrum_alwaysUpdate = false;
    // [FieldInformation("Пересоздать", "Некоторые из настроек данного графика требует пересоздания. Нажмите, чтобы создать актуальную версию графика.")]
    // public Action spectrum_regenerateSpectrum = null;

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        if (countMultiplier == 0)
        {
            countMultiplier = 1024 / spectrum_beatCount;
        }
        if (spectrum_distance == 0)
            spectrum_distance = Math.Abs(startPosition - endPosition) / spectrum_beatCount;
        ClearArray();
        FillArray();
        if (!useInverseSamples)
            VizualizeSpectrum();
        else
            VizualizeSamples();
    }

    private void Start()
    {
        // spectrum_regenerateSpectrum = new Action(() => { Regenerate(); });
        Regenerate();
    }
    void ClearArray()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            Destroy(objects[i].gameObject);
        }
        objects = new Transform[0];
    }

    void FillArray()
    {
        objects = new Transform[spectrum_beatCount];
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i] = Instantiate(prefab, prefParent).transform;
            objects[i].localPosition = new Vector3(startPosition + spectrum_distance * i, 0, 0);
            objects[i].localScale = new Vector2(5, 5);
        }
    }
    float[] reversedSamples = new float[1024];
    private void Update()
    {
        audSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
        if (!audSource.isPlaying && !spectrum_alwaysUpdate)
        {
            return;
        }
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
            objects[i].localScale = new Vector3(defaultWidthMultiplier, spectrum_sizeMultiplier * AverageFromSamplesRange(countMultiplier * i, countMultiplier), 0);
        }
    }

    void VizualizeSamples()
    {
        //reversedSamples = FastFourierTransform.ConvertComplexToFloat_Real(FastFourierTransform.iFFT(FastFourierTransform.ConvertFloatToComplex(samples)));
        audSource.GetOutputData(reversedSamples, 0);
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].localPosition = new Vector2(objects[i].localPosition.x, reversedSamples[i] * spectrum_sizeMultiplier);
            //objects[i].localScale = new Vector3(defaultWidthMultiplier, sizeMultiplier * AverageFromSamplesRange(countMultiplier * i, countMultiplier), 0);
        }
    }

}
