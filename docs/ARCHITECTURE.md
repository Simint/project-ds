# 아키텍처

횡스크롤 2D 픽셀 싱글 생존게임. **은신처(거점) 하나 + 여러 지역 맵 + 실내·층 맵**을 문과 맵 선택으로 오간다. 모든 맵은 손으로 만든다.
무엇을 만드는지는 `design/GAME_DESIGN.md`(비공개 서브모듈), 화면·레이어 규칙은 `RENDERING_RULES.md`에 있다.
새 시스템을 만들거나 기존 시스템 경계를 바꾸기 전에 이 문서를 먼저 읽는다.

## 1. 폴더 구조

```
Assets/_Project/
├── Art/                ※ 비공개 서브모듈 (Simint/project-ds-art)
│   ├── Characters/     플레이어·좀비·동물 (피벗 발밑)
│   ├── Props/          채집물, 작업대, 가구, 상자, 문 (피벗 발밑)
│   ├── Structures/     건설물 (피벗 발밑)
│   ├── Tiles/          땅·발판·실내 타일
│   ├── Backgrounds/    하늘, 패럴랙스 레이어
│   ├── Items/          아이템 아이콘
│   ├── FX/             파티클, 후각 흔적, 날씨
│   └── UI/
├── Audio/              ※ 비공개 서브모듈 (Simint/project-ds-audio)
│   ├── Music/
│   ├── SFX/
│   └── Ambience/
├── Data/               ※ 비공개 서브모듈 (Simint/project-ds-data) — ScriptableObject 에셋 (정의 데이터)
│   ├── Items/
│   ├── Recipes/
│   ├── Harvestables/
│   ├── Structures/     은신처 건설물·시설
│   ├── Crops/          작물
│   ├── Creatures/      좀비·동물
│   ├── Stats/          생존 스탯
│   ├── StatusEffects/  출혈, 골절, 질병, 피폭 단계
│   ├── Maps/           MapDefinition
│   ├── Weather/        WeatherDefinition, 계절
│   ├── Difficulty/     DifficultyDefinition (쉬움/보통/어려움)
│   └── Lore/           LoreNoteDefinition (쪽지)
├── Prefabs/
│   ├── Characters/
│   ├── Props/
│   ├── Structures/
│   ├── Pickups/
│   ├── Map/            MapTransitionDoor, SpawnPoint, DigSpot, LockedDoor, BreakableWall, Bridge
│   └── FX/
├── Scenes/
│   ├── Boot.unity      상주 씬 (항상 로드됨)
│   ├── Hub/            은신처
│   ├── Regions/        지역 맵 (예: Residential.unity)
│   └── Interiors/      실내·층 맵 (예: Residential_House01_F1.unity)
├── Settings/           Controls.inputactions 등
├── Scripts/
│   ├── Core/           GameLayers, PersistentId, GameServices(서비스 접근), GameLog, 공용 유틸
│   ├── Player/         이동, 입력, 상태 머신, 손 모드, 후각, 상호작용
│   ├── Maps/           MapDefinition, MapLoader, 맵 이동, 맵 상태, 맵 해금
│   ├── World/          WorldClock, 계절, 날씨, 낮밤, 패럴랙스, 전경 페이드, 환경 구역
│   ├── Harvesting/     Harvestable, DigSpot
│   ├── Survival/       생존 스탯, 상태 이상, 방사능
│   ├── Items/          ItemDefinition, ItemStack, Pickup, 부패·내구도·연료
│   ├── Inventory/      인벤토리 모델(슬롯+무게), 핫바, 장비 슬롯
│   ├── Crafting/       RecipeDefinition, CraftingStation, 수리
│   ├── Building/       BuildGrid, StructureDefinition, 은신처 확장, 농사
│   ├── Combat/         IDamageable, Health, Hitbox, 무기, 소음
│   ├── AI/             좀비·동물 행동, 스포너
│   ├── Lore/           쪽지 수집
│   ├── Save/           저장 DTO, SaveService, 마이그레이션
│   └── UI/
├── Editor/
│   └── Harness/        하네스 에디터 도구 (수정 시 주의)
└── Tests/
    └── EditMode/       순수 로직 테스트 (Game.Tests.EditMode.asmdef)
```

`Assets/_Project/Scripts/`는 `Game.Runtime.asmdef` 하나로 묶여 있다(3단계, 인벤토리·저장 전에 만듦). `Editor/` 폴더는 Unity가 자동으로 에디터 전용으로 컴파일하고, 플랫폼을 제한하지 않은 `Game.Runtime`을 그대로 참조할 수 있다. `Tests/EditMode/`는 `Game.Runtime`을 참조하는 별도 테스트 전용 어셈블리다.

## 2. 핵심 원칙

1. **정의(Definition)와 상태(State)를 나눈다.**
   - 정의는 바뀌지 않는 데이터다. `ScriptableObject`로 만든다.
   - 상태는 플레이 중에 바뀌는 값이다. 일반 C# 클래스로 만든다. 저장은 상태만 한다.
   - 런타임에 ScriptableObject 에셋의 필드를 바꾸지 않는다. 에디터에서는 플레이가 끝나도 값이 남는다.
2. **모든 정의는 문자열 `id`를 가진다.** 저장 파일은 에셋 참조 대신 `id`로 기록한다. `id`는 한번 정하면 바꾸지 않는다 (소문자 스네이크: `wood_log`, `map_residential`).
3. **맵에 손으로 놓은 저장 대상 오브젝트는 `PersistentId`를 가진다.**
   - 대상: 채집물, 상자, 잠긴 문, 부술 수 있는 벽, 다리, 땅 파기 지점, 스포너, 쪽지.
   - ID는 GUID라서 모든 씬에서 고유하다. 검증기(HX301)는 모든 씬을 합쳐서 중복을 찾는다.
   - 플레이 중 생성되는 것(건설물, 떨어진 아이템, 사망 주머니, 적 개체)에는 쓰지 않는다.
