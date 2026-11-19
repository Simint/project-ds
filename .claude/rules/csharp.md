---
paths:
  - "Assets/**/*.cs"
---

# C# 규칙 (Unity 6)

## 구조
- 네임스페이스: `Game.<Scripts 아래 폴더명>`. 에디터 코드는 `Game.Editor.<...>`.
- 한 파일에 한 public 타입. 파일 이름 = 타입 이름.
- MonoBehaviour는 작게. 계산 로직은 일반 C# 클래스로 빼서 MonoBehaviour가 호출한다.
- 의존성은 `[SerializeField]` 참조로 받는다. 전역 싱글톤은 `GameServices`(Core) 하나만 허용하고, 거기에 등록된 서비스만 꺼내 쓴다.

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

## 물리 쿼리
- `Physics2D.OverlapCircle(pos, r, filter, results)`처럼 결과 버퍼(`List<Collider2D>` 또는 배열)를 받는 오버로드를 쓰고, 버퍼는 필드로 재사용한다.
- 마스크는 `ContactFilter2D`에 `GameLayers`의 마스크를 넣어 필드로 만들어 둔다.

## 예외 표시
린트 규칙을 의도적으로 어길 때는 줄 끝에 이유와 함께 단다.
```csharp
var all = FindObjectsByType<Harvestable>(FindObjectsInactive.Exclude); // harness-ignore HX001 에디터 디버그 전용
```
