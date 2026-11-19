---
paths:
  - "**/*.unity"
  - "**/*.prefab"
  - "**/*.asset"
  - "**/*.meta"
  - "ProjectSettings/**"
---

# Unity YAML 파일 규칙

이 파일들은 Unity가 관리한다. 사람이 에디터에서 하는 것이 기본이고, 에이전트가 직접 고치는 것은 예외다.

## 직접 고쳐도 되는 경우 (사용자에게 먼저 말하고)
- 기존 컴포넌트의 **단순 값** 변경: 숫자, bool, 열거형 정수, 문자열.
  - 예: `m_SortingOrder`, `m_SpriteSortPoint`, `m_Mode`, 텍스처 `.meta`의 `filterMode`, `spritePixelsToUnits`.
- ScriptableObject 에셋(`.asset`)의 직렬화된 필드 값.

## 직접 고치지 않는 것
- `guid`, `fileID`, `m_Script`, `m_PrefabInstance`, `m_CorrespondingSourceObject` 등 참조 값.
- 컴포넌트·GameObject 추가·삭제, 계층 구조 변경, 프리팹 오버라이드 편집.
- `ProjectSettings/TagManager.asset`의 레이어 목록 → 대신 사용자에게 `Tools > Harness > Apply Project Settings` 실행을 요청.

이런 변경이 필요하면 **사용자가 에디터에서 할 수 있는 번호 매긴 체크리스트**를 만든다. 또는 같은 일을 하는 에디터 스크립트(`Assets/_Project/Editor/`)를 짜서 메뉴로 실행하게 한다.

## 값 참고
- SpriteRenderer `m_SpriteSortPoint`: 0 = Center, 1 = Pivot
- TilemapRenderer `m_Mode`: 0 = Chunk, 1 = Individual
- 텍스처 `.meta` `filterMode`: 0 = Point, 1 = Bilinear
- 텍스처 `.meta` `textureType`: 8 = Sprite
- 텍스처 `.meta` platformSettings `textureCompression`: 0 = None

고친 뒤에는 `python tools/harness/validate.py <파일>`로 확인한다 (훅이 자동으로도 돌린다).
