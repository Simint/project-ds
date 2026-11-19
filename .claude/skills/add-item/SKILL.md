---
name: add-item
description: 새 아이템(재료, 도구, 무기, 음식, 설치형 아이템)이나 제작 레시피를 추가·수정할 때 사용한다. 드롭 테이블이나 레시피가 아직 없는 아이템 id를 참조할 때도 먼저 이 스킬로 아이템을 만든다.
---

# 아이템·레시피 추가

## 1. ItemDefinition
`Assets/_Project/Data/Items/<id>.asset`
- `id`: 소문자 스네이크 (`wood_log`, `stone_axe`, `cooked_meat`). **저장 파일이 이 값을 쓰므로 한 번 정하면 바꾸지 않는다.** 이름을 바꾸고 싶으면 `displayName`만 바꾼다.
- `displayName`, `description`, `icon` (Art/Items, 16×16, Center 피벗)
- `maxStack`: 재료 99, 도구·무기 1, 음식 20 기본
- `category`: Material / Tool / Weapon / Food / Placeable / Misc
- 선택 데이터 (해당하는 것만 채운다)
  - `tool`: 종류(Axe, Pickaxe, Hammer...), 등급(int), 채집 데미지
  - `weapon`: 데미지, 공격 간격, 사거리
  - `consumable`: 스탯 id → 회복량 목록 (`SurvivalStatDefinition.id` 사용)
  - `placeable`: 설치할 `StructureDefinition`

새 선택 데이터 종류가 필요하면 `ItemDefinition`에 필드를 늘리기 전에 사용자와 상의한다.

## 2. 아이템 등록
- 모든 `ItemDefinition`은 `ItemDatabase` 에셋 하나에 등록한다 (id → 정의 조회, 저장 불러오기에 필요).
- 등록 누락을 막기 위해 `ItemDatabase`에는 에디터에서 `Data/Items` 폴더를 자동 수집하는 버튼(`Refresh`)을 둔다. 새 아이템을 만든 뒤 사용자에게 눌러 달라고 체크리스트에 적는다.
- id 중복은 `ItemDatabase.OnValidate`에서 경고한다.

## 3. 레시피
`Assets/_Project/Data/Recipes/<결과id>.asset` — `RecipeDefinition`
- `ingredients`: 아이템 id + 수량
- `result`: 아이템 id + 수량
- `station`: 필요한 작업대 종류 (None = 손 제작)
- `unlock`: 처음부터 / 아이템 획득 시 / 기타 (사용자에게 확인)

## 4. 떨어진 아이템
- 월드에 떨어지는 아이템은 공용 `Pickup` 프리팹 하나를 `ObjectPool`로 재사용한다. 아이템마다 프리팹을 만들지 않는다.
- `Pickup`: Sorting `Entities`, 물리 `Pickup`, Rigidbody2D(작은 튕김) + 트리거 흡수 범위.

## 5. 균형 수치를 정할 때
수치는 임의로 정하지 말고 근거를 남긴다. 예: "도끼 1등급 채집 데미지 3, 참나무 HP 9 → 3번에 채집". 사용자에게 표로 보여 주고 확인받는다.
