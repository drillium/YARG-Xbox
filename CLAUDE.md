# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

YARG Xbox is a fan-made fork of YARG (Yet Another Rhythm Game) targeting Xbox One/Series consoles via UWP sideloading in Dev Mode. It's a Unity 2021.3.36f1 project using IL2CPP with the WSAPlayer (Universal Windows Platform) build target.

## Build Commands

**Local development:**
```bash
# Clone with submodules (required)
git clone -b dev --recursive <repo-url>

# Restore NuGet packages (also available via Unity menu: NuGet > Restore Packages)
dotnet tool install --global NuGetForUnity.Cli
nugetforunity restore

# Unity must be opened with UWP platform selected (Build Settings > Universal Windows Platform)
```

**CI build (GitHub Actions):**
```bash
# Trigger manually via workflow dispatch
gh workflow run build-xbox-uwp.yml -f version=0.0.4
```

**Unity CLI batch build:**
```bash
Unity.exe -batchmode -nographics -quit -projectPath . -buildTarget WSAPlayer -executeMethod Editor.Build.BuildUWP.Build -logFile build.log
```

**MSBuild for APPX packaging (after Unity build):**
```bash
msbuild build/WSAPlayer/YARG.sln /p:Configuration=Master /p:Platform=x64 /p:AppxBundle=Never /p:UapAppxPackageBuildMode=SideloadOnly
```

## Architecture

### Submodules (must use `--recursive` clone)
- **Assets/YARG.Core** — Core backend library (engine, replays, song parsing, audio abstractions). Forked from YARC-Official/YARG.Core by gingerphoenix10.
- **Assets/ManagedBass** — C# wrapper for the BASS audio library. Forked by gingerphoenix10.

### Key Source Directories (Assets/Script/)
- **Audio/Bass/** — BASS audio implementation (`BassAudioManager` extends `AudioManager` from YARG.Core)
- **Audio/UWP/** — UWP-specific audio (e.g., `UWPMicDevice` for Xbox mic input)
- **Gameplay/** — Core gameplay loop, HUD, player controllers, visual note tracks
- **Input/** — Controller bindings, device management, serialization
- **Menu/** — 14+ menu subsystems (MusicLibrary, ProfileList, Settings, DifficultySelect, etc.)
- **Persistent/** — Objects surviving scene transitions, logging
- **Helpers/** — `PathHelper.cs` is critical for Xbox — handles platform-specific paths with `#if UNITY_WSA` guards
- **Settings/** — Game settings with type system and metadata

### Build Scripts (Assets/Editor/Build/)
- **BuildUWP.cs** — CI entry point (`Editor.Build.BuildUWP.Build`). Builds 5 scenes to `build/WSAPlayer/`.
- **BuildGitCommitVersion.cs** — Injects git commit info into builds.

### Scenes (build order matters)
1. PersistentScene — Persistent managers
2. MenuScene — Main menu
3. Gameplay — Core gameplay
4. CalibrationScene — Input calibration
5. ScoreScene — Results display

## Xbox/UWP-Specific Patterns

Code guarded with `#if UNITY_WSA && !UNITY_EDITOR` for Xbox-specific behavior. Key areas:
- **PathHelper.cs** — StreamingAssets path differs on UWP (uses PersistentDataPath + "StreamingAssets")
- **BassAudioManager.cs** — Has disabled (`&& false`) UWP async audio device enumeration stubs
- **ProjectSettings.asset** — `XboxOnePersistentLocalStorageSize` must be >0 (set to 256)
- **LogHandler.cs** — Reflection in `OverwriteUnityInternals()` wrapped in try-catch for IL2CPP/UWP safety

## CI/CD Notes

The GitHub Actions workflow (`build-xbox-uwp.yml`) has several non-obvious requirements:
- **Unity Personal license in CI**: Uses `unity-license-activate` (Puppeteer-based .alf→.ulf conversion) + Windows Firewall to block the Licensing Client's network access, forcing legacy ULF mode (Personal licenses lack `com.unity.editor.headless` entitlement)
- **WindowsMobile SDK**: Unity generates a reference to WindowsMobile extension SDK not present on GH runners; must be patched out of generated .vcxproj before MSBuild
- **Direct Unity installer**: Unity Hub CLI `--headless install` is async/unreliable; workflow uses direct EXE installers with `/S` flag

## Code Style

Follows `.editorconfig` rules — 4-space indent, CRLF line endings, UTF-8-BOM. ReSharper/Roslyn analyzer settings included. PRs must target `dev` branch (not `master`).

## License

LGPL-3.0-or-later. BASS audio library is proprietary (free for non-commercial use).
