#!/usr/bin/env python3
"""Create or repair AGENTS.local.md for the MinionLib repository."""

from __future__ import annotations

import argparse
import os
from pathlib import Path
from typing import Iterable


ROOT_MARKERS = ("MinionLib.sln", "Directory.Build.props")


def find_repo_root(start: Path) -> Path:
    current = start.resolve()
    for candidate in (current, *current.parents):
        if all((candidate / marker).exists() for marker in ROOT_MARKERS):
            return candidate
    return current


def candidate_roots(repo_root: Path) -> list[Path]:
    roots: list[Path] = []
    for path in [
        repo_root.parent,
        Path("D:/RiderProjects"),
        Path("C:/RiderProjects"),
        Path.home() / "RiderProjects",
        Path.home() / "source" / "repos",
        Path.home() / "Projects",
    ]:
        if path.exists() and path.is_dir() and path not in roots:
            roots.append(path)
    return roots


def iter_child_dirs(roots: Iterable[Path]) -> Iterable[Path]:
    for root in roots:
        try:
            yield from (child for child in root.iterdir() if child.is_dir())
        except OSError:
            continue


def has_any(path: Path, names: Iterable[str]) -> bool:
    return any((path / name).exists() for name in names)


def find_sts2_decompiled(roots: Iterable[Path]) -> Path | None:
    for child in iter_child_dirs(roots):
        name = child.name.lower()
        if "slay" not in name and "sts2" not in name and "spire" not in name:
            continue
        if has_any(child, ("sts2.sln", "sts2.csproj")) and (child / "project.godot").exists():
            return child
    return None


def find_reference_dlls(roots: Iterable[Path]) -> Path | None:
    for child in iter_child_dirs(roots):
        if (child / "sts2.dll").exists() and (child / "0Harmony.dll").exists():
            return child
    return None


def find_baselib(roots: Iterable[Path]) -> Path | None:
    for child in iter_child_dirs(roots):
        name = child.name.lower()
        if "base" not in name:
            continue
        if has_any(child, ("BaseLib.csproj", "BaseLib.sln")):
            return child
    return None


def find_ritsulib(roots: Iterable[Path]) -> Path | None:
    for child in iter_child_dirs(roots):
        name = child.name.lower()
        if "ritsu" not in name:
            continue
        if has_any(child, ("STS2-RitsuLib.csproj", "STS2-RitsuLib.sln")):
            return child
    return None


def fmt(path: Path | str | None) -> str:
    if path is None:
        return "TODO"
    return str(path)


def existing_values(path: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    if not path.exists():
        return values
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped.startswith("- `") or "`:" not in stripped:
            continue
        key = stripped.split("`:", 1)[0].removeprefix("- `")
        value = stripped.split("`:", 1)[1].strip().strip("`")
        if value:
            values[key] = value
    return values


def choose(key: str, detected: Path | None, existing: dict[str, str], forced: bool) -> str:
    current = existing.get(key)
    if current and current != "TODO" and not forced:
        return current
    return fmt(detected)


def write_agents_local(repo_root: Path, values: dict[str, str]) -> None:
    content = f"""# AGENTS.local.md

This file is machine-specific and should not be committed. `AGENTS.md` requires agents to read this file after the shared project instructions.

## Required Paths

- `Sts2DecompiledPath`: `{values["Sts2DecompiledPath"]}`

## Optional Paths

- `Sts2ReferenceDllPath`: `{values["Sts2ReferenceDllPath"]}`
- `BaseLibRepoPath`: `{values["BaseLibRepoPath"]}`
- `RitsuLibRepoPath`: `{values["RitsuLibRepoPath"]}`
- `Sts2InstallPath`: `{values["Sts2InstallPath"]}`
- `GodotPath`: `{values["GodotPath"]}`

## Notes

- The Slay the Spire 2 decompiled project path is required for base game API checks.
- BaseLib and RitsuLib are optional. Use them only when working on their adapters, compatibility behavior, or docs.
- If any path is missing or stale, use `.codex/skills/minionlib-local-context` to recreate this file.
"""
    (repo_root / "AGENTS.local.md").write_text(content, encoding="utf-8", newline="\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", type=Path)
    parser.add_argument("--force", action="store_true")
    parser.add_argument("--sts2-decompiled", type=Path)
    parser.add_argument("--sts2-reference-dlls", type=Path)
    parser.add_argument("--baselib", type=Path)
    parser.add_argument("--ritsulib", type=Path)
    parser.add_argument("--sts2-install", type=str)
    parser.add_argument("--godot", type=str)
    args = parser.parse_args()

    repo_root = (args.repo_root or find_repo_root(Path.cwd())).resolve()
    roots = candidate_roots(repo_root)
    local_file = repo_root / "AGENTS.local.md"
    existing = existing_values(local_file)

    detected_sts2 = args.sts2_decompiled or find_sts2_decompiled(roots)
    detected_dlls = args.sts2_reference_dlls or find_reference_dlls(roots)
    detected_baselib = args.baselib or find_baselib(roots)
    detected_ritsu = args.ritsulib or find_ritsulib(roots)

    values = {
        "Sts2DecompiledPath": choose("Sts2DecompiledPath", detected_sts2, existing, args.force),
        "Sts2ReferenceDllPath": choose("Sts2ReferenceDllPath", detected_dlls, existing, args.force),
        "BaseLibRepoPath": choose("BaseLibRepoPath", detected_baselib, existing, args.force),
        "RitsuLibRepoPath": choose("RitsuLibRepoPath", detected_ritsu, existing, args.force),
        "Sts2InstallPath": args.sts2_install or existing.get("Sts2InstallPath", "TODO"),
        "GodotPath": args.godot or existing.get("GodotPath", "TODO"),
    }

    write_agents_local(repo_root, values)
    print(f"Wrote {local_file}")
    for key, value in values.items():
        status = "MISSING" if value == "TODO" else "OK"
        print(f"{status}: {key} = {value}")

    if values["Sts2DecompiledPath"] == "TODO":
        print("REQUIRED_PATH_MISSING: ask the user for Sts2DecompiledPath.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
