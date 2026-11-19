# 횡스크롤 픽셀 생존게임 하네스 (Claude Code용)

Unity 6 + URP 2D로 횡스크롤 픽셀 생존게임을 만들 때, AI 코딩 에이전트(Claude Code)가 프로젝트 규칙을 지키게 하는 설정 묶음입니다.
규칙을 **문서로 알려 주고**(CLAUDE.md, rules, skills), 어기면 **자동으로 잡아냅니다**(훅, 검증기, 에디터 도구).

## 설치

1. 압축을 풀어 **Unity 프로젝트 루트**(Assets 폴더가 있는 곳)에 그대로 복사합니다.
2. Unity를 열면 `Assets/_Project/` 스크립트가 컴파일됩니다. Console에 오류가 없는지 확인하세요.
3. Unity 메뉴 **Tools > Harness > Apply Project Settings** 실행
   - Sorting Layer 10개 생성·정렬, 물리 Layer 9개 생성, 충돌 매트릭스 설정, Transparency Sort Mode = Default
4. 터미널에서 프로젝트 루트로 가서 확인합니다.
   ```bash
   python3 tools/harness/validate.py
   ```
5. 프로젝트 루트에서 `claude`를 실행하면 하네스가 적용된 상태로 시작합니다.

### Windows 사용자
`python3` 명령이 없으면 `.claude/settings.json`의 훅 명령을 `python` 또는 `py`로 바꾸세요.
```json
"command": "python \"${CLAUDE_PROJECT_DIR}/.claude/hooks/post_edit.py\""
```
Python 3.8 이상이 필요합니다. 외부 패키지는 쓰지 않습니다.

### Unity 설정 전제
- Project Settings > Editor > Asset Serialization Mode = **Force Text** (기본값). 검증기가 YAML을 직접 읽습니다.
- Input System 패키지, URP(2D Renderer), Cinemachine 설치 (Universal 2D 템플릿이면 대부분 이미 있음).

## 구성

```
CLAUDE.md                     에이전트가 매번 읽는 프로젝트 규칙 (절대 규칙 8개, 작업 절차)
harness.config.json           PPU, 레이어 목록 등 기준값 (검증기·에디터 도구가 함께 읽음)
docs/
  RENDERING_RULES.md          Sorting Layer, 물리 Layer, 임포트, 카메라, 패럴랙스, 조명 규칙
  ARCHITECTURE.md             폴더 구조와 시스템 설계 (플레이어, 채집, 생존, 제작, 건설, 저장)
.claude/
  settings.json               권한(Library 차단, 씬·프리팹·ProjectSettings 수정 시 확인) + 훅
  hooks/post_edit.py          파일을 고칠 때마다 자동 검사
  rules/                      파일 종류별 규칙 (해당 파일을 열 때만 로드)
    csharp.md  unity-yaml.md  art-import.md
  skills/                     반복 작업 절차
    add-harvestable/          채집물 (나무, 바위) + 배경/전경 장식
    add-structure/            건설물 (블록, 발판, 뒷벽, 작업대, 문)
    add-item/                 아이템, 레시피
    add-survival-stat/        허기, 체온 등
    tune-movement/            점프·이동 느낌
    debug-layering/           앞뒤 순서, 안 보임, 충돌 문제 진단
  agents/unity-reviewer.md    독립 리뷰 서브에이전트 (코드 수정 없이 보고만)
tools/harness/                검증기 (Unity 없이 실행)
Assets/_Project/
  Scripts/Core/GameLayers.cs  레이어 이름·마스크의 유일한 출처
  Scripts/Core/PersistentId.cs 고정 맵 오브젝트 저장용 고유 ID
  Editor/Harness/             임포트 자동 설정, 프로젝트 설정 적용, 씬 검사 메뉴
```

## Sorting Layer 한눈에 보기 (뒤 → 앞)

`Sky` → `FarBackground` → `Background` → `BackWall` → `Props` → `Terrain` → `Entities` → `Foreground` → `Effects` → `UI`

- 배경 나무 = `Background`, 채집 나무 = `Props`, 전경 나무 = `Foreground`
- 같은 스프라이트라도 **놓은 인스턴스의 레이어**로 앞뒤가 정해집니다. Y정렬은 쓰지 않습니다.

## 검사 규칙

| 번호 | 수준 | 내용 | 어디서 |
|---|---|---|---|
| HX001 | 오류 | Update 계열에서 GetComponent / Find / Camera.main | 훅, 검증기 |
| HX002 | 경고 | Update 계열에서 컬렉션·배열 new, LINQ, 문자열 보간 | 훅, 검증기 |
| HX003 | 오류 | 레이어 이름 문자열 사용 (GameLayers 밖) | 훅, 검증기 |
| HX004 | 경고 | MonoBehaviour/ScriptableObject의 public 필드 | 훅, 검증기 |
| HX005 | 오류 | 구 Input Manager (`Input.GetKey` 등) | 훅, 검증기 |
| HX006 | 오류 | `Rigidbody2D.velocity` (Unity 6 폐기) | 훅, 검증기 |
| HX007 | 오류 | `FindObjectOfType(s)`, `FindObjectsSortMode` (폐기) | 훅, 검증기 |
| HX008 | 경고 | Rigidbody2D 오브젝트를 transform으로 이동 | 훅, 검증기 |
| HX009 | 경고 | Update 계열의 Debug.Log | 훅, 검증기 |
| HX101~105 | 오류/경고 | 텍스처 타입, 필터, 밉맵, 압축, PPU | 훅, 검증기 |
| HX201 | 오류 | Transparency Sort Mode = Custom Axis | 훅, 검증기 |
| HX202 | 오류 | Sorting Layer 누락·순서 다름 | 검증기 |
| HX203 | 경고 | 렌더러가 Default Sorting Layer | 훅, 검증기, 에디터 |
| HX204 | 오류 | 삭제된 Sorting Layer 참조 | 훅, 검증기, 에디터 |
| HX205 | 경고 | 물리 Layer 누락 | 검증기 |
| HX206 | 경고 | Props 오브젝트에 비트리거 콜라이더 | 에디터 |
| HX207 | 경고 | 플레이어 Rigidbody2D Interpolate/Continuous 꺼짐 | 에디터 |
| HX301 | 오류 | PersistentId 중복 | 훅, 검증기, 에디터 |
| HX302 | 경고 | PersistentId 비어 있음 | 검증기, 에디터 |

- **훅**: Claude가 파일을 고칠 때마다 자동 실행. 오류가 있으면 Claude에게 바로 보여 주고 고치게 합니다.
- **검증기**: `python3 tools/harness/validate.py` (전체, `--only csharp` 등 그룹 선택, 파일 지정 가능)
- **에디터**: Unity 메뉴 `Tools > Harness > Validate Open Scenes` (Console 로그를 클릭하면 해당 오브젝트로 이동)
- 의도된 예외: 해당 줄 끝에 `// harness-ignore HX002 이유`

C# 검사는 정규식 기반 휴리스틱입니다. 완벽한 파서는 아니라서 드물게 오탐·누락이 있을 수 있습니다.

## 바꾸고 싶을 때

- **PPU, 해상도, 레이어 목록**: `harness.config.json` → `GameLayers.cs` → `docs/RENDERING_RULES.md` 순서로 함께 고치고 `Apply Project Settings`를 다시 실행.
- **충돌 조합**: `HarnessProjectSetup.cs`의 `CollidingPairs`와 `RENDERING_RULES.md` 3장.
- **새 반복 작업**: `.claude/skills/<이름>/SKILL.md`를 추가하고 `CLAUDE.md`의 작업 절차 목록에 한 줄 추가.
