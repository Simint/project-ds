"""Heuristic C# lint for Unity 6 project rules (HX0xx).

Not a parser: strips comments/strings, finds Update/FixedUpdate/LateUpdate bodies by
brace matching, then applies regex rules. Suppress a finding with
`// harness-ignore HX00N reason` on the same line (or on the line directly above).
"""
from __future__ import annotations

import re
from pathlib import Path
from typing import List, Tuple

from common import ERROR, WARN, Config, Finding, read_text

HOT_METHODS = ("Update", "FixedUpdate", "LateUpdate")


# --------------------------------------------------------------------------- #
# Source preprocessing
# --------------------------------------------------------------------------- #
def _blank(s: str) -> str:
    return "".join("\n" if c == "\n" else " " for c in s)


def strip_source(src: str) -> Tuple[str, str]:
    """Return (no_comments, no_comments_no_strings). Newlines and offsets preserved."""
    out_nc: List[str] = []   # comments removed, strings kept
    out_ns: List[str] = []   # comments and string contents removed
    i, n = 0, len(src)
    while i < n:
        c = src[i]
        nxt = src[i + 1] if i + 1 < n else ""
        # line comment
        if c == "/" and nxt == "/":
            j = src.find("\n", i)
            j = n if j == -1 else j
            seg = _blank(src[i:j])
            out_nc.append(seg); out_ns.append(seg); i = j
            continue
        # block comment
        if c == "/" and nxt == "*":
            j = src.find("*/", i + 2)
            j = n if j == -1 else j + 2
            seg = _blank(src[i:j])
            out_nc.append(seg); out_ns.append(seg); i = j
            continue
        # strings: "..." @"..." $"..." $@"..." @$"..."
        if c in "@$" or c == '"':
            k = i
            verbatim = False
            while k < n and src[k] in "@$":
                if src[k] == "@":
                    verbatim = True
                k += 1
            if k < n and src[k] == '"' and (k == i or src[i] in "@$"):
                j = k + 1
                while j < n:
                    if verbatim:
                        if src[j] == '"' and j + 1 < n and src[j + 1] == '"':
                            j += 2; continue
                        if src[j] == '"':
                            break
                    else:
                        if src[j] == "\\":
                            j += 2; continue
                        if src[j] == '"' or src[j] == "\n":
                            break
                    j += 1
                j = min(j + 1, n)
                lit = src[i:j]
                out_nc.append(lit)
                out_ns.append(src[i:k + 1] + _blank(src[k + 1:j - 1]) + src[j - 1] if j - 1 > k else lit)
                i = j
                continue
        # char literal
        if c == "'":
            j = i + 1
            while j < n and src[j] != "'" and src[j] != "\n":
                j += 2 if src[j] == "\\" else 1
            j = min(j + 1, n)
            out_nc.append(src[i:j]); out_ns.append("'" + _blank(src[i + 1:j - 1]) + src[j - 1] if j - i >= 2 else src[i:j])
            i = j
            continue
        out_nc.append(c); out_ns.append(c); i += 1
    return "".join(out_nc), "".join(out_ns)


def hot_ranges(code: str) -> List[Tuple[int, int, str]]:
    """Character ranges (start, end, name) of Update/FixedUpdate/LateUpdate bodies."""
    ranges = []
    pat = re.compile(r"\bvoid\s+(" + "|".join(HOT_METHODS) + r")\s*\(\s*\)\s*\{")
    for m in pat.finditer(code):
        start = m.end() - 1
        depth, j = 0, start
        while j < len(code):
            if code[j] == "{":
                depth += 1
            elif code[j] == "}":
                depth -= 1
                if depth == 0:
                    break
            j += 1
        ranges.append((start, j, m.group(1)))
    return ranges


