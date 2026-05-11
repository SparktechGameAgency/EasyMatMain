using System.Collections;
using UnityEngine;

namespace StackTower
{
    public class BubbleAnimation : MonoBehaviour
    {
        [Header("Normal Pulse Settings")]
        public float normalMinScale = 0.95f; // smallest size in normal loop
        public float normalMaxScale = 1.05f; // biggest size in normal loop
        public float normalSpeed = 1.5f;  // speed of normal pulse

        [Header("Score Burst Settings")]
        public float burstScale = 1.4f;  // how big it gets on score
        public float burstScaleSpeed = 8f;    // how fast it grows on score
        public float burstReturnSpeed = 4f;   // how fast it returns to normal loop

        private Vector3 originalScale;
        private bool isBursting = false;

        void Start()
        {
            originalScale = transform.localScale;
            StartCoroutine(NormalPulseLoop());
        }

        // ─── Normal Pulse Loop ───────────────────────────────────
        IEnumerator NormalPulseLoop()
        {
            while (true)
            {
                // Wait if bursting
                if (isBursting)
                {
                    yield return null;
                    continue;
                }

                // Scale UP
                yield return ScaleTo(
                    originalScale * normalMaxScale,
                    normalSpeed,
                    EaseInOut
                );

                if (isBursting) { yield return null; continue; }

                // Scale DOWN
                yield return ScaleTo(
                    originalScale * normalMinScale,
                    normalSpeed,
                    EaseInOut
                );
            }
        }

        // ─── Call this when score is added ──────────────────────
        public void PlayScoreBurst()
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(ScoreBurst());
        }

        IEnumerator ScoreBurst()
        {
            isBursting = true;

            // Burst UP fast
            yield return ScaleTo(
                originalScale * burstScale,
                burstScaleSpeed,
                EaseOutBack
            );

            // Return to normal size smoothly
            yield return ScaleTo(
                originalScale,
                burstReturnSpeed,
                EaseInOut
            );

            isBursting = false;
        }

        // ─── Generic Scale Coroutine ─────────────────────────────
        IEnumerator ScaleTo(Vector3 targetScale, float speed, System.Func<float, float> easing)
        {
            Vector3 startScale = transform.localScale;
            float t = 0f;
            float duration = 1f / speed;

            while (t < duration)
            {
                if (isBursting && easing == (System.Func<float, float>)EaseInOut)
                {
                    yield break; // interrupt normal loop if burst starts
                }

                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / duration);
                transform.localScale = Vector3.LerpUnclamped(
                    startScale,
                    targetScale,
                    easing(progress)
                );

                yield return null;
            }

            transform.localScale = targetScale;
        }

        // ─── Easing Functions ────────────────────────────────────
        float EaseInOut(float x)
        {
            return x < 0.5f
                ? 2f * x * x
                : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;
        }

        float EaseOutBack(float x)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}