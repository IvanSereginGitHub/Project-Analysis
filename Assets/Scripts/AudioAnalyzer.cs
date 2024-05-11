using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TMPro;
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
  public int segmentsSampleLength = 16384;
  public float segmenterDifferenceThreshold = 0.3f;
  //How many additional segments from right are required to calculate average?
  public int segmenterSmoothingWindowSize = 3;
  /// <summary>
  /// Uses last found segment's average value diff as comparison for the next segments
  /// </summary>
  public bool preserveLastDiffValueAsCompare = false;

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
  public Image loadingImage, hideOnLoadingImage;
  float splitProgress = 0f, analysisProgress = 0f;
  bool useAverageInsteadOfMax = false;

  public List<GameObject> analysisSettings = new List<GameObject>();
  Prompt analysisOptionsPrompt;
  public RawImage spectrogramTexture;
  public void Start()
  {
    analysisOptionsPrompt = new Prompt(PromptType.ExitOnly);
    Prompts.PreparePrompt(analysisOptionsPrompt, true);
    foreach (var obj in analysisSettings)
    {
      analysisOptionsPrompt.PlaceGameobjectInside(obj);
    }
  }
  public void OpenAnalysisSettings()
  {
    analysisOptionsPrompt.Show();
  }

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
  // Function for splitting audio into chunks
  public float[] AudioSplitter(float[] totalSamples, int segmentsSampleLength)
  {
    int arr_size = Mathf.CeilToInt(totalSamples.Length / segmentsSampleLength);
    float[] res = new float[arr_size + 1];
    int count = 0;
    float sum = 0;
    for (int i = 0; i < totalSamples.Length; i++)
    {
      sum += Math.Abs(totalSamples[i]);
      if ((i == segmentsSampleLength * (count + 1)) || i == totalSamples.Length - 1)
      {
        // Getting average value as result
        //Debug.LogObjects(arr_size, count);
        res[count] = sum / segmentsSampleLength;
        count++;
        sum = 0;
      }
      splitProgress = (float)i / totalSamples.Length;
    }
    return res;
  }
  public List<float> SegmentsAnalyzer_splitted(float[] splittedSamples, float clipLength)
  {
    Debug.LogObjects("info:", segmentsSampleLength, segmenterDifferenceThreshold, segmenterSmoothingWindowSize);
    List<float> min_times = new List<float>();
    float previousAverage = 0f;
    Debug.Log(splittedSamples.Length);
    for (int i = 0; i < splittedSamples.Length; i++)
    {
      float average = splittedSamples[i];

      // Add selected count of windows

      int min_value = Math.Min(splittedSamples.Length, i + segmenterSmoothingWindowSize);
      for (int j = i + 1; j < min_value; j++)
      {
        average += splittedSamples[j];
      }

      average /= segmenterSmoothingWindowSize;

      if (Math.Abs(previousAverage - average) > segmenterDifferenceThreshold)
      {
        //Debug.LogObjects(approx_time, previousAverage, average);
        if (preserveLastDiffValueAsCompare)
        {
          previousAverage = average;
          //i += segmentsSampleLength * (segmenterSmoothingWindowSize + 1);
        }
        float approx_time = ConvertSampleIndexToTime(i, splittedSamples.Length, clipLength);
        min_times.Add(approx_time);
      }

      if (!preserveLastDiffValueAsCompare)
        previousAverage = splittedSamples[i];
      analysisProgress = (float)i / splittedSamples.Length;
    }
    return min_times;
  }
  public Color32[] GenerateSpectrogram(int width, int height, float[] samples, bool convertToDb = true)
  {
    int numFrames = samples.Length / width;
    Color32[] pixels = new Color32[width * height];

    for (int x = 0; x < width; x++)
    {
      float[] frame = new float[height];
      int offset = x * numFrames;

      for (int j = 0; j < height; j++)
      {
        int ind = Math.Min(offset + j, samples.Length - 1);
        //Debug.LogObjects(ind, samples.Length);
        frame[j] = samples[ind];
      }

      System.Numerics.Complex[] complexes = FastFourierTransform.ConvertFloatToComplex(frame);
      FastFourierTransform.FFT(complexes);
      Debug.Log(complexes.Length);
      double[] magnitudes = new double[complexes.Length / 2];
      for (int l = 0; l < magnitudes.Length; l++)
      {
        magnitudes[l] = complexes[l].Magnitude;
        if (convertToDb)
          magnitudes[l] = 20 * Math.Log10(magnitudes[l]); // convert to db
      }

      for (int y = 0; y < magnitudes.Length; y++)
      {
        float intensity = (float)magnitudes[y];
        pixels[x + y * width] = new Color(intensity, intensity, intensity);
      }
    }
    return pixels;
  }
  public List<float> SegmenterAnalyzer(float[] totalSamples, float clipLength)
  {
    Debug.LogObjects("info:", segmentsSampleLength, segmenterDifferenceThreshold, segmenterSmoothingWindowSize);
    List<float> min_times = new List<float>();
    float previousAverage = 0f;
    for (int i = 0; i < totalSamples.Length; i += segmentsSampleLength)
    {
      IEnumerable<float> arr = totalSamples.Skip(i).Take(segmentsSampleLength * (segmenterSmoothingWindowSize + 1));
      //Smooth the volume difference throughout the segment to exclude sudden spikes
      // float average = sub_arr.Max();
      // float smoothedWindowAverage = average;
      float sum = 0;
      int count = 0;
      //List<float> values = new List<float>();
      //int time_ind = i /*+ arr.IndexOf(average)*/;
      // for (int j = 0; j <= segmenterSmoothingWindowSize; j++)
      // {
      //   IEnumerable<float> sub_arr = arr.Skip(j * segmentsSampleLength).Take(segmentsSampleLength).Select(x => Math.Abs(x));
      //   if (sub_arr.Count() < 1)
      //     continue;
      //   //values.Add(sub_arr.Max());
      //   sum += sub_arr.Average();
      //   count++;
      // }

      //Debug.LogList(values);
      float average = arr.Take(segmentsSampleLength * segmenterSmoothingWindowSize).Select(x => Math.Abs(x)).Average();

      float approx_time = ConvertSampleIndexToTime(i, totalSamples.Length, clipLength);

      if (Math.Abs(previousAverage - average) > segmenterDifferenceThreshold)
      {
        //Debug.LogObjects(approx_time, previousAverage, average);
        if (preserveLastDiffValueAsCompare)
        {
          previousAverage = average;
          //i += segmentsSampleLength * (segmenterSmoothingWindowSize + 1);
        }

        min_times.Add(approx_time);
      }

      if (!preserveLastDiffValueAsCompare)
        previousAverage = arr.Take(segmentsSampleLength).Select(x => Math.Abs(x)).Average();
      analysisProgress = (float)i / totalSamples.Length;
    }
    return min_times;
  }
  List<float> segments = new List<float>();
  float snippet_start = 0f;
  public void DoSongSegmentation(float[] totalSamples, float clipLength)
  {
    segments = SegmenterAnalyzer(totalSamples, clipLength);
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
  public void ChangePreserveLastDiffValueAsCompare(bool val)
  {
    preserveLastDiffValueAsCompare = val;
  }
  public void ChangeSegmenterDiffValue(string val)
  {
    segmenterDifferenceThreshold = Convert.ToSingle(val);
  }

  public void ChangeSegmenterWindowSize(string val)
  {
    segmenterSmoothingWindowSize = Convert.ToInt32(val);
  }

  public void ChangeSegmenterSampleSize(string val)
  {
    segmentsSampleLength = Convert.ToInt32(val);
  }
  public void AnalyzeSongSegmentsPos(float[] totalSamples, float clipLength)
  {
    DoSongSegmentation(totalSamples, clipLength);
  }

  public void AnalyzeSong()
  {
    AnalyzeSong(null);
  }

  IEnumerator CreateSegmentPrefabs()
  {
    foreach (Transform t in segmentPrefabParent)
    {
      Destroy(t.gameObject);
    }
    for (int i = 0; i < segments.Count; i++)
    {
      float time = segments[i];
      GameObject temp = Instantiate(segmentPrefab, segmentPrefabParent);
      temp.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = segments[i].ToString();
      temp.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(delegate { audSource.Stop(); audSource.time = time; audSource.Play(); });
      yield return null;
    }
  }

  public void StartAnalyzingSong()
  {
    if (isRunningAnalysis) { Prompts.QuickStrictPrompt("Analyzing is still in progress, please wait..."); return; }
    StartCoroutine(AnalyzeSong(null));
  }

  public void StartAnalyzingSong(float[] totalSamples = null)
  {
    if (isRunningAnalysis) { Prompts.QuickStrictPrompt("Analyzing is still in progress, please wait..."); return; }
    StartCoroutine(AnalyzeSong(totalSamples));
  }
  bool isRunningAnalysis = false;
  IEnumerator AnalyzeSong(float[] totalSamples = null)
  {
    spectrumsList.Clear();
    // foreach (Transform child in listPrefabParent)
    // {
    //   Destroy(child.gameObject);
    // }
    if (audSource.clip == null)
    {
      Prompts.QuickStrictPrompt("Load audiofile first!");
      yield break;
    }
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
    /*    for (int i = 0; i < totalSamples.Length; i += sampleRate)
       {
         StartCoroutine(ProcessSamples(counter, i, totalSamples, clipLength));
         counter++;
       } */
    splitProgress = 0f;
    analysisProgress = 0f;
    hideOnLoadingImage.enabled = false;
    int stored_sample_length = segmentsSampleLength;


    Texture2D specTex = new Texture2D((int)spectrogramTexture.rectTransform.rect.width, (int)spectrogramTexture.rectTransform.rect.height, TextureFormat.RGBA32, false);
    Color32[] colorsArr = new Color32[0];
    int tex_width = specTex.width;
    int tex_heigth = specTex.height;
    Thread myThread = new Thread(() =>
    {
      Debug.Log("splitting samples");
      isRunningAnalysis = true;
      float[] splitted_arr = AudioSplitter(totalSamples, stored_sample_length);
      colorsArr = GenerateSpectrogram(tex_width, tex_heigth, totalSamples);
      Debug.LogObjects("done splitting, result: ", splitted_arr.Length);
      Debug.Log("analyzing segments");
      segments = SegmentsAnalyzer_splitted(splitted_arr, clipLength);
      Debug.LogObjects("done analyzing, result: ", segments.Count);
      isRunningAnalysis = false;
      Debug.Log("finished");
      // segments = SegmenterAnalyzer(totalSamples, clipLength);
      // Debug.Log("finishing");
    });

    myThread.Start();
    while (myThread.IsAlive)
    {
      loadingImage.fillAmount = splitProgress / 2 + analysisProgress / 2;
      yield return null;
    }
    specTex.SetPixels32(colorsArr);
    specTex.Apply();
    spectrogramTexture.texture = specTex;
    hideOnLoadingImage.enabled = true;
    splitProgress = 0f;
    analysisProgress = 0f;
    loadingImage.fillAmount = splitProgress / 2 + analysisProgress / 2;
    StartCoroutine(CreateSegmentPrefabs());
    //DoSongSnippetFinding(totalSamples, clipLength);
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
