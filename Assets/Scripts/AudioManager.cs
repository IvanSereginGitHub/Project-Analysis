using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using TMPro;
using UnityEngine;

[System.Serializable]
class SongTimeEvent<T>
{
    public T param;
    public float time;
    public SongTimeEvent(T p, float t)
    {
        param = p;
        time = t;
    }
}
public class AudioManager : MonoBehaviour
{
    public bool analyzeSong;
    float[] samples = new float[1024];
    public int samplesAmountPerScan = 1024;
    [SerializeField]
    TextMeshProUGUI songTimeText;
    [SerializeField]
    AudioSource audSource;
    public float intensityTimeDelta = 1f;
    public float gameplayChangeTimeDelta = 1f;

    public
    void Start()
    {
        if (analyzeSong)
            StartCoroutine(ScanSong());
    }
    void Update()
    {
        audSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
        songTimeText.text = audSource.time.ToString();
    }
    public float[] GetSpectrumDataArr()
    {
        return samples;
    }

    public double GetSpectrumFluxValue(double[] spectrum, double[] prevSpectrum)
    {
        double sum = 0f;

        // Sum the spectral flux over all bins
        for (int i = 0; i < spectrum.Length; i++)
        {
            // The spectral flux is positive if the spectrum has increased in power from the previous frame
            // And zero otherwise
            sum += Math.Max(0, spectrum[i] - prevSpectrum[i]);
        }
        return sum;
    }

    public List<float> DetectIntensityChanges(List<double> spectralFlux, double changeThreshold, int totalSamplesLength)
    {
        List<float> detectedSeconds = new List<float>();
        for (int i = 1; i < spectralFlux.Count - 1; i++)
        {
            // Check if the percentage difference is above the threshold
            Debug.LogObjects(spectralFlux[i], spectralFlux[i - 1], changeThreshold);
            if (Math.Abs(spectralFlux[i] - spectralFlux[i - 1]) > changeThreshold)
            {
                float timeInSeconds = audSource.clip.length * ((float)(i * samplesAmountPerScan) / totalSamplesLength);
                detectedSeconds.Add(timeInSeconds);
            }
        }
        return detectedSeconds;
    }

    IEnumerator ScanSong()
    {
        var sw = Stopwatch.StartNew();
        Debug.Log("Started scanning the song...");
        float[] totalSamples = new float[audSource.clip.samples * audSource.clip.channels];
        audSource.clip.GetData(totalSamples, 0);
        List<double[]> allProcessedSamples = new List<double[]>();
        for (int i = 0; i < totalSamples.Length; i += samplesAmountPerScan)
        {
            float[] samplesArr = new float[samplesAmountPerScan];
            for (int j = i; j < Mathf.Min(i + samplesAmountPerScan, totalSamples.Length); j++)
            {
                samplesArr[j - i] = totalSamples[j];
            }
            Complex[] complexes = FastFourierTransform.ConvertFloatToComplex(samplesArr);
            FastFourierTransform.FFT(complexes);

            double[] magnitudes = new double[complexes.Length / 2];

            for (int l = 0; l < magnitudes.Length; l++)
            {
                magnitudes[l] = complexes[l].Magnitude;
            }
            allProcessedSamples.Add(magnitudes);
        }
        Debug.Log(allProcessedSamples.Count);

        sw.Stop();

        List<double> spectrumFlux = new List<double>();

        for (int i = 0; i < allProcessedSamples.Count - 1; i++)
        {
            spectrumFlux.Add(GetSpectrumFluxValue(allProcessedSamples[i], allProcessedSamples[i + 1]));
        }
        List<float> detectedIntesityChanges = DetectIntensityChanges(spectrumFlux, 5, totalSamples.Length);
        Debug.LogObjects("Time elapsed:", sw.ElapsedMilliseconds, "milliseconds");
        Debug.LogObjects(allProcessedSamples.Count, spectrumFlux.Count, detectedIntesityChanges.Count);
        for (int i = 0; i < detectedIntesityChanges.Count; i++)
        {
            Debug.Log(detectedIntesityChanges[i]);
        }
        // // gameplay changes
        // for (int i = 0; i < allProcessedSamples.Count; i++)
        // {
        //     double[] processedSamples = allProcessedSamples[i];
        //     // List<int> alreadyChecked = new List<int>();
        //     float timeInSeconds = audSource.clip.length * ((float)(i * samplesAmountPerScan) / totalSamples.Length);
        //     double intensity = processedSamples.Average();


        //     Debug.LogObjects(intensity, timeInSeconds);
        // }
        yield return null;
    }

}
