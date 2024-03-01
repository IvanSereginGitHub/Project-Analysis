using SFB;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
public class SelectFiles : MonoBehaviour
{
  public void SelectFile()
  {
    if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
    {
      Prompts.QuickCancelOnlyPrompt("Use an Explorer window to select audio file from any path.\nIt will be copied to the game's local music folder after.\n\n<u>Notes:</u>\n1.Songs should be in *.mp3 format to properly work (might add *.wav support in the future).\n2.Remember about <color=yellow>copyrights</color>...", out Prompt prompt);
      StartCoroutine(Delays.DelayAction(1f, () => { SelectFileWindows(); prompt.Close(); }));
    }
    else if (Application.platform == RuntimePlatform.Android)
    {
      Prompts.QuickCancelOnlyPrompt("Use an Explorer window to select audio file from any path.\nIt will be copied to the game's local music folder after.\n\n<u>Notes:</u>\n1.Songs should be in *.mp3 format to properly work (might add *.wav support in the future).\n2.Remember about <color=yellow>copyrights</color>...", out Prompt prompt);
      StartCoroutine(Delays.DelayAction(1f, () => { SelectFileMobile(); prompt.Close(); }));
    }
    else
    {
      Prompts.QuickCancelOnlyPrompt($"Local song usage on this platform ({Application.platform}) <color=red><b>is not implemented</b></color>!", out _);
    }
  }
  void PromptAfter(string[] path)
  {
    string songsPath = ProjectManager.GetProperPath(Application.persistentDataPath, "_music");
    Prompt prompt = new Prompt(PromptType.Normal);
    if (path.Length == 0)
    {
      prompt.promptType = PromptType.ExitOnly;
      prompt.promptText = "Nothing was selected!";
      prompt.Show();
      return;
    }
    string songName = Path.GetFileName(path[0]);
    string extensionlessSongName = Path.GetFileNameWithoutExtension(songName);
    if (Directory.GetFiles(songsPath).Contains(songName))
    {
      prompt.promptType = PromptType.ExitOnly;
      prompt.promptText = "This file is already in the music folder. Use a search box to find it!";
      prompt.Show();
      return;
    }
    prompt.promptText = "This song will be copied to game's local music folder.\n<color=yellow><b>Do you wish to continue?</b></color>";
    prompt.ok_action = delegate
    {
      if (File.Exists(ProjectManager.GetProperPath(songsPath, songName)))
      {
        Prompt alreadyExists = new Prompt(PromptType.YesOnly);
        alreadyExists.promptText = "Song with such name already exists in the music folder. Please, rename it and try again!";
        alreadyExists.Show();
        return;
      }
      Prompt prompt2 = new Prompt(PromptType.Normal);
      prompt2.promptText = $"Would you like to rename the song before finishing? It's current name is <color=yellow>{extensionlessSongName}</color>.";
      prompt2.ok_action = delegate
      {
        EventMethodsClass<string> fieldEvent = new EventMethodsClass<string>();
        EventMethodsClass buttonEvent = new EventMethodsClass();
        buttonEvent.ev.AddListener(delegate
        {
          Prompts.QuickPrompt("Cannot accept empty string as a song name!");
        });
        fieldEvent.ev.AddListener((string t) =>
        {
          buttonEvent.ev.RemoveAllListeners();
          buttonEvent.ev.AddListener(delegate
          {
            File.Copy(path[0], ProjectManager.GetProperPath(songsPath, t + ".mp3"));
            if (File.Exists(ProjectManager.GetProperPath(songsPath, t + ".mp3")))
            {
              Prompts.QuickPrompt($"Song {t} was successfully copied!");
            }
          });
        });
        Prompts.QuickAltSettingsPrompt($"Enter the song name without extension (Previous name: <color=yellow>{extensionlessSongName}</color>):", new List<IEventInterface>
        {
            new EventClass<string>("New song name...", fieldEvent, EventType.inputField),
            new EventClass("Rename", buttonEvent, EventType.button)
        });
      };
      prompt2.cancel_action = delegate
      {
        File.Copy(path[0], ProjectManager.GetProperPath(songsPath, songName));
        if (File.Exists(ProjectManager.GetProperPath(songsPath, songName)))
        {
          Prompt confirmSuccess = new Prompt(PromptType.StrictPanel);
          confirmSuccess.promptText = $"Song {extensionlessSongName} was successfully copied!";
          confirmSuccess.CloseAfter(1f);
          confirmSuccess.Show();
        }
      };
      prompt2.okName = "Yes";
      prompt2.cancelName = "No";
      prompt2.Show();
    };
    prompt.okName = "Yes";
    prompt.cancelName = "No";
    prompt.Show();
  }
  void SelectFileWindows()
  {
    var extensions = new[] { new ExtensionFilter("Any music file ", "mp3") };
    StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, false, (string[] paths) => { StartCoroutine(Delays.DelayAction(0.2f, () => { PromptAfter(paths); })); });
  }

  void SelectFileMobile()
  {
    NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
    {
      if (path == null)
        Debug.Log("Operation cancelled");
      else
      {
        StartCoroutine(Delays.DelayAction(0.2f, () => { PromptAfter(new string[] { path }); }));
      }
    }, new string[] { NativeFilePicker.ConvertExtensionToFileType("mp3") });

    Debug.Log("Permission result: " + permission);
  }

  public void ShowInExplorer()
  {
    string songsPath = ProjectManager.GetProperPath(Application.persistentDataPath, "_music");
    string path = songsPath.Replace(@"/", @"\");
    System.Diagnostics.Process.Start("explorer.exe", path);
  }
}
