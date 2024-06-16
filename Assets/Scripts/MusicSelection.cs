using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using System.Net.Http;
using vanIvan.Prompts;

[Serializable]
public class SpectrumObjects
{
  public Vector2 defaultSize;
  public RawImage spectrumObject;

  public SpectrumObjects(Vector2 size, RawImage img)
  {
    defaultSize = size;
    spectrumObject = img;
  }
}

public class MusicSelection : MonoBehaviour
{
  public static readonly string[] supportedExtensions = new string[] { ".mp3", ".wav", ".ogg", ".aac", ".wma" };
  public enum SortingSearch
  {
    Relevance = 0,
    DateDown = 1,
    DateUp = 2,
    ScoreUp = 3,
    ScoreDown = 4,
    ViewsUp = 5,
    ViewsDown = 6
  }
  public SortingSearch sortSearch;
  public bool streamingAudio;
  public bool streamingSpectrum;
  public float spectrum_multiplier = 1;
  public List<AudioClip> allPreloadedMusic = new List<AudioClip>();
  public bool blockAllCommunications = true;
  //persistentdatapath + "/_music/"
  // private AudioManager audManager;
  [SerializeField]
  private AudioSource audSource, snippetAudSource;
  //public TextAsset readmeMusic;
  public float[] samples = new float[64];
  private float[] spectrum = new float[0];

  private string songFinalName, songFinalAuthor;

  public InputField inputSearchString;
  public TMP_InputField customInfo;
  public GameObject musicPrefab;
  public Transform musicPrefabParent, customMusicPrefabParent;

  public Transform downloadParent;
  public Slider downloadProgress;
  public bool decreaseAmountOfRequests;
  public bool dontLoadImages;
  public bool loadAllAudio;
  public bool checkForAvailability = false;
  public bool useAlternateDownloadMethod;
  public bool autoApplySongOnDownload;
  public int maxAmountOfPages;
  public string importantMessage;

  public string mainMusicPath;
  //public List<string> additionalMusicPaths = new List<string>();
  string serviceActivatorPath;

  public List<SpectrumObjects> spectrumObjects;
  public Button cancelDownload;
  public Transform downloadSliderParent;
  public List<string> allPreloadedSongs = new List<string>();
  List<Tuple<string, string>> allSymbols = new List<Tuple<string, string>>
    {
        new Tuple<string, string>("&amp;", "&"),
        new Tuple<string, string>("\t", ""),
        new Tuple<string, string>("\n", ""),
    };
  public Sprite altMusicPrefabBtnSprite;

  public List<Slider> gameProgressSliders;
  public Sprite snippetSprite;
  public GameObject snippetSpectrumPrefab;
  bool didPauseMainAudSource = false;
  public AudioAnalyzer audAnalyzer;
  private void Start()
  {
    fadeIn = FadeSong(null, 0, 0);
    fadeOut = FadeSong(null, 0, 0);
    controlSnippet = ControlSnippet(0, 0, null);

    snippetPrompt = new Prompt(PromptType.ExitOnly);
    snippetPrompt.close_action = delegate { StopCoroutine(fadeIn); StopCoroutine(fadeOut); StopCoroutine(controlSnippet); snippetAudSource.Stop(); if (didPauseMainAudSource) { StartCoroutine(FadeSong(audSource, 0, 1)); audSource.UnPause(); didPauseMainAudSource = false; } };
    Prompts.PreparePrompt(snippetPrompt, true);
    GameObject spectrumObj = new GameObject("img");
    spectrumObj.transform.SetParent(snippetPrompt.promptPanel.textBehaviour.transform);
    RectTransform rectTr = spectrumObj.AddComponent<RectTransform>();
    rectTr.sizeDelta = new Vector2(300, 300);
    rectTr.localScale = new Vector3(100, 100, 100);
    SpectrumVizualizerManager specManager = spectrumObj.AddComponent<SpectrumVizualizerManager>();
    specManager.prefab = snippetSpectrumPrefab;
    specManager.audSource = snippetAudSource;
    specManager.prefParent = specManager.transform;
    specManager.spectrum_beatCount = 64;
    specManager.spectrum_sizeMultiplier = 7;
    specManager.defaultWidthMultiplier = 0.025f;
    specManager.endPosition = 2f;
    specManager.startPosition = -2f;
    specManager.countMultiplier = 2;
    LayoutElement layoutElement = spectrumObj.AddComponent<LayoutElement>();
    layoutElement.minWidth = 400;
    layoutElement.minHeight = 200;

    mainMusicPath = Application.persistentDataPath + "/_music/";
    if (!Directory.Exists(mainMusicPath))
    {
      Directory.CreateDirectory(mainMusicPath);
    }
    if (PlayerPrefs.HasKey("mainMusicPath"))
    {
      mainMusicPath = PlayerPrefs.GetString("mainMusicPath");
    }
    //FillPreloadedSongList();
    //StartLoadingSongsInfo();
    //LoadSongsFromFolder();

    if (PlayerPrefs.GetString("recentMusic") != "")
    {
      StartCoroutine(LoadAndApplySongFromPath(PlayerPrefs.GetString("recentMusic")));
    }
  }
  public void ChangeAudioPitch(float value)
  {
    audSource.pitch = value;
  }
  public void ChangeStreamingSetting(bool toggle)
  {
    streamingAudio = toggle;
  }

