"""Checks on Unity YAML assets (HX1xx textures, HX2xx rendering/project, HX3xx ids).

Reads .meta / .prefab / .unity / ProjectSettings text files directly, so it runs without Unity.
Requires Asset Serialization Mode = Force Text (Unity default).
"""
from __future__ import annotations

import re
from collections import defaultdict
from pathlib import Path
from typing import Dict, List, Optional, Tuple

from common import ERROR, WARN, Config, Finding, read_text

IMAGE_EXT = {".png", ".jpg", ".jpeg", ".tga", ".gif", ".bmp"}
FILTER_MODE_CODES = {"Point": 0, "Bilinear": 1, "Trilinear": 2}
CLASS_GAMEOBJECT = "1"
CLASS_SPRITE_RENDERER = "212"
CLASS_TILEMAP_RENDERER = "483"
CLASS_CANVAS = "223"
CANVAS_RENDER_MODE_WORLD_SPACE = 2
CLASS_MONOBEHAVIOUR = "114"
SORT_MODE_CUSTOM_AXIS = 3


def _line(text: str, idx: int) -> int:
    return text.count("\n", 0, idx) + 1


def _int(rx: str, text: str) -> Optional[Tuple[int, int]]:
    m = re.search(rx, text, re.M)
    return (int(m.group(1)), m.start()) if m else None


# --------------------------------------------------------------------------- #
# Textures (HX1xx)
# --------------------------------------------------------------------------- #
def check_texture_meta(meta: Path, cfg: Config) -> List[Finding]:
    rel = cfg.rel(meta)
    text = read_text(meta)
    if "TextureImporter:" not in text:
        return []
    out: List[Finding] = []

    def add(level, rule, idx, msg):
        out.append(Finding(level, rule, rel, _line(text, idx) if idx is not None else 0, msg))

    tt = _int(r"^\s*textureType:\s*(-?\d+)", text)
    if tt and tt[0] != 8:
        add(ERROR, "HX101", tt[1], f"Texture Type이 Sprite가 아님 (textureType={tt[0]}, Sprite=8).")
    fm = _int(r"^\s*filterMode:\s*(-?\d+)", text)
    expected_fm = FILTER_MODE_CODES.get(cfg.filter_mode, 1)
    if fm and fm[0] != expected_fm:
        add(ERROR, "HX102", fm[1],
            f"Filter Mode={fm[0]}, 기준은 harness.config.json filterMode='{cfg.filter_mode}'({expected_fm}).")
    mm = _int(r"^\s*enableMipMap:\s*(\d+)", text)
    if mm and mm[0] != 0:
        add(WARN, "HX103", mm[1], "Mip Map이 켜져 있음. 픽셀 아트는 끄세요.")
    ppu = _int(r"^\s*spritePixelsToUnits:\s*(\d+)", text)
    if ppu and ppu[0] != cfg.ppu:
        add(ERROR, "HX105", ppu[1], f"Pixels Per Unit={ppu[0]}, 기준은 {cfg.ppu}.")

    # platform settings: default platform + any overridden platform
    for m in re.finditer(r"buildTarget:\s*(\w+)(.*?)(?=\n\s*- serializedVersion:|\n\s*buildTarget:|\n  \w|\Z)",
                         text, re.S):
        target, block = m.group(1), m.group(2)
        comp = re.search(r"textureCompression:\s*(\d+)", block)
        overridden = re.search(r"overridden:\s*(\d+)", block)
        is_default = target == "DefaultTexturePlatform"
        if comp and int(comp.group(1)) != 0 and (is_default or (overridden and overridden.group(1) == "1")):
            add(ERROR, "HX104", m.start(), f"{target}: Compression이 None이 아님 (textureCompression={comp.group(1)}).")
    return out


def check_textures(cfg: Config) -> List[Finding]:
    base = cfg.root / cfg.art_root
    out: List[Finding] = []
    if not base.is_dir():
        return out
    for meta in sorted(base.rglob("*.meta")):
        if Path(meta.stem).suffix.lower() in IMAGE_EXT:
            out.extend(check_texture_meta(meta, cfg))
    return out


