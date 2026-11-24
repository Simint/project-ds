---
name: tune-movement
description: 횡스크롤 플레이어의 이동·점프·낙하 느낌을 조정하거나 "점프가 둔하다", "미끄럽다", "벽에 붙는다", "발판에서 떨어진다" 같은 조작감 문제를 고칠 때 사용한다.
---

# 이동·점프 조정

## 1. 수치는 한 곳에
모든 값은 `PlayerMovementSettings` ScriptableObject에 있다. 코드에 상수로 박지 않는다.
값은 **타일 단위**로 생각하고 코드에서 물리 값으로 바꾼다. 사용자에게도 타일 단위로 보여 준다.

| 파라미터 | 시작값 | 의미 |
|---|---|---|
| `maxRunSpeed` | 7 tiles/s | 최고 달리기 속도 |
| `accelTime` | 0.08 s | 정지 → 최고 속도 |
| `decelTime` | 0.06 s | 최고 속도 → 정지 |
| `airControl` | 0.7 | 공중 가속 배율 |
| `jumpHeight` | 3.2 tiles | 점프 버튼을 끝까지 눌렀을 때 |
| `timeToApex` | 0.38 s | 정점까지 걸리는 시간 |
| `fallGravityMultiplier` | 1.8 | 내려올 때 중력 배율 |
| `jumpCutMultiplier` | 0.5 | 버튼을 일찍 떼면 상승 속도 배율 |
| `maxFallSpeed` | 18 tiles/s | 낙하 속도 상한 |
| `coyoteTime` | 0.1 s | 발판을 벗어난 뒤에도 점프 허용 |
| `jumpBuffer` | 0.1 s | 착지 직전 입력 기억 |

중력과 점프 속도는 `jumpHeight`, `timeToApex`에서 계산한다.
- `gravity = 2 * jumpHeight / timeToApex²`
- `jumpVelocity = gravity * timeToApex`
- `Rigidbody2D.gravityScale = gravity / |Physics2D.gravity.y|` (낙하 중에는 `* fallGravityMultiplier`)

## 2. 흔한 문제와 해결
| 증상 | 원인 | 해결 |
|---|---|---|
| 벽에 붙어서 안 떨어짐 | 콜라이더 마찰 | 플레이어 콜라이더에 마찰 0인 `PhysicsMaterial2D` |
| 타일 이음새에 걸림 | 타일마다 개별 콜라이더 | 지형 타일맵에 `CompositeCollider2D` (Outlines), 플레이어는 Capsule |
| 발판 끝에서 점프가 씹힘 | 코요테 타임 없음 | `coyoteTime` |
| 착지 직전 점프가 씹힘 | 점프 버퍼 없음 | `jumpBuffer` |
| 화면이 떨림 | 보간 없음 | Rigidbody2D Interpolate, Cinemachine Damping 조정 |
| 경사로에서 미끄러짐 | 정지 시 중력 | 땅 위·입력 없음이면 경사 방향 속도 0, 또는 경사 없이 계단식 지형 |
| 일방 발판에서 내려가기 안 됨 | 미구현 | 아래+점프 시 0.25초간 해당 발판 콜라이더와 충돌 무시 (`Physics2D.IgnoreCollision`) |

## 3. 절차
1. 사용자가 말한 느낌을 위 파라미터 중 무엇으로 바꿀지 정리해 보여 준다 (변경 전 → 후).
2. `PlayerMovementSettings` 에셋 값을 바꾸는 체크리스트를 주거나, 사용자가 허락하면 `.asset`을 직접 고친다 (`.claude/rules/unity-yaml.md`).
3. 코드 변경이 필요하면 `PlayerController`의 해당 상태 클래스만 고친다.
4. 사용자에게 플레이 테스트 항목을 준다: 제자리 점프 높이(타일 수 세기), 3칸 간격 뛰어넘기, 발판 끝 점프, 짧게 누른 점프.