  public void LoadInformation()
  {
    foreach (Slider t in gameProgressSliders)
    {
      t.value = 0;
      t.maxValue = audSource.clip.length;
    }
  }

  public void ChangeStreamingSpectrumSetting(bool toggle)
  {
    if (toggle)
    {
      if (!streamingAudio)
      {
        Prompts.QuickPrompt("This option is not going to work until Streaming Audio toggle is not enabled.");
        return;
      }
      Prompt prompt = new Prompt()
      {
        promptText = "This setting may affect the performance depending on your hard drive's speed and other factors.\nDo you wish to enable this option?",
        ok_action = delegate { streamingSpectrum = true; }
      };
      prompt.Show();
    }
    else
    {
      streamingSpectrum = toggle;
    }
  }

  public void GetAudioData()
  {
    if (audSource.clip != null)
    {
      LoadInformation();
    }
    else
    {
      Popups.Popup("<color=red>No music was assigned to the level. Choose music from music list or download new from any internet music resource center!</color>");
    }
  }

  void ClearSpectrum(SpectrumObjects objectToApply)
  {
    RectTransform objTransform = objectToApply.spectrumObject.gameObject.GetComponent<RectTransform>();
    Vector2 size = objectToApply.defaultSize;
    if (size == Vector2.zero)
    {
      size = new Vector2(objTransform.rect.width, objTransform.rect.height);
      if (size.x == 0 || size.x == float.NaN)
      {
        size = new Vector2(objectToApply.defaultSize.x, size.y);
      }

      if (size.y == 0 || size.y == float.NaN)
      {
        size = new Vector2(size.x, objectToApply.defaultSize.y);
      }
    }
    int width = (int)size.x;
    int height = (int)size.y;
    Texture2D finalSpectrum = new Texture2D((int)size.x, (int)size.y, TextureFormat.RGBA32, false);

    float[] widthRange = new float[width];

    for (int x = 0; x < width; x++)
    {
      for (int y = 0; y < height; y++)
      {
        finalSpectrum.SetPixel(x, (height / 2) + y, new Color(0, 0, 0, 0));
        finalSpectrum.SetPixel(x, (height / 2) - y, new Color(0, 0, 0, 0));
      }
      finalSpectrum.SetPixel(x, (height / 2), Color.white);
    }
    finalSpectrum.Apply();
    objectToApply.spectrumObject.texture = finalSpectrum;
    if (objectToApply.spectrumObject.color.a == 0)
      objectToApply.spectrumObject.color = new Color(objectToApply.spectrumObject.color.r, objectToApply.spectrumObject.color.g, objectToApply.spectrumObject.color.b, 1);
  }

  public void CreateSpectrumOnFly()
  {
    float[] samplesArr = new float[1024];
    audSource.GetSpectrumData(samplesArr, 0, FFTWindow.Blackman);
    float finalVal = Mathf.Clamp01(samplesArr.Max() * spectrum_multiplier);

    foreach (SpectrumObjects objectToApply in spectrumObjects)
    {
      RectTransform objTransform = objectToApply.spectrumObject.gameObject.GetComponent<RectTransform>();
      if (objectToApply.spectrumObject.texture == null)
      {
        Texture2D emptyTexture = new Texture2D((int)objTransform.rect.width, (int)objTransform.rect.height, TextureFormat.RGBA32, false);
        for (int x = 0; x < emptyTexture.width; x++)
        {
          for (int y = 0; y < emptyTexture.height; y++)
          {
            emptyTexture.SetPixel(x, (emptyTexture.height / 2) + y, new Color(0, 0, 0, 0));
            emptyTexture.SetPixel(x, (emptyTexture.height / 2) - y, new Color(0, 0, 0, 0));
          }
          emptyTexture.SetPixel(x, (emptyTexture.height / 2), Color.white);
        }

        emptyTexture.Apply();
        objectToApply.spectrumObject.texture = emptyTexture;
        Debug.Log($"Created empty {emptyTexture.width}x{emptyTexture.height} texture");
      }
      Texture2D finalSpectrum = (Texture2D)objectToApply.spectrumObject.texture;
      int width = finalSpectrum.width;
      int height = finalSpectrum.height;

      int pos = (int)((audSource.time / audSource.clip.length) * width);

      for (int y = 0; y < Mathf.Abs(finalVal * (height / 2)); y++)
      {
        finalSpectrum.SetPixel(pos, (height / 2) + y, Color.white);
        finalSpectrum.SetPixel(pos, (height / 2) - y, Color.white);
      }

      finalSpectrum.Apply();
      objectToApply.spectrumObject.texture = finalSpectrum;
      if (objectToApply.spectrumObject.color.a == 0)
        objectToApply.spectrumObject.color = new Color(objectToApply.spectrumObject.color.r, objectToApply.spectrumObject.color.g, objectToApply.spectrumObject.color.b, 1);
    }
  }


