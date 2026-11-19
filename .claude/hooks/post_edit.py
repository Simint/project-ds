#!/usr/bin/env python3
"""Claude Code PostToolUse hook (Edit|Write).

- .cs                  -> C# lint (HX0xx)
- texture .meta        -> import settings (HX1xx)
- .prefab / .unity     -> sorting layer checks, persistent id duplicates (HX2xx, HX3xx)
- TagManager / GraphicsSettings / 2D Renderer .asset -> project checks

Errors  -> stderr + exit 2 (Claude sees it and must address it).
Warnings only -> additionalContext JSON on stdout, exit 0.
Never crashes the session: unexpected exceptions exit 0 silently.
"""
from __future__ import annotations

import json
import os
import sys
from pathlib import Path

CHECKED_SUFFIXES = {".cs", ".meta", ".prefab", ".unity", ".asset"}
SKIP_DIRS = {"Library", "Temp", "Logs", "obj", "Build", "Builds", "UserSettings", "Packages"}


def main() -> int:
    try:
        payload = json.load(sys.stdin)
    except Exception:
        return 0
    tool_input = payload.get("tool_input") or {}
    file_path = tool_input.get("file_path") or tool_input.get("path")
    if not file_path:
        return 0
    path = Path(file_path)
    if not path.is_absolute():
        path = Path(payload.get("cwd") or os.getcwd()) / path
    if path.suffix.lower() not in CHECKED_SUFFIXES or not path.is_file():
        return 0
    if any(part in SKIP_DIRS for part in path.parts):
        return 0

    project = Path(os.environ.get("CLAUDE_PROJECT_DIR") or payload.get("cwd") or os.getcwd())
    tools_dir = project / "tools" / "harness"
    if not tools_dir.is_dir():
        return 0
    sys.path.insert(0, str(tools_dir))
    try:
        from common import ERROR, find_project_root, load_config
        import asset_checks
        import csharp_lint

        cfg = load_config(find_project_root(project))
        if path.suffix.lower() == ".cs":
            findings = csharp_lint.lint_file(path, cfg)
        else:
            findings = asset_checks.check_asset_file(path, cfg)
    except Exception as exc:  # harness bug must not block work
        print(f"harness hook internal error (ignored): {exc}", file=sys.stderr)
        return 0

    if not findings:
        return 0
    errors = [f for f in findings if f.level == ERROR]
    warns = [f for f in findings if f.level != ERROR]

    if errors:
        lines = ["하네스 검사에서 규칙 위반이 발견됐습니다. 다음 작업 전에 고치세요.",
                 "의도된 예외라면 해당 줄 끝에 `// harness-ignore HX00N 이유`를 다세요.", ""]
        lines += [f.format() for f in errors]
        if warns:
            lines += ["", "경고:"] + [f.format() for f in warns]
        print("\n".join(lines), file=sys.stderr)
        return 2

    ctx = "하네스 경고 (고치는 것이 좋음, 의도라면 harness-ignore로 표시):\n" + \
          "\n".join(f.format() for f in warns)
    print(json.dumps({"hookSpecificOutput": {"hookEventName": "PostToolUse", "additionalContext": ctx}},
                     ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