# --------------------------------------------------------------------------- #
# Rules
# --------------------------------------------------------------------------- #
HOT_LOOKUP = [
    (re.compile(r"\bGetComponents?(InChildren|InParent)?\s*[<(]"), "GetComponent"),
    (re.compile(r"\bGameObject\s*\.\s*Find\w*\s*\("), "GameObject.Find"),
    (re.compile(r"\bFind(First|Any)ObjectByType\b|\bFindObjectsByType\b"), "Find*ObjectByType"),
    (re.compile(r"\bCamera\s*\.\s*main\b"), "Camera.main"),
]
HOT_ALLOC = [
    (re.compile(r"\bnew\s+(List|Dictionary|HashSet|Queue|Stack|StringBuilder|LinkedList)\s*<"), "컬렉션 new"),
    (re.compile(r"\bnew\s+[A-Za-z_][\w.]*\s*\[\s*[^\]\s]"), "배열 new"),
    (re.compile(r"\.\s*(Where|Select|SelectMany|OrderBy|OrderByDescending|ToList|ToArray|ToDictionary|GroupBy|Any|All|FirstOrDefault|LastOrDefault|Sum|Count)\s*\("), "LINQ"),
]
INTERP = re.compile(r'\$@?"|@\$"')
MAGIC_LAYER = re.compile(
    r'\bNameToLayer\s*\(\s*"|\bLayerMask\s*\.\s*GetMask\s*\(\s*"|\bsortingLayerName\s*=\s*"'
    r'|\bSortingLayer\s*\.\s*NameToID\s*\(\s*"|\bSortingLayer\s*\.\s*GetLayerValueFromName\s*\(\s*"'
)
LEGACY_INPUT = re.compile(
    r"\bInput\s*\.\s*(GetKey|GetKeyDown|GetKeyUp|GetAxis|GetAxisRaw|GetButton|GetButtonDown|GetButtonUp"
    r"|GetMouseButton|GetMouseButtonDown|GetMouseButtonUp|mousePosition|anyKey|anyKeyDown)\b"
)
RB_VELOCITY = re.compile(r"\.\s*velocity\b")
FIND_DEPRECATED = re.compile(r"\bFindObjectsOfType\b|\bFindObjectOfType\b|\bFindObjectsSortMode\b")
TRANSFORM_MOVE = re.compile(r"\btransform\s*\.\s*(position\s*[+\-]?=(?!=)|Translate\s*\()")
DEBUG_LOG = re.compile(r"\bDebug\s*\.\s*Log\w*\s*\(")
UNITY_OBJECT_CLASS = re.compile(r"class\s+\w+[^{]*:\s*[^{]*\b(MonoBehaviour|ScriptableObject|StateMachineBehaviour)\b")
# HX010: per-manager singleton (static Instance field/property)
SINGLETON_INSTANCE = re.compile(r"\bstatic\s+(?!readonly\b)[\w<>\[\],.?]+\s+Instance\b\s*(\{|=|;)")
# HX011: string-based calls
STRING_CALL = re.compile(
    r"\b(SendMessage|SendMessageUpwards|BroadcastMessage)\s*\("
    r"|\b(Invoke|InvokeRepeating|CancelInvoke|IsInvoking|StartCoroutine|StopCoroutine)\s*\(\s*\""
)
# HX012: async void
ASYNC_VOID = re.compile(r"\basync\s+void\b")
# HX013: info log (warnings/errors are allowed)
DEBUG_INFO_LOG = re.compile(r"\bDebug\s*\.\s*Log\s*\(")
# HX014: static mutable field (not const / readonly / event / method / property)
STATIC_FIELD = re.compile(
    r"^\s*(?:(?:public|private|protected|internal)\s+)*static\s+"
    r"(?!(?:readonly|extern|class|struct|enum|interface|event|delegate|partial|async|implicit|explicit|operator)\b)"
    r"(?:volatile\s+)?[\w<>\[\],.?\s]+?\s+[A-Za-z_]\w*\s*(=[^;]*)?;\s*$"
)
SERVICE_LOCATOR_FILES = ("GameServices.cs",)
LOGGER_FILES = ("GameLog.cs",)
PUBLIC_FIELD = re.compile(
    r"^\s*public\s+(?!(?:static|const|readonly|override|abstract|virtual|event|class|struct|enum|interface"
    r"|delegate|partial|sealed|record|async|unsafe|extern|new|implicit|explicit|operator)\b)"
    r"[\w<>\[\],.?\s]+?\s+[A-Za-z_]\w*\s*(=[^;]*)?;\s*$"
)


def _line_of(code: str, idx: int) -> int:
    return code.count("\n", 0, idx) + 1


def _suppressed(orig_lines: List[str], line: int, rule: str) -> bool:
    for ln in (line, line - 1):
        if 1 <= ln <= len(orig_lines):
            text = orig_lines[ln - 1]
            m = re.search(r"harness-ignore\b(.*)", text)
            if m:
                ids = re.findall(r"HX\d{3}", m.group(1))
                if not ids or rule in ids:
                    return True
    return False