4. **로직과 표시를 나눈다.** 인벤토리·스탯·제작·맵 상태는 MonoBehaviour 없이 테스트할 수 있는 C# 클래스로 짠다. MonoBehaviour와 UI는 그 모델을 구독만 한다 (C# `event`).
5. **게임 시간은 `WorldClock`만 쓴다.** 생존 수치, 리스폰, 부패, 작물, 날씨는 `Time.deltaTime`이 아니라 게임 내 분(minute) 단위로 계산한다. 그래야 맵 이동 시간 건너뛰기와 재방문 계산이 같은 코드로 된다.
   - 예외: **스태미나**는 달리기 키 입력에 실시간으로 반응해야 해서 `Time.deltaTime`으로 갱신한다(4.3). 하루 길이(`realSecondsPerDay`) 설정과 무관하게 항상 같은 체력 소모로 느껴져야 하기 때문이다.
6. **물리 이동은 Rigidbody2D로만.** 플레이어·적은 `Rigidbody2D.linearVelocity`로 움직인다. Interpolate 켬, 플레이어는 Collision Detection = Continuous.
   - 예외: 맵 이동 후 스폰 지점으로 옮길 때는 `rb.position`을 정하고 `linearVelocity`를 0으로 만든다. 이 경우에도 `transform.position`은 쓰지 않는다.
7. **입력은 Input System만.** `Assets/_Project/Settings/Controls.inputactions`에 액션을 정의한다. 구 `Input.GetKey`는 금지다.
   - Keyboard&Mouse와 Gamepad 컨트롤 스킴을 함께 둔다. 게임플레이 코드는 마우스 위치를 직접 읽지 않는다. 대신 `PlayerInputReader`가 주는 조준 방향을 쓴다.
   - `Assets/Settings/InputSystem_Actions.inputactions`(Unity 기본 템플릿)는 쓰지 않는다.
8. **레이어 이름·마스크는 `GameLayers`에서만.** 코드에 `"Ground"` 같은 문자열을 쓰지 않는다.
9. **물리 쿼리보다 등록부(registry).** 소음, 후각, 환경 구역처럼 "주변의 무엇"을 찾는 기능은 물리 레이어를 새로 만들지 않는다. 대상이 `OnEnable`/`OnDisable`에서 등록부에 들어가고, 시스템이 그 목록을 거리·영역으로 훑는다. 맵 하나에 대상이 수십~수백 개라 이 방식이 충분히 빠르고, 할당도 없다.

## 3. 씬 구조와 맵 이동

### 3.1 상주 씬 + 맵 씬

```
Boot.unity (항상 로드)                    맵 씬 (한 번에 하나, 가산 로드)
├─ GameServices                           ├─ MapRoot (MapDefinition 참조, 카메라 경계)
│  ├─ WorldClock                          ├─ Grid / Tilemaps
│  ├─ SeasonSystem / WeatherSystem        ├─ SpawnPoint × N
│  ├─ MapLoader / MapStateRegistry        ├─ MapTransitionDoor × N
│  ├─ SaveService                         ├─ 채집물, 상자, 문, 벽, DigSpot ... (PersistentId)
│  ├─ NoiseSystem / ScentSystem           ├─ EnemySpawner × N (PersistentId)
│  └─ EnvironmentZoneRegistry             ├─ RadiationZone / 실내 여부
├─ Player (몸, 인벤토리, 스탯)             └─ (Hub만) BuildGrid, 건설 범위
├─ Main Camera + Cinemachine
├─ Global Light 2D
└─ UI
```

- `Boot`는 게임 시작부터 끝까지 남는다. 플레이어·카메라·UI·서비스는 여기 있고, 맵이 바뀌어도 파괴되지 않는다. `DontDestroyOnLoad`를 쓰지 않고, 씬 하나에 모아 둔다.
- 맵 씬은 `MapLoader`가 `LoadSceneAsync(..., Additive)`로 하나만 로드하고, 이전 맵은 언로드한다.
- 맵 씬 안의 오브젝트는 `Boot`의 서비스를 `GameServices`(서비스 접근 지점, Core)로 찾는다. 매니저마다 싱글톤을 만들지 않는다 (`.claude/rules/csharp.md` "의존성과 전역 접근"). `Find*`나 `Camera.main`은 쓰지 않는다.
- 카메라 경계: `MapRoot`의 `PolygonCollider2D`(트리거, 콜라이더 레이어 없음)를 `MapLoader`가 `CinemachineConfiner2D`에 넣는다.
- 빛: Global Light 2D는 `Boot`에 하나 있다. 실내 맵은 `MapDefinition.isIndoor`로 낮밤 반영을 끄고 고정 실내 밝기를 쓴다.

### 3.2 MapDefinition (ScriptableObject)

| 필드 | 설명 |
|---|---|
| `id` | `map_hub`, `map_residential`, `map_residential_house01_f1` |
| `displayName` | 맵 선택 화면 이름 |
| `sceneName` | 로드할 씬 이름 (Build Profiles에 등록) |
| `kind` | `Hub` / `Region` / `Interior` |
| `isIndoor` | true면 날씨·낙진·낮밤 밝기의 영향을 받지 않음 |
| `baseRadiation` | 맵 전체 기본 오염도 (0이면 자연 회복 지역) |
| `travelMinutesFromHub` | 은신처에서 맵 선택으로 갈 때 드는 게임 내 시간 (`Region`만) |
| `unlockedByDefault` | 처음부터 맵 선택 목록에 있는지 (첫 지역만 true) |
| `ambience` | 배경음·배경 레이어 프리셋 (선택) |

