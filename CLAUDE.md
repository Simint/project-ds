# 프로젝트: 2D 횡스크롤 픽셀 생존게임 (Unity 6, URP 2D)

싱글 플레이 생존게임. 은신처(거점)와 손으로 만든 여러 지역·실내 맵을 오가며 채집 → 제작 → 건설(은신처에서만) → 생존(허기·체온·방사능 등)을 반복한다.
응답과 커밋 메시지는 한국어로 쓴다.

@docs/RENDERING_RULES.md

게임 기획은 `docs/design/GAME_DESIGN.md`에 있다. **아이템·적·맵을 만들거나 새 시스템을 기획하기 전에 읽는다.** `[미정]` 항목은 추측하지 말고 사용자에게 묻는다.

### 비공개 서브모듈

이 저장소(`Simint/project-ds`)는 공개 저장소다. 아래 네 폴더는 각각 비공개 저장소이고, git 서브모듈로 연결되어 있다.

| 폴더 | 저장소 |
|---|---|
| `Assets/_Project/Art/` | `Simint/project-ds-art` |
| `Assets/_Project/Audio/` | `Simint/project-ds-audio` |
| `Assets/_Project/Data/` | `Simint/project-ds-data` |
| `docs/design/` | `Simint/project-ds-design` |

- 아트·오디오 파일, 정의 데이터 에셋(ScriptableObject `.asset`), 그 `.meta`, 기획 문서는 **반드시 이 폴더 안에** 둔다. 이 폴더 밖에 두면 공개된다.
- 이 폴더 안을 바꾸면 **서브모듈 안에서 먼저 커밋·push**하고, 바깥 저장소에서 서브모듈 포인터를 커밋한다.
- 공개 저장소에 넣는 파일(코드, `docs/ARCHITECTURE.md`, CLAUDE.md, 스킬 등)에는 기획의 세부 내용(세계관, 스토리, 수치 밸런스)을 적지 않는다. 필요하면 `docs/design/`을 가리키기만 한다.
- 밸런스 수치의 실제 값은 `Assets/_Project/Data/`의 에셋에만 둔다. 코드 필드 기본값, 테스트 상수, 공개 씬·프리팹의 인스펙터 값에 실제 값을 넣지 않는다(코드 기본값은 0이나 중립 값, 테스트는 임의 값).
- 받는 법: `git clone --recurse-submodules`, 이미 받았다면 `git submodule update --init`

### 다른 에이전트(Codex)와 함께 쓰기

- `AGENTS.md`는 Codex가 이 파일의 규칙을 따르게 하는 연결 문서다. 규칙의 원본은 이 파일이다. 규칙을 바꾸면 `AGENTS.md`의 Codex 전용 부분(권한, 경로별 규칙 표)도 맞는지 확인한다.
- `.claude/skills/`를 고치면 `python tools/harness/sync_agent_skills.py`로 `.agents/skills/`(Codex용 사본)를 갱신한다. `.agents/skills/`는 직접 고치지 않는다.
- `.claude/hooks/post_edit.py`는 Claude와 Codex(`.codex/hooks.json`)가 함께 쓴다. 고칠 때 두 입력 형식(`file_path`, `apply_patch` 패치 텍스트)을 모두 유지한다.
- `.claude/agents/unity-reviewer.md`를 고치면 `.codex/agents/unity-reviewer.toml`도 같이 고친다.

시스템 구조, 폴더 위치, 데이터 흐름은 `docs/ARCHITECTURE.md`에 있다. **새 스크립트나 새 시스템을 만들기 전에 반드시 읽는다.**
수치 기준(PPU, 레이어 목록 등)의 원본은 `harness.config.json`이다.

## 절대 규칙

1. **횡스크롤이다. Y정렬을 쓰지 않는다.** 앞뒤는 Sorting Layer로 정한다. Transparency Sort Mode를 Custom Axis로 바꾸지 않는다.
2. **레이어 이름을 문자열로 쓰지 않는다.** Sorting Layer, 물리 Layer, LayerMask는 모두 `Assets/_Project/Scripts/Core/GameLayers.cs`에서 가져온다. 새 레이어가 필요하면 `harness.config.json` → `GameLayers.cs` → `RENDERING_RULES.md` 순서로 함께 고치고, 사용자에게 Unity에서 `Tools > Harness > Apply Project Settings`를 실행하라고 알린다.
3. **정의는 ScriptableObject, 상태는 C# 클래스.** 런타임에 ScriptableObject 필드를 바꾸지 않는다. 저장은 `id` 문자열로 한다.
4. **물리 오브젝트는 Rigidbody2D로 움직인다.** `rb.linearVelocity`를 쓴다 (`velocity`는 Unity 6에서 폐기됨). `transform.position`을 직접 바꾸지 않는다.
5. **입력은 Input System만.** `Input.GetKey`, `Input.GetAxis` 금지.
6. **Hot path(Update 계열)에서 할당·탐색 금지.** `GetComponent`, `Find*`, `Camera.main`, LINQ, `new List<>` 등은 `Awake`에서 캐시한다.
7. **Unity YAML(.unity, .prefab, .asset, .meta)은 직접 고치지 않는 것이 기본이다.** 꼭 필요하면 `.claude/rules/unity-yaml.md`를 따르고, 먼저 사용자에게 무엇을 바꿀지 말한다. `guid`와 `fileID`는 절대 바꾸지 않는다.
8. **`Library/`, `Temp/`, `Logs/`, `obj/`는 읽지도 고치지도 않는다.**

