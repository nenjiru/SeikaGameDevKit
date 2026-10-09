# Changelog

## [0.2.1] - 2026-10-10

Agent setup only. The kit's components are unchanged.

- At the start of a session, the agent checks whether it was opened in a git worktree (Claude Desktop can open sessions there). If so, it does not edit files, use Unity MCP or commit, because the Unity editor has the main project folder open and the changes would not reach it. `.claude/worktrees/` is now ignored by git
- When turning an idea into a task, the agent looks up facts in the project (design document, TODO, code, scenes) itself and only asks the student about decisions
- The guides point to the `Components` table in this README. The setup without Discord no longer mentions the Discord reporter

## [0.2.0] - 2026-10-09

First public release.

- Events (`SeikaGameDevKit.Events`): event assets without a value and with an int, float or string value, plus listener components wired in the Inspector. Custom value types take one line. `GameEventBase.AnyRaised` reports every raised event in one place
- Play log (`SeikaGameDevKit.Recording`): every Play Mode session in the editor automatically records session start and end, scene loads, events and console output to `Logs/PlayLog/` as JSON Lines. Entries that repeat too often are folded into one line per second
- Tuning changes (`SeikaGameDevKit.Editor.Tuning`): when Play Mode ends, ScriptableObject values in `Data/` that changed during play are listed so each can be overwritten or reverted. Changes and choices are appended to the play log
- Debug markers (`SeikaGameDevKit.Visualization`): objects with a `Debug Marker` are always drawn in the Scene view with a label (collider shapes, or a position marker when there is no collider). Colliders without visuals and all triggers are drawn without a marker. A menu item and a shortcut toggle all markers
- Convention check (`SeikaGameDevKit.Editor.Conventions`): checks placement, names, Art contents and scripts against the folder tree in `PROJECT_CONVENTIONS.md` and the Root namespace, and shows the result in a window and in `Logs/Conventions/latest.json`. Warns about disallowed names on import
