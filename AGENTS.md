# AGENTS.md

## Project Overview

MinionLib is a Slay the Spire 2 modding library built with C#/.NET and Godot. It provides:

- Minion support: summoned allied creatures, positioning, layout, animation helpers, and Guardian behavior.
- Creature actions: clickable power-like actions attached to creatures.
- Component cards: reusable `CardComponent` behavior, component-aware `ComponentsCardModel`, state serialization, dynamic vars, right-click hooks, and timing hooks.
- Custom targeting: extensible `TargetType` support for minions and composite target rules.
- Source generators: component registration/state code, dynamic vars, binary serialization, delegate registration, and BaseLib/RitsuLib adapter emission.

Important projects:

- `MinionLib/`: runtime Godot mod/library.
- `MinionLib.Generators/`: Roslyn source generators.
- `MinionLib.BaseLibAdapters/`: source templates embedded into the generator when BaseLib is detected.
- `MinionLib.RitsuAdapters/`: source templates embedded into the generator when RitsuLib is detected.
- `MinionLib.Example/`: example mod code. Do not treat it as always up to date with the runtime API.
- `docs/`: Starlight documentation site.

## Local Context File

Always read `AGENTS.local.md` after this file when working in this repository.

`AGENTS.local.md` stores machine-specific paths and must not be committed. It should include at least:

- Required: Slay the Spire 2 decompiled project directory.
- Optional: BaseLib repository path.
- Optional: RitsuLib repository path.
- Optional: Slay the Spire 2 reference DLL directory, Steam install path, Godot executable path, and other local tools.

If `AGENTS.local.md` is missing, or if a task needs a path that is missing or points to a non-existent directory, use the repository skill `.codex/skills/minionlib-local-context` to create or repair it. The skill can auto-detect common paths and should ask the user only for paths it cannot find. The Slay the Spire 2 decompiled directory is mandatory. BaseLib and RitsuLib paths are optional unless the task touches related adapter or compatibility code.

## Environment

This repository targets:

- .NET `net9.0`.
- Godot.NET.Sdk `4.5.1`.
- C# `preview`.
- Starlight/Astro docs under `docs/`.

Local game paths are normally configured through `LocalSettings.props` and `GameFolder.props`.

Useful properties:

- `GodotPath`: path to the MegaDot/Godot executable.
- `SteamLibraryPath`: Steam `steamapps` directory.
- `Sts2Path`: Slay the Spire 2 install directory.
- `ModsPath`: Slay the Spire 2 `mods/` directory.
- `Sts2DataDir`: directory containing `sts2.dll` and `0Harmony.dll`.

Do not commit personal path files such as `LocalSettings.props` or `AGENTS.local.md`.

## Common Commands

Use PowerShell on Windows unless the user asks otherwise.

- Build solution: `dotnet build MinionLib.sln`
- Build docs: from `docs/`, run `pnpm build`
- Start docs dev server: from `docs/`, run `pnpm dev`

If docs dependencies are missing, run `pnpm install` in `docs/`.

Builds may require valid `GodotPath` and STS2 reference paths. If those are missing, consult `AGENTS.local.md` and the local-context skill before changing code.

## Coding Notes

- Prefer `rg` / `rg --files` for search.
- Use `apply_patch` for hand edits.
- Do not edit generated files such as `*.g.cs` unless the user explicitly asks.
- Do not revert user changes in the working tree.
- Keep changes scoped. Avoid unrelated refactors while fixing docs or generator behavior.
- When changing adapter templates, remember that `MinionLib.Generators` embeds files from `MinionLib.BaseLibAdapters/*.cs` and `MinionLib.RitsuAdapters/*.cs`.
- Generated BaseLib/RitsuLib adapter namespaces are based on the consuming assembly name, not `MinionLib.BaseLibAdapters` or `MinionLib.RitsuAdapters`.
- `MinionModel.OnSummon` currently has signature `OnSummon(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options)`.

## Docs Notes

- The documentation site uses Starlight with locales `zh-cn` and `en`.
- Root `docs/src/pages/index.astro` performs language redirect.
- Docs content lives under `docs/src/content/docs/{locale}/`.
- The quickstart sidebar group is configured in `docs/astro.config.mjs`.
- Keep Chinese and English pages structurally aligned when adding public documentation.

## When To Use External Repositories

Use `AGENTS.local.md` paths for external context:

- Use the decompiled Slay the Spire 2 project when checking base game APIs, model signatures, commands, Godot scene paths, localization shape, or behavior not defined in this repo.
- Use BaseLib only when touching `MinionLib.BaseLibAdapters`, BaseLib integration docs, or code that depends on BaseLib concepts.
- Use RitsuLib only when touching `MinionLib.RitsuAdapters`, RitsuLib integration docs, or code that depends on RitsuLib scaffolding.
