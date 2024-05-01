# Project Beat
Project Beat is a crossplatform tool for analyzing different properties of audiofiles and exporting those results.

> [!CAUTION]
> This tool is by default integrated with another tool called "Prompts" for advanced UI interactions.
> 
> This repository **DOES NOT** contain it. See [Prompts](https://github.com/IvanSereginGitHub/Prompts) for more info.
>
>
> See [this branch](https://github.com/IvanSereginGitHub/Project-Beat-3D/tree/prompts-less) for version without integration. Most features of main branch would not be available.

 
> [!IMPORTANT]
> While analysis itself is indeed crossplatform (and only file format depending), as well as Unity Runtime module (which runs this tool) - you would need to install platform-specific file browsers and modify existing file "SelectFiles.cs" in order to search, add to local folder or simply read audiofiles via native platform browser.
> 
> If you want build and run this tool on unsupported platforms, check the following:
> * Place audiofiles in the platform-specific folder, described in the [Unity Documentation](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html)
> * Check if said directory contains _music directory **AFTER** launching this tool.


# How to install
1. Download Unity 2022.3.20f1
Any other version might work, but I personally don't recommend using any version below Unity 2020 LTS
2. Download this tool via `Code > Download Zip`
   * (or directly via `git clone https://github.com/IvanSereginGitHub/Project-Beat-3D`)
3. Add project to Unity Hub and launch it
   * (or open directly from already launched editor via `File > Open Project`)
4. Open `Scenes > SongAnalysis.scene` in Project Explorer
5. Use inside of editor or build via `File > Build Settings` for your platform



# What's next?
## Project Beat Extended
Project Beat Extended is an upcoming paid tool, containing different components for realtime/preanalyzed data usage.
Please visit [Project Beat Extended](https://github.com/IvanSereginGitHub/Project-Beat-Extended) for more info.
