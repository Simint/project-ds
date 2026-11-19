---
name: add-survival-stat
description: 허기, 갈증, 체온, 스태미나, 정신력 같은 생존 스탯을 새로 추가하거나 감소·회복·0일 때 효과를 바꿀 때 사용한다.
---

# 생존 스탯 추가

## 1. 정의
`Assets/_Project/Data/Stats/<id>.asset` — `SurvivalStatDefinition`
- `id` (`hunger`, `thirst`, `temperature`, `stamina`)
- `min`, `max`, `startValue`
- `changePerGameHour`: 기본 변화량 (음수 = 감소). 실제 시간이 아니라 **게임 내 시간** 기준.
- `thresholds`: 구간별 효과 목록. 예: 허기 20 이하 → 이동속도 0.9배, 0 → HP 시간당 -5.
- `uiIcon`, `uiColor`

## 2. 동작 원칙
- 값 갱신은 `SurvivalStats`가 `WorldClock.MinutePassed`에 맞춰 한 곳에서 한다. 스탯마다 `Update`를 돌리지 않는다.
- 외부 요인(추운 지역, 모닥불 근처, 달리기)은 `IStatModifier`로 등록·해제한다. 예: `CampfireWarmth`가 범위 트리거에 들어오면 체온 모디파이어 등록, 나가면 해제.
- 효과(이동속도 감소 등)는 스탯이 직접 다른 컴포넌트를 건드리지 않는다. `SurvivalStats.OnThresholdChanged` 이벤트를 `PlayerController` 등이 구독한다.
- 음식 회복은 `ItemDefinition.consumable`의 스탯 id로 연결한다 (`add-item` 참고).

## 3. 저장
현재 값만 `WorldSaveData.player.stats`에 `{ id, value }`로 저장한다. 모디파이어는 저장하지 않는다 (불러온 뒤 환경에서 다시 등록됨).

## 4. UI
HUD의 스탯 바는 `SurvivalStatDefinition` 목록을 읽어 자동 생성한다. 스탯을 추가할 때 HUD 코드를 고치지 않아도 되게 한다.

## 5. 테스트
`SurvivalStats`는 MonoBehaviour가 아닌 모델이므로 EditMode 테스트(`Assets/_Project/Tests/EditMode/`)로 확인한다.
- 게임 내 1시간 경과 시 감소량
- 임계값 진입·이탈 이벤트
- 모디파이어 등록·해제 후 원래 속도로 복귀
- min/max 경계에서 넘치지 않음
