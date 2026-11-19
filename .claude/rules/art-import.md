---
paths:
  - "Assets/_Project/Art/**"
---

# 아트 에셋 규칙

- 모든 텍스처: Sprite, PPU 16, Filter Point, Compression None, Mip Map 끔. `PixelArtImportEnforcer`가 자동 적용하지만, 검증기 경고가 나오면 `.meta`를 확인한다.
- 폴더가 피벗을 정한다.
  - `Characters/`, `Props/`, `Structures/` → Bottom Center (처음 임포트할 때 자동 지정)
  - `Tiles/`, `Backgrounds/`, `Items/`, `FX/`, `UI/` → Center
- 타일은 정확히 16×16. 캐릭터 프레임은 16의 배수 권장 (예: 16×32, 32×32).
- 패럴랙스 배경 이미지의 가로 길이는 화면(480px)보다 길게 하고, 좌우가 이어지게 그린다.
- 파일 이름: `소문자_스네이크.png` (`oak_tree.png`, `player_run.png`). 스프라이트 시트는 `_sheet` 접미사.
- 에이전트는 이미지 파일을 새로 그리거나 바꾸지 않는다. 필요한 아트는 사용자에게 사양(크기, 피벗, 프레임 수)을 적어 요청한다.
