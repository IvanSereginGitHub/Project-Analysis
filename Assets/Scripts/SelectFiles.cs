using SFB;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using static MusicSelection;
using System;
public class SelectFiles : MonoBehaviour
{
  public MusicSelection musicSelection;
  public void SelectFile()
  {
    if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
    {
      Prompts.ShowQuickStrictPrompt($"Используйте окно файлового менеджера для выбора аудиофайла из любого места на устройстве.\nЭтот файл можно будет скопировать в текущую папку или же сразу загрузить для работы\n\n<u>Примечания:</u>\n1.Файлы должны иметь один из следующих форматов {Debug.ArrayToString(MusicSelection.supportedExtensions)}.\n2.Помните об <color=yellow>авторских правах</color>...");
      StartCoroutine(Delays.DelayAction(1f, () => { SelectFileWindows(); }));
    }
    else if (Application.platform == RuntimePlatform.Android)
    {
      Prompts.ShowQuickStrictPrompt($"Используйте окно файлового менеджера для выбора аудиофайла из любого места на устройстве.\nЭтот файл можно будет скопировать в текущую папку или же сразу загрузить для работы\n\n<u>Примечания:</u>\n1.Файлы должны иметь один из следующих форматов {Debug.ArrayToString(MusicSelection.supportedExtensions)}.\n2.Помните об <color=yellow>авторских правах</color>...");
      StartCoroutine(Delays.DelayAction(1f, () => { SelectFileMobile(); }));
    }
    else
    {
      Prompts.ShowQuickExitOnlyPrompt($"Операции с файлами в данной операционной системе ({Application.platform}) <color=red><b>ещё не поддерживаются</b></color>!", out _);
    }
  }