  public void CreateSpectrumNew(SpectrumObjects objectToApply, AudioClip clip = null)
  {
    if (streamingAudio)
    {
      return;
    }
    if (clip == null)
      clip = audSource.clip;
    RectTransform objTransform = objectToApply.spectrumObject.gameObject.GetComponent<RectTransform>();
    Vector2 size = objectToApply.defaultSize;
    if (size == Vector2.zero)
    {
      size = new Vector2(objTransform.rect.width, objTransform.rect.height);
      if (size.x == 0 || size.x == float.NaN)
      {
        size = new Vector2(objectToApply.defaultSize.x, size.y);
      }

      if (size.y == 0 || size.y == float.NaN)
      {
        size = new Vector2(size.x, objectToApply.defaultSize.y);
      }
    }

    //Debug.Log(objTransform.name + " " + objTransform.sizeDelta + " " + objTransform.rect.size + " " + size);
    int width = (int)size.x/*15000*/;
    int height = (int)size.y/*(int)timeline.currentSize.y*/;

    //float[] samplesForSpectrum = new float[audManager.audSource.clip.samples * audManager.audSource.clip.channels];
    if (spectrum.Length < 1 || clip.samples * clip.channels != spectrum.Length)
    {
      spectrum = new float[clip.samples * clip.channels];
    }
    clip.GetData(spectrum, 0);
    Texture2D finalSpectrum = new Texture2D((int)size.x, (int)size.y, TextureFormat.RGBA32, false);

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
      // for (int j = value; j < value + deltaValue; j++)
      // {
      //   if (maxValue < spectrum[j])
      //   {
      //     maxValue = spectrum[j];
      //   }
      // }
      widthRange[i] = spectrum.Skip(value).Take(deltaValue).Select(x => Math.Abs(x)).Average();
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
    objectToApply.spectrumObject.texture = finalSpectrum;
    if (objectToApply.spectrumObject.color.a == 0)
      objectToApply.spectrumObject.color = new Color(objectToApply.spectrumObject.color.r, objectToApply.spectrumObject.color.g, objectToApply.spectrumObject.color.b, 1);
    //foreach (Transform t in layersParent)
    //{
    //    t.GetChild(4).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<RawImage>().texture = finalSpectrum;
    //    t.GetChild(4).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<RawImage>().color = Color.white;

    //    t.GetChild(4).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, t.GetChild(4).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta.y);
    //}     
  }


  static Color transparent = Color.clear;
  static Color white = Color.white;
  public static Texture2D GenerateSpectrum(AudioClip clip, Vector2 size, float[] spectrum, float offsetStart = 0f, float offsetEnd = 0f)
  {
    int width = (int)size.x;
    int height = (int)size.y;



    int start = Mathf.RoundToInt(offsetStart * clip.frequency * clip.channels);
    int end = Mathf.RoundToInt(clip.length * clip.frequency * clip.channels) - Mathf.RoundToInt(offsetEnd * clip.frequency * clip.channels);

    int length = end - start;
    // float[] spectrum = new float[length];
    // clip.GetData(spectrum, start);


    // float[] offsetSamples = new float[length];

    // Array.Copy(spectrum, start, offsetSamples, 0, length);
    Texture2D finalSpectrum = new Texture2D(width, height, TextureFormat.RGBA32, false);

    float[] widthRange = new float[width];

    Color[] pixels = new Color[width * height];

    // Set each color value to clear
    for (int i = 0; i < pixels.Length; i++)
    {
      pixels[i] = transparent;
    }

    int deltaValue = length / width;
    float maxValue = 0;
    int value = 0;
    for (int i = 0; i < width; i++)
    {
      for (int j = value; j < value + deltaValue; j++)
      {
        if (j + start < 0 || j + start >= spectrum.Length)
          continue;
        if (maxValue < spectrum[j + start])
        {
          maxValue = spectrum[j + start];
        }
      }
      widthRange[i] = maxValue;
      value += deltaValue;
      maxValue = 0;
    }
    // for (int x = 0; x < widthRange.Length; x++)
    // {
    //     finalSpectrum.SetPixel(x, height / 2, Color.white);
    //     for (int y = 0; y < Mathf.Abs(widthRange[x] * (height / 2)); y++)
    //     {
    //         finalSpectrum.SetPixel(x, (height / 2) + y, Color.white);
    //         finalSpectrum.SetPixel(x, (height / 2) - y, Color.white);
    //     }
    // }

    // Loop through the X axis in the middle of the texture
    for (int x = 0; x < widthRange.Length; x++)
    {
      // Calculate the index of the middle pixel
      int index = x + (height / 2) * width;

      // Set the middle pixel to white
      pixels[index] = white;
      float maxWidthRange = widthRange[x] * (height / 2);

      if (1 >= maxWidthRange)
        continue;
      // Loop through the Y axis above and below the middle pixel
      for (int y = 1; y <= maxWidthRange; y++)
      {
        // Calculate the indices of the pixels above and below
        int indexAbove = index + y * width;
        int indexBelow = index - y * width;

        // Set the pixels above and below to white
        pixels[indexAbove] = white;
        pixels[indexBelow] = white;
      }
    }

    finalSpectrum.SetPixels(pixels);
    finalSpectrum.Apply();

    return finalSpectrum;
  }

