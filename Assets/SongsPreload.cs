using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongsPreload : MonoBehaviour
{
  public List<AudioClip> songs = new List<AudioClip>();
  void Awake()
  {
    DontDestroyOnLoad(gameObject);
  }
}
