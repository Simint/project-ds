# AGENTS.md (Codex 등 Claude Code 외 에이전트용)

이 프로젝트의 규칙 원본은 **`CLAUDE.md`**다. 이 파일은 Codex가 그 규칙을 따르도록 연결하는 다리다.
규칙을 바꿀 때는 `CLAUDE.md`를 고치고, 여기에는 Codex에서만 다른 점만 적는다. 둘의 내용이 다르면 `CLAUDE.md`가 맞다.

응답과 커밋 메시지는 한국어로 쓴다.

## 1. 작업 전에 반드시 읽을 것

Claude Code는 `CLAUDE.md` 안의 `@docs/...` 줄을 자동으로 불러오지만 Codex는 그렇지 않다. 아래 문서를 직접 읽는다.

1. `CLAUDE.md` — 절대 규칙 8개, 작업 절차, 검증 방법, 비공개 서브모듈 규칙. **모두 이 에이전트에도 그대로 적용된다.**
2. `docs/RENDERING_RULES.md` — Sorting Layer, 물리 Layer, 임포트, 카메라 규칙
3. `docs/ARCHITECTURE.md` — 새 스크립트나 새 시스템을 만들기 전에
4. `docs/design/GAME_DESIGN.md` — 아이템·적·맵을 만들거나 기획이 필요할 때 (비공개 서브모듈. 비어 있으면 `git submodule update --init`, 권한이 없으면 사용자에게 묻는다)

## 2. 파일 종류별 규칙

Claude Code는 아래 규칙을 해당 파일을 열 때 자동으로 불러온다. Codex에서는 **해당 파일을 고치기 전에 직접 읽는다.**

| 고치려는 파일 | 먼저 읽을 규칙 |
|---|---|
| `Assets/**/*.cs` | `.claude/rules/csharp.md` |
| `.unity`, `.prefab`, `.asset`, `.meta` (Unity YAML) | `.claude/rules/unity-yaml.md` |
| `Assets/_Project/Art/**` 텍스처와 그 `.meta` | `.claude/rules/art-import.md` |

## 3. 권한 규칙

Claude Code에서는 `.claude/settings.json`이 아래를 강제한다. Codex에서는 강제되지 않으므로 직접 지킨다.

- **읽지도 고치지도 않는다:** `Library/`, `Temp/`, `Logs/`, `obj/`
- **고치기 전에 사용자에게 무엇을 바꿀지 말하고 확인받는다:**
  - `ProjectSettings/**`
  - `Packages/manifest.json`
  - `Assets/**/*.unity`, `Assets/**/*.prefab`, `Assets/**/*.meta`
  - `Assets/_Project/Editor/Harness/**`
  - `harness.config.json`
- **비공개 서브모듈 경계:** 이 저장소는 공개다. 아트·오디오·기획 파일은 `Assets/_Project/Art/`, `Assets/_Project/Audio/`, `docs/design/` 밖에 두지 않는다. 공개 파일과 커밋 메시지에 기획 세부를 쓰지 않는다 (`CLAUDE.md`의 "비공개 서브모듈" 절).

## 4. 스킬

- Codex용 스킬은 `.agents/skills/`에 있다. 이 폴더는 `.claude/skills/`의 **사본**이다.
- 스킬을 고칠 때는 `.claude/skills/`를 고치고 아래 명령으로 동기화한다. `.agents/skills/`를 직접 고치지 않는다.

  ```bash
  python tools/harness/sync_agent_skills.py
  ```

- `python tools/harness/sync_agent_skills.py --check`는 두 폴더가 어긋났는지만 확인한다 (어긋나면 종료 코드 1).
- `CLAUDE.md`의 "작업 절차"에 적힌 스킬(`add-harvestable`, `add-structure`, `add-item`, `add-survival-stat`, `tune-movement`, `debug-layering`)을 같은 상황에서 쓴다.
- Claude Code 플러그인 스킬(`unity:2d-pixel-perfect` 등)은 Codex에 없다.

## 5. 리뷰어 서브에이전트

`CLAUDE.md`가 "큰 변경을 끝낸 뒤 `unity-reviewer` 서브에이전트로 리뷰"라고 할 때, Codex에서는 `.codex/agents/unity-reviewer.toml`에 정의된 `unity-reviewer` 에이전트를 쓴다. 이 에이전트는 코드를 고치지 않고 보고만 한다.

## 6. 자동 검사 훅

- `.codex/hooks.json`이 파일 수정(`apply_patch`) 뒤에 `.claude/hooks/post_edit.py`를 실행한다. 이 스크립트는 Claude와 Codex 입력을 모두 읽는다.
- 처음 한 번은 사용자가 이 프로젝트를 **trusted**로 지정하고, `/hooks`에서 훅을 승인해야 실행된다. 승인 전에는 훅이 돌지 않으므로, 파일을 고친 뒤 직접 검증기를 실행한다.
- 훅이 오류를 보고하면 다음 작업으로 넘어가기 전에 고친다. 의도된 예외라면 해당 줄 끝에 `// harness-ignore HX00N 이유`를 단다.

## 7. 검증

```bash
python tools/harness/validate.py
python tools/harness/validate.py --only csharp
python tools/harness/validate.py path/to/File.cs
```

Unity 에디터가 켜져 있으면 Unity Pipeline으로 컴파일까지 직접 확인할 수 있다 (`.agents/skills/unity-pipeline/SKILL.md`).

```bash
unity status
unity command script recompile
unity command script recompile_status
unity command console status
```

`console status`의 `consoleErrors`가 0이고 `compilationFailed`가 false여야 한다. 에디터가 꺼져 있으면 `CLAUDE.md`의 "검증" 절대로 사용자에게 Unity Console 확인을 요청하고, 씬에서 해야 할 설정을 번호 매긴 체크리스트로 남긴다.