  public void SaveFile(byte[] bytes, string filename)
  {
    if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
    {
      Prompts.ShowQuickStrictPrompt($"Используйте окно файлового менеджера для сохранения файла в выбранную папку");
      StartCoroutine(Delays.DelayAction(1f, () => { SaveFileAsWindows(bytes, filename); }));
    }
    else if (Application.platform == RuntimePlatform.Android)
    {
      Prompts.ShowQuickStrictPrompt($"Используйте окно файлового менеджера для сохранения файла в выбранную папку");
      StartCoroutine(Delays.DelayAction(1f, () => { SaveFileAsMobile(bytes, filename); }));
    }
    else
    {
      try
      {
        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "saved_files", filename), bytes);
      }
      catch (Exception ex)
      {
        Prompts.ShowQuickExitOnlyPrompt($"Произошла ошибка при ручном сохранении файла на неподдерживаемой сторонними плагинами операционной системе ({Application.platform}): {ex.Message}", out _);
      }
    }
  }

  public void SaveFile(string text, string filename)
  {
    if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
    {
      Prompts.ShowQuickStrictPrompt($"Используйте окно файлового менеджера для сохранения файла в выбранную папку");
      StartCoroutine(Delays.DelayAction(1f, () => { SaveFileAsWindows(text, filename); }));
    }
    else if (Application.platform == RuntimePlatform.Android)
    {
      Prompts.ShowQuickStrictPrompt($"Используйте окно файлового менеджера для сохранения файла в выбранную папку");
      StartCoroutine(Delays.DelayAction(1f, () => { SaveFileAsMobile(text, filename); }));
    }
    else
    {
      try
      {
        File.WriteAllText(Path.Combine(Application.persistentDataPath, "saved_files", filename), text);
      }
      catch (Exception ex)
      {
        Prompts.ShowQuickExitOnlyPrompt($"Произошла ошибка при ручном сохранении файла на неподдерживаемой сторонними плагинами операционной системе ({Application.platform}): {ex.Message}", out _);
      }
    }
  }

  public void SelectFolder()
  {
    if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
    {
      Prompts.ShowQuickStrictPrompt("Используйте окно проводника для выбора папки по-умолчанию.");
      StartCoroutine(Delays.DelayAction(1f, () => { SelectFolderWindows(); }));
    }
    else if (Application.platform == RuntimePlatform.Android)
    {
      Prompts.ShowQuickStrictPrompt("Используйте окно проводника для выбора папки по-умолчанию.\n\nПримечание: операционная система Android не позволяет выбирать папку по-умолчанию напрямую. Пожалуйста, добавьте в данную папку любой аудиофайл и выберите его, чтобы установить папку по-умолчанию.", 5f);
      StartCoroutine(Delays.DelayAction(5f, () => { SelectFolderMobile(); }));
    }
    else
    {
      Prompts.ShowQuickExitOnlyPrompt($"Операции с файлами в данной операционной системе ({Application.platform}) <color=red><b>ещё не поддерживаются</b></color>!", out _);
    }
  }
  void PromptAfter(string[] path)
  {
    string songsPath = ProjectManager.GetProperPath(musicSelection.mainMusicPath);
    Prompt prompt = new Prompt(PromptType.Normal);
    if (path.Length == 0)
    {
      prompt.promptType = PromptType.StrictPanel;
      prompt.closingTime = 1f;
      prompt.promptText = "Файл не был выбран!";
      prompt.Show();
      return;
    }
    string songName = Path.GetFileName(path[0]);
    string extensionlessSongName = Path.GetFileNameWithoutExtension(songName);
    if (Directory.GetFiles(songsPath).Contains(songName))
    {
      prompt.promptType = PromptType.ExitOnly;
      prompt.promptText = "Файл с таким названием уже существует!";
      prompt.Show();
      return;
    }
    prompt.promptText = "\n<b>Хотите ли вы <color=green>сохранить выбранный файл</color> в текущую папку или <color=orange>загрузить его</color>?</b>\n\n<size=50%>Файлы, загруженные из вне <color=orange>текущей папки</color> не будут показаны в списке файлов и не смогут быть загружены после перезапуска программы</size>";
    prompt.ok_action = delegate
    {
      if (File.Exists(ProjectManager.GetProperPath(songsPath, songName)))
      {
        Prompt alreadyExists = new Prompt(PromptType.YesOnly);
        alreadyExists.promptText = "Файл с таким названием уже существует!";
        alreadyExists.Show();
        return;
      }
      Prompt prompt2 = new Prompt(PromptType.Normal);
      prompt2.promptText = $"Хотите ли вы переименовать файл перед копированием? Текущее имя: <color=yellow>{extensionlessSongName}</color>.";
      prompt2.ok_action = delegate
      {
        EventMethodsClass<string> fieldEvent = new EventMethodsClass<string>();
        EventMethodsClass buttonEvent = new EventMethodsClass();
        buttonEvent.ev.AddListener(delegate
        {
          Prompts.QuickPrompt("Нельзя использовать пустую строку в качестве имени файла!");
        });
        fieldEvent.ev.AddListener((string t) =>
        {
          buttonEvent.ev.RemoveAllListeners();
          buttonEvent.ev.AddListener(delegate
          {
            File.Copy(path[0], ProjectManager.GetProperPath(songsPath, t + Path.GetExtension(path[0])));
            if (File.Exists(ProjectManager.GetProperPath(songsPath, t + Path.GetExtension(path[0]))))
            {
              Prompts.QuickPrompt($"Аудиофайл {t} был успешно скопирован!");
              musicSelection.StartLoadingSongsInfo();
            }
          });
        });
        Prompts.ShowQuickAltSettingsPrompt($"Измените название файла перед сохранением:", new List<IEventInterface>
        {
            new EventClass<string>("Новое название аудиофайла...", fieldEvent, EventType.inputField, extensionlessSongName),
            new EventClass("Переименовать", buttonEvent, EventType.button)
        });
      };
      prompt2.cancel_action = delegate
      {
        File.Copy(path[0], ProjectManager.GetProperPath(songsPath, songName));
        if (File.Exists(ProjectManager.GetProperPath(songsPath, songName)))
        {
          Prompt confirmSuccess = new Prompt(PromptType.StrictPanel);
          confirmSuccess.promptText = $"Аудиофайл {extensionlessSongName} был успешно скопирован!";
          musicSelection.StartLoadingSongsInfo();
          confirmSuccess.CloseAfter(1f);
          confirmSuccess.Show();
        }
      };
      prompt2.okName = "Да";
      prompt2.cancelName = "Нет";
      prompt2.Show();
    };
    prompt.cancel_action = delegate
    {
      StartCoroutine(musicSelection.SongLoadWrapper(path[0], false, () => { Prompts.ShowQuickStrictPrompt($"Аудиофайл был успешно загружен."); }));
    };
    prompt.okName = "Сохранить";
    prompt.cancelName = "Загрузить";
    prompt.Show();
  }
  void SelectFileWindows()
  {
    var extensions = new[] { new ExtensionFilter("Любой аудиофайл", "mp3") };
    StandaloneFileBrowser.OpenFilePanelAsync("Открыть файд", "", extensions, false, (string[] paths) => { StartCoroutine(Delays.DelayAction(0.2f, () => { PromptAfter(paths); })); });
  }

  void SelectFolderWindows()
  {
    StandaloneFileBrowser.OpenFolderPanelAsync("Выберите папку", "", false, (string[] paths) => { StartCoroutine(Delays.DelayAction(0.2f, () => { if (paths.Length > 0) ApplyNewMusicPath(paths[0]); })); });
  }

  void SaveFileAsWindows(byte[] bytes, string filename)
  {
    StandaloneFileBrowser.SaveFilePanelAsync("Сохранить результат", "", filename, Path.GetExtension(filename).Replace(".", ""), (string path) =>
    {
      if (path.Length > 0)
      {
        File.WriteAllBytes(path, bytes);
        Debug.Log($"File {Path.GetFileName(path)} was successfully exported!");
      }
    });
  }

  void SaveFileAsMobile(byte[] bytes, string filename)
  {
    string filePath = Path.Combine(Application.temporaryCachePath, filename);
    File.WriteAllBytes(filePath, bytes);
    NativeFilePicker.Permission permission = NativeFilePicker.ExportFile(filePath, (success) => Debug.Log($"File {filename} was successfully exported!"));
  }
  void SaveFileAsWindows(string text, string filename)
  {
    StandaloneFileBrowser.SaveFilePanelAsync("Сохранить результат", "", filename, Path.GetExtension(filename).Replace(".", ""), (string path) =>
    {
      File.WriteAllText(path, text);
      Debug.Log($"File {Path.GetFileName(path)} was successfully exported!");
    });
  }

  void SaveFileAsMobile(string text, string filename)
  {
    string filePath = Path.Combine(Application.temporaryCachePath, filename);
    File.WriteAllText(filePath, text);
    NativeFilePicker.Permission permission = NativeFilePicker.ExportFile(filePath, (success) => Debug.Log($"File {filename} was successfully exported!"));
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

  void SelectFolderMobile()
  {
    NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
    {
      if (path == null)
        Debug.Log("Operation cancelled");
      else
      {
        StartCoroutine(Delays.DelayAction(0.2f, () => { string directoryPath = Path.GetDirectoryName(path); ApplyNewMusicPath(directoryPath); }));
      }
    }, new string[] { NativeFilePicker.ConvertExtensionToFileType("mp3") });

    Debug.Log("Permission result: " + permission);
  }

  void ApplyNewMusicPath(string directoryPath)
  {
    if (Directory.Exists(directoryPath))
    {
      musicSelection.mainMusicPath = directoryPath;
      PlayerPrefs.SetString("mainMusicPath", directoryPath);
      Prompts.ShowQuickStrictPrompt($"Folder {directoryPath} was successfully selected!");
      musicSelection.StartLoadingSongsInfo();
    }
  }

  public static string[] GetFilesFromFolder_Array(string path, string[] extensions, string search = "")
  {
    var myFiles = Directory
    .EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
    .Where(s => extensions.Contains(Path.GetExtension(s).ToLowerInvariant()));
    if (search != "")
    {
      myFiles = myFiles.Where(s => Path.GetFileName(s).ToLowerInvariant().Contains(search.ToLowerInvariant()));
    }
    return myFiles.ToArray();
  }

  public static List<string> GetFilesFromFolder_List(string path, string[] extensions, string search = "")
  {
    var myFiles = Directory
    .EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
    .Where(s => extensions.Contains(Path.GetExtension(s).ToLowerInvariant()));
    if (search != "")
    {
      myFiles = myFiles.Where(s => Path.GetFileName(s).ToLowerInvariant().Contains(search.ToLowerInvariant()));
    }
    return myFiles.ToList();
  }


  public void ShowInExplorer()
  {
    string songsPath = ProjectManager.GetProperPath(Application.persistentDataPath, "_music");
    string path = songsPath.Replace(@"/", @"\");
    System.Diagnostics.Process.Start("explorer.exe", path);
  }
}
