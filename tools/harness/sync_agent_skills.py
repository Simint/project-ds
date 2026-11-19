#!/usr/bin/env python3
"""Mirror .claude/skills/ (source of truth) into .agents/skills/ for Codex.

Usage:
  python tools/harness/sync_agent_skills.py          # copy, delete stale skills
  python tools/harness/sync_agent_skills.py --check  # exit 1 if out of sync

Only standard library. Edit skills under .claude/skills/, never .agents/skills/.
"""
from __future__ import annotations

import argparse
import filecmp
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / ".claude" / "skills"
DST = ROOT / ".agents" / "skills"
README = "README.md"
README_TEXT = (
    "# .agents/skills (자동 생성)\n\n"
    "이 폴더는 `.claude/skills/`의 사본이다 (Codex가 읽는 위치).\n"
    "직접 고치지 말고 `.claude/skills/`를 고친 뒤 "
    "`python tools/harness/sync_agent_skills.py`를 실행한다.\n"
)


def _files(base: Path) -> set:
    return {p.relative_to(base) for p in base.rglob("*") if p.is_file()}


def diff() -> list:
    """Return human-readable differences between SRC and DST."""
    out = []
    src = _files(SRC)
    dst = {p for p in _files(DST) if p != Path(README)} if DST.is_dir() else set()
    for rel in sorted(src - dst):
        out.append(f"없음: .agents/skills/{rel.as_posix()}")
    for rel in sorted(dst - src):
        out.append(f"남는 파일: .agents/skills/{rel.as_posix()}")
    for rel in sorted(src & dst):
        if not filecmp.cmp(SRC / rel, DST / rel, shallow=False):
            out.append(f"내용 다름: .agents/skills/{rel.as_posix()}")
    return out


def sync() -> int:
    DST.mkdir(parents=True, exist_ok=True)
    src_skills = {p.name for p in SRC.iterdir() if p.is_dir()}
    for p in DST.iterdir():
        if p.is_dir() and p.name not in src_skills:
            shutil.rmtree(p)
    for name in sorted(src_skills):
        target = DST / name
        if target.exists():
            shutil.rmtree(target)
        shutil.copytree(SRC / name, target)
    (DST / README).write_text(README_TEXT, encoding="utf-8")
    return len(src_skills)


def main() -> int:
    for stream in (sys.stdout, sys.stderr):  # Windows 콘솔(cp949)에서 한글이 깨지지 않게
        try:
            stream.reconfigure(encoding="utf-8")
        except (AttributeError, ValueError):
            pass
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--check", action="store_true", help="동기화 여부만 확인")
    args = ap.parse_args()
    if not SRC.is_dir():
        print(f"원본 폴더가 없습니다: {SRC}", file=sys.stderr)
        return 2
    if args.check:
        problems = diff()
        if problems:
            print(".agents/skills가 .claude/skills와 다릅니다. "
                  "`python tools/harness/sync_agent_skills.py`를 실행하세요.")
            print("\n".join(problems))
            return 1
        print(".agents/skills 동기화 상태 OK")
        return 0
    n = sync()
    print(f"스킬 {n}개를 .agents/skills/로 복사했습니다.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
