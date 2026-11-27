using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 달릴 때(두 발·네 발 모두) 일정 간격으로 소음을 낸다 (ARCHITECTURE.md 4.6, docs/design/GAME_DESIGN.md
    /// 6.2). 앉아 걷기는 소음을 내지 않는다(6.5). 걷기는 소음원이 아니다(ARCHITECTURE.md 4.6 "소음원:
    /// 달리기·네 발 달리기").
    /// </summary>
    public sealed class PlayerNoiseEmitter : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerHands hands;
        [SerializeField] private NoiseSettings settings;

        private float intervalRemaining;

        private void Awake()
        {
            if (inputReader == null) Debug.LogError("[PlayerNoiseEmitter] inputReader 참조가 비어 있습니다.", this);
            if (hands == null) Debug.LogError("[PlayerNoiseEmitter] hands 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[PlayerNoiseEmitter] settings 참조가 비어 있습니다.", this);
        }

        private void Update()
        {
            if (inputReader == null || settings == null) return;

            bool crouching = inputReader.CrouchHeld;
            bool running = inputReader.RunHeld;
            bool moving = inputReader.MoveInput.sqrMagnitude > 0.0001f;

            if (crouching || !running || !moving)
            {
                intervalRemaining = 0f; // 멈추면 리셋해, 다시 뛰기 시작할 때 바로 한 번 울린다.
                return;
            }

            intervalRemaining -= Time.deltaTime;
            if (intervalRemaining > 0f) return;
            intervalRemaining = settings.FootstepIntervalSeconds;

            bool beast = hands != null && hands.Mode == HandMode.Beast;
            float radius = beast ? settings.BeastRunNoiseRadiusTiles : settings.RunNoiseRadiusTiles;
            GameServices.NoiseSystem?.Emit(transform.position, radius, NoiseKind.Footstep);
        }
    }
}