### 3.3 맵 이동 흐름

```
[문 상호작용] or [은신처 맵 선택]
  1. MapLoader.Travel(targetMapId, spawnPointId, travelMinutes)
  2. 화면 페이드 아웃, 플레이어 입력 잠금
  3. 현재 맵의 모든 IPersistentObject → MapStateRegistry에 상태 기록 (Capture),
     은신처면 BuildGrid.SyncNow로 건설물 기록, 떠나는 맵과 플레이어 위치 기억
  4. 현재 맵 씬 언로드 (BuildGrid.OnDisable은 기록하지 않는다 — 언로드 중엔 구조물이 먼저 파괴될 수 있다)
  5. travelMinutes > 0이면 WorldClock.Advance(minutes) → 스탯·부패·작물이 그만큼 진행
  6. 새 맵 씬 가산 로드
  7. 새 맵의 IPersistentObject → 저장된 상태 적용 (Restore), 리스폰 계산
  8. 사망 주머니가 이 맵에 있으면 생성
  9. 플레이어를 SpawnPoint로 이동 (rb.position), 카메라 경계 교체
 10. 자동 저장, 페이드 인, 입력 해제
 11. 이동이 완전히 끝난 뒤(이동 중 표시가 풀린 뒤) MapChanged 발행 — 구독자가 그 안에서 다시 Travel을 부를 수 있다
```

- 새 맵 씬을 못 불러오거나 MapRoot가 없으면 그 씬을 내리고, 떠난 맵의 마지막 위치로 한 번만 되돌아간다. 이미 흐른 이동 시간은 되돌리지 않는다. 실패한 이동은 저장하지 않고 MapChanged도 보내지 않는다.
- 저장(`SaveService.Save`)이 디스크 오류로 실패하면 오류 로그만 남기고 false를 돌려준다. 이동 흐름은 끊기지 않는다.

- `MapTransitionDoor`(`IInteractable`): `targetMap`(MapDefinition), `targetSpawnPointId`. 인접 지역, 실내, 층 이동이 모두 이 컴포넌트를 쓴다. 이동 시간은 현재 맵과 목표 맵 중 어느 쪽이 Hub인지로 정해진다: Hub↔Region이면 Region 쪽 `travelMinutesFromHub`를 양방향에 그대로 쓰고, 그 외(같은 지역 내부, 실내, 층 이동)는 `0`이다.
- `SpawnPoint`: 맵 안에서 고유한 `spawnPointId` 문자열 (`from_hub`, `door_house01`, `stairs_up`).
- 맵 선택(은신처의 출발 지점 UI)은 해금된 `Region` 목록을 보여 주고 `travelMinutesFromHub`를 쓴다. 돌아올 때도 `MapTransitionDoor`가 같은 시간을 쓴다.
- **해금:** `MapUnlocks`(해금된 map id 집합)에 추가한다. 맵 끝 출구에 처음 도달했을 때(`MapUnlockTrigger`), 또는 지도·쪽지 아이템을 얻었을 때 추가된다.

### 3.4 맵 상태 (영구 변경 + 리스폰)

- `MapStateRegistry`: `Dictionary<string mapId, MapState>`
- `MapState`: `Dictionary<string persistentId, ObjectStateRecord>` + `lastVisitedMinute`
- `ObjectStateRecord`: `{ int state; long untilMinute; string extra; }`. 상태 번호의 뜻은 컴포넌트마다 다르다.

| 컴포넌트 | 저장하는 것 | 리스폰 |
|---|---|---|
| `Harvestable` | `Depleted` 여부, 리스폰 시각 | 정의의 리스폰 일수가 지나면 재생성 |
| `Container`(상자) | 열림 여부, 남은 내용물(아이템 id·수량) | 보급 상자만 리스폰 |
| `LockedDoor` | 잠김/열림(열쇠)/열림(쇠지렛대) (무기로 치면 흔들리기만 해서 부서짐 상태는 없다) | 없음 (영구) |
| `BreakableWall` | 부서짐 여부 (남은 내구는 저장하지 않는다) | 없음 |
| `Bridge` | 이어짐 여부 | 없음 |
| `DigSpot` | 파냄 여부 | 없음 |
| `EnemySpawner` | 다음 리스폰 시각, 남은 개체 수 | 정의의 리스폰 시간 |
| `LoreNotePickup` | 주웠는지 | 없음 |

- 모든 컴포넌트는 `IPersistentObject { string Id; ObjectStateRecord Capture(); void Restore(ObjectStateRecord rec, long nowMinute); }`를 구현한다.
- 재방문 시 시간이 지났는지는 `Restore`에 넘긴 `nowMinute`와 저장된 `untilMinute`를 비교해서 판단한다. 방문하지 않은 맵은 아무것도 업데이트하지 않는다.
- 적 개체 하나하나는 저장하지 않는다. 맵을 떠나면 사라지고, 스포너 상태만 남는다.

## 4. 시스템 개요

### 4.1 시간·계절·날씨 (`World/`)