def lint_source(src: str, rel_path: str) -> List[Finding]:
    findings: List[Finding] = []
    orig_lines = src.splitlines()
    no_comments, code = strip_source(src)
    rel_posix = rel_path.replace("\\", "/")
    file_name = rel_posix.rsplit("/", 1)[-1]
    is_gamelayers = file_name == "GameLayers.cs"
    is_editor = "/Editor/" in "/" + rel_posix
    has_rb2d = "Rigidbody2D" in code
    is_unity_obj = bool(UNITY_OBJECT_CLASS.search(code))

    def add(level, rule, idx_or_line, msg, by_line=False, text=None):
        line = idx_or_line if by_line else _line_of(code, idx_or_line)
        if _suppressed(orig_lines, line, rule):
            return
        snippet = orig_lines[line - 1] if 1 <= line <= len(orig_lines) else ""
        findings.append(Finding(level, rule, rel_path, line, msg, snippet))

    # --- hot path rules
    for start, end, name in hot_ranges(code):
        body = code[start:end]
        for rx, label in HOT_LOOKUP:
            for m in rx.finditer(body):
                add(ERROR, "HX001", start + m.start(),
                    f"{name}() 안에서 {label} 호출. Awake에서 캐시한 필드를 쓰세요.")
        for rx, label in HOT_ALLOC:
            for m in rx.finditer(body):
                add(WARN, "HX002", start + m.start(),
                    f"{name}() 안에서 {label} (매 프레임 GC 할당). 버퍼를 필드로 재사용하세요.")
        for m in INTERP.finditer(no_comments[start:end]):
            add(WARN, "HX002", start + m.start(),
                f"{name}() 안에서 문자열 보간 (매 프레임 GC 할당). 값이 바뀔 때만 문자열을 만드세요.")
        for m in DEBUG_LOG.finditer(body):
            add(WARN, "HX009", start + m.start(),
                f"{name}() 안에서 Debug.Log. 매 프레임 로그는 지우거나 조건부로 바꾸세요.")

    # --- whole-file rules
    if not is_gamelayers:
        for m in MAGIC_LAYER.finditer(no_comments):
            add(ERROR, "HX003", m.start(),
                "레이어 이름을 문자열로 사용. GameLayers의 상수/마스크를 쓰세요.")
    for m in LEGACY_INPUT.finditer(code):
        add(ERROR, "HX005", m.start(),
            "구 Input Manager API. Input System(InputActionReference 등)을 쓰세요.")
    if has_rb2d:
        for m in RB_VELOCITY.finditer(code):
            add(ERROR, "HX006", m.start(),
                "Rigidbody2D.velocity는 Unity 6에서 폐기됨. linearVelocity를 쓰세요.")
        for m in TRANSFORM_MOVE.finditer(code):
            add(WARN, "HX008", m.start(),
                "Rigidbody2D가 있는 스크립트에서 transform으로 직접 이동. rb.linearVelocity / MovePosition을 쓰세요.")
    for m in FIND_DEPRECATED.finditer(code):
        add(ERROR, "HX007", m.start(),
            "폐기된 API (FindObjectOfType(s) / FindObjectsSortMode). 참조 주입, 꼭 필요하면 FindFirstObjectByType 또는 FindObjectsByType<T>(FindObjectsInactive).")
    if is_unity_obj:
        for i, line in enumerate(code.splitlines(), start=1):
            if "=>" in line or "(" in line:
                continue
            if PUBLIC_FIELD.match(line):
                add(WARN, "HX004", i,
                    "public 필드. [SerializeField] private + 읽기 전용 프로퍼티로 바꾸세요.", by_line=True)

    # --- design rules (.claude/rules/csharp.md "의존성과 전역 접근" 등)
    if not is_editor:
        if file_name not in SERVICE_LOCATOR_FILES:
            for m in SINGLETON_INSTANCE.finditer(code):
                add(WARN, "HX010", m.start(),
                    "매니저 싱글톤(static Instance). 전역 접근은 GameServices에 등록해서 쓰세요.")
        for m in STRING_CALL.finditer(code):  # 문자열 내용은 지워지고 여는 따옴표는 남아 있음
            add(ERROR, "HX011", m.start(),
                "문자열로 메서드 호출 (SendMessage/Invoke(\"...\")/StartCoroutine(\"...\")). 직접 호출·이벤트·StartCoroutine(Method())를 쓰세요.")
        for m in ASYNC_VOID.finditer(code):
            add(WARN, "HX012", m.start(),
                "async void. async Awaitable + destroyCancellationToken을 쓰세요 (UI 핸들러면 harness-ignore HX012).")
        if file_name not in LOGGER_FILES:
            for m in DEBUG_INFO_LOG.finditer(code):
                add(WARN, "HX013", m.start(),
                    "Debug.Log 직접 사용. GameLog.Info(태그, 메시지)를 쓰세요 (경고·오류는 Debug.LogWarning/LogError 허용).")
        if file_name not in SERVICE_LOCATOR_FILES:
            for i, line in enumerate(code.splitlines(), start=1):
                if "(" in line or "=>" in line or " const " in f" {line} ":
                    continue
                if STATIC_FIELD.match(line) and not SINGLETON_INSTANCE.search(line):
                    add(WARN, "HX014", i,
                        "정적 가변 필드. 도메인 리로드를 끄면 값이 남습니다. 서비스의 인스턴스 필드나 static readonly/const로 바꾸세요.",
                        by_line=True)

    uniq = {}
    for f in findings:
        uniq.setdefault((f.line, f.rule, f.message), f)
    return sorted(uniq.values(), key=lambda f: (f.line, f.rule))


def lint_file(path: Path, cfg: Config) -> List[Finding]:
    return lint_source(read_text(path), cfg.rel(path))


def lint_tree(cfg: Config) -> List[Finding]:
    out: List[Finding] = []
    for root in cfg.scan_roots:
        base = cfg.root / root
        if base.is_dir():
            for p in sorted(base.rglob("*.cs")):
                out.extend(lint_file(p, cfg))
    return out
