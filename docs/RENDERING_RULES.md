# 렌더링·레이어 규칙 (횡스크롤 픽셀 아트)

이 문서는 화면에 무엇이 어떤 순서로 그려지고, 무엇이 무엇과 부딪히는지에 대한 **유일한 기준**이다.
수치의 원본은 `harness.config.json`이다. 둘이 다르면 config가 맞고, 이 문서를 고친다.

## 1. 기본 수치

| 항목 | 값 |
|---|---|
| 타일 크기 | 16×16 px |
| Pixels Per Unit | 16 (타일 1칸 = 1 유닛) |
| Pixel Perfect Camera Reference Resolution | 320×180 (16:9, 정수배 업스케일) |
| 렌더 파이프라인 | URP, 2D Renderer |
| Transparency Sort Mode | **Default** (Custom Axis 금지) |

## 2. Sorting Layer (뒤 → 앞)

횡스크롤에서 앞뒤 관계는 **어느 레이어에 두느냐로만 결정**한다. Y정렬(Custom Axis 0,1,0)은 점프할 때마다 순서가 바뀌므로 절대 쓰지 않는다.

| 순서 | Sorting Layer | 무엇을 두나 | 콜라이더 |
|---|---|---|---|
| 1 | `Sky` | 하늘, 해·달, 구름 | 없음 |
| 2 | `FarBackground` | 먼 산, 먼 숲 실루엣 (패럴랙스) | 없음 |
| 3 | `Background` | **배경 나무**, 배경 건물, 장식 (패럴랙스 약하게 또는 고정) | 없음 |
| 4 | `BackWall` | 건물 뒷벽, 동굴 벽 (테라리아의 "벽") | 없음 |
| 5 | `Props` | **채집 나무**, 바위, 덤불, 작업대, 상자, 문, 가구 | 트리거만 (문·상자는 예외 가능) |
| 6 | `Terrain` | 땅 타일, 발판, 플레이어가 지은 **단단한 블록** | 실제 충돌 |
| 7 | `Entities` | 플레이어, 몬스터, NPC, 떨어진 아이템, 투사체 | 실제 충돌 |
| 8 | `Foreground` | **전경 나무**, 앞쪽 풀잎, 덤불, 기둥 | 없음 (반투명 트리거만) |
| 9 | `Effects` | 파티클, 피격 이펙트, 데미지 숫자 | 없음 |
| 10 | `UI` | 월드 스페이스 UI (머리 위 HP바 등) | 없음 |

### 판단 순서 (새 오브젝트를 놓을 때)

1. 플레이어가 그 위에 서거나 부딪히나? → `Terrain` (또는 몸이 있는 것이면 `Entities`)
2. 플레이어가 상호작용(채집, 열기, 제작)하나? → `Props`
3. 플레이어를 가려야 하나? → `Foreground`
4. 나머지 장식 → 거리감에 따라 `Background` / `FarBackground` / `Sky`

같은 나무 스프라이트도 용도에 따라 `Background`, `Props`, `Foreground` 어디든 갈 수 있다. 스프라이트가 아니라 **배치한 인스턴스**가 레이어를 결정한다.

### Order in Layer

- 같은 레이어 안에서 겹칠 때만 Order in Layer로 조정한다. 기본 0.
- 여러 스프라이트로 이뤄진 캐릭터(몸·팔·무기)는 루트에 `SortingGroup`을 붙이고, 자식끼리만 Order를 쓴다.
- 땅 위 오브젝트의 밑동이 풀에 살짝 묻히는 효과는 `Props`(뒤) → `Terrain`(앞) 순서로 자연스럽게 생긴다. 일부러 Order를 올려 땅 위로 꺼내지 않는다.

## 3. 물리 Layer

