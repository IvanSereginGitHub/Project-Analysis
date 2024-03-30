# Project Beat
Project Beat is a crossplatform tool for analyzing different properties of audiofiles and exporting them.

> [!CAUTION]
> This tool **DEPENDS** on another tool called "Prompts" and because of the certain reasons I've decided to separate it from any other projects into it's own.
> This repository **DOES NOT** contain it. See [Prompts](https://github.com/IvanSereginGitHub/Prompts) for more info.

> [!WARNING]
> While analysis itself is indeed crossplatform (and only file format depending), as well as Unity Runtime module (which runs this tool) - you would need to install platform-specific file browsers and modify existing file "SelectFiles.cs" in order to search, add to local folder or simply read audiofiles via native platform browser.
> 
> If you don't want to bother with that, check following:
> * Place audiofiles in the platform-specific folder, described in the [Unity Documentation](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html)
> * Check if said directory contains /files directory after launching this tool.


# How to install
* Download Unity 2022.3.20f1

Any other version might work, but I personally don't recommend using any version below 2020
* Download this tool via `Code > Download Zip`
  * (or directly via `git clone https://github.com/IvanSereginGitHub/Project-Beat-3D`)
* Add project to Unity Hub
  * (or open directly from editor via `File > Open Project`)
* Open `Scenes > SongAnalysis.scene` in Project Explorer
* Use inside of editor or build via `File > Build Settings` for your platform



# What's next?
## Project Beat Extended
Project Beat Extended is an upcoming paid tool, containing different components for realtime/preanalyzed data usage.
Please visit [Project Beat Extended](https://github.com/IvanSereginGitHub/Project-Beat-Extended) for more info.
