using UnityEditor;
using UnityEngine;

namespace Game.Editor.Harness
{
    /// <summary>
    /// harness.config.json의 artRoot 아래 텍스처에 픽셀 아트 임포트 설정을 강제한다.
    /// - 항상 강제: Sprite 타입, filterMode 설정값(기본 Bilinear), 압축 없음, Mip Map 끔
    /// - 처음 임포트할 때만: PPU, Wrap Mode, 피벗(bottomPivotFolders면 Bottom Center)
    ///   → 사람이 나중에 Sprite Editor에서 바꾼 피벗/PPU를 덮어쓰지 않는다.
    /// </summary>
    public sealed class PixelArtImportEnforcer : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            HarnessConfig cfg = HarnessConfig.Load();
            if (!cfg.IsUnderArtRoot(assetPath)) return;

            var importer = (TextureImporter)assetImporter;
            bool firstImport = importer.importSettingsMissing;

            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = cfg.ResolvedFilterMode;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            if (!firstImport) return;

            importer.spritePixelsPerUnit = cfg.pixelsPerUnit;
            importer.wrapMode = TextureWrapMode.Clamp; // 반복 배경은 사람이 Repeat로 바꿔도 유지됨

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            if (cfg.UsesBottomPivot(assetPath))
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(0.5f, 0f);
            }
            importer.SetTextureSettings(settings);
        }
    }
}