| Layer | 용도 |
|---|---|
| `Player` | 플레이어 몸 |
| `Enemy` | 몬스터 몸 |
| `Ground` | 땅 타일맵, 그리고 손으로 놓은 맵 경계(벽)·높이가 다른 발판처럼 타일맵이 아닌 단단한 지형 콜라이더도 같은 레이어를 쓴다. 플레이어가 지은 것만 `Structure`로 구분한다 |
| `OneWayPlatform` | 아래에서 뚫고 올라가는 발판 (PlatformEffector2D) |
| `Structure` | 플레이어가 지은 단단한 블록·벽·닫힌 문 |
| `Interactable` | 채집물, 작업대, 상자 등의 **상호작용 트리거** |
| `Pickup` | 떨어진 아이템 (흡수 트리거) |
| `PlayerAttack` / `EnemyAttack` | 공격 히트박스 |

### Layer Collision Matrix (켜야 하는 것만)

- `Player` ↔ `Ground`, `OneWayPlatform`, `Structure`, `Enemy`(접촉 데미지가 있을 때), `Interactable`(트리거), `Pickup`(트리거), `EnemyAttack`
- `Enemy` ↔ `Ground`, `OneWayPlatform`, `Structure`, `PlayerAttack`
- `Pickup` ↔ `Ground`, `OneWayPlatform`, `Structure`
- 나머지는 모두 끈다. 특히 `Interactable`은 `Player`와만 닿는다.

땅 판정(Ground Check)은 `Ground | OneWayPlatform | Structure` 마스크로 한다. 마스크는 `GameLayers`에서만 가져온다.

## 4. 스프라이트 임포트

`Assets/_Project/Art/` 아래 모든 텍스처는 다음을 따른다 (에디터의 `PixelArtImportEnforcer`가 자동 적용, `tools/harness/validate.py`가 검사).

- Texture Type: Sprite (2D and UI)
- Pixels Per Unit: 16
- Filter Mode: **Point (no filter)**
- Compression: **None**
- Generate Mip Maps: 끔
- 피벗: `Characters`, `Props`, `Structures` 폴더는 **Bottom Center**(발밑). 타일과 배경은 Center.
- 스프라이트 시트는 Sprite Mode = Multiple, Grid by Cell Size 16×16(또는 캐릭터 프레임 크기)로 자른다.

## 5. 카메라

- Main Camera에 `PixelPerfectCamera` (Assets PPU 16, Reference 320×180, Crop Frame None, Grid Snapping = Upscale Render Texture 또는 Pixel Snapping).
- 따라가기는 Cinemachine + `CinemachinePixelPerfect` 확장 + `CinemachineConfiner2D`로 맵 밖을 비추지 않게 한다.
- 2D 조명을 쓸 때 빛이 매끈한 그라데이션으로 보이면 Upscale Render Texture를 켠다.

## 6. 패럴랙스

- `Sky`, `FarBackground`, `Background`는 카메라 이동량에 계수를 곱해 움직인다. 예: Sky 0.95, FarBackground 0.8, Background 0.4~0.0.
- 패럴랙스 레이어 위치는 **최종 위치를 1/PPU 단위로 스냅**한다. 스냅하지 않으면 픽셀이 떨린다.
- `Props`, `Terrain`, `Entities`, `Foreground`는 패럴랙스 계수 0 (월드에 고정). 전경에 약한 역방향 패럴랙스를 주고 싶으면 Foreground만 예외로 -0.1 정도까지 허용한다.

## 7. 전경 가림 처리

`Foreground` 오브젝트가 플레이어를 가리면 `ForegroundFader`(트리거 + 알파 0.4로 페이드)를 붙인다. 플레이어가 전경 뒤에서 완전히 사라지면 안 된다.

## 8. 2D 조명

- `Global Light 2D` 하나로 낮밤 색·밝기를 바꾼다 (`WorldClock`이 구동).
- 횃불 등 `Light 2D`의 Target Sorting Layers: `BackWall`, `Props`, `Terrain`, `Entities`, `Foreground`. 하늘·먼 배경은 빼서 실내 빛이 하늘을 밝히지 않게 한다.
- `Sky`, `FarBackground`는 별도의 Global Light 2D(그 레이어만 대상)로 하늘색을 따로 제어해도 된다.