# --------------------------------------------------------------------------- #
# Project settings (HX2xx)
# --------------------------------------------------------------------------- #
def read_tag_manager(cfg: Config) -> Tuple[Optional[Dict[int, str]], List[str], str]:
    """Returns ({uniqueID: name} or None, physics layer names, rel path)."""
    p = cfg.root / "ProjectSettings" / "TagManager.asset"
    if not p.is_file():
        return None, [], cfg.rel(p)
    text = read_text(p)
    sorting: Dict[int, str] = {}
    sm = re.search(r"m_SortingLayers:\s*\n((?:\s+-\s+name:.*\n(?:\s+\w+:.*\n)*)*)", text)
    if sm:
        for nm, uid in re.findall(r"-\s+name:\s*(.*?)\s*\n\s+uniqueID:\s*(-?\d+)", sm.group(1)):
            sorting[int(uid)] = nm
    layers: List[str] = []
    lm = re.search(r"^\s*layers:\s*\n((?:\s*-.*\n?)*)", text, re.M)
    if lm:
        layers = [ln.strip()[1:].strip() for ln in lm.group(1).splitlines() if ln.strip().startswith("-")]
    return sorting, layers, cfg.rel(p)


def check_project(cfg: Config) -> List[Finding]:
    out: List[Finding] = []
    sorting, layers, tm_rel = read_tag_manager(cfg)
    if sorting is None:
        out.append(Finding(WARN, "HX200", tm_rel, 0,
                           "TagManager.asset을 찾지 못함. 프로젝트 루트에서 실행했는지 확인하세요."))
    else:
        actual = [sorting[k] for k in sorting]  # insertion order = Unity order
        expected = cfg.sorting_layers
        missing = [n for n in expected if n not in actual]
        if missing:
            out.append(Finding(ERROR, "HX202", tm_rel, 0,
                               f"Sorting Layer 없음: {', '.join(missing)}. Unity에서 Tools > Harness > Apply Project Settings 실행."))
        present = [n for n in actual if n in expected]
        if not missing and present != expected:
            out.append(Finding(ERROR, "HX202", tm_rel, 0,
                               f"Sorting Layer 순서가 다름. 현재: {' > '.join(present)} / 기준: {' > '.join(expected)}"))
        miss_phys = [n for n in cfg.physics_layers if n not in layers]
        if miss_phys:
            out.append(Finding(WARN, "HX205", tm_rel, 0,
                               f"물리 Layer 없음: {', '.join(miss_phys)}. Tools > Harness > Apply Project Settings 실행."))

    # transparency sort mode (side-scroller must not use custom axis)
    candidates = [cfg.root / "ProjectSettings" / "GraphicsSettings.asset"]
    assets = cfg.root / "Assets"
    if assets.is_dir():
        for p in assets.rglob("*.asset"):
            try:
                head = read_text(p)
            except OSError:
                continue
            if "m_TransparencySortAxis" in head:
                candidates.append(p)
    for p in candidates:
        out.extend(check_sort_mode_file(p, cfg))
    return out


def check_sort_mode_file(p: Path, cfg: Config) -> List[Finding]:
    if not p.is_file():
        return []
    text = read_text(p)
    m = re.search(r"m_TransparencySortMode:\s*(\d+)", text)
    if m and int(m.group(1)) == SORT_MODE_CUSTOM_AXIS and cfg.view == "SideScroller":
        return [Finding(ERROR, "HX201", cfg.rel(p), _line(text, m.start()),
                        "Transparency Sort Mode가 Custom Axis. 횡스크롤에서는 Default로 두세요 (점프 시 앞뒤가 바뀜).")]
    return []


# --------------------------------------------------------------------------- #
# Scenes & prefabs (HX2xx renderers)
# --------------------------------------------------------------------------- #
DOC_SPLIT = re.compile(r"^--- !u!(\d+) &(-?\d+)( stripped)?\s*$", re.M)


def iter_docs(text: str):
    """Yield (class_id, file_id, stripped, body, body_start_index)."""
    matches = list(DOC_SPLIT.finditer(text))
    for i, m in enumerate(matches):
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        yield m.group(1), m.group(2), bool(m.group(3)), text[m.end():end], m.end()


