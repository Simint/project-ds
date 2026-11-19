"""Shared helpers for the harness checks: config loading, project root, findings."""
from __future__ import annotations

import json
import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import List, Optional

ERROR = "ERROR"
WARN = "WARN"


@dataclass
class Finding:
    level: str          # ERROR or WARN
    rule: str           # e.g. HX001
    path: str           # project-relative path
    line: int           # 1-based, 0 if not applicable
    message: str
    snippet: str = ""

    def format(self) -> str:
        loc = f"{self.path}:{self.line}" if self.line else self.path
        out = f"[{self.level} {self.rule}] {loc} - {self.message}"
        if self.snippet:
            out += f"\n    > {self.snippet.strip()[:160]}"
        return out


@dataclass
class Config:
    root: Path
    data: dict = field(default_factory=dict)

    @property
    def ppu(self) -> int:
        return int(self.data.get("pixelsPerUnit", 16))

    @property
    def art_root(self) -> str:
        return self.data.get("artRoot", "Assets/_Project/Art").rstrip("/")

    @property
    def sorting_layers(self) -> List[str]:
        return list(self.data.get("sortingLayers", []))

    @property
    def physics_layers(self) -> List[str]:
        return list(self.data.get("physicsLayers", []))

    @property
    def scan_roots(self) -> List[str]:
        return list(self.data.get("scanRoots", ["Assets/_Project"]))

    @property
    def view(self) -> str:
        return self.data.get("view", "SideScroller")

    def rel(self, path: Path) -> str:
        try:
            return path.resolve().relative_to(self.root.resolve()).as_posix()
        except ValueError:
            return path.as_posix()


def find_project_root(start: Optional[Path] = None) -> Path:
    """Project root = nearest ancestor containing harness.config.json.
    Falls back to CLAUDE_PROJECT_DIR, then cwd."""
    env = os.environ.get("CLAUDE_PROJECT_DIR")
    candidates = []
    if start is not None:
        candidates.append(Path(start).resolve())
    if env:
        candidates.append(Path(env).resolve())
    candidates.append(Path.cwd().resolve())
    for base in candidates:
        cur = base if base.is_dir() else base.parent
        for p in [cur, *cur.parents]:
            if (p / "harness.config.json").is_file():
                return p
    return Path(env).resolve() if env else Path.cwd().resolve()


def load_config(root: Path) -> Config:
    cfg_path = root / "harness.config.json"
    data = {}
    if cfg_path.is_file():
        with open(cfg_path, encoding="utf-8") as f:
            data = json.load(f)
    return Config(root=root, data=data)


def read_text(path: Path) -> str:
    with open(path, encoding="utf-8-sig", errors="replace") as f:
        return f.read()
