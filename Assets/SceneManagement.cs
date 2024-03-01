using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
  public void LoadScene(int index)
  {
    SceneManager.LoadScene(index);
  }

  public void WarnAboutLoadingScene(int index)
  {
    Prompt prompt = new Prompt($"Do you really want to exit?");
    prompt.ok_action = delegate { SceneManager.LoadSceneAsync(index, LoadSceneMode.Single); };
    prompt.Show();
  }
}
