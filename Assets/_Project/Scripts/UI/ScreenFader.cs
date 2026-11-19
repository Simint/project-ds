using System.Threading;
using UnityEngine;

namespace Game.UI
{
    /// <summary>화면 페이드. 맵 이동 중 MapLoader가 호출한다 (ARCHITECTURE.md 3.3).</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        [SerializeField] private float defaultFadeSeconds = 0.35f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public async Awaitable FadeOut(CancellationToken token)
        {
            await Fade(canvasGroup.alpha, 1f, token);
            canvasGroup.blocksRaycasts = true;
        }

        public async Awaitable FadeIn(CancellationToken token)
        {
            canvasGroup.blocksRaycasts = false;
            await Fade(canvasGroup.alpha, 0f, token);
        }

        private async Awaitable Fade(float from, float to, CancellationToken token)
        {
            float elapsed = 0f;
            while (elapsed < defaultFadeSeconds)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / defaultFadeSeconds);
                await Awaitable.NextFrameAsync(token);
            }
            canvasGroup.alpha = to;
        }
    }
}
