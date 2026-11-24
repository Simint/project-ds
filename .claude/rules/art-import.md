---
paths:
  - "Assets/_Project/Art/**"
---

# 아트 에셋 규칙

도트풍 일반 2D 렌더링이다 (진짜 도트/Pixel Perfect가 아니다, `docs/RENDERING_RULES.md` 1장). 화면은 모니터
해상도로 그대로 그리고, 확대해 둔 도트 그림을 크기만 맞춰 넣는다.

- 모든 텍스처: Sprite, PPU 128, Filter Mode는 `harness.config.json`의 `filterMode`(기본 Bilinear), Compression None, Mip Map 끔. `PixelArtImportEnforcer`가 자동 적용하지만, 검증기 경고가 나오면 `.meta`를 확인한다.
- 폴더가 피벗을 정한다.
  - `Characters/`, `Props/`, `Structures/` → Bottom Center (처음 임포트할 때 자동 지정)
  - `Tiles/`, `Backgrounds/`, `Items/`, `FX/`, `UI/` → Center
- 1유닛(타일 1칸) = 이미지 128px. 캐릭터는 키 2유닛 = 높이 256px 기준(점 굵기를 맞추기 위함, `docs/design/GAME_DESIGN.md` 3.0절). 프레임은 128의 배수 권장 (예: 128×256, 256×256).
- 패럴랙스 배경 이미지의 가로 길이는 화면 가로 폭보다 길게 하고, 좌우가 이어지게 그린다.
- 파일 이름: `소문자_스네이크.png` (`oak_tree.png`, `player_run.png`). 스프라이트 시트는 `_sheet` 접미사.
- 에이전트는 이미지 파일을 새로 그리거나 바꾸지 않는다. 필요한 아트는 사용자에게 사양(크기, 피벗, 프레임 수)을 적어 요청한다. 기존 그림을 최근접 방식으로 정수배 확대·축소하는 임포트 기준 변경(지금 128px 전환처럼)은 예외다.