- `WorldClock` (`IWorldClock` 구현): 게임 시간의 유일한 출처. 다른 시스템은 `IWorldClock`으로 받는다 (테스트에서 가짜 시계로 교체). 내부 값은 `long totalMinutes`이다. 현실 1초 = 게임 1.2분 (하루 1440분 = 현실 20분, `WorldSettings`에서 조절). 이벤트는 `MinutePassed`, `HourPassed`, `DayChanged`이다. `Advance(minutes)`는 시간을 건너뛴다 (맵 선택 이동, 잠자기).
  - `Advance`는 분마다 이벤트를 쏘지 않는다. 받는 쪽은 `Advanced(long minutes)`를 받아 한 번에 계산한다. 건너뛰는 동안 날짜가 바뀌었으면 `DayChanged`를 한 번만 보낸다 (며칠을 건너뛰어도 한 번). 받는 쪽은 현재 날짜를 다시 계산한다.
  - 하루는 항상 게임 내 1440분(24시간)이다. 설정으로 바꾸는 것은 하루가 현실로 몇 초인지뿐이다.
- `SeasonSystem`: `totalMinutes`와 `WorldSettings.seasonLengthDays`로 현재 계절(봄·여름·가을·겨울)을 계산한다.
- `WeatherSystem`: 계절별 가중치로 `WeatherDefinition`을 고르고 일정 시간마다 바꾼다.
  - `WeatherDefinition`: id, 온도 보정, 시야 보정, 소음 반경 배율, 낙진 피폭량/분, 비·눈 여부, FX 프리팹.
  - 현재 맵이 `isIndoor`이면 효과를 적용하지 않는다. 화면 FX도 끈다.
- `DayNightCycle`: `WorldClock` 시각을 Global Light 2D 밝기·색에 반영한다 (실제 구현은 아래 8단계 현황).
- **[MVP 8단계 현황]**
  - 등록 순서는 시계 → `SeasonSystem` → `WeatherSystem` → `DayNightCycle`(`IDayNight`) → 맵·저장 → 나머지다(`GameBootstrap`).
  - `SeasonSystem`은 `SeasonSettings`(계절별 시간당 체온 변화, 실내에도 적용)를 함께 들고 있다. 겨울에는 `HarvestableDefinition.isPlant` 채집물을 막고, 실외 `WaterSource`가 언다.
  - `WeatherSystem`은 순수 C# 서비스다. `WeatherSettings`(계절별 가중치 목록, 맑음은 fallback)로 뽑고, 끝나는 시각이 되면 다음 날씨를 뽑는다. `Advance`로 건너뛴 시간은 한꺼번에 넘긴다. 지금 날씨 id와 끝나는 시각은 저장된다.
  - 효과 계산은 순수 함수 `WeatherRules`(실내면 체온·시야·소음 효과 없음)가 맡는다. 표시는 Boot의 `WeatherPresenter`가 맡는다: 화면 덮개(짙기 = 덮개 색 알파와 '플레이어 시야 배율' 중 진한 쪽), 카메라를 따라다니는 비·눈 파티클. 폭우 소음 감쇠는 규칙이라 `NoiseSystem`이 생성 때 받은 배율 함수로 `WeatherRules`를 직접 읽는다. 적 감지 배율은 `CreatureBrain`이 `GameServices.WeatherEnemySightMultiplier`로 읽는다. 날씨 소리와 낙진은 아직 없다.
  - `DayNightCycle`은 `Gradient` 대신 `DayNightDefinition`(해 뜨고 지는 분, 전이 시간, 낮·밤 밝기·색, 하늘색)과 순수 함수 `DayNightMath`로 계산한다. 실내에도 같은 밝기를 쓴다. 하늘색은 카메라 배경색이고, 해·달은 `Sky` 레이어 스프라이트를 카메라 자식으로 두고 호 위의 로컬 위치를 분마다 바꾼다(Cinemachine보다 한 프레임 늦지 않게). 하늘만 비추는 Global Light 2D(`SkyLight`)를 따로 두어 해·달이 어두워지지 않게 한다(RENDERING_RULES.md 8장).
  - 밤 좀비: `CreatureDefinition`의 밤 배율(속도·피해·감지)과 `SpawnGroupDefinition.nightExtraCount`를 쓴다. 해가 지면 스포너가 추가분만 더 내보내고, 낮에 잡은 적은 리스폰 시간이 지나야 돌아온다. 밤에 맵에 들어오면 처음부터 추가분까지 낸다.
  - 빗물 통: `WeatherSystem.RainMinutesTotal`(비가 온 분의 누적, 저장됨)과 마지막으로 본 값의 차이만큼 채워서, 은신처에 없을 때나 잠으로 건너뛴 시간에 내린 비도 받는다. 계산은 `CreatureStatRules`가 맡는다.
- `EnvironmentZoneRegistry` + `RadiationZone`·`HeatZone`(모닥불 주변 등): 맵에 놓은 사각형 영역이다. 물리 콜라이더가 아니라 `Bounds`로 등록한다. 생존 시스템이 분마다 플레이어 위치가 들어 있는 구역을 조회한다.

### 4.2 플레이어 (`Player/`)