  public void OpenSongsGithubRepo()
  {
    Application.OpenURL("https://github.com/IvanSereginGitHub/ProjectBeatSongsAttribution");
  }

  public static IEnumerator LoadAndApplySongFromPath_MainMenu(string path, AudioSource audSource, TextMeshProUGUI musicNameText, bool streamAudio = true)
  {
    audSource.Stop();

    string musicName = path.Replace(Application.persistentDataPath + "/_music/", "").Replace(".mp3", "");
    musicNameText.text = "<color=#cc0000>" + musicName;
    if (musicName.Contains("_"))
      musicNameText.text = "<link=\"\"><color=#cc0000>" + musicName.Substring(0, musicName.IndexOf("_")) + "</color> <color=#7d7575>by</color> <color=#c9bebe>" + musicName.Substring(musicName.IndexOf("_") + 1) + "</color></link>";
    if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.LinuxPlayer)
    {
      path = "file://" + path;
    }

    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN);
    ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = streamAudio;
    UnityWebRequestAsyncOperation progress = www.SendWebRequest();

    while (!progress.isDone)
    {
      yield return null;
    }

    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);

    audSource.clip = myClip;
    audSource.clip.name = musicName;
    audSource.time = 0;
    audSource.Play();
  }

  public class LoadSongParameters
  {
    public bool streamAudio;
    public float clipFrom;
    public float clipTo;

    public float bitrateMultiplier;

    public LoadSongParameters(bool stream = true, float from = 0f, float to = 0f, float bitrate = 1000)
    {
      streamAudio = stream;
      clipFrom = from;
      clipTo = to;
      bitrateMultiplier = bitrate;
    }
  }
  // public static IEnumerator LoadFileAsSongFromPath(string path, LoadSongParameters loadParameters, System.Action<AudioClip> callback = null)
  // {
  //   if (Application.platform == RuntimePlatform.Android)
  //   {
  //     path = "file://" + path;
  //   }
  //   UnityWebRequest www = UnityWebRequest.Get(path);
  //   //((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = loadParameters.streamAudio;
  //   if (loadParameters.clipTo != 0)
  //   {
  //     www.SetRequestHeader("Range", $"bytes={(int)(loadParameters.clipFrom * loadParameters.bitrateMultiplier)}-{(int)(loadParameters.clipTo * loadParameters.bitrateMultiplier)}");
  //   }
  //   Debug.Log(www.GetRequestHeader("Range"));
  //   yield return www.SendWebRequest();

  //   AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
  //   callback.Invoke(myClip);
  // }

  public static IEnumerator LoadSongFromPath(string path, bool streamAudio = false, System.Action<AudioClip> callback = null)
  {
    if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.LinuxPlayer)
    {
      path = "file://" + path;
    }
    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN);
    ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = streamAudio;
    yield return www.SendWebRequest();

    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
    callback.Invoke(myClip);
  }
  public IEnumerator FadeSong(AudioSource audSource, float startVolume, float endVolume)
  {
    if (audSource == null)
      yield break;
    float time = 0;
    while (time < 1)
    {
      audSource.volume = Mathf.Lerp(startVolume, endVolume, time);
      time += Time.deltaTime / 2;
      yield return null;
    }
    audSource.volume = endVolume;
  }

  public void ToggleAudSourceStatus(AudioSource source)
  {
    if (source.isPlaying)
    {
      source.Pause();
    }
    else
    {
      source.Play();
    }
  }

  public IEnumerator ControlSnippet(float play_from, float play_to, Prompt invokedPrompt)
  {
    if (audSource.isPlaying)
    {
      didPauseMainAudSource = true;
      audSource.Pause();
    }

    snippetAudSource.time = play_from;
    snippetAudSource.volume = 0f;
    snippetAudSource.Play();
    yield return fadeIn;
    while (snippetAudSource.time <= play_to)
    {
      yield return null;
    }
    yield return fadeOut;
    snippetAudSource.Stop();
    invokedPrompt.Close();
    if (didPauseMainAudSource) { StartCoroutine(FadeSong(audSource, 0, 1)); audSource.UnPause(); didPauseMainAudSource = false; }
  }
  Prompt snippetPrompt;
  IEnumerator fadeIn, fadeOut, controlSnippet;
  public void PlaySongSnippet(string path, float snippetStart = 50f, float snippetLength = 30f)
  {
    StopCoroutine(fadeIn);
    StopCoroutine(fadeOut);
    StopCoroutine(controlSnippet);

    snippetAudSource.Stop();
    StartCoroutine(LoadSongFromPath(path, true, (clip) =>
    {
      if (clip.length < 80f && clip.length > 50f)
      {
        snippetStart = 20f;
      }
      else if (clip.length < 80f && clip.length < 50f && clip.length > 5f)
      {
        snippetStart = 0f;
        snippetLength = clip.length - 2f;
      }
      else if (clip.length < 5f)
      {
        Prompts.ShowQuickStrictPrompt("Sorry, song is too short for playing the snippet.");
        return;
      }
      fadeIn = FadeSong(snippetAudSource, 0, 1);
      fadeOut = FadeSong(snippetAudSource, 1, 0);
      controlSnippet = ControlSnippet(snippetStart, snippetStart + snippetLength, snippetPrompt);
      string coloredSongInfo = GetColoredSongNameAuthorFromFilename(Path.GetFileNameWithoutExtension(path));
      snippetPrompt.promptText = $"Воспроизведение отрывка: {coloredSongInfo}";

      snippetAudSource.clip = clip;
      Prompts.UpdatePrompt(snippetPrompt);
      snippetPrompt.Show();
      StartCoroutine(controlSnippet);
      // StartCoroutine(FadeInSong());
      // StartCoroutine(ActionAfterCoroutineWrapper(FadeOutSong(), () => { promptAudSource.Stop(); }));
    }));
  }

  public void RefreshSceneInfo()
  {
    audSource.Stop();
    GetAudioData();
    foreach (SpectrumObjects t in spectrumObjects)
    {
      ClearSpectrum(t);
      CreateSpectrumNew(t);
    }
    audAnalyzer.StartAnalyzingSong();
  }

  public IEnumerator SongLoadWrapper(string path, bool saveAsRecent = true, System.Action callback = null)
  {
    yield return LoadAndApplySongFromPath(path, saveAsRecent);
    callback.Invoke();
  }
  public IEnumerator ActionAfterCoroutineWrapper(IEnumerator coroutine, System.Action callback = null)
  {
    yield return coroutine;
    callback.Invoke();
  }

  public void ApplyLoadedAudioInfo(AudioClip myClip, string path = null)
  {
    path ??= myClip.name;
    string fileName = Path.GetFileName(path)/*path.IndexOf(musicPath) != -1 ? path.Substring(path.IndexOf(musicPath) + musicPath.Length) : */;
    string songName = Path.GetFileNameWithoutExtension(path);
    string author = "";
    if (fileName.Contains("_"))
    {
      songName = fileName.Substring(0, fileName.IndexOf("_"));
      author = Path.GetFileNameWithoutExtension(fileName.Replace(songName + "_", ""));
    }
    audSource.clip = myClip;
    //audManager.offset = 0;
    audSource.clip.name = songName + "_" + author;
    //audManager.offset = 0;
    RefreshSceneInfo();
  }

  public IEnumerator LoadAndApplySongFromPath(string path, bool saveAsRecent = true)
  {
    AudioClip myClip = null;
    try
    {
      myClip = Resources.Load(path) as AudioClip;
    }
    catch
    {

    }

    if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.LinuxPlayer)
    {
      path = "file://" + path;
    }

    if (myClip == null)
    {
      using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN))
      {
        if (streamingAudio)
          ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;

        UnityWebRequestAsyncOperation progress = www.SendWebRequest();

        while (!progress.isDone)
        {
          yield return null;
        }

        try
        {
          myClip = DownloadHandlerAudioClip.GetContent(www);
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"An issue happened during song loading ({ex.Message}).\nSong path is: {path}");
          PlayerPrefs.DeleteKey("recentMusic");
        }
      }
    }

    if (myClip == null)
    {
      yield break;
    }
    ApplyLoadedAudioInfo(myClip, path);
    if (saveAsRecent)
      PlayerPrefs.SetString("recentMusic", path.Replace("file://", ""));
  }
  public void ClearList()
  {
    foreach (Transform t in musicPrefabParent)
    {
      if (t.gameObject.activeSelf)
      {
        Destroy(t.gameObject);
      }
    }
  }

  public void ClearList(Transform content)
  {
    foreach (Transform t in content)
    {
      if (t.gameObject.activeSelf)
      {
        Destroy(t.gameObject);
      }
    }
  }

  public void ScrollSongs(RectTransform content)
  {
    ScrollPooling(content.GetChild(0).GetChild(1).gameObject.GetComponent<RectTransform>(), content, 0, 768, 0, -1);
  }

  public void ScrollPooling(RectTransform content, RectTransform mainPanel, float lowPointOffset, float highPointOffset, int prefabsOffset, int objectsOffset)
  {
    //string str = "";

    float scrollLowPoint = content.localPosition.y + lowPointOffset;
    float scrollHighPoint = mainPanel.sizeDelta.y + content.localPosition.y + highPointOffset;

    //ignores the prefab
    for (int j = 0; j < content.childCount + prefabsOffset; j++)
    {
      Transform t = content.GetChild(j);
      RectTransform rect = t.gameObject.GetComponent<RectTransform>();
      float objectLowPoint = Math.Abs(rect.localPosition.y) + rect.sizeDelta.y / 2;
      float objectHighPoint = Math.Abs(rect.localPosition.y) - rect.sizeDelta.y / 2;

      Vector3 pos = rect.localPosition;
      Vector2 size = rect.sizeDelta;
      if ((objectLowPoint < scrollLowPoint && objectHighPoint < scrollLowPoint) || (objectLowPoint > scrollHighPoint && objectHighPoint > scrollHighPoint))
      {
        //str += t.gameObject.name + "(" + objectLowPoint + ":" + objectHighPoint + ")\n";
        //Replace with fake object
        for (int i = 0; i < t.childCount + objectsOffset; i++)
        {
          Transform g = t.GetChild(i);
          if (g.gameObject != t)
          {
            g.gameObject.SetActive(false);
          }
          rect.localPosition = pos;
          rect.sizeDelta = size;
        }
        t.gameObject.GetComponent<Image>().enabled = false;
        continue;
      }
      else
      {
        //enable (restore) this object
        for (int i = 0; i < t.childCount + objectsOffset; i++)
        {
          Transform g = t.GetChild(i);
          g.gameObject.SetActive(true);
        }
        t.gameObject.GetComponent<Image>().enabled = true;
      }
    }
  }

  public (string, string) GetSongNameAuthorFromFilename(string fileName)
  {
    string songName = "", author = "";
    if (fileName.Contains("_") && !fileName.Contains(" "))
    {
      songName = fileName.Substring(0, fileName.IndexOf("_"));
      author = fileName.Replace(songName + "_", "");
    }
    else if (fileName.Contains(" - "))
    {
      author = fileName.Substring(0, fileName.IndexOf(" - "));
      songName = fileName.Replace(author + " - ", "");
    }
    return (songName, author);
  }

  public string GetColoredSongNameAuthorFromFilename(string fileName, string htmlColor = "#cc0000")
  {
    (string songName, string author) = GetSongNameAuthorFromFilename(fileName);
    if (songName == "" && author == "")
      return fileName;
    return $"<color={htmlColor}>{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(songName)}</color> <color=#7d7575>от</color> <color=#c9bebe>{author}</color>";
  }
  IEnumerator songLoadingCoroutine;

  public void StopLoadingSongsInfo()
  {
    if (songLoadingCoroutine != null)
      StopCoroutine(songLoadingCoroutine);
    ClearList(customMusicPrefabParent);
  }
  public void StartLoadingSongsInfo(string searchText = "")
  {
    StopLoadingSongsInfo();
    songLoadingCoroutine = LoadSongsInfo(searchText);
    StartCoroutine(songLoadingCoroutine);
  }

  public IEnumerator LoadSongsInfo(string searchText = "")
  {
    if (!Directory.Exists(mainMusicPath))
    {
      Directory.CreateDirectory(mainMusicPath);
    }
    List<string> allMusic = SelectFiles.GetFilesFromFolder_List(mainMusicPath, MusicSelection.supportedExtensions, searchText);

    foreach (string t in allPreloadedSongs)
    {
      if (searchText != "" && !Path.GetFileNameWithoutExtension(t).ToLower().Contains(searchText.ToLower()))
        continue;
      else
      {
        GameObject prefab = Instantiate(musicPrefab, customMusicPrefabParent);
        Button songPlayBtn = prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>();

        songPlayBtn.gameObject.GetComponent<Image>().sprite = altMusicPrefabBtnSprite;

        songPlayBtn.onClick.AddListener(delegate
        {
          string songPath = $"PreloadedSongs/Songs/{Path.GetFileNameWithoutExtension(t)}/" + Path.GetFileNameWithoutExtension(t);
          audSource.clip = Resources.Load(songPath) as AudioClip;
          audSource.Stop();
          GetAudioData();
          LoadInformation();
          foreach (SpectrumObjects t in spectrumObjects)
          {
            ClearSpectrum(t);
            CreateSpectrumNew(t);
          }
          PlayerPrefs.SetString("recentMusic", songPath);
        });
        prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = GetColoredSongNameAuthorFromFilename(Path.GetFileNameWithoutExtension(t));

        prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Button>().enabled = false;//onClick.AddListener(delegate { inputSearchString.text = $"<author>{author}</author>"; SearchOnThatSite(inputSearchString); });

        prefab.transform.GetChild(1).GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = Path.GetFileName(t);
        songPlayBtn.gameObject.SetActive(true);

        prefab.SetActive(true);

        Destroy(prefab.transform.GetChild(3).gameObject);
        yield return null;
      }
    }

    for (int i = 0; i < allMusic.Count; i++)
    {
      string songName = Path.GetFileNameWithoutExtension(allMusic[i]);

      GameObject prefab = Instantiate(musicPrefab, customMusicPrefabParent);
      string temp = allMusic[i];
      Button songPlayBtn = prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>();

      songPlayBtn.gameObject.GetComponent<Image>().sprite = altMusicPrefabBtnSprite;

      songPlayBtn.onClick.AddListener(delegate { StartCoroutine(LoadAndApplySongFromPath(temp)); });
      prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = GetColoredSongNameAuthorFromFilename(songName);

      prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Button>().enabled = false;

      prefab.transform.GetChild(1).GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = Path.GetFileName(allMusic[i]);
      songPlayBtn.gameObject.SetActive(true);
      prefab.GetComponent<DeleteFile>().filePath = temp;
      prefab.transform.GetChild(3).gameObject.GetComponent<Button>().onClick.AddListener(delegate { prefab.GetComponent<Animator>().Play("music_prefab_deletion"); });
      prefab.transform.GetChild(3).gameObject.SetActive(true);
      prefab.transform.GetChild(4).gameObject.GetComponent<Button>().onClick.AddListener(delegate { PlaySongSnippet(temp); });
      prefab.SetActive(true);
      yield return null;
    }
  }


  public void LoadSongsFromFolder(string searchText = "")
  {
    //StopAllCoroutines();
    //ClearAllCoroutines();
    //ClearList();
    ClearList(customMusicPrefabParent);
    if (!Directory.Exists(mainMusicPath))
    {
      Directory.CreateDirectory(mainMusicPath);
    }
    List<string> allMusic = SelectFiles.GetFilesFromFolder_List(mainMusicPath, MusicSelection.supportedExtensions, searchText);

    // foreach (string t in allPreloadedSongs)
    // {
    //   if (searchText != "" && !Path.GetFileNameWithoutExtension(t).ToLower().Contains(searchText.ToLower()))
    //     continue;
    //   else
    //   {
    //     GameObject prefab = Instantiate(musicPrefab, customMusicPrefabParent);
    //     Button songPlayBtn = prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>();

    //     songPlayBtn.gameObject.GetComponent<Image>().sprite = altMusicPrefabBtnSprite;

    //     songPlayBtn.onClick.AddListener(delegate
    //     {
    //       string songPath = $"PreloadedSongs/Songs/{Path.GetFileNameWithoutExtension(t)}/" + Path.GetFileNameWithoutExtension(t);
    //       audSource.clip = Resources.Load(songPath) as AudioClip;
    //       audSource.Stop();
    //       GetAudioData();
    //       LoadInformation();
    //       foreach (SpectrumObjects t in spectrumObjects)
    //       {
    //         ClearSpectrum(t);
    //         CreateSpectrumNew(t);
    //       }
    //       PlayerPrefs.SetString("recentMusic", songPath);
    //     });
    //     prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = GetColoredSongNameAuthorFromFilename(Path.GetFileNameWithoutExtension(t));

    //     prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Button>().enabled = false;//onClick.AddListener(delegate { inputSearchString.text = $"<author>{author}</author>"; SearchOnThatSite(inputSearchString); });

    //     prefab.transform.GetChild(1).GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = Path.GetFileName(t);
    //     songPlayBtn.gameObject.SetActive(true);

    //     prefab.SetActive(true);

    //     Destroy(prefab.transform.GetChild(3).gameObject);
    //   }
    // }

    for (int i = 0; i < allMusic.Count; i++)
    {
      string songName = Path.GetFileNameWithoutExtension(allMusic[i]);
      // if (substr.Contains("_"))
      // {
      //   songName = substr.Substring(0, substr.IndexOf("_"));
      //   author = substr.Replace(songName + "_", "").Replace(".mp3", "");
      // }


      GameObject prefab = Instantiate(musicPrefab, customMusicPrefabParent);
      //Debug.Log(allMusic[i]);
      string temp = allMusic[i];
      Button songPlayBtn = prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>();

      songPlayBtn.gameObject.GetComponent<Image>().sprite = altMusicPrefabBtnSprite;

      songPlayBtn.onClick.AddListener(delegate { StartCoroutine(LoadAndApplySongFromPath(temp)); });
      //PlayerPrefs.GetString("recentMusic")w
      //prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>().onClick.AddListener(delegate { PlayerPrefs.SetString("recentMusic", temp); });
      prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = GetColoredSongNameAuthorFromFilename(songName);
      // if (substr.Contains("_"))
      // {
      //   songName = substr.Substring(0, substr.IndexOf("_"));
      //   author = substr.Replace(songName + "_", "").Replace(".mp3", "");
      //   Debug.LogObjects(author, songName);
      //   prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = $"<color=#cc0000>{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(songName)}</color> <color=#7d7575>by</color> <color=#c9bebe>{author}</color>";
      // }
      // else if (substr.Contains(" - "))
      // {
      //   author = substr.Substring(0, substr.IndexOf(" - "));
      //   songName = substr.Replace(author + " - ", "").Replace(".mp3", "");
      //   Debug.LogObjects(author, songName);
      //   prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = $"<color=#cc0000>{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(songName)}</color> <color=#7d7575>by</color> <color=#c9bebe>{author}</color>";
      // }

      //prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "<color=#cc0000>" + songName + "</color>" + (substr.Contains("_") ? (" <color=#7d7575>by</color> " + "<color=#c9bebe>" + author + "</color>") : "");
      prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Button>().enabled = false;//onClick.AddListener(delegate { inputSearchString.text = "<author>" + author + "</author>"; SearchOnThatSite(inputSearchString); });

      prefab.transform.GetChild(1).GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = Path.GetFileName(allMusic[i]);
      songPlayBtn.gameObject.SetActive(true);
      prefab.GetComponent<DeleteFile>().filePath = temp;
      prefab.transform.GetChild(3).gameObject.GetComponent<Button>().onClick.AddListener(delegate { prefab.GetComponent<Animator>().Play("music_prefab_deletion"); });
      //prefab.transform.GetChild(3).gameObject.GetComponent<Button>().onClick.AddListener(delegate { LoadSongsFromFolder(searchText); });
      prefab.transform.GetChild(3).gameObject.SetActive(true);
      prefab.transform.GetChild(4).gameObject.GetComponent<Button>().onClick.AddListener(delegate { PlaySongSnippet(temp); });
      prefab.SetActive(true);
    }

  }
  public void MakeDownloadAvailable(bool toggle)
  {
    blockAllCommunications = toggle;
  }

  public void ToggleAvailabilityCheck(bool toggle)
  {
    checkForAvailability = toggle;
  }

  void StopDownload(Coroutine cor, GameObject slider)
  {
    //audSource.volume = 1;
    StopCoroutine(cor);
    Debug.Log("Download was stopped...");
    Destroy(slider.transform.parent.gameObject);
  }

  void RefactorString(ref string str)
  {
    foreach (Tuple<string, string> t in allSymbols)
    {
      str = str.Replace(t.Item1, t.Item2);
    }
  }

  [ContextMenu("Fill Preloaded songs list")]
  public void FillPreloadedSongList()
  {
    allPreloadedSongs.Clear();
    if (!PlayerPrefs.HasKey("preloadedSongsList") || String.IsNullOrWhiteSpace(PlayerPrefs.GetString("preloadedSongsList")))
    {
      foreach (UnityEngine.Object file in Resources.LoadAll("PreloadedSongs", typeof(AudioClip)).ToList())
      {
        allPreloadedSongs.Add(file.name);
      }
      PlayerPrefs.SetString("preloadedSongsList", String.Join(",", allPreloadedSongs));
      Debug.Log("Successfully refreshed preloaded songs list!");
    }
    else
    {
      PlayerPrefs.GetString("preloadedSongsList").Split(',').ToList().ForEach((path) => allPreloadedSongs.Add(path));
    }
  }

  public void ForceRefreshSongsList()
  {
    Prompt prompt = new Prompt("Are you really sure you want to forcefully refresh preloaded songs list?<br><i>This action will freeze editor for a while, so be sure to stop music and save the level incase something wrong happens there!</i>");
    prompt.ok_action = delegate
    {
      PlayerPrefs.DeleteKey("preloadedSongsList");
      FillPreloadedSongList();
    };
    prompt.Show();
  }
  //IDE says that it is not used, but I actually use it in delegates
  //private void StopDownload(IEnumerator cor, GameObject slider)
  //{
  //    //audSource.volume = 1;
  //    StopCoroutine(cor);
  //    Debug.Log("Download was stopped...");
  //    Destroy(slider.transform.parent.gameObject);
  //}
}
