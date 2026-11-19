---
name: debug-layering
description: 스프라이트가 엉뚱한 앞뒤 순서로 보이거나, 플레이어가 무언가에 가려지거나, 오브젝트가 안 보이거나, 점프할 때 앞뒤가 깜빡이거나, 통과해야 할 것에 막히고 막혀야 할 것을 통과할 때 사용한다.
---

# 앞뒤 순서·충돌 문제 진단

먼저 자동 검사를 돌린다.
```bash
python tools/harness/validate.py
```
대부분의 원인이 여기서 바로 나온다. 결과를 사용자에게 요약해 준 뒤 아래를 순서대로 확인한다.

## A. 그리는 순서 문제

1. **Transparency Sort Mode가 Default인가?**
   2D Renderer 에셋에서 Custom Axis (0,1,0)로 되어 있으면 점프할 때 앞뒤가 바뀐다. 횡스크롤에서는 Default로 둔다. (검증기 HX201)
2. **오브젝트가 올바른 Sorting Layer에 있나?**
   `docs/RENDERING_RULES.md` 2장의 판단 순서대로 레이어를 정했는지 본다. `Default` 레이어에 남아 있는 스프라이트는 실수다. (검증기 HX203)
3. **여러 스프라이트로 된 오브젝트에 SortingGroup이 있나?**
   없으면 캐릭터의 팔이 다른 캐릭터 몸 뒤로 들어가는 등 부품끼리 섞인다.
4. **Order in Layer가 쓸데없이 크지 않나?**
   같은 레이어 안에서만 의미가 있다. 다른 레이어보다 앞에 오게 하려고 Order를 올리는 건 잘못된 해결이다. 레이어를 옮긴다.
5. **Z 좌표**
   2D Renderer에서는 Sorting Layer/Order가 같을 때만 카메라 거리(Z)가 영향을 준다. 같은 레이어·같은 Order에서 깜빡이면 Order를 다르게 준다.
6. **패럴랙스 배경이 게임 레이어 앞으로 오면**
   패럴랙스 스크립트가 Sorting Layer를 바꾸는지, 배경 오브젝트가 `Foreground`에 잘못 들어갔는지 확인한다.

## B. 안 보이는 문제
- Sprite가 비어 있음, 알파 0, 카메라 Culling Mask에서 레이어가 빠짐
- 2D 조명: Light 2D의 Target Sorting Layers에 해당 레이어가 빠져 있으면 **완전히 검게** 보인다. Global Light 2D의 대상 레이어도 확인.
- 스프라이트 머티리얼이 `Sprite-Lit-Default`(URP 2D)인지. Built-in 머티리얼이면 분홍색이거나 안 보인다.

## C. 충돌 문제
1. 물리 Layer가 맞나? (`RENDERING_RULES.md` 3장 표)
2. Layer Collision Matrix에서 해당 조합이 켜져 있나?
3. 통과해야 하는데 막힘 → 그 오브젝트에 비트리거 콜라이더가 있는지. 채집 나무·작업대는 트리거만 있어야 한다.
4. 막혀야 하는데 통과 → 콜라이더가 트리거로 되어 있는지, Rigidbody2D가 Kinematic인데 Full Kinematic Contacts가 꺼져 있는지, 빠른 물체인데 Collision Detection이 Discrete인지.
5. 일방 발판: `PlatformEffector2D` + 콜라이더의 **Used By Effector** 체크.
6. 타일맵 지형: `TilemapCollider2D` + `CompositeCollider2D` + 정적 `Rigidbody2D`. 타일 스프라이트의 Physics Shape가 비정상적이지 않은지.

## D. 결과 보고
원인과 고친 방법을 한두 줄로 말하고, 에디터에서 직접 바꿔야 하는 설정은 번호 매긴 체크리스트로 준다.
