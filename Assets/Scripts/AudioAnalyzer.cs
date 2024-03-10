using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AudioAnalyzer : MonoBehaviour
{
  Color transparent = new Color(0, 0, 0, 0);
  [SerializeField]
  Transform listPrefabParent;
  [SerializeField]
  GameObject listPrefab;
  [SerializeField]
  Color textureMainColor = Color.white;
  [SerializeField]
  AudioSource audSource;
  int sampleRate = 4096;
  public float quitePartsThreshold = 0.1f;
  int segmentsSampleLength = 16384;
  public float segmenterDifferenceThreshold = 0.3f;
  //How many additional segments from right are required to calculate average?
  public int segmenterSmoothingWindowSize = 3;

  [SerializeField]
  Transform segmentPrefabParent;
  [SerializeField]
  GameObject segmentPrefab;
  // max 0.4f
  List<double[]> spectrumsList = new List<double[]>();

  // void Start()
  // {
  //   AnalyzeSong();
  // }

  bool useAverageInsteadOfMax = false;

  IEnumerator ProcessSamples(int ind, int start_sample, float[] totalSamples, float clipLength)
  {
    float minTime = 0, maxTime = 0;
    double[] magnitudes = new double[0];
    float[] samplesArr = new float[sampleRate];
    Thread myThread = new Thread(() =>
    {
      for (int j = start_sample; j < Mathf.Min(start_sample + sampleRate, totalSamples.Length); j++)
      {
        samplesArr[j - start_sample] = totalSamples[j];
      }
      System.Numerics.Complex[] complexes = FastFourierTransform.ConvertFloatToComplex(samplesArr);
      FastFourierTransform.FFT(complexes);
      magnitudes = new double[complexes.Length / 2];
      for (int l = 0; l < magnitudes.Length; l++)
      {
        magnitudes[l] = complexes[l].Magnitude;
      }
      minTime = clipLength * ((float)Mathf.Max(0, start_sample - sampleRate) / totalSamples.Length);
      maxTime = clipLength * ((float)Mathf.Max(0, start_sample) / totalSamples.Length);
    });
    myThread.Start();
    while (myThread.IsAlive)
    {
      yield return null;
    }
    // GameObject newPrefab = Instantiate(listPrefab, listPrefabParent);
    // RawImage samplesImage = newPrefab.transform.GetChild(0).gameObject.GetComponent<RawImage>();
    // RawImage spectrumImage = newPrefab.transform.GetChild(1).gameObject.GetComponent<RawImage>();
    // TextMeshProUGUI timeText = newPrefab.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>();
    // samplesImage.texture = GetSamplesTexture((int)samplesImage.gameObject.GetComponent<RectTransform>().rect.width, (int)samplesImage.gameObject.GetComponent<RectTransform>().rect.height, samplesArr);
    // spectrumImage.texture = GetSpectrumTexture((int)spectrumImage.gameObject.GetComponent<RectTransform>().rect.width, (int)spectrumImage.gameObject.GetComponent<RectTransform>().rect.height, magnitudes.Take(magnitudes.Length / 2).ToArray());
    // timeText.text = $"{minTime} - {maxTime}";
    spectrumsList[ind] = magnitudes;
    //allProcessedSamples.Add(magnitudes);
  }
  public List<float> SegmenterAnalyzer(float[] totalSamples, float clipLength)
  {
    List<float> min_times = new List<float>();
    float? previousAverage = null;
    for (int i = 0; i < totalSamples.Length; i += segmentsSampleLength)
    {
      List<float> arr = totalSamples.Skip(i).Take(segmentsSampleLength * (segmenterSmoothingWindowSize + 1)).ToList();
      //Smooth the volume difference throughout the segment to exclude sudden spikes
      // float average = sub_arr.Max();
      // float smoothedWindowAverage = average;
      float sum = 0;
      int count = 0;
      List<float> values = new List<float>();
      int time_ind = i /*+ arr.IndexOf(average)*/;
      for (int j = 0; j <= segmenterSmoothingWindowSize; j++)
      {
        IEnumerable<float> sub_arr = arr.Skip(j * segmentsSampleLength).Take(segmentsSampleLength);
        if (sub_arr.Count() < 1)
          continue;
        values.Add(sub_arr.Max());
        sum += sub_arr.Max();
        count++;
      }

      Debug.LogList(values);
      float average = sum / count;

      if (previousAverage != null)
      {
        float approx_time = ConvertSampleIndexToTime(time_ind, totalSamples.Length, clipLength);
        Debug.LogObjects(approx_time, Debug.ListToString(values), average);
        if (Math.Abs(previousAverage.Value - average) > segmenterDifferenceThreshold)
        {
          min_times.Add(approx_time);
          i += segmenterSmoothingWindowSize * segmentsSampleLength;
        }
      }
      previousAverage = average;
    }
    return min_times;
  }
  List<float> segments = new List<float>();
  float snippet_start = 0f;
  public void DoSongSegmentation(float[] totalSamples, float clipLength)
  {
    segments = SegmenterAnalyzer(totalSamples, clipLength);
    for (int i = 0; i < segments.Count; i++)
    {
      float time = segments[i];
      GameObject temp = Instantiate(segmentPrefab, segmentPrefabParent);
      temp.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = segments[i].ToString();
      temp.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(delegate { audSource.Stop(); audSource.time = time; audSource.Play(); });
    }
    // Debug.LogList(GetSegments());
  }
  public static int ConvertTimeToSampleIndex(float time, int totalSamplesLength, float totalTime)
  {
    return (int)(time / totalTime * totalSamplesLength);
  }

  public static float ConvertSampleIndexToTime(int ind, int totalSamplesLength, float totalTime)
  {
    return totalTime * ((float)ind / totalSamplesLength);
  }
  int FindNearestSegmentAt(float time)
  {
    for (int i = 0; i < segments.Count; i++)
    {
      if (segments[i].IsNearlyEqualTo(time, 0.01f))
      {
        return i;
      }
    }
    return -1;
  }

  public void PlaySongSnippet()
  {
    if (segments.Count < 1)
    {
      Prompts.QuickStrictPrompt("Analyze song segments first!");
      return;
    }
    audSource.Stop();
    audSource.time = snippet_start;
    audSource.Play();
  }
  Stopwatch timer;

  void StartStopwatch(string message = "")
  {
    Debug.Log("Startwatch| " + message);
    timer = new Stopwatch();
    timer.Start();
  }

  void StopStopwatch()
  {
    timer.Stop();
    TimeSpan timeTaken = timer.Elapsed;
    Debug.Log("Startwatch| Time taken:" + timeTaken.ToString(@"m\:ss\.f"));
  }

  public void DoSongSnippetFinding(float[] totalSamples, float clipLength)
  {
    float sampleMinVal = 0.2f;
    int move_delta = totalSamples.Length / 10;

    int move_delta_sub = ConvertTimeToSampleIndex(5f, totalSamples.Length, clipLength);
    int divides_amount = 4;
    int final_ind = -1;
    for (int i = 1; i < divides_amount; i++)
    {
      if (final_ind != -1)
        continue;

      int checkSampleInd = i * (totalSamples.Length / divides_amount);
      Debug.LogObjects("Starting at", ConvertSampleIndexToTime(checkSampleInd, totalSamples.Length, clipLength));
      for (int j = checkSampleInd; j > checkSampleInd - move_delta; j--)
      {
        if (final_ind != -1)
          continue;
        int diff = checkSampleInd - j;
        float approx_time_1 = ConvertSampleIndexToTime(checkSampleInd - diff, totalSamples.Length, clipLength);
        float approx_time_2 = ConvertSampleIndexToTime(checkSampleInd + diff, totalSamples.Length, clipLength);
        int segment_1 = FindNearestSegmentAt(approx_time_1);
        int segment_2 = FindNearestSegmentAt(approx_time_2);
        int found_segment_ind = -1;
        if (segment_1 > 0)
        {
          found_segment_ind = segment_1;
        }
        else if (segment_2 > 0)
        {
          found_segment_ind = segment_2;
        }

        if (found_segment_ind != -1)
        {
          int segmentSampleInd = ConvertTimeToSampleIndex(segments[found_segment_ind], totalSamples.Length, clipLength);
          for (int k = segmentSampleInd - move_delta_sub; k < segmentSampleInd; k++)
          {
            if (Math.Abs(totalSamples[k]) <= sampleMinVal)
            {
              Debug.LogObjects("Snippet starts at:", ConvertSampleIndexToTime(k, totalSamples.Length, clipLength));
              final_ind = k;
              break;
            }
          }
          if (final_ind == -1)
          {
            final_ind = segmentSampleInd - move_delta_sub;
          }
        }
      }
    }

    if (final_ind != -1)
    {
      snippet_start = ConvertSampleIndexToTime(final_ind, totalSamples.Length, clipLength);
      return;
    }
    Debug.LogWarning("Didn't found any appropriate snippets!");
  }

  public List<float> GetSegments()
  {
    return segments;
  }

  public float[] GetTotalSamples()
  {
    float[] totalSamples = new float[audSource.clip.samples * audSource.clip.channels];
    //Getting the samples array
    audSource.clip.GetData(totalSamples, 0);
    return totalSamples;
  }

  public void ChangeSampleDiffCalculationType(int val)
  {
    useAverageInsteadOfMax = val > 0;
  }
  public void ChangeSegmenterSampleCount(string val)
  {
    segmentsSampleLength = Convert.ToInt32(val);
  }
  public void ChangeSegmenterDiffValue(string val)
  {
    segmenterDifferenceThreshold = Convert.ToSingle(val);
  }

  public void ChangeSegmenterWindowSize(string val)
  {
    segmenterSmoothingWindowSize = Convert.ToInt32(val);
  }

  public void AnalyzeSongSegmentsPos(float[] totalSamples, float clipLength)
  {
    segments.Clear();
    foreach (Transform t in segmentPrefabParent)
    {
      Destroy(t.gameObject);
    }
    DoSongSegmentation(totalSamples, clipLength);
  }

  public void AnalyzeSong()
  {
    AnalyzeSong(null);
  }
  public void AnalyzeSong(float[] totalSamples = null)
  {
    spectrumsList.Clear();
    // foreach (Transform child in listPrefabParent)
    // {
    //   Destroy(child.gameObject);
    // }
    float clipLength = audSource.clip.length;
    if (totalSamples == null)
    {
      totalSamples = GetTotalSamples();
    }
    //TODO: Multithreading to speed it up
    for (int i = 0; i < totalSamples.Length; i += sampleRate)
    {
      spectrumsList.Add(new double[0]);
    }
    Debug.LogObjects("count", spectrumsList.Count);
    int counter = 0;
    for (int i = 0; i < totalSamples.Length; i += sampleRate)
    {
      StartCoroutine(ProcessSamples(counter, i, totalSamples, clipLength));
      counter++;
    }
    //TODO: Multithread that crap
    AnalyzeSongSegmentsPos(totalSamples, clipLength);
    DoSongSnippetFinding(totalSamples, clipLength);
  }

  public Texture2D GetSpectrumTexture(int width, int height, double[] spectrum_values)
  {
    Texture2D finalSpectrum = new Texture2D(width, height, TextureFormat.RGBA32, false);

    for (int y = 0; y < height; y++)
    {
      for (int x = 0; x < width; x++)
      {
        finalSpectrum.SetPixel(x, y, y < spectrum_values[(int)(spectrum_values.Length * ((float)x / width))] ? textureMainColor : transparent);
      }
    }

    finalSpectrum.Apply();
    return finalSpectrum;
  }

  public Texture2D GetSamplesTexture(int width, int height, float[] samples)
  {
    Texture2D finalSpectrum = new Texture2D(width, height, TextureFormat.RGBA32, false);

    for (int x = 0; x < width; x++)
    {
      for (int y = 0; y < height; y++)
      {
        finalSpectrum.SetPixel(x, y, transparent);
      }
    }

    for (int x = 0; x < width; x++)
    {
      finalSpectrum.SetPixel(x, (int)((height + height * samples[(int)(samples.Length * ((float)x / width))]) / 2), textureMainColor);
    }

    finalSpectrum.Apply();
    return finalSpectrum;
  }
  public void PlaySongAtBeginning()
  {
    float[] totalSamples = new float[audSource.clip.samples * audSource.clip.channels];
    //Getting the samples array
    audSource.clip.GetData(totalSamples, 0);
    audSource.time = FindSongQuietParts(totalSamples)[0];
    audSource.Play();
  }
  public List<float> FindSongQuietParts(float[] total_samples)
  {
    List<float> times = new List<float>();
    for (int i = 0; i < total_samples.Length; i += sampleRate)
    {
      if (Math.Abs(total_samples[i]) >= quitePartsThreshold)
      {
        times.Add(audSource.clip.length * ((float)i / total_samples.Length));
        continue;
      }
    }
    Debug.LogObjects("Quite parts amount", times.Count);
    return times;
  }

  public Texture2D CreateSongSpectrumTexture(int width, int height, AudioClip clip = null)
  {
    if (clip == null)
      clip = audSource.clip;

    float[] spectrum = new float[clip.samples * clip.channels];
    clip.GetData(spectrum, 0);
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

    int deltaValue = spectrum.Length / width;
    float maxValue = 0;
    int value = 0;
    for (int i = 0; i < width; i++)
    {
      for (int j = value; j < value + deltaValue; j++)
      {
        if (maxValue < spectrum[j])
        {
          maxValue = spectrum[j];
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
