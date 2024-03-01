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
  private string path;
  //public List<AudioClip> music;
  public List<string> musicPathes;
  public List<string> musicNames;
  // private AudioManager audManager;
  [SerializeField]
  private AudioSource audSource;
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

  bool useCustomLink;
  public string musicPath;
  List<Coroutine> allCoroutines = new List<Coroutine>();
  string magicLink1, magicLink2, magicLink3, magicLink4;
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
  private void Awake()
  {
    serviceActivatorPath = Application.persistentDataPath + "/_serviceEnabler/service_Activator.servAct";
    if (File.Exists(serviceActivatorPath))
    {
      string[] links = File.ReadAllLines(serviceActivatorPath);
      magicLink1 = links[0];
      magicLink2 = links[1];
      magicLink3 = links[2];
      magicLink4 = links[3];
      blockAllCommunications = false;
    }
    DecreaseRequests(true);
    path = Application.persistentDataPath + "/_music";
    if (!Directory.Exists(path))
    {
      Directory.CreateDirectory(path);
    }
    musicPath = Application.persistentDataPath + "/_music/";
    FillPreloadedSongList();
    LoadSongsFromFolder();

    if (PlayerPrefs.GetString("recentMusic") != "")
    {
      StartCoroutine(LoadAndApplySongFromPath(PlayerPrefs.GetString("recentMusic")));
    }
  }

  public void ChangeStreamingSetting(bool toggle)
  {
    streamingAudio = toggle;
  }

  public void LoadInformation()
  {
    foreach (Slider t in gameProgressSliders)
    {
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

  public void SetSorting(GameObject obj)
  {
    switch (obj.name)
    {
      case "Relevance":
        sortSearch = SortingSearch.Relevance;
        break;
      case "DateUp":
        sortSearch = SortingSearch.DateUp;
        break;
      case "DateDown":
        sortSearch = SortingSearch.DateDown;
        break;
      case "ScoreUp":
        sortSearch = SortingSearch.ScoreUp;
        break;
      case "ScoreDown":
        sortSearch = SortingSearch.ScoreDown;
        break;
      case "ViewsUp":
        sortSearch = SortingSearch.ViewsUp;
        break;
      case "ViewsDown":
        sortSearch = SortingSearch.ViewsDown;
        break;
    }
  }
  public void Refresh()
  {
    musicPathes.Clear();
    musicNames.Clear();
    foreach (string t in Directory.GetFiles(path))
    {
      if (t.EndsWith(".wav"))
      {
        musicPathes.Add(t);
        string tmp = t.Substring(t.LastIndexOf("\\") + 1);
        tmp = tmp.Replace(".wav", "");
        musicNames.Add(tmp);
      }

    }
    musicNames.Add("None");
  }
  public IEnumerator DownloadFromThatSite(string songID, GameObject tmp)
  {
    Slider sl = CreateDownloadSlider();
    if (!useAlternateDownloadMethod)
    {
      Coroutine cor = StartCoroutine(LoadHelper(magicLink1.Replace("listen", "download") + songID, tmp, sl, songFinalName, songFinalAuthor));
      sl.gameObject.transform.GetChild(3).gameObject.GetComponent<Button>().onClick.AddListener(delegate { StopDownload(cor, sl.gameObject); });
      yield return cor;
    }
    else
    {
      string link = songID;

      link = link.Replace("\\/\\/", "//");
      while (link.IndexOf("\\/") != -1)
      {
        link = link.Replace("\\/", "//");
      }

      Coroutine cor = StartCoroutine(LoadHelper(link, tmp, sl, songFinalName, songFinalAuthor));
      sl.gameObject.transform.GetChild(3).gameObject.GetComponent<Button>().onClick.AddListener(delegate { StopDownload(cor, sl.gameObject); });
      yield return cor;
    }

  }

  public IEnumerator GetAuthorNameAndMusicName(string songID, GameObject tmp)
  {
    if (blockAllCommunications)
    {
      Prompts.ShowPrompt(new Prompt("This service is not activated, therefore it cannot be used."));
      //Prompts.ShowStrictPrompt("This service is not activated, therefore it cannot be used.", null);
      //DisplayErrorMessage(importantMessage);
      yield break;
    }

    using (UnityWebRequest www = UnityWebRequest.Get(magicLink1 + songID))
    {
      UnityWebRequestAsyncOperation progress = www.SendWebRequest();
      Popups.Popup("<color=yellow>Getting song info...</color>");
      while (!progress.isDone)
      {
        yield return null;
      }

      if (!useAlternateDownloadMethod)
      {
        if (www.downloadHandler.text.Contains("class=\"icon-download\""))
        {
          string songAuthor = www.downloadHandler.text.Substring(www.downloadHandler.text.IndexOf("<div class=\"item-details-main\">") + 31);
          if (songAuthor.Contains("<h4 class=\"smaller\">"))
          {
            songAuthor = songAuthor.Replace("<h4 class=\"smaller\">", "");
          }
          songAuthor = songAuthor.Substring(songAuthor.IndexOf(magicLink4) + 17);
          songAuthor = songAuthor.Substring(0, songAuthor.IndexOf("</a>"));
          songAuthor = songAuthor.Replace("\t", "");
          songAuthor = songAuthor.Replace("\n", "");
          songAuthor = songAuthor.Replace("&amp;", "&");

          string songName = www.downloadHandler.text.Substring(www.downloadHandler.text.IndexOf("<title>") + 7);
          songName = songName.Substring(0, songName.IndexOf("</title>"));

          songFinalName = songName;
          //Debug.Log(songFinalName);
          songFinalAuthor = songAuthor;
          //Debug.Log(songFinalAuthor);
          string filePath = path + "/" + songName + "_" + songAuthor + ".mp3";

          if (File.Exists(filePath))
          {
            Popups.Popup("<color=red>This music file already exists!</color>");
            HideDownload(tmp);
            yield break;
          }
          else
          {
            StartCoroutine(DownloadFromThatSite(songID, tmp));
          }
        }
        else
        {
          Popups.Popup("<color=red>This song is not available for download!</color>");
          HideDownload(tmp);
          yield break;
        }
      }
      else
      {
        string temp = www.downloadHandler.text.Substring(www.downloadHandler.text.IndexOf(magicLink3) + 29);
        temp = temp.Substring(0, temp.IndexOf(".mp3") + 4);
        temp = magicLink3 + temp;
        //Debug.Log(temp);
        StartCoroutine(DownloadFromThatSite(temp, tmp));
      }

    }
  }

  void ClearAllCoroutines()
  {
    if (allCoroutines.Count > 0)
    {
      foreach (Coroutine i in allCoroutines.ToList())
      {
        if (i == null)
        {
          allCoroutines.Remove(i);
          break;
        }
        StopCoroutine(i);
        allCoroutines.Remove(i);
      }
    }
  }

  IEnumerator LoadSong(int i, List<string> elements)
  {
    if (elements[i].IndexOf("<div class=\"detail-title\">") == -1)
    {
      yield break;
    }
    GameObject prefab = Instantiate(musicPrefab, musicPrefabParent);
    prefab.SetActive(true);
    string songName = elements[i].Substring(elements[i].IndexOf("<div class=\"detail-title\">") + 27);

    songName = songName.Replace("<h4>", "");
    songName = songName.Replace("</h4>", "");
    songName = songName.Replace("<mark class=\"search-highlight\">", "");
    songName = songName.Replace("</mark>", "");
    songName = songName.Replace("&amp;", "&");
    songName = songName.Replace("&#x27;", "\'");
    songName = songName.Replace("&#x2F;", "/");

    songName = songName.Substring(0, songName.IndexOf("<span>") - 1);

    string clearSongName = songName;

    //SongID

    string songID = elements[i].Substring(elements[i].IndexOf("<a href=\"") + 9);
    songID = songID.Substring(0, songID.IndexOf("\""));

    songID = songID.Substring(songID.IndexOf(magicLink1) + 40);

    prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>().onClick.AddListener(delegate { });

    prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>().onClick.AddListener(delegate { StartCoroutine(ShowSongDownloadProcess(songID)); });
    prefab.name = clearSongName;

    songName = "<color=#cc0000>" + songName + "</color>";

    songName += " <color=#7d7575>by</color> ";

    string SongAuthor = elements[i].Substring(elements[i].IndexOf("<strong>") + 8);
    SongAuthor = SongAuthor.Substring(0, SongAuthor.IndexOf("</strong>"));

    songName += "<color=#c9bebe>" + SongAuthor + "</color>";

    prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = songName;
    prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Button>().enabled = false;//onClick.AddListener(delegate { inputSearchString.text = "<author>" + SongAuthor + "</author>"; });
                                                                                               //prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Button>().onClick.AddListener(delegate { SearchOnThatSite(inputSearchString); });

    //SongDescription
    string songDescription = elements[i].Substring(elements[i].IndexOf("<div class=\"detail-description\">") + 33);
    songDescription = songDescription.Substring(0, songDescription.IndexOf("</div>"));
    songDescription = songDescription.Replace("\n", " ");
    songDescription = songDescription.Replace("&#x2F;", "/");
    songDescription = songDescription.Replace("<mark class=\"search-highlight\">", "");
    songDescription = songDescription.Replace("</mark>", "");
    songDescription = songDescription.Replace("&#x27;", "\'");
    prefab.transform.GetChild(1).GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = songDescription;


    //SongType + SongViews + SongScore
    if (elements[i].IndexOf("<div class=\"item-details-meta\">") != -1)
    {
      string songScore = elements[i].Substring(elements[i].IndexOf("<div class=\"star-score\" title=\"") + 31);
      songScore = songScore.Substring(0, songScore.IndexOf("\">"));
      if (songScore == "er")
      {
        prefab.transform.GetChild(2).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "No score...";
      }
      else
      {
        prefab.transform.GetChild(2).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = songScore;
      }


      string songType = elements[i].Substring(elements[i].IndexOf("<div class=\"item-details-meta\">") + 30);

      songType = songType.Substring(songType.IndexOf("</dd>") + 5);

      songType = songType.Substring(songType.IndexOf("<dd>") + 4, songType.IndexOf("</dd>") - 5);
      prefab.transform.GetChild(2).GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = songType;


      string songViews = elements[i].Substring(elements[i].IndexOf("<div class=\"item-details-meta\">") + 30);
      songViews = songViews.Substring(songViews.IndexOf("</dd>") + 5);
      songViews = songViews.Substring(songViews.IndexOf("</dd>") + 5);
      if (songViews.IndexOf("</dd>") - 5 < 0)
      {
        prefab.transform.GetChild(2).GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = "No type...";
        prefab.transform.GetChild(2).GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = songType;
      }
      else
      {
        songViews = songViews.Substring(songViews.IndexOf("<dd>") + 4, songViews.IndexOf("</dd>") - 5);
        prefab.transform.GetChild(2).GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = songViews;
      }

    }

    if (!useAlternateDownloadMethod)
    {
      if (checkForAvailability)
      {
        using (UnityWebRequest webRequest2 = UnityWebRequest.Get(magicLink1 + songID))
        {
          UnityWebRequestAsyncOperation progress2 = webRequest2.SendWebRequest();
          while (!progress2.isDone)
          {
            yield return null;
          }

          if (!webRequest2.downloadHandler.text.Contains("class=\"icon-download\""))
          {
            //If not available
            prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
            prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>().onClick.AddListener(delegate
            {
              Popups.Popup("<color=red>This song is not available for download!</color>");
            });
            prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, -45);

            ColorBlock col = prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>().colors;
            col.disabledColor = Color.red;
            prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>().colors = col;
            prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>().interactable = false;
          }
        }
      }
    }

    prefab.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);

    //ImagePreview
    if (!dontLoadImages)
    {
      string imageLink = elements[i].Substring(elements[i].IndexOf("<img src=") + 10);
      imageLink = imageLink.Substring(0, imageLink.IndexOf(" ") - 1);

      using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(imageLink))
      {
        UnityWebRequestAsyncOperation progress1 = webRequest.SendWebRequest();
        while (!progress1.isDone)
        {
          yield return null;
        }
        Texture2D texture = new Texture2D(0, 0);

        try
        {
          texture = DownloadHandlerTexture.GetContent(webRequest);
        }
        catch
        {
          webRequest.Dispose();
          yield break;
        }

        prefab.transform.GetChild(0).gameObject.GetComponent<RawImage>().texture = texture;
        prefab.transform.GetChild(0).gameObject.GetComponent<RawImage>().color = new Color(1, 1, 1, 1);
      }
    }
  }

  public static string ParseFileNameIntoSongAuthorString(string text, Color? songNameColor = null)
  {
    if (songNameColor == null)
      songNameColor = new Color(204, 0, 0, 1);
    if (text.Contains("_"))
    {
      return $"<color=#{ColorUtility.ToHtmlStringRGBA((Color)songNameColor)}>{text.Substring(0, text.IndexOf("_"))}</color> <color=#7d7575>by</color> <color=#c9bebe>{text.Substring(text.IndexOf("_") + 1)}</color>";
    }
    return "<color=#cc0000>" + text + "</color>";
  }

  public static (string, string) ParseFileNameIntoSongAuthorPair(string text, Color? songNameColor = null)
  {
    if (songNameColor == null)
      songNameColor = new Color(204, 0, 0, 1);
    if (text.Contains("_"))
    {
      return ($"<color=#{ColorUtility.ToHtmlStringRGBA((Color)songNameColor)}>{text.Substring(0, text.IndexOf("_"))}</color>", $"<color=#c9bebe>{text.Substring(text.IndexOf("_") + 1)}</color>");
    }
    return ($"<color=#cc0000>{text}</color>", "");
  }
  IEnumerator ShowSongDownloadProcess(string songID)
  {
    //Debug.Log(songID);
    if (blockAllCommunications)
    {
      Prompts.ShowPrompt(new Prompt("This service is not activated, therefore it cannot be used."));
      //Prompts.ShowStrictPrompt("This service is not activated, therefore it cannot be used.", null);
      //DisplayErrorMessage(importantMessage);
      yield break;
    }
    //old method
    //GameObject tmp = Instantiate(musPref, downloadParent, true);
    //tmp.transform.SetSiblingIndex(downloadParent.childCount - 2);
    //downloadProgress.value = 0;
    //downloadParent.gameObject.SetActive(true);
    ////ClearAllCoroutines();
    ////StartCoroutine(PlaceInCenter(tmp));

    if (!useCustomLink)
    {
      StartCoroutine(GetAuthorNameAndMusicName(songID, null));
    }
    else
    {
      Slider sl = CreateDownloadSlider();
      Coroutine cor = StartCoroutine(LoadHelper(songID, null, sl, "", ""));
      sl.gameObject.transform.GetChild(3).gameObject.GetComponent<Button>().onClick.AddListener(delegate { StopDownload(cor, sl.gameObject); });
    }
  }

  void HideDownload(GameObject tmp)
  {
    Destroy(tmp);
    downloadParent.gameObject.SetActive(false);
    downloadProgress.gameObject.SetActive(false);
    downloadParent.gameObject.GetComponent<Image>().color = new Color(0, 0, 0, 0);
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


  public void CreateSpectrumNew(SpectrumObjects objectToApply)
  {
    if (streamingAudio)
    {
      return;
    }
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
    if (spectrum.Length < 1 || audSource.clip.samples * audSource.clip.channels != spectrum.Length)
    {
      spectrum = new float[audSource.clip.samples * audSource.clip.channels];
    }
    audSource.clip.GetData(spectrum, 0);
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

  public void DecreaseRequests(bool toggle)
  {
    decreaseAmountOfRequests = toggle;
  }

  public void DontLoadImages(bool toggle)
  {
    dontLoadImages = toggle;
  }

  public void LoadAllSongs(bool toggle)
  {
    loadAllAudio = toggle;
  }

  public void UseAltDownloadMethod(bool toggle)
  {
    useAlternateDownloadMethod = toggle;
  }

  Slider CreateDownloadSlider(/*Coroutine cor*/)
  {
    Slider downloadProgressSlider = Instantiate(downloadProgress.transform.parent, downloadSliderParent).GetChild(0).GetComponent<Slider>();
    //downloadProgressSlider.gameObject.transform.GetChild(3).gameObject.GetComponent<Button>().onClick.AddListener(delegate { StopDownload(cor, downloadProgressSlider.gameObject); });
    downloadProgressSlider.gameObject.SetActive(true);
    return downloadProgressSlider;
  }

  IEnumerator LoadHelper(string uri, GameObject tmp, Slider downloadProgressSlider, string name, string author)
  {
    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG);
    UnityWebRequestAsyncOperation progress = www.SendWebRequest();

    bool isStopped = false;
    Popups.Popup("<color=yellow>Trying to download audio...</color>");

    //Slider downloadProgressSlider = CreateDownloadSlider(this, );
    float finalSize = 0;
    while (!progress.isDone)
    {
      if (www.downloadProgress != 0)
      {
        finalSize = (float.Parse(www.GetResponseHeader("Content-Length")) / 1048576);
        //editor.log.text = "Downloading " + (finalSize * www.downloadProgress).ToString("F2") + "mb. out of " + finalSize.ToString("F2") + "mb.";
        //editor.log.gameObject.GetComponent<Animator>().Play(0, 0, 0.2f);
        downloadProgressSlider.transform.GetChild(1).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = (finalSize * www.downloadProgress).ToString("F2") + "mb./" + finalSize.ToString("F2") + "mb.";
        downloadProgressSlider.value = www.downloadProgress;
        if (autoApplySongOnDownload)
        {
          if (!isStopped)
          {
            audSource.volume = 1 - www.downloadProgress;
          }
          if (!isStopped && www.downloadProgress > 0.8f)
          {
            //StartCoroutine(audManager.LowerVolume());
            isStopped = true;
          }
        }
      }
      yield return null;
    }
    if (www.error != null)
    {
      Popups.Popup($"<color=red>{www.error}");
      Debug.LogError(www.error);
      Destroy(downloadProgressSlider.transform.parent.gameObject);
      yield break;
    }

    if (autoApplySongOnDownload)
    {
      audSource.Stop();
    }
    downloadProgressSlider.transform.GetChild(1).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = (finalSize * www.downloadProgress).ToString("F2") + "mb./" + finalSize.ToString("F2") + "mb.";
    downloadProgressSlider.transform.GetChild(1).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "";
    //downloadProgress.value = 0;
    if (!Directory.Exists(musicPath))
    {
      Directory.CreateDirectory(musicPath);
    }

    if (useAlternateDownloadMethod)
    {
      string searchString = customInfo.text;

      if (customInfo.text != "")
      {
        int underscoreIndex = searchString.IndexOf("_");
        //Debug.Log(firstIndex + " " + underscoreIndex);
        name = searchString.Substring(0, underscoreIndex);
        author = searchString.Substring(underscoreIndex + 1);
      }
      else
      {
        name = "Custom Song";
        author = "Unknown Author";
      }
    }

    if (useCustomLink)
    {
      string searchString = customInfo.text;
      if (customInfo.text != "")
      {
        int underscoreIndex = searchString.IndexOf("_");
        //Debug.Log(firstIndex + " " + underscoreIndex);
        name = searchString.Substring(0, underscoreIndex);
        author = searchString.Substring(underscoreIndex + 1);
      }
      else
      {
        name = "Song from link";
        author = "Unknown Author";
      }
    }

    useCustomLink = false;
    RefactorString(ref name);
    RefactorString(ref author);

    File.WriteAllBytes(musicPath + name + "_" + author + ".mp3", www.downloadHandler.data);
    if (autoApplySongOnDownload)
    {
      yield return StartCoroutine(LoadAndApplySongFromPath(musicPath + name + "_" + author + ".mp3"));
    }
    HideDownload(tmp);
    Destroy(downloadProgressSlider.transform.parent.gameObject);
    Popups.Popup("<color=green> Download has finished!");

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
    if (Application.platform == RuntimePlatform.Android)
    {
      path = "file://" + path;
    }

    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG);
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

  public static IEnumerator LoadSongFromPath(string path, bool streamAudio = true, System.Action<AudioClip> callback = null)
  {
    if (Application.platform == RuntimePlatform.Android)
    {
      path = "file://" + path;
    }
    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG);
    ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = streamAudio;
    UnityWebRequestAsyncOperation progress = www.SendWebRequest();

    while (!progress.isDone)
    {
      yield return null;
    }

    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
    callback.Invoke(myClip);
  }

  IEnumerator LoadAndApplySongFromPath(string path)
  {
    AudioClip myClip = null;
    try
    {
      myClip = Resources.Load(path) as AudioClip;
    }
    catch
    {

    }

    if (Application.platform == RuntimePlatform.Android)
    {
      path = "file://" + path;
    }

    if (myClip == null)
    {
      using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
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
        catch
        {
          Debug.Log($"An issue happened during song loading. Song's path was/is {path}");
        }
      }
    }


    if (myClip == null)
    {
      Debug.LogError($"An issue happened during song loading. Tried to load song from path {path}");
      PlayerPrefs.DeleteKey("recentMusic");
      yield break;
    }
    string fileName = Path.GetFileName(path)/*path.IndexOf(musicPath) != -1 ? path.Substring(path.IndexOf(musicPath) + musicPath.Length) : */;
    string songName = Path.GetFileNameWithoutExtension(path);
    string author = "";
    if (fileName.Contains("_"))
    {
      songName = fileName.Substring(0, fileName.IndexOf("_"));
      author = fileName.Replace(songName + "_", "").Replace(".mp3", "");
    }
    audSource.clip = myClip;
    //audManager.offset = 0;
    audSource.clip.name = songName + "_" + author;
    //audManager.offset = 0;
    audSource.Stop();
    Refresh();
    GetAudioData();
    LoadInformation();
    PlayerPrefs.SetString("recentMusic", path.Replace("file://", ""));
    foreach (SpectrumObjects t in spectrumObjects)
    {
      ClearSpectrum(t);
      CreateSpectrumNew(t);
    }
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

  public void LoadSongsFromFolder(string searchText = "")
  {
    //StopAllCoroutines();
    //ClearAllCoroutines();
    //ClearList();
    ClearList(customMusicPrefabParent);
    if (!Directory.Exists(musicPath))
    {
      Directory.CreateDirectory(musicPath);
    }
    List<string> allMusic = Directory.GetFiles(musicPath, "*.mp3").ToList();

    if (searchText != "")
    {
      foreach (string t in allMusic.ToList())
      {
        if (!t[(t.IndexOf(musicPath) + musicPath.Length)..].ToLower().Contains(searchText.ToLower()))
        {
          allMusic.Remove(t);
        }
      }
    }

    foreach (string t in allPreloadedSongs)
    {
      if (searchText != "" && !t.ToLower().Contains(searchText.ToLower()))
        continue;
      else
      {
        string songName = t;
        string author = "";
        if (t.Contains("_"))
        {
          songName = t.Substring(0, t.IndexOf("_"));
          author = t.Replace(songName + "_", "").Replace(".mp3", "");
        }

        GameObject prefab = Instantiate(musicPrefab, customMusicPrefabParent);
        Button songPlayBtn = prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>();

        songPlayBtn.gameObject.GetComponent<Image>().sprite = altMusicPrefabBtnSprite;

        songPlayBtn.onClick.AddListener(delegate
        {
          string songPath = $"PreloadedSongs/Songs/{Path.GetFileNameWithoutExtension(t)}/" + Path.GetFileNameWithoutExtension(t);
          audSource.clip = Resources.Load(songPath) as AudioClip;
          audSource.Stop(); Refresh();
          GetAudioData();
          LoadInformation();
          foreach (SpectrumObjects t in spectrumObjects)
          {
            ClearSpectrum(t);
            CreateSpectrumNew(t);
          }
          PlayerPrefs.SetString("recentMusic", songPath);
        });

        prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = $"<color=green>{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(songName)}</color>" + (t.Contains("_") ? $" <color=#7d7575>by</color> <color=#c9bebe>{author}</color>" : "");
        prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Button>().enabled = false;//onClick.AddListener(delegate { inputSearchString.text = $"<author>{author}</author>"; SearchOnThatSite(inputSearchString); });

        prefab.transform.GetChild(1).GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = "";
        songPlayBtn.gameObject.SetActive(true);

        prefab.SetActive(true);

        Destroy(prefab.transform.GetChild(3).gameObject);
      }
    }

    for (int i = 0; i < allMusic.Count; i++)
    {
      string substr = allMusic[i].Substring(allMusic[i].IndexOf(musicPath) + musicPath.Length);
      string songName = Path.GetFileNameWithoutExtension(allMusic[i]);
      string author = "";
      if (substr.Contains("_"))
      {
        songName = substr.Substring(0, substr.IndexOf("_"));
        author = substr.Replace(songName + "_", "").Replace(".mp3", "");
      }


      GameObject prefab = Instantiate(musicPrefab, customMusicPrefabParent);
      //Debug.Log(allMusic[i]);
      string temp = allMusic[i];
      Button songPlayBtn = prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>();

      songPlayBtn.gameObject.GetComponent<Image>().sprite = altMusicPrefabBtnSprite;

      songPlayBtn.onClick.AddListener(delegate { StartCoroutine(LoadAndApplySongFromPath(temp)); });
      //PlayerPrefs.GetString("recentMusic")w
      //prefab.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Button>().onClick.AddListener(delegate { PlayerPrefs.SetString("recentMusic", temp); });
      prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "<color=#cc0000>" + songName + "</color>" + (substr.Contains("_") ? (" <color=#7d7575>by</color> " + "<color=#c9bebe>" + author + "</color>") : "");
      prefab.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Button>().enabled = false;//onClick.AddListener(delegate { inputSearchString.text = "<author>" + author + "</author>"; SearchOnThatSite(inputSearchString); });

      prefab.transform.GetChild(1).GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = "";
      songPlayBtn.gameObject.SetActive(true);

      prefab.transform.GetChild(3).gameObject.GetComponent<Button>().onClick.AddListener(delegate { File.Delete(temp); });
      prefab.transform.GetChild(3).gameObject.GetComponent<Button>().onClick.AddListener(delegate { LoadSongsFromFolder(searchText); });
      prefab.transform.GetChild(3).gameObject.SetActive(true);

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