- `PlayerController`: 상태 머신. Idle/Run/BeastRun은 물리 계산이 같고 프리셋(`PlayerMovementSettings`)만 다르므로 `Grounded` 상태 하나가 맡고, Jump와 Fall은 중력 계산이 달라 따로 상태를 둔다(`Grounded`/`Jumping`/`Falling`, `IPlayerMotionState`). Attack, Harvest, Dig, Interact, Stunned는 해당 시스템이 생기는 단계(3·6·7단계)에서 상태를 추가한다.
- `PlayerAnimator`(표시 전용): `PlayerController`의 접지 상태(`IsInGroundedState`)·속도·달리기 프리셋을 읽어 Animator 파라미터(`Speed`, `VerticalSpeed`, `Grounded`, `Running`, `MoveSpeedScale`)만 갱신한다. 이동 로직은 애니메이션을 모른다. Animator Controller와 클립은 Art 서브모듈(`Art/Characters/Player/Animations/`)에 있다. 플레이어 그림은 왼쪽을 보고 그렸으므로 `PlayerFacing.artFacesLeft`를 켠다. 반대쪽을 보는 그림(지금은 대기)은 그 클립에서 그림 오브젝트의 가로 스케일을 -1로 키 해 `flipX`와 함께 뒤집히게 한다(다른 클립은 1로 키).
- `PlayerInputReader`: `Controls.inputactions`를 읽어 이동·조준·버튼을 내보낸다. 게임플레이 코드는 Input System을 직접 만지지 않는다.
- 이동 파라미터는 `PlayerMovementSettings` ScriptableObject에 모은다. 최고 속도, 가속/감속, 점프 높이, 코요테 타임(0.1s), 점프 버퍼(0.1s), 가변 점프, 최대 낙하 속도가 들어간다.
  - 세 프리셋을 둔다: `walk`(두 발 걷기, 기본), `run`(사람 손 + 달리기 키), `beastRun`(짐승 손 + 달리기 키, 네 발). 짐승 손이라도 달리기 키를 누르지 않으면 `walk`다.
  - 무게 보정: `InventoryModel.WeightRatio` → 속도·점프 배율 곡선(`AnimationCurve`)
  - 부상 보정: 골절이면 속도·점프 배율을 낮춘다.
- **`PlayerHands`(손 모드):** `HandMode { Human, Beast }`
  - 전환은 `transitionSeconds`(정의 데이터, 값은 `docs/design/`) 동안 걸린다. 그동안 공격과 상호작용을 막는다. 이벤트는 `HandModeChanged`이다.
  - `Beast` 동안: 허기 감소 배율을 생존 스탯 모디파이어로 등록한다. 손에 든 아이템 사용을 막는다. 장갑 슬롯은 해제한다.
  - 행동 허용 검사는 `HandRequirement { Any, Human, Beast }` 하나로 통일한다. 아이템(`ItemDefinition.requiredHand`), 레시피, 상호작용(`IInteractable.RequiredHand`)이 이 값을 가진다.
- **`ScentSense`(후각):** 쿨타임과 허기 비용이 있다. `ScentSystem`에 등록된 `ScentSource`(음식, 아이템, 적, 방사능 구역, DigSpot)를 반경 안에서 찾아 FX 풀로 흔적을 보여 준다.
  - **[MVP 7단계 현황]** 대상은 `DigSpot`, 떨어진 아이템·사망 주머니·캐비닛(털리면 끔), 적이다. 대상 고르기는 순수 함수 `ScentQuery`(반경 + `SolidMask` Linecast로 벽 너머 제외)이고, 흔적은 `Awake`에서 미리 만든 `SpriteRenderer`(`Effects` 레이어, 종류별 색)를 재사용해 대상을 따라가게 한다. 수치는 `ScentSettings`(Data/Player/).
- 땅 판정: 발밑 `Physics2D.OverlapBox`, 마스크 `GameLayers.GroundCheckMask`
- 상호작용: 플레이어 주변 `OverlapCircle`로 `Interactable` 레이어를 찾고 가장 가까운 `IInteractable`을 고른다. 이때 `RequiredHand`가 현재 손 모드와 맞아야 한다. 결과 배열은 미리 할당한다 (`OverlapCircle(..., ContactFilter2D, Collider2D[])`).

### 4.3 생존 (`Survival/`)

- `SurvivalStatDefinition`: id, 최대값, 분당 감소량, 0일 때 효과. 스탯은 HP, 허기, 갈증, 체온, 스태미나, 방사능 피폭이다.
- `SurvivalStats`: 여러 `SurvivalStat`을 묶은 모델. `WorldClock.MinutePassed`마다 갱신하고, `Advanced(minutes)`에서는 한 번에 계산한다.
  - 모디파이어(배율·가산)는 출처별로 등록·해제한다: 손 모드, 날씨, 구역, 상태 이상, 난이도, 장비. 저장하지 않는다 (불러온 뒤 환경에서 다시 등록됨).
  - **스태미나는 예외다.** 달리기 키에 실시간으로 반응해야 해서 `SurvivalStats.Tick`이 아니라 `PlayerSurvival.Update`가 `Time.deltaTime`으로 직접 갱신한다(2장 원칙 5의 예외). 나머지 네 스탯(HP·허기·갈증·체온)은 그대로 `WorldClock` 기준이다.
