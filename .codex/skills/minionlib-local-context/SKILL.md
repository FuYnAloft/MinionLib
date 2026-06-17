---
name: minionlib-local-context
description: Create, repair, or validate MinionLib's AGENTS.local.md machine-specific path file. Use when AGENTS.local.md is missing, required local paths are TODO or invalid, the Slay the Spire 2 decompiled directory must be located, or BaseLib/RitsuLib repository paths are needed for adapter or integration work.
---

# MinionLib Local Context

Use this skill to create or repair `AGENTS.local.md` for a MinionLib checkout.

## Workflow

1. Run `scripts/create_agents_local.py` from this skill.
2. Read the script output.
3. If `Sts2DecompiledPath` is missing, ask the user for the Slay the Spire 2 decompiled project directory. This path is mandatory.
4. If BaseLib or RitsuLib paths are missing, ask only when the current task touches those integrations.
5. Re-run the script with explicit arguments or edit `AGENTS.local.md` directly with the user-provided paths.
6. Re-read `AGENTS.local.md` before continuing the original task.

## Script

From the repository root:

```powershell
python .codex\skills\minionlib-local-context\scripts\create_agents_local.py
```

Useful options:

```powershell
python .codex\skills\minionlib-local-context\scripts\create_agents_local.py --force
python .codex\skills\minionlib-local-context\scripts\create_agents_local.py --sts2-decompiled D:\RiderProjects\SlayTheSpire2
python .codex\skills\minionlib-local-context\scripts\create_agents_local.py --baselib D:\RiderProjects\BaseLib-StS2
python .codex\skills\minionlib-local-context\scripts\create_agents_local.py --ritsulib D:\RiderProjects\STS2-RitsuLib
```

The script searches common local project roots near the checkout. It does not perform a full disk crawl.

## Path Requirements

- `Sts2DecompiledPath` is required for base game API checks.
- `BaseLibRepoPath` is optional unless working on BaseLib adapters or docs.
- `RitsuLibRepoPath` is optional unless working on RitsuLib adapters or docs.
- `Sts2ReferenceDllPath`, `Sts2InstallPath`, and `GodotPath` are optional convenience paths.