def check_yaml_renderers(path: Path, cfg: Config, sorting: Optional[Dict[int, str]]) -> List[Finding]:
    text = read_text(path)
    rel = cfg.rel(path)
    out: List[Finding] = []
    go_names: Dict[str, str] = {}
    docs = list(iter_docs(text))
    for cid, fid, stripped, body, _ in docs:
        if cid == CLASS_GAMEOBJECT:
            nm = re.search(r"m_Name:[ \t]*(.*)", body)
            if nm:
                go_names[fid] = nm.group(1).strip()
    for cid, fid, stripped, body, start in docs:
        if stripped or cid not in (CLASS_SPRITE_RENDERER, CLASS_TILEMAP_RENDERER, CLASS_CANVAS):
            continue
        if cid == CLASS_CANVAS:
            # 화면 스페이스 캔버스는 Sorting Layer와 무관하다. 월드 스페이스(머리 위 체력바 등)만
            # RENDERING_RULES.md 2장의 UI 레이어 규칙을 따른다.
            mode = re.search(r"m_RenderMode:\s*(-?\d+)", body)
            if not mode or int(mode.group(1)) != CANVAS_RENDER_MODE_WORLD_SPACE:
                continue
            kind = "월드 스페이스 Canvas"
        else:
            kind = "SpriteRenderer" if cid == CLASS_SPRITE_RENDERER else "TilemapRenderer"
        go = re.search(r"m_GameObject:\s*\{fileID:\s*(-?\d+)", body)
        name = go_names.get(go.group(1), "?") if go else "?"
        lid = re.search(r"m_SortingLayerID:\s*(-?\d+)", body)
        if not lid:
            continue
        layer_id = int(lid.group(1))
        line = _line(text, start + lid.start())
        if layer_id == 0:
            out.append(Finding(WARN, "HX203", rel, line,
                               f"'{name}' {kind}가 Default Sorting Layer. RENDERING_RULES.md 2장 기준으로 레이어를 지정하세요."))
        elif sorting is not None and layer_id not in sorting:
            out.append(Finding(ERROR, "HX204", rel, line,
                               f"'{name}' {kind}의 Sorting Layer ID {layer_id}가 TagManager에 없음 (삭제된 레이어)."))
    return out


def check_scenes_and_prefabs(cfg: Config) -> List[Finding]:
    sorting, _, _ = read_tag_manager(cfg)
    out: List[Finding] = []
    for root in cfg.scan_roots:
        base = cfg.root / root
        if not base.is_dir():
            continue
        for p in sorted(list(base.rglob("*.prefab")) + list(base.rglob("*.unity"))):
            out.extend(check_yaml_renderers(p, cfg, sorting))
    return out


# --------------------------------------------------------------------------- #
# Persistent ids (HX3xx)
# --------------------------------------------------------------------------- #
DIRECT_ID = re.compile(r"^[ \t]*persistentId:[ \t]*(.*?)[ \t]*$", re.M)
OVERRIDE_ID = re.compile(r"propertyPath:[ \t]*persistentId[ \t]*\n[ \t]*value:[ \t]*(.*?)[ \t]*$", re.M)


def collect_ids(path: Path, cfg: Config):
    text = read_text(path)
    rel = cfg.rel(path)
    found = []  # (id, rel, line, direct)
    for m in DIRECT_ID.finditer(text):
        found.append((m.group(1).strip("'\""), rel, _line(text, m.start()), True))
    for m in OVERRIDE_ID.finditer(text):
        found.append((m.group(1).strip("'\""), rel, _line(text, m.start()), False))
    return found


def check_ids(cfg: Config, scenes: Optional[List[Path]] = None) -> List[Finding]:
    if scenes is None:
        scenes = []
        for root in cfg.scan_roots:
            base = cfg.root / root
            if base.is_dir():
                scenes.extend(sorted(base.rglob("*.unity")))
    seen = defaultdict(list)
    out: List[Finding] = []
    for s in scenes:
        for pid, rel, line, direct in collect_ids(s, cfg):
            if not pid:
                if direct:
                    out.append(Finding(WARN, "HX302", rel, line,
                                       "PersistentId가 비어 있음. 씬을 열고 저장하면 자동 생성됩니다."))
                continue
            seen[pid].append((rel, line))
    for pid, locs in seen.items():
        if len(locs) > 1:
            where = ", ".join(f"{r}:{l}" for r, l in locs)
            rel, line = locs[0]
            out.append(Finding(ERROR, "HX301", rel, line,
                               f"PersistentId '{pid}'가 {len(locs)}번 중복 ({where}). "
                               "복제한 오브젝트의 PersistentId 우클릭 > Regenerate."))
    return out


# --------------------------------------------------------------------------- #
# Single file dispatch (used by the post-edit hook)
# --------------------------------------------------------------------------- #
def check_asset_file(path: Path, cfg: Config) -> List[Finding]:
    name = path.name
    suffix = path.suffix.lower()
    if suffix == ".meta" and Path(path.stem).suffix.lower() in IMAGE_EXT:
        return check_texture_meta(path, cfg)
    if suffix in (".prefab", ".unity"):
        sorting, _, _ = read_tag_manager(cfg)
        out = check_yaml_renderers(path, cfg, sorting)
        if suffix == ".unity":
            out.extend(check_ids(cfg))
        return out
    if name in ("TagManager.asset", "GraphicsSettings.asset"):
        return check_project(cfg)
    if suffix == ".asset":
        return check_sort_mode_file(path, cfg)
    return []
