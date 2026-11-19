#!/usr/bin/env python3
"""Harness validator for the side-scroller pixel survival project.

Usage (run from the project root):
  python3 tools/harness/validate.py                 # everything
  python3 tools/harness/validate.py --only csharp   # csharp | textures | project | scenes | ids
  python3 tools/harness/validate.py Assets/_Project/Scripts/Player/PlayerController.cs
  python3 tools/harness/validate.py --errors-only

Exit code: 1 if any ERROR, else 0.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from common import ERROR, find_project_root, load_config  # noqa: E402
import asset_checks  # noqa: E402
import csharp_lint  # noqa: E402

GROUPS = ("csharp", "textures", "project", "scenes", "ids")


def check_path(p: Path, cfg):
    if p.suffix.lower() == ".cs":
        return csharp_lint.lint_file(p, cfg)
    return asset_checks.check_asset_file(p, cfg)


def main(argv=None) -> int:
    for stream in (sys.stdout, sys.stderr):  # Windows 콘솔(cp949)에서 한글이 깨지지 않게
        try:
            stream.reconfigure(encoding="utf-8")
        except (AttributeError, ValueError):
            pass
    ap = argparse.ArgumentParser(description="Unity side-scroller harness validator")
    ap.add_argument("paths", nargs="*", help="특정 파일만 검사 (생략하면 전체)")
    ap.add_argument("--only", choices=GROUPS, action="append", help="검사 그룹 선택 (여러 번 가능)")
    ap.add_argument("--root", help="프로젝트 루트 (기본: harness.config.json이 있는 상위 폴더)")
    ap.add_argument("--errors-only", action="store_true", help="경고는 숨김")
    args = ap.parse_args(argv)

    root = Path(args.root).resolve() if args.root else find_project_root()
    cfg = load_config(root)

    findings = []
    if args.paths:
        for raw in args.paths:
            p = Path(raw)
            if not p.is_absolute():
                p = (Path.cwd() / p)
            if p.is_file():
                findings.extend(check_path(p, cfg))
            else:
                print(f"파일 없음: {raw}", file=sys.stderr)
    else:
        groups = args.only or list(GROUPS)
        if "csharp" in groups:
            findings.extend(csharp_lint.lint_tree(cfg))
        if "textures" in groups:
            findings.extend(asset_checks.check_textures(cfg))
        if "project" in groups:
            findings.extend(asset_checks.check_project(cfg))
        if "scenes" in groups:
            findings.extend(asset_checks.check_scenes_and_prefabs(cfg))
        if "ids" in groups:
            findings.extend(asset_checks.check_ids(cfg))

    if args.errors_only:
        findings = [f for f in findings if f.level == ERROR]

    errors = sum(1 for f in findings if f.level == ERROR)
    warns = len(findings) - errors
    for f in findings:
        print(f.format())
    print(f"\n하네스 검사: 오류 {errors}개, 경고 {warns}개 (루트: {root})")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
