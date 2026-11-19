---
name: add-harvestable
description: 횡스크롤 맵에 새 채집물(나무, 바위, 덤불, 광석, 풀 등)을 추가하거나 기존 채집물의 드롭·도구 조건·리스폰을 바꿀 때 사용한다. 배경/전경 장식용 나무처럼 채집하지 않는 오브젝트를 놓을 때도 이 스킬의 '장식 오브젝트' 절을 따른다.
---

# 채집물 추가

## 0. 먼저 확인할 것 (모르면 사용자에게 묻는다)
- 채집 가능한가, 장식인가? 장식이면 6절로 간다.
- 필요한 도구 종류와 최소 등급 (예: 도끼 1등급 이상, 맨손 가능)
- HP(몇 번 때려야 하나), 드롭(아이템 id, 수량 범위, 확률)
- 리스폰 여부와 기간(게임 내 일수)
- 스프라이트: 기본 / 채집 후(그루터기 등) / 피격 흔들림 여부

## 1. 정의 데이터
`Assets/_Project/Data/Harvestables/<id>.asset` — `HarvestableDefinition`
- `id`: 소문자 스네이크, 한 번 정하면 바꾸지 않음 (`oak_tree`)
- `requiredTool`, `minToolTier`, `maxHealth`, `drops[]`, `respawnDays` (0 = 리스폰 없음)
- 드롭에 쓰이는 아이템이 아직 없으면 `add-item` 스킬로 먼저 만든다.

## 2. 프리팹 구조
`Assets/_Project/Prefabs/Props/<id>.prefab`
```
oak_tree (root)                 Layer: Interactable
├─ Harvestable                  definition = oak_tree.asset
├─ PersistentId                 (프리팹에서는 비워 둔다. 씬에 놓을 때 자동 생성)
├─ BoxCollider2D (isTrigger)    상호작용 범위. 밑동 주변만 (폭 1~1.5칸, 높이 1.5~2칸)
└─ Visual (child)
   └─ SpriteRenderer            Sorting Layer: Props, Order 0
```
- **몸통 콜라이더는 없다.** 채집 나무는 플레이어가 통과한다 (Props 레이어는 플레이어 뒤에 그려짐).
- 피벗은 스프라이트 발밑 (Art/Props 폴더면 자동). 루트 위치 = 땅 표면.
- 큰 바위처럼 **플레이어를 막거나 올라설 수 있어야 하는** 채집물이라면 자식 `Body`(물리 Layer `Ground`, Sorting은 그대로 `Props`)에 비트리거 콜라이더를 추가한다. 이 경우 사용자에게 의도를 먼저 확인한다.
- 흔들림·피격 플래시는 `Visual` 자식에만 적용해서 트리거 위치가 흔들리지 않게 한다.

## 3. 코드가 필요하면
- 공통 동작은 `Harvestable` 하나로 처리한다. 특정 채집물 전용 서브클래스는 만들지 않는다.
- 특수 동작(예: 벌집을 치면 벌이 나옴)은 `IHarvestEffect` 컴포넌트를 추가하는 방식으로.
- 데미지 계산은 `HarvestableDefinition`과 도구의 `ToolData`만으로 한다. 레이어 문자열 금지, `GameLayers` 사용.

## 4. 저장
- `Harvestable`은 `IPersistentObject`를 구현한다. 상태(남은 HP는 저장하지 않음, `Depleted` 여부와 리스폰 예정 시각만)를 `PersistentId`로 `MapStateRegistry`에 기록하고, 저장 파일에서는 `WorldSaveData.maps[].objects`에 들어간다 (`ARCHITECTURE.md` 3.4).
- 리스폰은 `Restore(rec, nowMinute)`에서 시각을 비교해 판단한다. 방문하지 않은 맵에서는 아무것도 돌리지 않는다.
- 겨울 채집 제한이 있는 식물이면 정의에 가능 계절을 둔다 (`docs/design/GAME_DESIGN.md`).
- 씬에 여러 개 복제한 뒤 `python tools/harness/validate.py --only ids`로 ID 중복을 확인한다.

## 5. 사용자 체크리스트로 남길 것
1. 스프라이트를 `Assets/_Project/Art/Props/`에 넣기 (자동 임포트 설정 확인)
2. 프리팹 만들고 위 구조대로 컴포넌트 연결
3. 씬에 배치 → `PersistentId`에 ID가 생겼는지 확인
4. 플레이해서 채집 → 드롭 → 저장/불러오기 후 상태 유지 확인

## 6. 장식 오브젝트 (채집하지 않음)
같은 나무 스프라이트라도 **배치한 인스턴스의 Sorting Layer**로 앞뒤를 정한다.
- 플레이어 뒤에 보이는 배경 나무 → `Background` (멀수록 패럴랙스 계수 큼, 색을 약간 어둡게/채도 낮게)
- 플레이어를 가리는 전경 나무·덤불 → `Foreground` + `ForegroundFader`(트리거, 알파 0.4)
- 장식에는 `Harvestable`, `PersistentId`, 콜라이더를 붙이지 않는다 (`ForegroundFader` 트리거 제외).
