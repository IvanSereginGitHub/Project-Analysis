using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static EasingFunction;

public class MusicPlayer : MonoBehaviour
{
    public AudioSource audSource;
    public TextMeshProUGUI musicName;

    public float delayBeforeAnimBegins;
    public float animationTime;
    public Ease easingMethod;
    List<string> allMusic;
    string musicPath;
    bool isReverse;
    float time, animTimer, count = 0;

    float fadeSongTimer = 0;
    bool fadeOut = false;
    [SerializeField]
    RectTransform objRectTransform;
    Vector3 pos;
    float titleTimer = 0;
    public Slider songProgress;
    readonly System.Random rand = new System.Random();
    int musicIndex;
    [HideInInspector]
    public float[] samplesArr = new float[1024];

    public bool skipSongAfterFinish = true;

    public bool streamSong = true;
    void Start()
    {
        musicPath = Application.persistentDataPath + "/_music/";
        if (Directory.Exists(musicPath))
            allMusic = Directory.GetFiles(musicPath).OrderBy(a => rand.Next()).ToList();
        StartCoroutine(AnimateTitle());
        StartCoroutine(Delays.DelayAction(1f, () => { StartCoroutine(GetSong()); }));
    }
    // Start is called before the first frame update
    public void SetMusicMode(bool play)
    {
        if (play)
            audSource.Play();
        else
            audSource.Pause();
    }

    public void FadeInSong()
    {
        fadeSongTimer = 0;
        fadeOut = false;
    }

    public void FadeOutSong()
    {
        fadeSongTimer = 0;
        fadeOut = true;
    }

    void RestartTitleAnimation()
    {
        titleTimer = 0;
        isReverse = false;
    }

    public void SetSongPosition(float value)
    {
        audSource.time = value;
    }

    public IEnumerator FadeSong()
    {
        while (true)
        {
            while (fadeSongTimer <= 1f)
            {
                audSource.volume = ConvertToFloat(Ease.None, fadeOut ? 1 : 0, fadeOut ? 0 : 1, fadeSongTimer);
                fadeSongTimer += Time.unscaledDeltaTime / 3;
                yield return null;
            }
            yield return null;
        }
    }

    public void GetSongAndStuff()
    {
        musicIndex++;
        StartCoroutine(GetSong());
        RestartTitleAnimation();
    }

    public void GetPreviousSong()
    {
        StartCoroutine(PreviousSong());
        RestartTitleAnimation();
    }

    IEnumerator PreviousSong()
    {
        if (musicIndex - 1 < 0)
        {
            yield break;
        }

        ResetTextAnim();
        musicIndex--;
        yield return StartCoroutine(MusicSelection.LoadAndApplySongFromPath_MainMenu(allMusic[musicIndex], audSource, musicName));
        songProgress.maxValue = audSource.clip.length;
    }

    void Update()
    {
        if (objRectTransform != null)
        {
            time += Time.deltaTime;

            if (time >= delayBeforeAnimBegins && (objRectTransform.sizeDelta.x > 500))
            {
                if (animTimer < 1)
                {
                    if (count % 2 == 0)
                    {
                        pos = ConvertToVector3(easingMethod, new Vector3(-325, 0, 0), new Vector3(500 - objRectTransform.sizeDelta.x, 0, 0), animTimer);
                    }
                    else
                    {
                        pos = ConvertToVector3(easingMethod, new Vector3(500 - objRectTransform.sizeDelta.x, 0, 0), new Vector3(-325, 0, 0), animTimer);
                    }
                    animTimer += Time.deltaTime / animationTime;
                }
                else
                {
                    time = 0;
                    count++;
                    animTimer = 0;
                }
            }

            //objRectTransform.position = pos;
        }

        if (songProgress != null)
        {
            songProgress.SetValueWithoutNotify(audSource.time);
        }
        if (skipSongAfterFinish)
        {
            if (audSource.clip != null && audSource.time >= audSource.clip.length)
            {
                GetSongAndStuff();
                audSource.time = 0;
            }
        }
        audSource.GetSpectrumData(samplesArr, 0, FFTWindow.Blackman);
    }

    void ResetTextAnim()
    {
        time = 0;
        pos = Vector3.zero;
        animTimer = 0;
        count = 0;
    }

    public IEnumerator GetSong()
    {
        System.Random rand = new System.Random();

        if (musicIndex == allMusic.Count)
        {
            Debug.Log("No more songs in list, regenerating...");
            musicIndex = 0;
            allMusic = Directory.GetFiles(musicPath).OrderBy(a => rand.Next()).ToList();

            if (Directory.GetFiles(musicPath).Length == 0)
            {
                Debug.Log("No songs are available right now... Start downloading them by using editor!");
                musicName.text = "Playing...nothing?..";
            }
        }

        if (allMusic.Count > 0)
        {
            ResetTextAnim();
            yield return StartCoroutine(MusicSelection.LoadAndApplySongFromPath_MainMenu(allMusic[musicIndex], audSource, musicName, streamSong));
            songProgress.maxValue = audSource.clip.length;
        }
    }

    IEnumerator AnimateTitle()
    {
        RectTransform musicTransform = musicName.GetComponent<RectTransform>();
        RectTransform parent = musicTransform.parent.GetComponent<RectTransform>();
        while (true)
        {
            while (titleTimer <= 1)
            {
                if (musicTransform.sizeDelta.x <= parent.sizeDelta.x)
                {
                    titleTimer = 0;
                    isReverse = false;
                }
                Vector3 end = new Vector3(-musicTransform.sizeDelta.x + parent.sizeDelta.x, 0, 0);
                musicTransform.localPosition = ConvertToVector3(easingMethod, isReverse ? end : Vector3.zero, isReverse ? Vector3.zero : end, titleTimer);
                titleTimer += Time.deltaTime / 10;
                yield return null;
            }
            isReverse = !isReverse;
            titleTimer = 0;
            yield return null;
        }
    }
    public float GetSpectrumDataAt(int value)
    {
        return samplesArr[value];
    }
}
