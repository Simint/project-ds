using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 공격 한 번이 받는 쪽에 전달하는 정보 (ARCHITECTURE.md 4.6). 출혈은 받는 쪽이 BleedChance로 직접
    /// 굴린다(더러운 물 식중독 확률과 같은 방식, ItemUseController 참고). 넉백은 미는 속도를 그대로
    /// 담아서(방향·세기를 공격한 쪽이 이미 계산) 받는 쪽은 Rigidbody2D에 그대로 적용만 한다.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly DamageKind Kind;
        public readonly float BleedChance;
        public readonly Vector2 KnockbackVelocity;
        public readonly GameObject Attacker;

        public DamageInfo(float amount, DamageKind kind, float bleedChance, Vector2 knockbackVelocity, GameObject attacker)
        {
            Amount = amount;
            Kind = kind;
            BleedChance = bleedChance;
            KnockbackVelocity = knockbackVelocity;
            Attacker = attacker;
        }
    }
}
