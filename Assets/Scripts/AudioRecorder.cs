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
        recorderPrompt.promptText = $"Идёт запись с устройства: {Microphone.devices[0]}";
        recorderPrompt.closingTime = recordingLength;
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
        specManager.beatCount = 64;
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
        musicSelection.audSource.Pause();
        recorderPrompt.Show();
        Microphone.GetDeviceCaps(customDevice, out int min, out int max);
        audSource.clip = Microphone.Start(customDevice, false, recordingLength, min);
        while (!(Microphone.GetPosition(customDevice) > 0)) { }
        audSource.Play();
    }
    public void StartRecording()
    {
        StartRecording(null);
    }
    public void StopRecording(bool applyResults = true)
    {
        StopRecording(null, applyResults);
    }
    public void StopRecording(string customDevice = null, bool applyResults = true)
    {
        Microphone.GetDeviceCaps(customDevice, out int min, out int max);
        var data_arr = new float[Microphone.GetPosition(customDevice)];
        audSource.clip.GetData(data_arr, 0);
        Microphone.End(null);
        var clip = AudioClip.Create(audSource.clip.name, data_arr.Length, 1, min, false);
        clip.SetData(data_arr, 0);
        if (applyResults == true)
            musicSelection.ApplyLoadedAudioInfo(clip);
        recorderPrompt.Close();
    }
    // Big TODO...
    public void SaveAudioClip(string path) {

    }
}
