using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioAnalyzer : MonoBehaviour
{
  [SerializeField]
  Color textureMainColor = Color.white;
  Color transparent = new Color(0, 0, 0, 0);

  [Header("REFERENCES")]
  [SerializeField]
  AudioSource audSource;
  [SerializeField]
  GameObject listPrefab;
  [SerializeField]
  Transform listPrefabParent;
  [SerializeField]
  Transform segmentPrefabParent;
  [SerializeField]
  GameObject segmentPrefab;
  [SerializeField]
  Image loadingImage, hideOnLoadingImage;


  [Header("SEGMENTER SETTINGS")]
  public float quitePartsThreshold = 0.1f;
  public int segmentsSampleLength = 16384;
  public float segmenterDifferenceThreshold = 0.3f;
  //How many additional segments from right are required to calculate average?
  public int segmenterSmoothingWindowSize = 3;
  /// <summary>
  /// Uses last found segment's average value diff as comparison for the next segments
  /// </summary>
  public bool progressiveSegmentsFinding = false;

  float splitProgress = 0f, analysisProgress = 0f;
  [SerializeField]
  List<GameObject> analysisSettings = new List<GameObject>();
  Prompt analysisOptionsPrompt, spectrogramOptionsPrompt, spectrumOptionsPrompt;
  [Header("SPECTROGRAM SETTINGS")]
  public RawImage spectrogramTexture;
  [SerializeField]
  Gradient spectrogramGradient;
  [SerializeField]
  float spectrogramMultiply, spectrogramDBOffset;
  [SerializeField]
  float spectogramDefaultWidth = 1024, spectogramDefaultHeight = 1024;
  [Range(1, 8)]
  public int spectrogramWidthMultiplier, spectrogramHeightMultiplier;
  [SerializeField]
  List<GameObject> spectogramSettings = new List<GameObject>();
  [Header("SPECTRUM SETTINGS")]
  public List<GameObject> spectrumSettings = new List<GameObject>();
  public void Start()
  {
    analysisOptionsPrompt = new Prompt(PromptType.ExitOnly)
    {
      promptText = "Настройки анализа сегментов:"
    };
    Prompts.PreparePrompt(analysisOptionsPrompt, true);
    foreach (var obj in analysisSettings)
    {
      analysisOptionsPrompt.PlaceGameobjectInside(obj);
    }

    spectrogramOptionsPrompt = new Prompt(PromptType.ExitOnly)
    {
      promptText = "Настройки спектрограммы:"
    };
    Prompts.PreparePrompt(spectrogramOptionsPrompt, true);
    foreach (var obj in spectogramSettings)
    {
      spectrogramOptionsPrompt.PlaceGameobjectInside(obj);
    }

    spectrumOptionsPrompt = new Prompt(PromptType.ExitOnly)
    {
      promptText = "Настройки графика спектральной функции:"
    };
    Prompts.PreparePrompt(spectrumOptionsPrompt, true);
    foreach (var obj in spectrumSettings)
    {
      spectrumOptionsPrompt.PlaceGameobjectInside(obj);
    }
  }

  public void ChangeSpectrogramWidth(float val)
  {
    spectrogramWidthMultiplier = (int)val;
  }
  public void ChangeSpectrogramHeight(float val)
  {
    spectrogramHeightMultiplier = (int)val;
  }

  public void ChangeSpectrogramMultiply(float val)
  {
    spectrogramMultiply = val;
  }
  public void OpenAnalysisSettings()
  {
    analysisOptionsPrompt.Show();
  }

  public void OpenSpectrogramSettings()
  {
    spectrogramOptionsPrompt.Show();
  }

  public void OpenSpectrumSettings()
  {
    spectrumOptionsPrompt.Show();
  }
  public SelectFiles selectFiles;
  public void ExportTextureToFile(RawImage tex)
  {
    selectFiles.SaveFile((tex.texture as Texture2D).EncodeToPNG(), $"{tex.gameObject.name}.png");
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
        if (progressiveSegmentsFinding)
        {
          previousAverage = average;
        }
        float approx_time = ConvertSampleIndexToTime(i, splittedSamples.Length, clipLength);
        min_times.Add(approx_time.Round(1));
      }

      if (!progressiveSegmentsFinding)
        previousAverage = splittedSamples[i];
      analysisProgress = (float)i / splittedSamples.Length;
    }
    return min_times;
  }

  public Color GetGradientColor(Gradient gradient, float value)
  {
    return gradient.Evaluate(value);
  }
  public Color32[] GenerateSpectrogram(int width, int height, float[] samples, bool convertToDb = false, bool fixDualChannelBug = true)
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
        frame[j] = samples[ind];
      }

      Complex[] complexes = AnalysisFunctions.PerformFFT(Array.ConvertAll(frame, Convert.ToDouble));
      double[] magnitudes = new double[complexes.Length / 2];
      if (fixDualChannelBug)
        magnitudes = new double[complexes.Length / 4];

      for (int l = 0; l < magnitudes.Length; l++)
      {
        magnitudes[l] = complexes[l].Magnitude;
        if (convertToDb)
          magnitudes[l] = 20 * Math.Log10(magnitudes[l]) + spectrogramDBOffset; // convert to db incase needed
      }

      for (int y = 0; y < magnitudes.Length; y++)
      {
        float intensity = (float)magnitudes[y] * spectrogramMultiply;

        pixels[x + y * width] = GetGradientColor(spectrogramGradient, intensity);
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

      float average = arr.Take(segmentsSampleLength * segmenterSmoothingWindowSize).Select(x => Math.Abs(x)).Average();

      float approx_time = ConvertSampleIndexToTime(i, totalSamples.Length, clipLength);

      if (Math.Abs(previousAverage - average) > segmenterDifferenceThreshold)
      {
        if (progressiveSegmentsFinding)
        {
          previousAverage = average;
        }
        min_times.Add(approx_time);
      }

      if (!progressiveSegmentsFinding)
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
      Prompts.ShowQuickStrictPrompt("Analyze song segments first!");
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
  public void ChangeSegmenterSampleCount(string val)
  {
    segmentsSampleLength = Convert.ToInt32(val);
  }
  public void ChangePreserveLastDiffValueAsCompare(bool val)
  {
    progressiveSegmentsFinding = val;
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
    if (isRunningAnalysis) { Prompts.ShowQuickStrictPrompt("Analyzing is still in progress, please wait..."); return; }
    StartCoroutine(AnalyzeSong(null));
  }

  public void StartAnalyzingSong(float[] totalSamples = null)
  {
    if (isRunningAnalysis) { Prompts.ShowQuickStrictPrompt("Analyzing is still in progress, please wait..."); return; }
    StartCoroutine(AnalyzeSong(totalSamples));
  }
  bool isRunningAnalysis = false;
  IEnumerator AnalyzeSong(float[] totalSamples = null)
  {
    if (audSource.clip == null)
    {
      Prompts.ShowQuickStrictPrompt("Load audiofile first!");
      yield break;
    }
    float clipLength = audSource.clip.length;
    totalSamples ??= GetTotalSamples();

    splitProgress = 0f;
    analysisProgress = 0f;
    hideOnLoadingImage.enabled = false;
    int stored_sample_length = segmentsSampleLength;

    spectrogramTexture.rectTransform.sizeDelta = new UnityEngine.Vector2(spectogramDefaultWidth * spectrogramWidthMultiplier, spectogramDefaultHeight * spectrogramHeightMultiplier);
    Debug.LogObjects((int)spectrogramTexture.rectTransform.rect.width, (int)spectrogramTexture.rectTransform.rect.height);
    Texture2D specTex = new Texture2D((int)spectrogramTexture.rectTransform.rect.width, (int)spectrogramTexture.rectTransform.rect.height, TextureFormat.RGBA32, false);
    Color32[] colorsArr = new Color32[0];
    int tex_width = specTex.width;
    int tex_heigth = specTex.height;
    Thread myThread = new Thread(() =>
    {
      Debug.Log("splitting samples");
      isRunningAnalysis = true;
      float[] splitted_arr = AudioSplitter(totalSamples, stored_sample_length);
      Debug.LogObjects("done splitting, result: ", splitted_arr.Length);
      Debug.Log("generating spectrogram using FFT");
      colorsArr = GenerateSpectrogram(tex_width, tex_heigth, totalSamples);
      Debug.Log("done generating spectrogram using FFT");
      Debug.Log("analyzing segments");
      segments = SegmentsAnalyzer_splitted(splitted_arr, clipLength);
      Debug.LogObjects("done analyzing, result: ", segments.Count);
      isRunningAnalysis = false;
      Debug.Log("finished");
    });
    myThread.IsBackground = true;
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