## 작업 절차

- 새 채집물(나무, 바위, 덤불) → `add-harvestable` 스킬
- 새 건설물(블록, 벽, 발판, 작업대, 문) → `add-structure` 스킬
- 새 아이템·도구·음식 → `add-item` 스킬
- 새 생존 스탯 → `add-survival-stat` 스킬
- 점프·이동 느낌 조정 → `tune-movement` 스킬
- "앞뒤가 이상하다", "안 보인다", "가려진다" → `debug-layering` 스킬
- 큰 변경을 끝낸 뒤 → `unity-reviewer` 서브에이전트로 리뷰

## 검증

에이전트는 `unity-pipeline` 스킬(`unity command ...`)로 Unity 에디터에 직접 연결해 컴파일·Console·Play Mode·씬 작업을 확인할 수 있다. 정적 규칙 검사는 별도로 Python 하네스를 쓴다.

```bash
python tools/harness/validate.py            # 전체 검사 (임포트, 레이어, 프리팹·씬, 중복 ID)
python tools/harness/validate.py --only csharp
python tools/harness/validate.py path/to/File.cs   # 파일 하나
```

- `.cs`, `.prefab`, `.unity`, `.meta` 파일을 고치면 `post_edit` 훅이 자동으로 검사한다. **훅이 오류를 보고하면 다음 작업으로 넘어가기 전에 고친다.** 의도된 예외라면 해당 줄 끝에 `// harness-ignore HX00N 이유`를 단다.
- 컴파일·Console 확인은 에이전트가 직접 한다: `unity command script recompile` → `script recompile_status`로 완료 대기 → `console status`/`console get --level error`로 오류 0건을 확인한다. 오류가 있으면 다음 작업으로 넘어가기 전에 고친다.
- 런타임 동작(맵 이동, 상호작용 등)을 바꿨으면 Play Mode(`editor.playmode start`)에 들어가 실제로 확인한다. 테스트가 끝나면 Play Mode를 종료한다.
- 씬·프리팹 생성/배치는 `unity-pipeline`의 `script run`(Editor 전용 스크립트를 `Assets/_Project/Editor/`에 두고 메뉴 항목으로 실행)으로 만든다. 에셋을 새로 만든 직후 **같은 호출 안에서** `EditorSceneManager.NewScene(..., Single)`을 부르면 방금 만든 ScriptableObject 참조가 다음 `AssetDatabase.LoadAssetAtPath`에서 끊기는 경우가 있었다 — `NewSceneMode.Additive` + 끝에 명시적 `CloseScene`을 쓰고, 같은 경로의 씬이 이미 열려 있으면(`SaveScene`이 "Overwriting the same path" 오류로 조용히 실패) 먼저 닫는다. 작업이 끝나면 씬 작성용 임시 Editor 스크립트는 지운다.
- 사람만 할 수 있는 작업(Unity 창 포커스가 필요한 모달 승인 등)이 남으면 **번호 매긴 체크리스트**로 알린다.

## 코드 스타일 요약

자세한 내용은 `.claude/rules/csharp.md` (C# 파일을 열면 자동 로드).
- 네임스페이스 `Game.<폴더명>` (예: `Game.Survival`).
- 인스펙터 노출은 `[SerializeField] private`. `public` 필드 금지 (프로퍼티로 노출).
- 한 파일에 한 MonoBehaviour. 파일 이름 = 클래스 이름.
- 설계: SOLID(Unity판), 상황별 디자인 패턴, 조합 우선(상속은 추상 기반 1단계까지), 이벤트는 C# `event`, 비동기는 `Awaitable`, 로그는 `GameLog`.
- 전역 접근은 `GameServices` 하나만. 매니저마다 싱글톤 금지. 시간은 `IWorldClock`으로 받는다.