- 방사능 피폭: 분마다 들어오는 양 = 맵 기본 오염도 + 구역 + 낙진(실외일 때) + 변이 좀비 피격. 여기서 방호 장비 등급만큼 줄인다. 들어오는 양이 0이고 오염 맵이 아니면 서서히 회복한다. 피폭 단계는 `StatusEffect`로 표현한다.
- `StatusEffectDefinition`: id, 지속(시간 또는 치료할 때까지), 스탯 모디파이어, 이동 보정, 치료 아이템 태그. 출혈, 골절, 식중독·질병, 피폭 단계 1~4가 여기에 해당한다.
- `StatusEffects`: 현재 걸린 상태 목록 (상태 C# 클래스). 저장 대상이다.
- 새 스탯이나 상태 이상은 `add-survival-stat` 스킬 절차를 따른다.

### 4.4 아이템·인벤토리·제작

- `ItemDefinition`: id, 이름, 아이콘, 최대 스택, **무게**, 카테고리, `requiredHand`, 선택 데이터:
  - `ToolData`: 도구 종류, 등급, 근접 피해, **최대 내구도**
  - `WeaponData`: 둔기/날붙이/권총, 피해, 공격 속도, 소음 반경, 출혈·넘어뜨리기 확률, 탄약 id
  - `ConsumableData`: 스탯 회복량, 상태 이상 부여·치료, **부패 시간**(분, 0이면 안 상함), 상한 뒤의 아이템 id, 생고기 여부
  - `LightData`: 광원 반경, **최대 연료/배터리**(분)
  - `ProtectionData`: 방사능 방호 등급(0~3), 체온 보정
  - `PlaceableData`: 설치할 `StructureDefinition` (은신처에서만)
  - `LoreData`: 읽으면 수집되는 `LoreNoteDefinition`, 해금할 map id (선택)
- `ItemStack`(상태): `itemId`, `count`, 그리고 인스턴스 값인 `durability`, `charge`, `spoilAtMinute`
  - 내구도나 연료가 있는 아이템은 최대 스택이 1이다.
  - 부패하는 아이템끼리 합치면 `spoilAtMinute`는 더 이른 쪽을 따른다.
  - 부패는 매 분 검사하지 않는다. 표시하거나 사용할 때 `spoilAtMinute`과 현재 시각을 비교한다 (lazy).
- `InventoryModel`: 고정 슬롯 배열 + 장비 슬롯(머리·몸·손·가방·광원). 총 무게와 한도를 관리하고, 가방은 슬롯 수와 무게 한도를 늘린다. 추가·제거·이동은 이 클래스만 한다.
- `RecipeDefinition`: 재료 목록, 결과물, 필요한 시설 종류(없으면 손 제작), `requiredHand`(대부분 Human)
- 수리: 작업대 레시피의 한 종류(`IsRepair`)로, 결과물 대신 대상의 내구도를 회복시킨다.
- **재료 공급원(`IMaterialSource`, MVP 5단계):** 제작·건설·수리는 재료가 어디 있는지 몰라도 되게
  `IMaterialSource`(`CountOf`/`Remove`) 하나로 다룬다. `PlayerInventory`와 보관함(`StorageContainer`)이
  각각 구현하고, `MaterialSourceAggregator`가 은신처의 모든 보관함(`StorageRegistry`) + 가방을 하나로
  묶는다. **뺄 때는 보관함을 먼저 쓰고, 모자란 만큼만 가방에서 뺀다** — 가방은 계속 채집하며 채워지는
  공간이라 최대한 비워 두는 쪽이 유리하기 때문이다. 이 순서는 `MaterialSourceAggregator.Remove` 한
  곳에만 있다.

### 4.5 건설·은신처 (`Building/`)

- 건설은 **Hub 맵에서만** 한다. `BuildGrid`는 Hub 씬에만 있다. 다른 맵에서는 건설 모드에 들어갈 수 없다.
- `BuildGrid`: Unity `Grid`(셀 1×1)를 기준으로 셀 점유 상태를 `Dictionary<Vector2Int, StructureInstance>`로 관리한다.
- **건설 범위:** `HubBuildArea`가 허용 셀 범위(사각형 목록)를 갖는다. 장애물(`HubObstacle`, PersistentId)을 치우거나 확장 레시피를 쓰면 범위가 넓어진다. 범위 밖에는 설치할 수 없다.
- `StructureDefinition`: id, 프리팹, 차지 셀 크기(`Vector2Int footprint`, 기준은 **왼쪽 아래 셀**), 종류(`Solid`, `Platform`, `BackWall`, `Prop`), 설치 조건, 시설 종류(선택)
- 설치 조건 기본값:
  - `Solid`, `Prop`: 아래 칸이 `Ground`·`Solid`·`Platform` 중 하나로 받쳐져야 한다.
  - `BackWall`: 인접한 `BackWall` 또는 `Solid`가 있어야 한다.
  - 다른 구조물이나 지형 타일과 셀이 겹치면 안 된다. 플레이어·몬스터와 겹쳐도 안 된다.
- 종류별 레이어: `Solid` → Sorting `Terrain`, 물리 `Structure`. `Platform` → `Terrain`, `OneWayPlatform`. `BackWall` → `BackWall`, 콜라이더 없음. `Prop` → `Props`, `Interactable` 트리거.
- 배치 미리보기: 반투명 스프라이트, 가능하면 초록, 불가능하면 빨강 틴트
- **시설(`StationKind`):** Workbench, Bed, Storage, FarmPlot, Stove, WaterPurifier, RainCollector, Greenhouse, Smokehouse, Generator, Decontamination
  - 침대: 잠자기 = `WorldClock.Advance` + 자동 저장
  - 발전기: 연료를 넣으면 Hub 전체가 "전력 있음" 상태가 된다. 전등과 충전기가 이 상태를 본다 (전선 시뮬레이션은 하지 않는다).
- **농사:** `CropDefinition`(id, 씨앗 아이템, 성장 분, 가능 계절, 수확물). `FarmPlot`이 심은 작물 id와 심은 시각을 저장하고, 성장은 시각 차이로 계산한다. 겨울에는 `Greenhouse` 안의 밭만 자란다.

### 4.6 전투·소음 (`Combat/`)

- `IDamageable { void TakeDamage(DamageInfo info); }`, `Health` 컴포넌트
- `DamageInfo`: 피해량, 종류(둔기·날붙이·발톱·총·방사능), 출혈·골절·피폭 부여량, 넘어뜨리기, 공격자
- 히트박스는 공격 순간에만 켜는 트리거 (`PlayerAttack` / `EnemyAttack` 레이어)
- 무기 사용 시 내구도가 깎이고, 0이 되면 부서진다. 권총은 탄약 아이템을 소모한다.
- **`NoiseSystem`:** `Emit(Vector2 position, float radius, NoiseKind kind)`. 반경에는 현재 날씨의 소음 배율을 곱한다. 등록된 `INoiseListener`(적) 중 반경 안에 있는 것에 알린다.
  - 소음원: 달리기·네 발 달리기(발소리), 공격, 총성, 문 부수기, 쇠지렛대, 벽 부수기
  - 앉아 걷기는 소음을 내지 않는다.

### 4.7 적 (`AI/`)

- `CreatureDefinition`: id, 종류(`Zombie`/`Animal`), 체력, 이동, 공격, 감지 범위(시야·청각), 드롭 테이블, 그리고 행동 보정 3가지:
  - **밤 보정:** 속도·감지·스폰 수 배율
  - **계절 보정:** 겨울잠(스폰 안 함), 겨울 공격성 등
  - **손 모드 반응:** 플레이어가 `Beast`일 때 감지 범위 배율과 태도 (`Ignore` / `Wary` / `Aggressive` / `Flee`)
- `CreatureBrain`: 상태 머신 (Idle, Wander, Investigate(소음), Chase, Attack, Flee, Return(영역))
  - 좀비는 소음에 강하게 반응한다. 동물은 영역(`TerritoryArea`)을 기준으로 행동한다.
- `EnemySpawner`(PersistentId): 생성할 정의, 최대 수, 밤 추가 수, 리스폰 시간. 생성은 `ObjectPool<T>`로 한다.
  - 스폰 규모(최대 수·흩어지는 반경)는 `SpawnGroupDefinition`(ScriptableObject, `Data/` 비공개 에셋)에 둔다. 지역별 적 수는 밸런스 수치라 공개 씬의 인스펙터에 직접 쓰지 않는다.
  - **[MVP 6단계 현황]** 적 수가 적어 `ObjectPool` 대신 `Instantiate`/`Destroy`를 쓴다(떨어진 아이템 `Pickup`과 같은 기준). 밤 추가 수는 8단계(낮밤)에서 넣는다.
- 화면 밖의 적은 `CreatureBrain` 갱신 빈도를 줄인다.
  - **[보류]** MVP 6단계에서는 구현하지 않았다. 지금은 모든 `CreatureBrain`이 화면 안팎 구분 없이 매 `FixedUpdate`에 판단한다(시야 Linecast 포함). 적 수가 늘어 성능 문제가 보이면 그때 넣는다.
- 은신처(Hub)에는 스포너를 두지 않는다. 습격도 없다.

### 4.8 사망·난이도

- `DifficultyDefinition`(쉬움/보통/어려움): 스탯 감소 배율, 적 피해·체력 배율, 사망 드롭 규칙(`DropRule`: 재료만 / 가방 전부 / 장비 포함 전부)
  - **[MVP 8단계 현황]** 실제 필드는 허기·갈증 감소 배율, 적 체력·피해 배율, 상자 보상 개수 배율, 채집물·적 리스폰 시간 배율, `DeathDropScope`다. 적의 수는 바꾸지 않는다. 적용은 순수 함수 `DifficultyRules`·`DeathDropRules`가 맡고, 지금 난이도는 `GameServices.Difficulty`다. 장비 칸이 아직 없어서 어려움(`BagAndEquipment`)은 보통과 같게 가방 전부를 떨어뜨린다.
  - **타이틀 화면:** Boot의 `TitleScreenUI`가 '이어하기'(저장이 있을 때만)와 '새 게임'을 고르게 한다. 새 게임은 난이도와 계절 길이(`WorldClockDefaults.seasonLengthOptionsDays`)를 고르고, 저장이 있으면 덮어쓸지 한 번 묻는다. 고르기 전에는 `WorldClock.IsPaused`로 시간이 멈추고 플레이어 Rigidbody2D는 꺼져 있으며, `MapLoader.BeginGame()`이 불릴 때 첫 맵을 불러온다. 시작하면 `GameServices.GameStarted`가 발행된다.
- `WorldSettings`(저장되는 C# 클래스): 난이도 id, 계절 길이(일), 하루가 현실로 몇 초인지(`realSecondsPerDay`). 월드를 만들 때 고르고 저장한다. 하루 자체는 항상 게임 내 1440분(`WorldClock.MinutesPerDay`)으로 고정이다.
- 사망 처리:
  1. 난이도의 `DropRule`대로 아이템을 꺼내 `DeathBagRecord { mapId, position, items }`를 만든다. 이전 기록은 덮어쓴다 (마지막 하나만).
  2. 은신처의 부활 지점으로 이동하고 스탯을 리셋한다.
  3. 그 맵에 들어가면 `MapLoader`가 사망 주머니를 생성한다. 주머니를 열면 기록이 지워진다.
  - 죽은 자리는 `MapLoader.TryGetPlayerLocation`으로 정한다. 맵 이동 도중(떠난 맵을 내린 뒤)에 죽으면 떠난 맵의 마지막 위치다.
  - 진행 중인 이동이 있으면 끝날 때까지 기다렸다가 은신처로 보낸다. 기다리는 동안 입력을 잠그고, 은신처로 가는 중에 또 죽으면 주머니를 다시 만들지 않는다(부활만).
  - 시간 건너뛰기(잠·먼 이동) 도중 죽으면 생존 스탯은 남은 시간을 계산하지 않는다. 시계와 다른 `Advanced` 구독자는 전체 시간을 쓴다.

### 4.9 쪽지 (`Lore/`)

- `LoreNoteDefinition`: id, 제목, 본문(로컬라이즈 키), 분류
- 맵에는 `LoreNotePickup`(PersistentId)으로 놓는다. 주우면 `LoreCollection`(읽은 id 집합)에 추가된다. 일부 쪽지는 맵을 해금한다.
- 은신처의 게시판 UI가 `LoreCollection`을 보여 준다.
- **[MVP 7단계 현황]** 게시판 대신 어디서나 여는 `LoreNotesUI`(N키, 패드 십자 위)가 목록과 본문을 보여 준다. 쪽지를 주우면 그 쪽지가 펼쳐진 채로 열린다. 쪽지 정의는 `LoreDatabase`(Data/Lore/)로 id에서 찾는다. 맵 해금은 아직 넣지 않았다.

### 4.10 저장 (`Save/`)

자동 저장 시점: 맵 이동 완료, 잠자기, 게임 종료. 저장 슬롯은 여러 개다 (월드마다 하나).

완성된 모습(목표 스키마)은 이렇다. 단계가 늘수록 이 모양에 맞춰 필드를 채워 나간다.

```
WorldSaveData (목표 스키마, 지금은 일부만 구현됨)
├─ settings        WorldSettings (난이도 id, 계절 길이, 하루 현실 길이(초))
├─ clock           totalMinutes, 현재 날씨 id, 날씨 끝나는 시각
├─ player          현재 mapId, spawnPointId(또는 위치), 손 모드,
│                  stats[{id, value}], statusEffects[{id, untilMinute, data}]
├─ inventory       slots[{itemId, count, durability, charge, spoilAtMinute}], equipment[...]
├─ maps            [{ mapId, lastVisitedMinute, objects[{persistentId, state, untilMinute, extra}] }]
├─ hub             structures[{id, cell, health, state}], buildArea[...],
│                  farmPlots[{cell, cropId, plantedMinute}], storage[{cell, items}], powered
├─ mapUnlocks      [mapId...]
├─ lore            [noteId...]
└─ deathBag        { mapId, position, items } 또는 null
```

3단계에서 구현한 `version 1`은 `settings`, `clock.totalMinutes`, `player`(mapId·위치만), `inventory.slots`(itemId·count만), `maps`만 채운다. `version 2`(4·5단계)에서 `stats`, `statusEffects`, `inventory.slots[].durability`, `hubStructures`가 늘었다. `version 4`(6단계)에서 `deathBag`, `version 5`(7단계)에서 `loreNoteIds`(lore), `version 6`(8단계)에서 `weatherId`·`weatherEndsAtMinute`가 늘었다. `settings`(난이도·계절 길이)는 이어하기 때 `WorldClock.ApplySettings`로 되돌린다. 손 모드·장비·hub의 나머지(파밍·전력)·mapUnlocks는 해당 단계에서 필드를 추가하며 채운다.

- `JsonUtility` 또는 Newtonsoft로 `Application.persistentDataPath/saves/<slot>.json`에 저장한다.

**버전과 옛 저장 파일.** `WorldSaveData.CurrentVersion`을 저장 형식이 바뀔 때마다 올린다(필드를 추가·변경할 때마다 — 꼭 호환이 깨지는 변경이 아니어도). 옛 버전 파일을 불러올 때는:
- 새로 생긴 필드는 `JsonUtility`가 C# 기본값으로 채운다(`durability = -1`, `List<T>`는 빈 리스트 등) — 이 기본값만으로 뜻이 통하는 한(문 닫힘=0, 내구도 없음=-1처럼) 추가 코드가 필요 없다.
- `SaveService.Load()`가 `data.version < CurrentVersion`이면 `GameLog.Info`로 한 번 남긴다. 그 이상의 처리(별도 변환)는 하지 않는다.
- 기본값만으로 뜻이 안 통하는 변경(필드 이름을 바꾸거나 구조를 재배치하는 경우)이 생기면 그때 `Save/Migrations/`에 `version`별 변환 코드를 추가한다. 지금까지는 전부 단순 필드 추가라 필요하지 않았다.

## 5. 성능 기준 (생존게임은 오브젝트가 많다)

- `Update`/`FixedUpdate`/`LateUpdate`에서 `GetComponent`, `Find*`, `Camera.main`, LINQ, `new` 컬렉션 금지. `Awake`에서 캐시한다.
- 자주 생기고 사라지는 것(투사체, 드롭 아이템, 이펙트, 후각 흔적, 적 개체)은 `UnityEngine.Pool.ObjectPool<T>`로 재사용한다.
- 화면 밖 채집물·적은 업데이트를 줄이거나 끈다.
- 게임 시간 기반 로직(부패, 작물, 리스폰)은 매 프레임 돌리지 않는다. 시각 비교로 필요할 때 계산한다.
- 이 규칙은 `.claude/hooks/post_edit.py`가 자동 검사한다.

## 6. 구현 순서 (MVP: 은신처 + 주택가)

1. `Boot` 씬, `GameServices`, `GameLog`, `WorldClock`(`IWorldClock`), `MapDefinition`, `MapLoader`, `SpawnPoint`, `MapTransitionDoor` → 빈 은신처와 빈 주택가를 오가기
2. 플레이어 이동·점프(`tune-movement`), `PlayerInputReader`, `PlayerHands`(손 전환과 네 발 달리기)
3. 아이템·인벤토리(슬롯+무게), 채집물(`add-harvestable`), `MapStateRegistry`와 저장
4. 생존 스탯(허기·갈증·체온·스태미나·HP), 상태 이상(출혈·골절·질병), 음식 부패
5. 제작·수리, 은신처 건설(`add-structure`), 시설(작업대·침대·화로·창고)
6. 수인 좀비와 들개, 소음, 근접 전투, 사망 주머니
7. 잠긴 문·부술 수 있는 벽·DigSpot·후각, 쪽지, 주택가 실내 2층
8. 날씨·계절, 낮밤, 난이도
9. (MVP 이후) 방사능 지역, 방호 장비, 농사·온실, 발전기, 추가 지역
