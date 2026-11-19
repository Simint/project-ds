---
name: add-structure
description: 플레이어가 설치하는 건설물(블록, 벽, 뒷벽, 발판, 작업대, 상자, 문, 횃불, 침대 등)을 새로 추가하거나 설치 규칙·건설 그리드를 바꿀 때 사용한다.
---

# 건설물 추가

## 0. 종류부터 정한다
`StructureDefinition.kind` 하나를 고른다. 이 값이 레이어·충돌·설치 조건을 결정한다.

| kind | 예 | Sorting Layer | 물리 Layer | 콜라이더 | 기본 설치 조건 |
|---|---|---|---|---|---|
| `Solid` | 나무 블록, 돌벽 | `Terrain` | `Structure` | 셀 크기 BoxCollider2D | 아래 칸 지지 필요 |
| `Platform` | 나무 발판 | `Terrain` | `OneWayPlatform` | 얇은 Box + `PlatformEffector2D` (Use One Way) | 옆이나 아래 칸 지지 필요 |
| `BackWall` | 집 뒷벽 | `BackWall` | (없음) | 없음 | 인접한 BackWall 또는 Solid |
| `Prop` | 작업대, 상자, 침대, 횃불 | `Props` | `Interactable` | 트리거 | 아래 칸 지지 필요 (횃불은 벽 부착 허용) |
| `Door` | 문 | `Props` | 닫힘: `Structure` / 열림: 없음 | Box (상태에 따라 켜고 끔) | 아래 칸 지지 + 세로 2칸 |

목록에 없는 종류가 필요하면 새 kind를 만들기 전에 사용자와 상의한다.

## 1. 정의 데이터
`Assets/_Project/Data/Structures/<id>.asset` — `StructureDefinition`
- `id` (예: `wood_block`), `kind`, `prefab`, `footprint` (셀 단위 `Vector2Int`, 기준은 **왼쪽 아래 셀**)
- `buildCost`: 재료 아이템 id와 수량, `maxHealth`(부술 수 있다면), `dropOnBreak`
- 인벤토리에서 들고 설치하는 방식이면 해당 `ItemDefinition.placeable`에 이 정의를 연결한다 (`add-item` 참고).

## 2. 프리팹
`Assets/_Project/Prefabs/Structures/<id>.prefab`
- 루트 위치 = footprint 왼쪽 아래 셀의 **왼쪽 아래 모서리**. 스프라이트 피벗은 Bottom Center이므로 `Visual` 자식을 x로 `footprint.x * 0.5`만큼 옮겨 맞춘다.
- `StructureInstance` 컴포넌트 (정의 참조, HP, 셀 좌표). `PersistentId`는 붙이지 않는다. 설치된 구조물은 셀 좌표 + 정의 id로 저장한다.
- 레이어와 콜라이더는 0절 표를 따른다.
- `Solid` 블록이 많아지면 개별 프리팹 대신 **구조물 전용 Tilemap**(`StructuresTilemap`, Sorting `Terrain`, TilemapCollider2D + CompositeCollider2D, 물리 `Structure`)에 타일로 찍는 방식을 고려한다. 이 결정은 사용자와 상의한다.

## 3. BuildGrid 규칙
- 모든 좌표 변환은 `BuildGrid.WorldToCell` / `CellToWorld`로만 한다. `Mathf.Floor`로 직접 계산하지 않는다.
- 점유 확인: footprint의 모든 셀이 비어 있고, 지형 타일(`Ground`)과 겹치지 않고, 플레이어·몬스터 콜라이더와 겹치지 않아야 한다 (`Physics2D.OverlapBox` + `GameLayers.ActorMask`).
- 지지 확인은 kind별 규칙 함수(`PlacementRules`)에 모은다. 프리팹이나 MonoBehaviour에 흩어 놓지 않는다.
- 구조물을 부수면 위에 얹힌 구조물의 지지를 다시 검사한다 (연쇄 붕괴 여부는 사용자에게 확인).

## 4. 미리보기
- `PlacementPreview`가 같은 스프라이트를 반투명(알파 0.5)으로 그린다. 가능: 초록 틴트, 불가: 빨강 틴트.
- 미리보기에는 콜라이더를 두지 않는다. Sorting Layer는 실제와 같게, Order +1.

## 5. 저장
`WorldSaveData.hub.structures`에 `{ id, cell, health, state }`. 문 열림 같은 상태는 `state` 문자열/정수 하나로 표현한다.
- 건설은 Hub(은신처) 맵에서만 한다. `BuildGrid`는 Hub 씬에만 있고, `HubBuildArea` 범위 밖에는 설치할 수 없다 (`ARCHITECTURE.md` 4.5).
- 지역 맵에 놓는 잠긴 문·부술 수 있는 벽·다리는 건설물이 아니다. `PersistentId` + `IPersistentObject` 맵 오브젝트로 만든다 (`ARCHITECTURE.md` 3.4).

## 6. 사용자 체크리스트로 남길 것
1. 스프라이트를 `Art/Structures/`에 넣기 (16px 격자에 딱 맞게)
2. 정의 에셋 생성, 프리팹 생성, 레이어·콜라이더 지정
3. 플레이: 설치 가능/불가 위치에서 틴트 확인, 설치 후 충돌 확인 (Solid는 막힘, Platform은 아래에서 뚫고 올라감)
4. 저장 → 불러오기 후 같은 셀에 복원되는지 확인
