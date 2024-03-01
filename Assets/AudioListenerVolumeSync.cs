using UnityEngine;

public class AudioListenerVolumeSync : MonoBehaviour
{
  void Awake()
  {
    AudioListener.volume = PlayerPrefs.GetFloat("_globalMusicVolume", 1);
  }
}
