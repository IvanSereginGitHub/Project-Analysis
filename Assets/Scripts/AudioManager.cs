using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using PathCreation;
using PathCreation.Examples;

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
    public AudioSource audSource;
    public float intensityTimeDelta = 1f;
    public float gameplayChangeTimeDelta = 1f;
    public float sensitivity = 0.2f;
    public int samplesPerThread = 10000;
    private ConcurrentQueue<string> logMessages = new ConcurrentQueue<string>();
    public PathCreator pathCreator;
    public RoadMeshCreator roadMeshCreator;
    public int pathCreatorPointsOffset = 50;
    public float pathCreatorPointsMultiplier = 0.5f;
    public float pathCreatorTurnTreshold = 50f;
    public float pathCreatorPointsDistance = 10f;
    void ScanForSegments(float[] songSamples)
    {
        float average = 0;
        for (int i = 0; i < songSamples.Length; i++)
        {
            average += Mathf.Abs(songSamples[i]);
        }
        average /= songSamples.Length;

        float diff = average + sensitivity;
        Debug.Log("done");
        // int numThreads = songSamples.Length / samplesPerThread;
        // int numThreads = 5;

        // for (int t = 0; t <= numThreads; t++)
        // {
        //     int start = t * samplesPerThread;
        //     int end = start + samplesPerThread;
        //     if (end >= songSamples.Length)
        //     {
        //         end = songSamples.Length;
        //     }
        //     Task task = new Task(() =>
        //     {
        //         for (int i = start; i < end; i++)
        //         {
        //             if (Mathf.Abs(songSamples[i]) > diff)
        //             {
        //                 logMessages.Enqueue("Significant volume change detected at sample " + i);
        //             }
        //         }
        //     });
        //     task.Start();
        // }
        // for (int i = 0; i < songSamples.Length; i++)
        // {
        //     Debug.LogObjects(i, songSamples.Length, (float)i / songSamples.Length);
        //     float sample = Mathf.Abs(songSamples[i]);
        //     if (sample > diff)
        //     {
        //         Debug.Log("Significant volume change detected at sample " + i);
        //     }
        //     yield return null;
        // }
        // Debug.Log("done2");
    }
    void Awake()
    {
        if (analyzeSong)
            StartCoroutine(ScanSong());
    }

    public void ChangeSongPos(float multiplier) {
        audSource.time = audSource.clip.length * multiplier;
    }
    void Update()
    {
        while (logMessages.TryDequeue(out string message))
        {
            Debug.Log(message);
        }
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

    BezierPath GeneratePath(Vector3[] points, bool closedPath)
    {
        // Create a closed, 2D bezier path from the supplied points array
        // These points are treated as anchors, which the path will pass through
        // The control points for the path will be generated automatically
        return new BezierPath(points, closedPath, PathSpace.xyz);
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
    List<Vector3> points = new List<Vector3>();
    public (int, Vector3) GetNextPosition()
    {
        float time = audSource.time / audSource.clip.length;
        int i = Mathf.FloorToInt(time * (points.Count - 1));
        return (i, points[i + 1]);
    }
    IEnumerator ScanSong()
    {
        var sw = Stopwatch.StartNew();
        Debug.Log("Started scanning the song...");
        float[] totalSamples = new float[audSource.clip.samples * audSource.clip.channels];
        audSource.clip.GetData(totalSamples, 0);
        // ScanForSegments(totalSamples);
        List<double[]> allProcessedSamples = new List<double[]>();
        for (int i = 0; i < totalSamples.Length; i += samplesAmountPerScan)
        {
            float[] samplesArr = new float[samplesAmountPerScan];
            for (int j = i; j < Mathf.Min(i + samplesAmountPerScan, totalSamples.Length); j++)
            {
                samplesArr[j - i] = totalSamples[j];
            }
            System.Numerics.Complex[] complexes = FastFourierTransform.ConvertFloatToComplex(samplesArr);
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
        for (int i = 0; i < allProcessedSamples.Count; i += pathCreatorPointsOffset)
        {
            // float avg = (float)allProcessedSamples[i].Average();
            // if (avg - pathCreatorTurnTreshold <= 0)
            // {
            //     avg = 0;
            // }
            // else
            // {
            //     avg -= pathCreatorTurnTreshold;
            // }
            float timeInSeconds = audSource.clip.length * ((float)(i * samplesAmountPerScan) / totalSamples.Length);
            Vector3 pathPoint = pathCreatorPointsMultiplier * new Vector3(0, 0, timeInSeconds);
            // Instantiate(point, pathPoint, Quaternion.identity);
            points.Add(pathPoint);
            Debug.LogObjects(pathPoint, timeInSeconds);
        }
        // pathCreator.bezierPath = GeneratePath(points.ToArray(), false);
        // pathCreator.bezierPath.ControlPointMode = BezierPath.ControlMode.Automatic;
        // roadMeshCreator.TriggerUpdate();
        // Debug.Log(pathCreator.bezierPath.NumPoints);
        audSource.Play();
        // List<double> spectrumFlux = new List<double>();

        // for (int i = 0; i < allProcessedSamples.Count - 1; i++)
        // {
        //     spectrumFlux.Add(GetSpectrumFluxValue(allProcessedSamples[i], allProcessedSamples[i + 1]));
        // }
        // List<float> detectedIntesityChanges = DetectIntensityChanges(spectrumFlux, 5, totalSamples.Length);
        // Debug.LogObjects("Time elapsed:", sw.ElapsedMilliseconds, "milliseconds");
        // Debug.LogObjects(allProcessedSamples.Count, spectrumFlux.Count, detectedIntesityChanges.Count);
        // for (int i = 0; i < detectedIntesityChanges.Count; i++)
        // {
        //     Debug.Log(detectedIntesityChanges[i]);
        // }
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
