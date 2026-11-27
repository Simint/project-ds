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

        public void Emit(Vector2 position, float radius, NoiseKind kind)
        {
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnNoiseHeard(position, radius, kind);
        }
    }
}
