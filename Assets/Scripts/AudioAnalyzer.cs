using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using vanIvan.Prompts;

[System.AttributeUsage(System.AttributeTargets.Class |
                       System.AttributeTargets.Struct | System.AttributeTargets.Field)
]
public class FieldInformationAttribute : System.Attribute
{
  public string Name;
  public string Description;
  public float CustomWidth = 0;
  public float CustomHeight = 0;

  public FieldInformationAttribute(string name, string description)
  {
    Name = name;
    Description = description;
  }
  public FieldInformationAttribute(string name, string description, float customWidth, float customHeight)
  {
    Name = name;
    Description = description;
    CustomWidth = customWidth;
    CustomHeight = customHeight;
  }
}


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
  [SerializeField]
  SelectFiles selectFiles;


  [Header("SEGMENTER SETTINGS")]
  [FieldInformation("Размерность блоков", "Количество семплов, определяющих размер одного блок при анализе")]
  public int segmenter_segmentsSampleLength = 1024; // Количество семплов, считающееся за один блок при анализе
  [FieldInformation("Абсолютное значение разницы сегментов", "Значение разницы в громкости звука, которое обозначает появление нового сегмента")]
  public float segmenter_DifferenceThreshold = 0.15f; // Значение разницы в звуке, которая обозначает появление нового сегмента
  [FieldInformation("Количество окон для вычислений", "Количество блоков размерностью в <color=yellow>'Размерность блоков'</color>, которые усредняют и вычисляют громкость следующего блока")]
  public int segmenter_SmoothingWindowSize = 60; // Количество блоков, которые помогают усреднить и вычислить громкость следующего блока
  /// <summary>
  /// Прогрессивное сканирование за счёт сохранения предыдущего значения сегмента и сравнения его с текущим
  /// </summary>
  [FieldInformation("Прогрессивное сканирование", "Прогрессивное сканирование за счёт сохранения предыдущего значения сегмента и сравнения его с текущим.\n\nСледует логике формирования сегментов во многих жанрах музыки, но может выдавать разные значения при небольших отклонениях в настройках.", 250, 30)]
  public bool segmenter_progressiveSegmentsFinding = false;

  float splitProgress = 0f, analysisProgress = 0f;
  Prompt analysisOptionsPrompt, spectrogramOptionsPrompt, spectrumOptionsPrompt, waveformOptionsPrompt;
  [Header("SPECTROGRAM SETTINGS")]
  public RawImage spectrogramTexture;
  [SerializeField]
  Gradient spectrogramGradient;
  [SerializeField]
  [Range(0.01f, 0.5f)]
  [FieldInformation("Множитель значений спектрограммы", "Множитель, увеличивающий значение полученных результатов из преобразования Фурье.")]
  float spectrogram_Multiply;
  float spectrogramDBOffset; // Коэффициенты, влияющие на яркость графика
  [SerializeField]
  float spectogramDefaultWidth = 1024;
  [SerializeField]
  float spectogramDefaultHeight = 1024; // Стандартные размеры текстуры спектрограммы
  [Range(1, 4)]
  [FieldInformation("Множитель длины спектрограммы", "Множитель, увеличивающий длину полученной спектрограммы. Улучшает качество финального изображения.")]
  public int spectrogram_WidthMultiplier;
  [Range(1, 4)]
  [FieldInformation("Множитель ширины спектрограммы", "Множитель, увеличивающий ширину полученной спектрограммы. Улучшает качество финального изображения.")]
  public int spectrogram_HeightMultiplier; // Множители, увеличивающие размер спектрограммы.
  [FieldInformation("Исправление стерео", "Исправление изображения спектрограммы для некоторых двухканальных аудиофайлов.\nПри отключении этой настройки изображение спектрограммы может быть отзеркалено.")]
  public bool spectrogram_fixDualChannel = true; // Исправление изображения спектрограммы для некоторых двухканальных аудиофайлов
  [FieldInformation("Растянуть спектрограмму", "Применить растягивание результатов на всю ширину текстуры (в состоянии false занимает только половину текстуры/четверь текстуры с включенной настройкой fixDualChannel).")]
  public bool spectrogram_stretchTexture = true; // Применить растягивание результатов на всю ширину текстуры? (в состоянии false занимает только половину текстуры/четверь текстуры с включенной настройкой fixDualChannel)  
  [Header("SPECTRUM SETTINGS")]
  public RawImage spectrumTexture;
  [Header("WAVEFORM SETTINGS")]
  [SerializeField]

  float waveformDefaultWidth = 1024;

  float waveformDefaultHeight = 1024; // Стандартные размеры текстуры звуковой волны
  [Range(1, 8)]

  public int waveformWidthMultiplier;

  public int waveformHeightMultiplier; // Множители, увеличивающие размер звуковой волны

  public void ChangeSpectrogramWidth(float val)
  {
    spectrogram_WidthMultiplier = (int)val;
  }
  public void ChangeSpectrogramHeight(float val)
  {
    spectrogram_HeightMultiplier = (int)val;
  }
  public void ChangeSpectrogramFixDualChannels(bool toggle)
  {
    spectrogram_fixDualChannel = toggle;
  }
  public void ChangeSpectrogramStretchTexture(bool toggle)
  {
    spectrogram_stretchTexture = toggle;
  }
  public void ChangeSpectrogramMultiply(float val)
  {
    spectrogram_Multiply = val;
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

  public void ExportTextureToFile(RawImage tex)
  {
    selectFiles.SaveFile((tex.texture as Texture2D).EncodeToPNG(), $"{audSource.clip.name}_spectrogram.png");
  }

  public void ExportJSONToFile(string text)
  {
    selectFiles.SaveFile(text, "export.json");
  }

  // Функция, занимающаяся разделением массива семплов на окна и вычисляющая их среднее значение
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
  // Анализ сэмплов, которые были заранее обработаны функцией AudioSplitter
  public List<float> SegmentsAnalyzer_splitted(float[] splittedSamples, float clipLength)
  {
    // Debug.LogObjects("info:", segmentsSampleLength, segmenterDifferenceThreshold, segmenterSmoothingWindowSize);
    List<float> min_times = new List<float>();
    float previousAverage = 0f;
    // Debug.Log(splittedSamples.Length);
    for (int i = 0; i < splittedSamples.Length; i++)
    {
      float average = splittedSamples[i];

      // Add selected count of windows

      int min_value = Math.Min(splittedSamples.Length, i + segmenter_SmoothingWindowSize);
      for (int j = i + 1; j < min_value; j++)
      {
        average += splittedSamples[j];
      }

      average /= segmenter_SmoothingWindowSize;

      if (Math.Abs(previousAverage - average) > segmenter_DifferenceThreshold)
      {
        if (segmenter_progressiveSegmentsFinding)
        {
          previousAverage = average;
        }
        float approx_time = ConvertSampleIndexToTime(i, splittedSamples.Length, clipLength);
        min_times.Add(approx_time.Round(1));
      }

      if (!segmenter_progressiveSegmentsFinding)
        previousAverage = splittedSamples[i];
      analysisProgress = (float)i / splittedSamples.Length;
    }
    return min_times;
  }

  public Color GetGradientColor(Gradient gradient, float value)
  {
    return gradient.Evaluate(value);
  }
  public Color GetGradientColor(Gradient gradient, double value)
  {
    return gradient.Evaluate((float)value);
  }
  // 
  /// <summary>
  /// Функция для создания спектрограммы
  /// </summary>
  /// <param name="width">Длина текстуры</param>
  /// <param name="height">Ширина текстуры</param>
  /// <param name="samples">Массив семплов</param>
  /// <param name="convertToDb">Изменяет тип громкости на децибелы</param>
  /// <param name="fixDualChannel">Исправление текстуры при использовании двух каналов</param>
  /// <param name="stretchTexture">Растягивание текстуры на всю высоту</param>
  /// <returns></returns>
  public Color32[] GenerateSpectrogram(int width, int height, float[] samples, bool stereoAudio = true, bool convertToDb = false, bool fixDualChannel = true, bool stretchTexture = true)
  {
    int numFrames = samples.Length / width;
    int sizeMultiplier = 1;
    if (stereoAudio)
      sizeMultiplier = 2;
    if (fixDualChannel)
      sizeMultiplier *= 2;
    Color32[] pixels = new Color32[width * height];
    float[] frame = new float[height];

    double[] magnitudes = new double[height / sizeMultiplier];
    if (!stretchTexture)
    {
      sizeMultiplier = 1;
    }
    for (int x = 0; x < width; x++)
    {
      int count = 0;
      int offset = x * numFrames;

      for (int j = 0; j < height; j++)
      {
        int ind = Math.Min(offset + j, samples.Length - 1);
        frame[j] = samples[ind];
      }
      Complex[] complexes = FastFourierTransform.FFT(FastFourierTransform.ConvertFloatToComplex(frame));

      for (int l = 0; l < magnitudes.Length; l++)
      {
        magnitudes[l] = complexes[l].Magnitude;
        if (convertToDb)
          magnitudes[l] = 20 * Math.Log10(magnitudes[l]) + spectrogramDBOffset; // convert to db incase needed
        float intensity = (float)magnitudes[l] * spectrogram_Multiply;
        for (int m = 1; m <= sizeMultiplier; m++)
        {
          //Debug.LogObjects("size", l, "max_size", magnitudes.Length, "width", x, "height", count * width, "index", x + count * width, "max_size", pixels.Length);
          pixels[x + count * width] = spectrogramGradient.Evaluate(intensity);
          count++;
        }

      }
    }
    return pixels;
  }

  // Анализ сэмплов без предварительной обработки, гораздо медленнее
  public List<float> SegmenterAnalyzer(float[] totalSamples, float clipLength)
  {

    Debug.LogObjects("info:", segmenter_segmentsSampleLength, segmenter_DifferenceThreshold, segmenter_SmoothingWindowSize);
    List<float> min_times = new List<float>();
    float previousAverage = 0f;
    for (int i = 0; i < totalSamples.Length; i += segmenter_segmentsSampleLength)
    {
      IEnumerable<float> arr = totalSamples.Skip(i).Take(segmenter_segmentsSampleLength * (segmenter_SmoothingWindowSize + 1));

      float average = arr.Take(segmenter_segmentsSampleLength * segmenter_SmoothingWindowSize).Select(x => Math.Abs(x)).Average();

      float approx_time = ConvertSampleIndexToTime(i, totalSamples.Length, clipLength);

      if (Math.Abs(previousAverage - average) > segmenter_DifferenceThreshold)
      {
        if (segmenter_progressiveSegmentsFinding)
        {
          previousAverage = average;
        }
        min_times.Add(approx_time);
      }

      if (!segmenter_progressiveSegmentsFinding)
        previousAverage = arr.Take(segmenter_segmentsSampleLength).Select(x => Math.Abs(x)).Average();
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

  Stopwatch stopwatch_timer;

  void StartStopwatch(Stopwatch timer)
  {
    timer = new Stopwatch();
    timer.Start();
  }

  void StartStopwatch_current(Stopwatch timer)
  {
    timer.Reset();
    timer.Start();
  }

  string StopStopwatch(Stopwatch timer)
  {
    timer.Stop();
    TimeSpan timeTaken = timer.Elapsed;
    return timeTaken.ToString(@"m\:ss\.f");
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
    segmenter_segmentsSampleLength = Convert.ToInt32(val);
  }
  public void ChangePreserveLastDiffValueAsCompare(bool val)
  {
    segmenter_progressiveSegmentsFinding = val;
  }
  public void ChangeSegmenterDiffValue(string val)
  {
    segmenter_DifferenceThreshold = Convert.ToSingle(val);
  }

  public void ChangeSegmenterWindowSize(string val)
  {
    segmenter_SmoothingWindowSize = Convert.ToInt32(val);
  }

  public void ChangeSegmenterSampleSize(string val)
  {
    segmenter_segmentsSampleLength = Convert.ToInt32(val);
  }
  public void AnalyzeSongSegmentsPos(float[] totalSamples, float clipLength)
  {
    DoSongSegmentation(totalSamples, clipLength);
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
    if (isRunningAnalysis) { return; }
    StartCoroutine(AnalyzeFile(null));
  }

  public void StartAnalyzingSong(float[] totalSamples = null)
  {
    if (isRunningAnalysis) { return; }
    StartCoroutine(AnalyzeFile(totalSamples));
  }

  void PushNewLine(ref string str, string line)
  {
    str += line + System.Environment.NewLine;
  }
  void PushNewLine(ref string str, params object[] lines)
  {
    foreach (var line in lines)
    {
      str += line;
    }
    str += System.Environment.NewLine;
  }
  bool isRunningAnalysis = false;

  Thread stoppableThread;
  // Анализ файла
  IEnumerator AnalyzeFile(float[] totalSamples = null)
  {
    if (audSource.clip == null)
    {
      Prompts.ShowQuickStrictPrompt("Load audiofile first!");
      yield break;
    }
    float clipLength = audSource.clip.length;
    // 1. Получается массив семплов из файла через функцию GetTotalSaples
    totalSamples ??= GetTotalSamples();

    splitProgress = 0f;
    analysisProgress = 0f;
    hideOnLoadingImage.enabled = false;
    int stored_sample_length = segmenter_segmentsSampleLength;
    Debug.LogObjects(spectogramDefaultWidth, spectrogram_WidthMultiplier);
    spectrogramTexture.rectTransform.sizeDelta = new UnityEngine.Vector2(spectogramDefaultWidth * spectrogram_WidthMultiplier, spectogramDefaultHeight * spectrogram_HeightMultiplier);
    Debug.LogObjects((int)spectrogramTexture.rectTransform.rect.width, (int)spectrogramTexture.rectTransform.rect.height);
    Texture2D specTex = new Texture2D((int)spectrogramTexture.rectTransform.rect.width, (int)spectrogramTexture.rectTransform.rect.height, TextureFormat.RGBA32, false);
    Texture2D spectrumTex = new Texture2D((int)spectrumTexture.rectTransform.rect.width, (int)spectrumTexture.rectTransform.rect.height, TextureFormat.RGBA32, false);
    Color32[] colorsArr = new Color32[0];
    Color32[] colorsSpectrumArr = new Color32[0];
    int tex_width = specTex.width;
    int tex_heigth = specTex.height;
    int specTex_width = spectrumTex.width;
    int specText_height = spectrumTex.height;

    string catched_error = "";
    string result_path = Path.Combine(Application.persistentDataPath, "analysis_result.txt");

    stoppableThread = new Thread(() =>
    {
      string output = "";
      try
      {
        PushNewLine(ref output, "started analysis");
        Stopwatch totalTimer = new Stopwatch();
        StartStopwatch_current(totalTimer);
        PushNewLine(ref output, "splitting samples");
        Stopwatch timer = new Stopwatch();
        isRunningAnalysis = true;
        // 2. Массив сэмплов разделяется на окна
        StartStopwatch_current(timer);
        float[] splitted_arr = AudioSplitter(totalSamples, stored_sample_length);
        PushNewLine(ref output, "done splitting, result array size:", splitted_arr.Length, " | execution time:", StopStopwatch(timer));
        PushNewLine(ref output, "generating spectrogram using FFT");
        // 3. Строится спектрограмма и результат записывается в текстуру
        StartStopwatch_current(timer);
        colorsArr = GenerateSpectrogram(tex_width, tex_heigth, totalSamples, true, false, spectrogram_fixDualChannel, spectrogram_stretchTexture);
        PushNewLine(ref output, "done generating spectrogram using FFT | execution time:", StopStopwatch(timer));
        // 4. Строится волновое представление сэмплов и результат записывается в текстуру
        StartStopwatch(timer);
        colorsSpectrumArr = DrawWave(specTex_width, specText_height, totalSamples);
        Debug.LogObjects("done drawing wave, execution time:", StopStopwatch(timer));
        PushNewLine(ref output, "analyzing segments");
        // 5. Производится анализ сегментов и результат записывается в массив
        StartStopwatch_current(timer);
        segments = SegmentsAnalyzer_splitted(splitted_arr, clipLength);
        PushNewLine(ref output, "done analyzing segments, result array size:", segments.Count, " | execution time:", StopStopwatch(timer));
        isRunningAnalysis = false;
        PushNewLine(ref output, "finished | execution time:", StopStopwatch(totalTimer));
      }
      catch (Exception ex)
      {
        Debug.LogObjects(ex);
        catched_error = ex.Message;
        PushNewLine(ref output, "Caught an error: ", catched_error);
      }
      File.WriteAllText(result_path, output);
    });

    stoppableThread.IsBackground = true;
    stoppableThread.Name = "SONG_ANALYSIS";
    stoppableThread.Start();
    while (stoppableThread.IsAlive)
    {
      loadingImage.fillAmount = splitProgress / 2 + analysisProgress / 2;
      yield return null;
    }
    foreach (var line in File.ReadAllLines(Path.Combine(Application.persistentDataPath, "analysis_result.txt")))
    {
      Debug.Log(line);
    }
    Debug.LogObjects("Log available at", Path.Combine(Application.persistentDataPath, "analysis_result.txt"));
    if (catched_error != "")
    {
      Prompts.ShowQuickExitOnlyPrompt($"Произошла ошибка при попытке анализа файла, логи доступны в: {Path.Combine(Application.persistentDataPath, "analysis_result.txt")}");
      isRunningAnalysis = false;
      hideOnLoadingImage.enabled = true;
      splitProgress = 0f;
      analysisProgress = 0f;
      loadingImage.fillAmount = 0;
      yield break;
    }
    // 6. Применяются изменения

    // spectrumTex.SetPixels32(colorsSpectrumArr);
    // spectrumTex.Apply();
    specTex.SetPixels32(colorsArr);
    specTex.Apply();
    spectrogramTexture.texture = specTex;
    // spectrumTexture.texture = spectrumTex;
    hideOnLoadingImage.enabled = true;
    splitProgress = 0f;
    analysisProgress = 0f;
    loadingImage.fillAmount = splitProgress / 2 + analysisProgress / 2;
    StartCoroutine(CreateSegmentPrefabs());
  }

  // Функция для отрисовки волнового представления аудиофайла
  public Color32[] DrawWave(int textureWidth, int textureHeight, float[] samples)
  {
    Color32[] pixels = new Color32[textureWidth * textureHeight];
    for (int x = 0; x < textureWidth; x++)
    {
      for (int y = 0; y < textureHeight; y++)
      {
        float intensity = samples[x];

        pixels[x + y * textureWidth] = intensity > 0.5 ? Color.white : Color.clear;
      }
    }

    return pixels;
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

  // Функция, отрисовывающая предварительный результат волнового представления аудиофайла для использования в виде слайдера
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
