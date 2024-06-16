using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using vanIvan.Prompts;
public class AudioRecorder : MonoBehaviour
{
    public int recordingLength = 60;
    public MusicSelection musicSelection;
    public Prompt recorderPrompt;
    public GameObject spectrumPrefab;
    public AudioSource audSource;
    void Start()
    {
        recorderPrompt = new Prompt(PromptType.YesOnly);
        recorderPrompt.okName = "Завершить";
        recorderPrompt.ok_action = delegate { StopRecording(true); };
        recorderPrompt.close_action = delegate { StopRecording(false); };
        recorderPrompt.cancel_action = recorderPrompt.close_action;
        Prompts.PreparePrompt(recorderPrompt, true);
        GameObject spectrumObj = new GameObject("img");
        spectrumObj.transform.SetParent(recorderPrompt.promptPanel.textBehaviour.transform);
        RectTransform rectTr = spectrumObj.AddComponent<RectTransform>();
        rectTr.sizeDelta = new Vector2(300, 300);
        rectTr.localScale = new Vector3(100, 100, 100);
        SpectrumVizualizerManager specManager = spectrumObj.AddComponent<SpectrumVizualizerManager>();
        specManager.prefab = spectrumPrefab;
        specManager.audSource = audSource;
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
    }
    public void StartRecording(string customDevice = null)
    {
        recorderPrompt.Show();
        audSource.clip = Microphone.Start(customDevice, true, recordingLength, AudioSettings.outputSampleRate);
        audSource.Play();
    }
    public void StartRecording()
    {
        recorderPrompt.Show();
        audSource.clip = Microphone.Start(null, true, recordingLength, AudioSettings.outputSampleRate);
        audSource.Play();
    }
    public void StopRecording(bool applyResults = true)
    {
        Microphone.End(null);
        if (applyResults == true)
            musicSelection.ApplyLoadedAudioInfo(audSource.clip);
        recorderPrompt.Close();
    }
}
