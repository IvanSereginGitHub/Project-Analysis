using System.Collections.Generic;
using UnityEngine;

public class AudioSegmentDetector : MonoBehaviour
{
    public static List<float> DetectSegments(float[] samples, float totalTime, int segmentSize = 8192, float segmentThreshold = 0.1f, int smoothingWindowSize = 3)
    {
        List<float> segments = new List<float>();
        // Calculate the average volume of each segment
        int numSegments = samples.Length / segmentSize;
        float[] segmentVolumes = new float[numSegments];

        for (int i = 0; i < numSegments; i++)
        {
            float sum = 0f;
            int startSample = i * segmentSize;
            int endSample = Mathf.Min((i + 1) * segmentSize, samples.Length);

            for (int j = startSample; j < endSample; j++)
            {
                sum += Mathf.Abs(samples[j]);
            }

            segmentVolumes[i] = sum / segmentSize;
        }

        // Smooth the volume data using a simple moving average
        float[] smoothedVolumes = SmoothVolumeData(segmentVolumes, smoothingWindowSize);

        // Detect segments based on differences in smoothed average volume
        for (int i = 1; i < numSegments; i++)
        {
            float volumeDifference = Mathf.Abs(smoothedVolumes[i] - smoothedVolumes[i - 1]);
            if (volumeDifference > segmentThreshold)
            {
                Debug.LogObjects("Segment detected at sample: " + (i * segmentSize), "volume diff is", volumeDifference);
                segments.Add(AudioAnalyzer.ConvertSampleIndexToTime(i * segmentSize, samples.Length, totalTime));
                // You can trigger events or perform actions based on segment detection here
            }
        }
        return segments;
    }

    static float[] SmoothVolumeData(float[] data, int windowSize)
    {
        float[] smoothedData = new float[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            float sum = 0f;
            int count = 0;

            for (int j = Mathf.Max(0, i - windowSize / 2); j < Mathf.Min(data.Length, i + windowSize / 2 + 1); j++)
            {
                sum += data[j];
                count++;
            }

            smoothedData[i] = sum / count;
        }

        return smoothedData;
    }
}
