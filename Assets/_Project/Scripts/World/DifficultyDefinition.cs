using Game.Core;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 난이도 하나 (ARCHITECTURE.md 4.8 DifficultyDefinition, docs/design/GAME_DESIGN.md 5.6의 8단계 범위·5.8). 생존 스탯
    /// 감소 속도, 적 강도(체력·피해), 상자 보상의 양, 채집물·적 리스폰 시간, 사망 주머니 범위를 바꾼다. 적의 수는
    /// 바꾸지 않는다. 배율 기본값은 1(영향 없음). 실제 값은 비공개 Data/World/Difficulty/ 에셋에만 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/World/Difficulty Definition")]
    public sealed class DifficultyDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [Tooltip("허기·갈증이 줄어드는 속도 배율.")]
        [SerializeField] [Min(0f)] private float survivalDecayMultiplier = 1f;
        [SerializeField] [Min(0f)] private float enemyHealthMultiplier = 1f;
        [SerializeField] [Min(0f)] private float enemyDamageMultiplier = 1f;
        [Tooltip("상자·묻힌 상자 보상 개수 배율(반올림, 최소 1개).")]
        [SerializeField] [Min(0f)] private float lootMultiplier = 1f;
        [Tooltip("채집물·적이 다시 생기기까지 걸리는 시간 배율.")]
        [SerializeField] [Min(0f)] private float respawnTimeMultiplier = 1f;
        [SerializeField] private DeathDropScope deathDropScope = DeathDropScope.Bag;

        public string Id => id;
        public string DisplayName => displayName;
        public float SurvivalDecayMultiplier => survivalDecayMultiplier;
        public float EnemyHealthMultiplier => enemyHealthMultiplier;
        public float EnemyDamageMultiplier => enemyDamageMultiplier;
        public float LootMultiplier => lootMultiplier;
        public float RespawnTimeMultiplier => respawnTimeMultiplier;
        public DeathDropScope DeathDropScope => deathDropScope;
    }
}
