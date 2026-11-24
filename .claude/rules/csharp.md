---
paths:
  - "Assets/**/*.cs"
---

# C# 규칙 (Unity 6)

## 구조
- 네임스페이스: `Game.<Scripts 아래 폴더명>`. 에디터 코드는 `Game.Editor.<...>`.
- 한 파일에 한 public 타입. 파일 이름 = 타입 이름.
- MonoBehaviour는 작게. 계산 로직은 일반 C# 클래스로 빼서 MonoBehaviour가 호출한다.
- 의존성은 `[SerializeField]` 참조로 받는다. 전역 싱글톤은 `GameServices`(Core) 하나만 허용하고, 거기에 등록된 서비스만 꺼내 쓴다 (아래 "의존성과 전역 접근").

## 설계 원칙 (SOLID를 Unity에 맞게)
- **단일 책임:** 클래스 하나는 한 가지 이유로만 바뀐다. 정의(ScriptableObject) / 상태(C# 클래스) / 표시(MonoBehaviour·UI)를 섞지 않는다. 한 MonoBehaviour가 입력·이동·애니메이션·UI를 다 하면 나눈다.
- **개방-폐쇄:** 새 종류(아이템, 적, 채집물, 구조물)는 코드 수정 없이 정의 데이터를 추가해서 늘린다. 종류마다 `switch`/`if`가 늘어나는 코드는 Strategy나 정의 데이터로 바꾼다.
- **리스코프 치환:** 인터페이스·기반 클래스의 약속을 구현체가 깨지 않는다 (예: `IDamageable.TakeDamage`는 무시하지 않고 항상 처리, `IPersistentObject.Restore`는 `Capture` 결과를 그대로 되돌림). 지원하지 않는 기능을 `NotImplementedException`으로 막아야 한다면 인터페이스를 잘못 나눈 것이다.
- **인터페이스 분리:** 인터페이스는 역할 하나에 메서드 몇 개만 (`IInteractable`, `IDamageable`, `IPersistentObject`, `INoiseListener`). "만능" 인터페이스를 만들지 않는다.
- **의존성 역전:** 여러 시스템이 의존하는 서비스는 인터페이스로 받는다. 특히 시간은 `IWorldClock`으로 받아 테스트에서 가짜 시계로 바꿀 수 있게 한다.

## 의존성과 전역 접근
- 같은 씬 안의 참조는 `[SerializeField]`로 주입한다.
- 상주 씬(`Boot`)의 서비스는 `GameServices`에서만 꺼낸다. 서비스는 `GameServices` 초기화에서 정해진 순서로 등록한다 (시계 → 계절 → 날씨 → 맵·저장 → 나머지).
- **매니저마다 싱글톤(`XxxManager.Instance`)을 만들지 않는다.** 이유: 맵 씬 언로드 때 파괴된 인스턴스 참조, `Awake` 순서 의존, 숨은 의존성, 테스트 불가, 도메인 리로드를 끈 상태에서 남는 정적 값. 린트 HX010.
- `Awake`에서는 자기 참조만 캐시한다. 다른 서비스 접근은 `Start` 이후, 또는 `GameServices`의 준비 완료 이벤트 뒤.
- 정적 가변 필드(`static int count`)를 두지 않는다. 정적은 `const`, `static readonly`, 순수 함수만. 린트 HX014.

## 디자인 패턴 (이 상황에는 이것)
| 상황 | 패턴 | 규칙 |
|---|---|---|
| 플레이어·적 행동, 손 모드 | State | 상태마다 클래스 하나. 거대한 `switch` Update 금지 |
| 값 변화 알림 (스탯→UI, 시계→생존) | Observer (C# `event`) | 아래 "이벤트" 절 참조 |
| 종류별로 달라지는 규칙 (설치 조건, 피해 종류, 동물 반응) | Strategy | `if` 사슬 대신 교체 가능한 객체나 정의 데이터 |
| 정의 공유 | Flyweight | 개체는 ScriptableObject 정의를 공유하고 개체별 값은 상태 클래스에 |
| 정의로 오브젝트 생성 (적, 구조물, 드롭) | Factory | `Instantiate`는 생성 담당 한 곳(스포너·팩토리)에서만 |
| 자주 생기고 사라지는 것 | Object Pool | `UnityEngine.Pool.ObjectPool<T>` |
| 저장·복원 | Memento | `IPersistentObject.Capture/Restore`. 저장 DTO와 런타임 객체를 섞지 않음 |
| 전역 서비스 | Service Locator | `GameServices` 하나만 |
| 되돌리기 (필요해지면) | Command | 지금은 도입하지 않음 |

## 상속과 조합
- 조합(컴포넌트 + 정의 데이터)을 우선한다.
- 상속은 **추상 기반 클래스 1단계까지** (`CreatureBrain` → `ZombieBrain`은 가능, 그 아래로 더 내리지 않음). 차이는 정의 데이터와 컴포넌트로 만든다.

## 이벤트
- 기본은 C# `event Action<...>`. 구독은 `OnEnable`, 해제는 `OnDisable`. 해제할 수 없는 람다 구독 금지.
- `UnityEvent`는 인스펙터 연결이 꼭 필요한 곳(UI 버튼, 연출 트리거)에만.
- ScriptableObject 이벤트 채널과 정적 EventBus는 쓰지 않는다 (SO에 런타임 상태가 생기고, 흐름 추적이 어려워짐).
- 이름: `public event Action<int> HealthChanged;` (과거형·`Changed`), 발행 메서드는 `OnHealthChanged()`가 아니라 바로 `HealthChanged?.Invoke(value)`.

## 비동기
- 씬 로드, 맵 이동, 페이드처럼 기다리는 흐름은 Unity 6 `Awaitable` + `destroyCancellationToken`.
- 단순한 시간 연출은 코루틴도 허용 (`StartCoroutine(nameof(...))`가 아니라 `StartCoroutine(Method())`).
- `async void` 금지 (UI 이벤트 핸들러만 예외, `// harness-ignore HX012 UI 핸들러`). `async Task`는 Unity 생명주기를 모르므로 MonoBehaviour에서 쓰지 않는다. 린트 HX012.

## 문자열 호출 금지
- `SendMessage`, `BroadcastMessage`, `Invoke("이름")`, `InvokeRepeating("이름")`, `StartCoroutine("이름")` 금지. 린트 HX011.
- Animator 파라미터는 `static readonly int Speed = Animator.StringToHash("Speed");`로 캐시한다.

## 수치와 이름
- 밸런스 수치(체력, 피해, 감소량, 시간)는 코드에 쓰지 않고 ScriptableObject 정의에 둔다. 코드 상수는 기술 값(버퍼 크기 등)만. 공개 저장소에 밸런스가 노출되지 않게 하는 목적도 있다. SO 에셋은 비공개 서브모듈 `Assets/_Project/Data/`에 있으므로, SO 클래스의 필드 기본값은 0이나 중립 값으로 두고 테스트도 실제 값이 아닌 임의 값을 쓴다.
- private 필드는 접두사 없는 `camelCase` (`moveSpeed`). 매개변수와 겹치면 `this.`를 쓴다. 직렬화 필드 이름을 바꿀 때는 `[FormerlySerializedAs("old")]`를 단다.
- bool은 `is/has/can`으로 시작 (`isGrounded`, `hasKey`).

## 로그
- `Debug.Log` 대신 `GameLog`(Core)를 쓴다: `GameLog.Info("Save", "...")`. 정보 로그는 `[Conditional]`로 빌드에서 빠진다. 린트 HX013.
- 경고·오류(`Debug.LogWarning`, `Debug.LogError`)는 그대로 써도 된다. 비어 있는 `[SerializeField]` 참조는 `Awake`에서 `Debug.LogError(..., this)`로 알린다.

## 테스트
- 인벤토리, 스탯, 제작, 맵 상태, 저장 같은 순수 로직은 EditMode 테스트를 함께 쓴다 (`unity-testing` 스킬).
- asmdef 분리와 테스트 도입은 MVP 3단계(인벤토리·저장) 전에 한다 (`docs/ARCHITECTURE.md` 6장).

## 인스펙터와 필드
- `[SerializeField] private float moveSpeed = 6f;`처럼 쓴다. `public` 필드 금지.
- 외부 노출은 읽기 전용 프로퍼티: `public float MoveSpeed => moveSpeed;`
- 단위가 있는 값은 이름이나 `[Tooltip]`에 단위를 적는다 (`jumpHeightTiles`, `coyoteTimeSeconds`).
- 정의 데이터는 `[CreateAssetMenu(menuName = "Game/<분류>/<이름>")]` ScriptableObject.

## 생명주기
- 참조 캐시: `Awake`. 다른 오브젝트 접근: `Start` 이후.
- 이벤트 구독은 `OnEnable`, 해제는 `OnDisable`. 해제를 빠뜨리지 않는다.
- 물리 이동: `FixedUpdate`. 입력 읽기와 시각 처리: `Update`. 카메라: `LateUpdate` (Cinemachine을 쓰면 직접 짜지 않음).

## 금지 / 대체
| 쓰지 말 것 | 대신 |
|---|---|
| `Input.GetKey`, `Input.GetAxis` | Input System (`InputActionReference`, `PlayerInput`) |
| `rb.velocity` | `rb.linearVelocity` |
| `FindObjectOfType`, `FindObjectsSortMode` 인자 (Unity 6.5 폐기) | 참조 주입. 정말 필요하면 `FindFirstObjectByType` 또는 `FindObjectsByType<T>(FindObjectsInactive)` (Awake에서 1회) |
| `LayerMask.NameToLayer("...")`, `GetMask("...")`, `sortingLayerName = "..."` | `GameLayers.*` |
| Update 안의 `GetComponent`, `Find*`, `Camera.main` | `Awake`에서 캐시 |
| Update 안의 LINQ, `new List<>`, 문자열 보간 | 미리 만든 버퍼 재사용, `NonAlloc`/`List` 오버로드 |
| 물리 오브젝트의 `transform.position +=`, `Translate` | `rb.linearVelocity`, `rb.MovePosition` |
| 자주 생성·파괴 (`Instantiate`/`Destroy` 반복) | `UnityEngine.Pool.ObjectPool<T>` |
| `XxxManager.Instance` 싱글톤 | `GameServices`에 등록 (HX010) |
| `SendMessage`, `Invoke("...")` 등 문자열 호출 | 직접 호출, 이벤트, `StartCoroutine(Method())` (HX011) |
| `async void` | `async Awaitable` + 취소 토큰 (HX012) |
| `Debug.Log` (정보 로그) | `GameLog.Info` (HX013) |
| 정적 가변 필드 | 서비스의 인스턴스 필드 (HX014) |

## 물리 쿼리
- `Physics2D.OverlapCircle(pos, r, filter, results)`처럼 결과 버퍼(`List<Collider2D>` 또는 배열)를 받는 오버로드를 쓰고, 버퍼는 필드로 재사용한다.
- 마스크는 `ContactFilter2D`에 `GameLayers`의 마스크를 넣어 필드로 만들어 둔다.

## 예외 표시
린트 규칙을 의도적으로 어길 때는 줄 끝에 이유와 함께 단다.
```csharp
var all = FindObjectsByType<Harvestable>(FindObjectsInactive.Exclude); // harness-ignore HX001 에디터 디버그 전용
```
