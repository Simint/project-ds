using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 레이어 이름·마스크의 유일한 출처. 다른 코드에서 레이어 문자열을 쓰지 않는다 (하네스 HX003).
    /// 이름은 harness.config.json, docs/RENDERING_RULES.md와 일치해야 한다.
    /// 값은 처음 접근할 때 계산한다 (MonoBehaviour 필드 초기화에서 NameToLayer를 부르면 Unity가 오류를 낸다).
    /// </summary>
    public static class GameLayers
    {
        // ---------------- Sorting Layers (뒤 -> 앞) ----------------
        public static class Sorting
        {
            public const string Sky = "Sky";
            public const string FarBackground = "FarBackground";
            public const string Background = "Background";
            public const string BackWall = "BackWall";
            public const string Props = "Props";
            public const string Terrain = "Terrain";
            public const string Entities = "Entities";
            public const string Foreground = "Foreground";
            public const string Effects = "Effects";
            public const string UI = "UI";

            public static int SkyId => Id(Sky);
            public static int FarBackgroundId => Id(FarBackground);
            public static int BackgroundId => Id(Background);
            public static int BackWallId => Id(BackWall);
            public static int PropsId => Id(Props);
            public static int TerrainId => Id(Terrain);
            public static int EntitiesId => Id(Entities);
            public static int ForegroundId => Id(Foreground);
            public static int EffectsId => Id(Effects);
            public static int UIId => Id(UI);

            /// <summary>렌더러에 Sorting Layer를 지정한다. renderer.sortingLayerName = "..." 대신 사용.</summary>
            public static void Apply(Renderer renderer, string layer, int order = 0)
            {
                renderer.sortingLayerID = Id(layer);
                renderer.sortingOrder = order;
            }

            private static int Id(string name)
            {
                int id = SortingLayer.NameToID(name);
                if (SortingLayer.IDToName(id) != name)
                    Debug.LogError($"[GameLayers] Sorting Layer '{name}' 없음. Tools > Harness > Apply Project Settings 실행.");
                return id;
            }
        }

        // ---------------- Physics Layers ----------------
        private static int player = -2, enemy = -2, ground = -2, oneWayPlatform = -2, structure = -2,
                           interactable = -2, pickup = -2, playerAttack = -2, enemyAttack = -2;

        public static int Player => Resolve(ref player, "Player");
        public static int Enemy => Resolve(ref enemy, "Enemy");
        public static int Ground => Resolve(ref ground, "Ground");
        public static int OneWayPlatform => Resolve(ref oneWayPlatform, "OneWayPlatform");
        public static int Structure => Resolve(ref structure, "Structure");
        public static int Interactable => Resolve(ref interactable, "Interactable");
        public static int Pickup => Resolve(ref pickup, "Pickup");
        public static int PlayerAttack => Resolve(ref playerAttack, "PlayerAttack");
        public static int EnemyAttack => Resolve(ref enemyAttack, "EnemyAttack");

        // ---------------- Masks (처음 접근 시 한 번 계산, 이후 캐시) ----------------
        private static int groundCheckMask = -1, solidMask = -1, actorMask = -1,
                           buildBlockingMask = -1, interactableMask = -1, pickupMask = -1, playerMask = -1;

        /// <summary>땅 판정: 서 있을 수 있는 모든 것.</summary>
        public static LayerMask GroundCheckMask => Cached(ref groundCheckMask, Ground, OneWayPlatform, Structure);

        /// <summary>단단한 것 (일방 발판 제외). 벽 판정, 시야 차단.</summary>
        public static LayerMask SolidMask => Cached(ref solidMask, Ground, Structure);

        /// <summary>몸이 있는 캐릭터.</summary>
        public static LayerMask ActorMask => Cached(ref actorMask, Player, Enemy);

        /// <summary>건설 시 셀이 막혔는지 확인할 대상. Interactable도 넣어 장애물(HubObstacle)·채집물·
        /// 상자 위에는 못 짓게 한다 (MVP 5단계부터 실제로 쓰임).</summary>
        public static LayerMask BuildBlockingMask => Cached(ref buildBlockingMask, Ground, Structure, Player, Enemy, Interactable);

        public static LayerMask InteractableMask => Cached(ref interactableMask, Interactable);
        public static LayerMask PickupMask => Cached(ref pickupMask, Pickup);

        /// <summary>플레이어 몸만. 문이 닫히기 전 그 칸에 플레이어가 서 있는지 확인할 때 쓴다.</summary>
        public static LayerMask PlayerMask => Cached(ref playerMask, Player);

        public static bool IsInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        private static LayerMask Cached(ref int cache, int a, int b = -1, int c = -1, int d = -1, int e = -1)
        {
            if (cache == -1)
            {
                int m = 0;
                if (a >= 0) m |= 1 << a;
                if (b >= 0) m |= 1 << b;
                if (c >= 0) m |= 1 << c;
                if (d >= 0) m |= 1 << d;
                if (e >= 0) m |= 1 << e;
                cache = m;
            }
            return cache;
        }

        private static int Resolve(ref int cache, string name)
        {
            if (cache == -2)
            {
                cache = LayerMask.NameToLayer(name);
                if (cache < 0)
                    Debug.LogError($"[GameLayers] 물리 Layer '{name}' 없음. Tools > Harness > Apply Project Settings 실행.");
            }
            return cache;
        }
    }
}
