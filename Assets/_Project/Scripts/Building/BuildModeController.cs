using System;
using System.Collections.Generic;
using Game.Core;
using Game.Crafting;
using Game.Inventory;
using Game.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Building
{
    /// <summary>
    /// 건설 모드의 유일한 창구 — B로 켜고 끄고, 켜져 있는 동안 커서를 따라 미리보기를 보여주고
    /// 왼쪽 클릭으로 설치·오른쪽 클릭으로 철거한다 (docs/design/GAME_DESIGN.md 3.1, 8). 은신처 씬에만
    /// 있다. 켜지면 PlayerInputReader를 잠가 이동·점프·상호작용 등 평소 조작을 멈춘다.
    /// </summary>
    public sealed class BuildModeController : MonoBehaviour
    {
        [SerializeField] private BuildGrid buildGrid;
        [SerializeField] private StructureDatabase structureDatabase;
        [SerializeField] private PlacementPreview preview;
        [SerializeField] private BuildSettings settings;

        private readonly Dictionary<StructureDefinition, Sprite> previewSpriteCache = new Dictionary<StructureDefinition, Sprite>();
        private readonly Dictionary<StructureDefinition, Vector3> previewOffsetCache = new Dictionary<StructureDefinition, Vector3>();

        // 은신처 씬 오브젝트가 Boot 씬의 플레이어를 계속 참조해야 해서(한 번뿐인 interactor 매개변수로는
        // 안 됨) 직렬화 참조 대신 GameServices를 거친다 — 두 씬이 같이 열려 있어야만 성립하는 크로스 씬
        // 참조를 아예 안 만든다.
        private PlayerInputReader inputReader;
        private PlayerInventory playerInventory;

        private Vector2 cursorWorldPosition;

        public bool IsActive { get; private set; }
        public StructureDefinition SelectedDefinition { get; private set; }
        public IReadOnlyList<StructureDefinition> Options => structureDatabase != null ? structureDatabase.Structures : System.Array.Empty<StructureDefinition>();

        public event Action<bool> ActiveChanged;
        public event Action<StructureDefinition> SelectedDefinitionChanged;

        private void Awake()
        {
            if (buildGrid == null) Debug.LogError("[BuildModeController] buildGrid 참조가 비어 있습니다.", this);
            if (structureDatabase == null) Debug.LogError("[BuildModeController] structureDatabase 참조가 비어 있습니다.", this);
            if (preview == null) Debug.LogError("[BuildModeController] preview 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[BuildModeController] settings 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            GameServices.RegisterBuildModeController(this);
            GameServices.Ready += HandleServicesReady;
            if (GameServices.IsReady) HandleServicesReady();
        }

        private void OnDisable()
        {
            if (GameServices.BuildModeController == (object)this) GameServices.RegisterBuildModeController(null);
            GameServices.Ready -= HandleServicesReady;
            if (inputReader != null)
            {
                inputReader.ToggleBuildModePressed -= HandleTogglePressed;
                inputReader.BuildPlacePressed -= HandlePlacePressed;
                inputReader.BuildDemolishPressed -= HandleDemolishPressed;
            }
            if (IsActive) Deactivate();
        }

        private void HandleServicesReady()
        {
            inputReader = GameServices.PlayerInputReader;
            playerInventory = GameServices.PlayerInventory;
            if (inputReader == null)
            {
                Debug.LogError("[BuildModeController] GameServices.PlayerInputReader가 비어 있습니다.", this);
                return;
            }
            inputReader.ToggleBuildModePressed += HandleTogglePressed;
            inputReader.BuildPlacePressed += HandlePlacePressed;
            inputReader.BuildDemolishPressed += HandleDemolishPressed;
        }

        private void Update()
        {
            if (!IsActive) return;

            if (inputReader.ActiveControlScheme == "Gamepad")
                cursorWorldPosition += inputReader.BuildCursorMoveInput * settings.CursorMoveSpeedTiles * Time.deltaTime;
            else
                cursorWorldPosition = inputReader.PointerWorldPosition;

            RefreshPreview();
        }

        public void SelectDefinition(StructureDefinition definition)
        {
            SelectedDefinition = definition;
            SelectedDefinitionChanged?.Invoke(definition);
        }

        private void HandleTogglePressed()
        {
            if (IsActive) { Deactivate(); return; }
            // 다른 UI가 잠궈 둔 동안이거나(상점·제작대 등 열린 채) 이동 입력을 누르고 있는 동안은
            // 건설 모드로 들어가지 않는다 — 걷다가 갑자기 커서 모드로 바뀌는 걸 막는다.
            if (inputReader != null && (inputReader.IsLockedByOthers(this) || inputReader.MoveInput.sqrMagnitude > 0.01f)) return;
            Activate();
        }

        private void Activate()
        {
            if (buildGrid == null) return;
            IsActive = true;
            cursorWorldPosition = transform.position;
            inputReader?.SetLocked(this, true);
            if (SelectedDefinition == null && Options.Count > 0) SelectDefinition(Options[0]);
            ActiveChanged?.Invoke(true);
        }

        private void Deactivate()
        {
            IsActive = false;
            inputReader?.SetLocked(this, false);
            preview?.Hide();
            ActiveChanged?.Invoke(false);
        }

        private void RefreshPreview()
        {
            if (preview == null || buildGrid == null) return;
            if (SelectedDefinition == null) { preview.Hide(); return; }

            Vector2Int cell = buildGrid.WorldToCell(cursorWorldPosition);
            bool canPlace = buildGrid.CanPlace(SelectedDefinition, cell);
            preview.Refresh(GetPreviewSprite(SelectedDefinition), buildGrid.CellToWorld(cell), GetPreviewOffset(SelectedDefinition), canPlace);
        }

        private Sprite GetPreviewSprite(StructureDefinition definition)
        {
            if (previewSpriteCache.TryGetValue(definition, out Sprite sprite)) return sprite;
            SpriteRenderer renderer = definition.Prefab != null ? definition.Prefab.GetComponentInChildren<SpriteRenderer>() : null;
            sprite = renderer != null ? renderer.sprite : null;
            previewSpriteCache[definition] = sprite;
            return sprite;
        }

        /// <summary>프리팹의 Visual 자식이 루트에서 떨어진 로컬 오프셋(add-structure 스킬 1절). 미리보기가
        /// 실제 설치 위치와 어긋나지 않도록 PlacementPreview.Refresh에 같이 넘긴다.</summary>
        private Vector3 GetPreviewOffset(StructureDefinition definition)
        {
            if (previewOffsetCache.TryGetValue(definition, out Vector3 offset)) return offset;
            SpriteRenderer renderer = definition.Prefab != null ? definition.Prefab.GetComponentInChildren<SpriteRenderer>() : null;
            offset = renderer != null ? renderer.transform.localPosition : Vector3.zero;
            previewOffsetCache[definition] = offset;
            return offset;
        }

        /// <summary>마우스로 건설 메뉴 버튼(BuildMenuUI)을 누른 그 클릭이 그 아래 월드에도 같이 먹어
        /// 설치·철거되지 않게 한다. 패드는 포인터가 없으니 검사하지 않는다.</summary>
        private bool IsPointerOverUI() =>
            inputReader != null && inputReader.ActiveControlScheme == "Keyboard&Mouse"
            && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void HandlePlacePressed()
        {
            if (!IsActive || SelectedDefinition == null || buildGrid == null || IsPointerOverUI()) return;

            Vector2Int cell = buildGrid.WorldToCell(cursorWorldPosition);
            if (!buildGrid.CanPlace(SelectedDefinition, cell)) return;

            var materials = new MaterialSourceAggregator(playerInventory, GameServices.StorageRegistry);
            if (!materials.TryConsume(SelectedDefinition.BuildCost)) return;

            buildGrid.Place(SelectedDefinition, cell);
        }

        private void HandleDemolishPressed()
        {
            if (!IsActive || buildGrid == null || IsPointerOverUI()) return;

            Vector2Int cell = buildGrid.WorldToCell(cursorWorldPosition);
            StructureInstance instance = buildGrid.GetAt(cell);
            if (instance == null || instance.Definition == null) return;

            RefundBuildCost(instance.Definition);
            DumpStorageContents(instance);
            buildGrid.Remove(cell);
        }

        private void RefundBuildCost(StructureDefinition definition)
        {
            foreach (RecipeIngredient ingredient in definition.BuildCost)
                playerInventory.Collect(ingredient.ItemId, ingredient.Count);
        }

        private void DumpStorageContents(StructureInstance instance)
        {
            if (instance.Definition.Station != StationKind.Storage) return;
            StorageContainer storage = instance.GetComponent<StorageContainer>();
            if (storage == null) return;
            foreach ((string itemId, int count, long spoilAtMinute, int durability) in storage.EnumerateSlots())
                playerInventory.CollectWithState(itemId, count, spoilAtMinute, durability);
        }
    }
}
