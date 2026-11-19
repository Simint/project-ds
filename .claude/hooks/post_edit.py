#!/usr/bin/env python3
"""PostToolUse hook for Claude Code (Edit|Write) and Codex (apply_patch).

- .cs                  -> C# lint (HX0xx)
- texture .meta        -> import settings (HX1xx)
- .prefab / .unity     -> sorting layer checks, persistent id duplicates (HX2xx, HX3xx)
- TagManager / GraphicsSettings / 2D Renderer .asset -> project checks

Input:
- Claude Code: tool_input.file_path (or tool_input.path)
- Codex apply_patch: tool_input.command holds the patch text; file paths come
  from "*** Add File:", "*** Update File:" and "*** Move to:" lines.

Output:
- Claude Code: errors -> stderr + exit 2. Warnings -> additionalContext JSON, exit 0.
- Codex: always exit 0 with ASCII-escaped JSON on stdout. Errors use
  {"decision": "block", "reason": ...}; warnings use additionalContext.
  (On Windows, PowerShell -Command maps exit 2 to 1 and re-encodes pipes,
  so exit codes and raw Korean text are not reliable there.)
Never crashes the session: unexpected exceptions exit 0 silently.
"""
from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path

CHECKED_SUFFIXES = {".cs", ".meta", ".prefab", ".unity", ".asset"}
SKIP_DIRS = {"Library", "Temp", "Logs", "obj", "Build", "Builds", "UserSettings", "Packages"}
PATCH_FILE_LINE = re.compile(r"^\*\*\* (?:Add File|Update File|Move to): (.+?)\s*$", re.M)
PATCH_DELETE_LINE = re.compile(r"^\*\*\* Delete File: (.+?)\s*$", re.M)

# .claude/hooks/post_edit.py -> project root (works no matter which harness or cwd)
SCRIPT_PROJECT = Path(__file__).resolve().parents[2]


def _utf8_streams() -> None:
    for stream in (sys.stdout, sys.stderr):  # Windows 콘솔(cp949)에서 한글이 깨지지 않게
        try:
            stream.reconfigure(encoding="utf-8")
        except (AttributeError, ValueError):
            pass


def _read_payload() -> dict:
    raw = sys.stdin.buffer.read()
    return json.loads(raw.decode("utf-8-sig"))


def _edited_paths(payload: dict) -> list:
    """Return the file paths the tool call wrote, for either harness."""
    tool_input = payload.get("tool_input") or {}
    if not isinstance(tool_input, dict):
        return []
    direct = tool_input.get("file_path") or tool_input.get("path")
    if direct:
        return [direct]
    patch = tool_input.get("command") or tool_input.get("patch") or tool_input.get("input")
    if isinstance(patch, list):
        patch = "\n".join(str(p) for p in patch)
    if not isinstance(patch, str) or "*** Begin Patch" not in patch:
        return []
    deleted = set(PATCH_DELETE_LINE.findall(patch))
    seen, out = set(), []
    for p in PATCH_FILE_LINE.findall(patch):
        if p not in seen and p not in deleted:
            seen.add(p)
            out.append(p)
    return out


def _is_codex(payload: dict) -> bool:
    # Codex sends turn_id/model and names file edits apply_patch; Claude Code does neither.
    return payload.get("tool_name") == "apply_patch" or "turn_id" in payload


def _project_root(payload: dict) -> Path:
    env = os.environ.get("CLAUDE_PROJECT_DIR")
    for cand in (env, str(SCRIPT_PROJECT), payload.get("cwd"), os.getcwd()):
        if cand and (Path(cand) / "tools" / "harness").is_dir():
            return Path(cand)
    return SCRIPT_PROJECT


def main() -> int:
    _utf8_streams()
    try:
        payload = _read_payload()
    except Exception:
        return 0

    base = Path(payload.get("cwd") or os.getcwd())
    targets = []
    for file_path in _edited_paths(payload):
        path = Path(file_path)
        if not path.is_absolute():
            path = base / path
        if path.suffix.lower() not in CHECKED_SUFFIXES or not path.is_file():
            continue
        if any(part in SKIP_DIRS for part in path.parts):
            continue
        targets.append(path)
    if not targets:
        return 0

    project = _project_root(payload)
    tools_dir = project / "tools" / "harness"
    if not tools_dir.is_dir():
        return 0
    sys.path.insert(0, str(tools_dir))
    findings = []
    try:
        from common import ERROR, find_project_root, load_config
        import asset_checks
        import csharp_lint

        cfg = load_config(find_project_root(project))
        for path in targets:
            if path.suffix.lower() == ".cs":
                findings += csharp_lint.lint_file(path, cfg)
            else:
                findings += asset_checks.check_asset_file(path, cfg)
    except Exception as exc:  # harness bug must not block work
        print(f"harness hook internal error (ignored): {exc}", file=sys.stderr)
        return 0

    if not findings:
        return 0
    errors = [f for f in findings if f.level == ERROR]
    warns = [f for f in findings if f.level != ERROR]
    codex = _is_codex(payload)

    if errors:
        lines = ["하네스 검사에서 규칙 위반이 발견됐습니다. 다음 작업 전에 고치세요.",
                 "의도된 예외라면 해당 줄 끝에 `// harness-ignore HX00N 이유`를 다세요.", ""]
        lines += [f.format() for f in errors]
        if warns:
            lines += ["", "경고:"] + [f.format() for f in warns]
        message = "\n".join(lines)
        if codex:
            print(json.dumps({"decision": "block", "reason": message}, ensure_ascii=True))
            return 0
        print(message, file=sys.stderr)
        return 2

    ctx = "하네스 경고 (고치는 것이 좋음, 의도라면 harness-ignore로 표시):\n" + \
          "\n".join(f.format() for f in warns)
    print(json.dumps({"hookSpecificOutput": {"hookEventName": "PostToolUse", "additionalContext": ctx}},
                     ensure_ascii=codex))
    return 0


if __name__ == "__main__":
    sys.exit(main())
