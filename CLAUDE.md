# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Stabat_v2 is a 2D multiplayer party/battle game built in Unity 6 (`6000.4.0f1`). Players fight in an arena,
collect money, and build shops, either locally (split-screen/local multiplayer via the custom `KoitanInput`
system) or online via Photon Fusion 2 (Shared game mode).

The codebase is organized by developer namespace/folder rather than by feature:
- `Assets/Scripts/Koitan/` (namespace `Koitan`) — core battle loop, offline/local play, shops, items, results.
- `Assets/Scripts/Zakky/` (namespace default/none) — online play (Photon Fusion), character/stage select flow.
- `Assets/KoitanLib/` (namespace `KoitanLib`) — shared engine-agnostic utilities: local input abstraction,
  debug overlay, scene-loading helpers, editor tools. Treat this as a reusable internal library, not
  feature code.

Git commit messages and in-code comments are largely in Japanese; this is a small (2-developer) project, not
an enterprise codebase — keep changes pragmatic and don't over-engineer.

## Working with this repo (important constraints)

- **There is no CLI build/test/lint pipeline.** This is a Unity Editor project; compilation, running, and
  testing all happen through the Unity Editor (or `Unity.exe -batchmode` if invoked manually). There is no
  `package.json`/`Makefile`/CI script to run. Do not invent build/test commands — verify changes by reading
  the code and checking they compile logically (matching Unity/Fusion API shapes), since there is no
  automated test suite (`com.unity.test-framework` is a package dependency but no test assemblies/`Tests`
  folders currently exist in `Assets`).
- **An MCP Unity bridge is configured** (`com.gamelovers.mcp-unity` package, `ProjectSettings/McpUnitySettings.json`,
  server port 8070, auto-start enabled). If MCP Unity tools are available in the session, prefer using them
  to query editor state, trigger compilation, or read console errors instead of guessing.
- Source files are Shift-JIS/UTF-8 mixed with Japanese comments; some older files show mojibake when read as
  UTF-8 — don't "fix" garbled comments unless asked, they may just be an encoding artifact of the tool, not
  the actual file.
- Never touch `Library/`, `Temp/`, `Logs/`, `obj/`, `.vs/` — these are Unity/IDE-generated caches, not source.
- `.meta` files must stay paired 1:1 with their asset (same name + `.meta`). If you add/rename/delete a file
  under `Assets/`, add/rename/delete its `.meta` file too, or Unity will regenerate one with a new GUID and
  break asset references (prefabs, scenes) that pointed at the old GUID.
- Scene files (`.unity`) and prefabs are large YAML text files. Prefer scripting changes over hand-editing
  scene/prefab YAML; if you must edit one, keep diffs minimal and be aware merge conflicts here are not
  safely auto-resolvable.

## Architecture

### Local (offline) battle flow — `Koitan` namespace

- `BattleManager` (`Assets/Scripts/Koitan/Battle/BattleManager.cs`) is the per-scene singleton (`instance`)
  that owns the battle: player list, money totals per player index, item spawning, the battle timer, and
  win/end sequencing. It transitions `BeforeBattle → Battle → AfterBattle` and loads the `Result` scene when
  the timer ends, populating the static `Result` class with final standings.
- `BattleSetting` / `BattleGlobal` hold static cross-scene config: `BattleGlobal.MaxPlayerNum` (4),
  available stage scene names, per-player controller assignment.
- `PlayerController` is the local-play character controller (as opposed to `PlayerAvatar`, which is the
  networked one — see below). `ShopController`, `Money`, `Bomb` are battle-arena interactables.
- `KoitanInput` (`Assets/KoitanLib/Scripts/KoitanInput/`) is a custom multi-controller input abstraction
  (supports up to 4 simultaneous human/CPU controllers with `ButtonCode` enum: A/B/X/Y/Start/Select/Up/Down/
  Right/Left), independent of Unity's Input System. `ControllerInput`/`InputSystemPlayer`/`SimpleAI` are
  the concrete input sources registered into it. This system is only used for local/offline play.
- Scene flow: `SelectFlowUI` (character select) → stage select → `BattleManager.StartBattle()` loads one of
  `BattleGlobal.stageSceneNames` → `Result` scene.

### Online battle flow — Photon Fusion 2, Shared mode

- `GameLauncher` (`Assets/Scripts/Zakky/Online/GameLauncher.cs`) boots the `NetworkRunner` in `GameMode.Shared`,
  and once scene load finishes (`OnSceneLoadDone`), spawns the local player's `PlayerAvatar` networked prefab
  at a position from `BattleManager.instance.GetInitPosition(...)`.
- `PlayerAvatar` (`Assets/Scripts/Zakky/Online/PlayerAvater.cs`, note the filename typo) is a `NetworkBehaviour`
  — the networked counterpart to `PlayerController`. Movement/actions run in `FixedUpdateNetwork()`, gated by
  `HasStateAuthority`; input comes from `GetInput<NetworkInputData>()`.
- `NetworkInputData` (`INetworkInput` struct: `Stick` + `NetworkButtons`) is filled every tick in
  `GameLauncher.OnInput` from `LocalInputReader.Instance.ConsumeFusionInput()`.
- `LocalInputReader` reads the new Unity Input System (`InputActionAsset`) locally and buffers press/hold
  state for the *next* Fusion input poll — this is the online-play input path and is separate from
  `KoitanInput` (offline path). Don't conflate the two input systems.
- `OnlineShopController` is the networked equivalent of `ShopController`.
- Movement physics come from the third-party PC2D asset (`Assets/PC2D`, `PlatformerMotor2D`), used by both
  the offline `PlayerController` and the online `PlayerAvatar`.
- Photon Fusion lives under `Assets/Photon/Fusion` (imported as source, not a Package Manager dependency) and
  has its own asmdefs (`Fusion.Unity`, `Fusion.Unity.Editor`, `Fusion.CodeGen`). Scripting define symbols
  `FUSION2`, `FUSION_2_0`, etc. are set per build target in ProjectSettings — don't assume a specific Fusion
  version without checking `ProjectSettings/ProjectSettings.asset`.

### Debug/dev tooling

- `KOITAN_DEBUG` and `KOITAN_MANAGER` scripting define symbols (set for Standalone and WebGL) gate debug-only
  behavior, e.g. `ManagerSceneAutoLoader` additively loads a `DebugScene`/`ManagerScene` on startup when
  defined.
- `KoitanDebug.Display(...)` (`Assets/KoitanLib/Scripts/Debug/KoitanDebug.cs`) is an on-screen debug text
  overlay used throughout for runtime state (e.g. controller button states, money instance counts) — prefer
  this over `Debug.Log` spam for per-frame diagnostic text.
- `SceneLaunchWindow` (`KoitanLib/Scene Launcher` editor menu) is a custom EditorWindow for quickly switching
  between scenes in the Build Settings list without using the Unity File menu.

## Key third-party assets/packages

- **Photon Fusion 2** (`Assets/Photon`) — multiplayer networking, Shared game mode.
- **PC2D** (`Assets/PC2D`) — 2D platformer character motor (`PlatformerMotor2D`), used for all player physics.
- **DOTween** (`Assets/Plugins/Demigiant`) — tweening.
- **Cinemachine 2.x**, **TextMeshPro**, **Unity Input System**, **URP** — standard Unity packages, see
  `Packages/manifest.json` for exact versions.
- **RubyTextMeshPro** (`Assets/KoitanLib/OtherLib`) — adds ruby (furigana) annotation support to TextMeshPro,
  for Japanese text.
