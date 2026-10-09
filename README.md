# Seika Game Dev Kit

A genre-agnostic set of behind-the-scenes components for Unity 6, built to work with **Seika Game Dev Agent** — the Claude Code agent setup for a university game development course.
Before writing new code, the agent checks the component list below and reuses a component when one fits.

Code comments and editor UI text are in Japanese, because the students read them.

## Installation

### With the agent setup (used in the course)

The agent setup (AGENTS.md, skills, guides and project conventions) is published as `SeikaGameDevAgent.zip` in this repository's [Releases](https://github.com/nenjiru/SeikaGameDevKit/releases). Run the following line in PowerShell at the root of a Unity project (the folder that contains `Assets` and `Packages`). It installs the agent setup and registers this kit in `Packages/manifest.json`. Add ` -WithDiscord` at the end to include the Discord reporter.

```powershell
irm https://github.com/nenjiru/SeikaGameDevKit/releases/latest/download/install.ps1 -OutFile install.ps1; powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1
```

Existing files are never overwritten.

### Kit only

Add one line to `dependencies` in `Packages/manifest.json` (the part after `#` is the version tag).

```json
"jp.digicre.seika-game-dev-kit": "https://github.com/nenjiru/SeikaGameDevKit.git#v0.2.1"
```

A kit installed through the Package Manager is read-only. Report bugs and missing features to the instructor.

## Components

| Component | Namespace | Purpose | Usage |
|---|---|---|---|
| Events | `SeikaGameDevKit.Events` | Components notify each other of game events without direct references. One asset per event | [Events](#events) |
| Play log | `SeikaGameDevKit.Recording` | Automatically records what happened every time you enter Play Mode in the editor | [Play log](#play-log) |
| Debug markers | `SeikaGameDevKit.Visualization` | Always draws trigger areas, spawn points and other hard-to-see objects in the Scene view, with a label. Colliders without visuals and all triggers are drawn even without a marker | [Debug markers](#debug-markers) |
| Convention check | `SeikaGameDevKit.Editor.Conventions` | Lists files that break the rules in `PROJECT_CONVENTIONS.md` (placement, names, Art contents, scripts) | [Convention check](#convention-check) |
| Tuning changes | `SeikaGameDevKit.Editor.Tuning` | When you exit Play Mode, lists the tuning values you changed during play so you can overwrite or revert each one | [Tuning changes](#tuning-changes) |

## Events

Create one event asset per game event ("ball drained", "score changed", ...). Senders and receivers connect simply by referencing the same asset in the Inspector.

- Create: right-click in the Project window → Create > Seika Game Dev Kit > Events. Use `Game Event` for events without a value, and `Int` / `Float` / `String Game Event` for events with one value. Put them in `Data/Events/` and describe the event in `Event Description`
- Send: declare `[SerializeField] GameEvent ballDrained;` and call `ballDrained.Raise();` (with a value: `scoreChanged.Raise(100);`)
- Receive in code: `AddListener(OnBallDrained)` in `OnEnable`, `RemoveListener(OnBallDrained)` in `OnDisable`
- Receive without code: add `Game Event Listener` (or `Int Game Event Listener`, etc.) and wire the event and the response in the Inspector
- Custom value types: write a one-line class, e.g. `[CreateAssetMenu] public class StateChangedEvent : GameEvent<StateChange> { }`. To receive it without code, also write `public class StateChangedEventListener : GameEventListener<StateChange, StateChangedEvent> { }`
- Every raised event is also reported through `GameEventBase.AnyRaised` (used by behind-the-scenes components such as the play log)
- Listeners from the previous play session are cleared automatically when Play Mode starts, even when domain reload is disabled in the Enter Play Mode settings. Register receivers in `OnEnable` every time

## Play log

Nothing to set up. Once the kit is installed, every Play Mode session in the editor is recorded.

- Location: `Logs/PlayLog/<date-time>.jsonl` in the project root (the newest 20 files are kept; `Logs/` is ignored by git)
- Contents: session start and end, scene loads, kit events (name and value), and console logs, warnings and errors — one JSON object per line
- When the same entry occurs more than 10 times in one second, the rest of that second is folded into a single `summary` line (count and last value)
- Event values are written with `ToString()`. Override `ToString()` on custom value types so the log is readable
- To disable it, add `SEIKA_NO_PLAYLOG` to Player Settings > Scripting Define Symbols
- Built players are not recorded

## Tuning changes

Nothing to set up. Watches the ScriptableObjects (tuning values) in `Assets/_Project/Data/`.

- Changes made to a ScriptableObject in the Inspector during Play Mode persist after you stop. So that changes are not lost track of, a "調整値の変更" (tuning changes) window lists every changed value when you exit Play Mode
- For each value, or for all at once, choose 上書き (green, keep the new value) or 戻す (red, restore the value from before play). Closing the window keeps the remaining values. Reverts can be undone with Ctrl+Z
- The changes and the choices are appended to the play log of that session (`tuning_change`, `tuning_overwrite`, `tuning_revert`)
- Values inside scenes and prefabs are reverted by Unity when Play Mode ends, so they are out of scope. Move values you want to tune into ScriptableObjects

## Debug markers

Makes hard-to-see objects (trigger areas, spawn points, ...) easy to find in the Scene view.

- Objects with a `Debug Marker` component are always drawn, even when not selected (Add Component > Seika Game Dev Kit > Visualization; works on any GameObject)
  - If the object has colliders, their shapes are drawn (even on visible objects such as walls). Otherwise a small marker is drawn at its position (for spawn points, etc.)
  - Shows a label (e.g. 落下判定; the object name when empty). The color can be customized
- Colliders without visuals and all triggers are drawn even without a marker (their label appears only when selected)
- Shapes come from the colliders (Box, Sphere, Capsule, Mesh, CharacterController; 2D Box, Circle, Polygon, Edge; others are drawn as their bounds)
- Colors: triggers are orange, solid colliders are light blue. Click a filled shape to select its object
- Toggle all markers: menu Seika Game Dev Kit > デバッグマーカーを表示, or Shift+Alt+G (rebind in Edit > Shortcuts). Nothing is drawn while Gizmos are off in the Scene view

## Convention check

Menu Seika Game Dev Kit > 決まりを点検 checks whether `Assets/` follows the project conventions (no files are changed).

- What is checked
  - Placement: files outside the defined folders, and folders that are not defined. The folder structure is read from the tree diagram (`├─` `│` `└─`) in `PROJECT_CONVENTIONS.md` at the project root. Anything under a folder whose contents are not defined is allowed
  - Names: names inside `Assets/_Project/` may only use ASCII letters, digits, `_` and `-` (extensions excluded)
  - Art contents: scripts inside `Art/`, and prefabs there with colliders, Rigidbodies or project scripts (Unity components such as UI Image are not counted)
  - Scripts: scripts outside `Scripts/`, files without a class of the same name, and namespaces that are not "Root namespace + folders under `Scripts/`"
- The game namespace comes from Project Settings > Editor > Root namespace. When it is empty, namespaces are not checked
- Contents outside `_Project/` (`_ThirdParty/`, `Settings/`, ...) are not checked
- Results are shown in a window (click an item to ping it) and written to `Logs/Conventions/latest.json`. The agent calls `SeikaGameDevKit.Editor.Conventions.ConventionChecker.Run()` and reads that file
- When a file is imported into or moved within `Assets/_Project/` with a disallowed name, a warning is logged immediately

## License

[CC0 1.0](LICENSE)
