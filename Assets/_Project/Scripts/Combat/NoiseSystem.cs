using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 소음 발행/구독 등록부 (ARCHITECTURE.md 2장 원칙 9, 4.6). MonoBehaviour 없이 테스트할 수 있는
    /// 순수 C# 클래스다. 물리 레이어를 새로 만들지 않고, 적마다 OnEnable/OnDisable로 등록·해제하는
    /// 목록을 훑는다.
    /// </summary>
    public sealed class NoiseSystem
    {
        private readonly List<INoiseListener> listeners = new List<INoiseListener>();

        public void Register(INoiseListener listener)
        {
            if (listener != null && !listeners.Contains(listener)) listeners.Add(listener);
        }

        public void Unregister(INoiseListener listener) => listeners.Remove(listener);

        private readonly System.Func<float> radiusMultiplier;

        /// <param name="radiusMultiplier">적이 듣는 반경에 곱할 배율을 돌려준다(폭우의 빗소리가 소음을 묻음,
        /// docs/design/GAME_DESIGN.md 5.6). 비우면 1.</param>
        public NoiseSystem(System.Func<float> radiusMultiplier = null)
        {
            this.radiusMultiplier = radiusMultiplier;
        }

        /// <summary>지금 적용되는 배율(날씨가 없으면 1).</summary>
        public float RadiusMultiplier => radiusMultiplier != null ? radiusMultiplier() : 1f;

        public void Emit(Vector2 position, float radius, NoiseKind kind)
        {
            float heardRadius = radius * RadiusMultiplier;
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnNoiseHeard(position, heardRadius, kind);
        }
    }
}
