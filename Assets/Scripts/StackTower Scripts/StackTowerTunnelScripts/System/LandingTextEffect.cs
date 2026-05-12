using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StackTower
{
    public class LandingTextEffect : MonoBehaviour
    {
        public static LandingTextEffect Instance;

        [Header("Image References")]
        public Image perfectImage;
        public Image greatImage;
        public Image badImage;

        [Header("Canvas Reference")]
        public Canvas canvas;
        public Camera gameCamera;

        [Header("Animation Settings")]
        public float scaleUpTime = 0.2f;
        public float holdTime = 0.3f;
        public float fadeTime = 0.4f;
        public float peakScale = 1.3f;

        void Awake()
        {
            Instance = this;

            Hide(perfectImage);
            Hide(greatImage);
            Hide(badImage);
        }

        public void ShowPerfect(Vector3 worldPos) => Show(perfectImage, worldPos);
        public void ShowGreat(Vector3 worldPos) => Show(greatImage, worldPos);
        public void ShowBad(Vector3 worldPos) => Show(badImage, worldPos);

        void Show(Image img, Vector3 worldPos)
        {
            if (img == null) return;

            StopAllCoroutines();

            Hide(perfectImage);
            Hide(greatImage);
            Hide(badImage);

            // Convert world pos to canvas pos
            Vector2 canvasPos = WorldToCanvasPos(worldPos);

            RectTransform rt = img.GetComponent<RectTransform>();
            rt.anchoredPosition = canvasPos;

            // Reset state
            SetAlpha(img, 1f);
            rt.localScale = Vector3.zero;
            img.gameObject.SetActive(true);

            StartCoroutine(AnimateImage(img, rt));
        }

        IEnumerator AnimateImage(Image img, RectTransform rt)
        {
            // ── Phase 1: Scale Up ──────────────────────────────
            float t = 0f;
            while (t < scaleUpTime)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / scaleUpTime);
                float scale = Mathf.Lerp(0f, peakScale, EaseOutBack(progress));
                rt.localScale = Vector3.one * scale;
                yield return null;
            }

            rt.localScale = Vector3.one;

            // ── Phase 2: Hold ──────────────────────────────────
            yield return new WaitForSecondsRealtime(holdTime);

            // ── Phase 3: Fade Out ──────────────────────────────
            t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / fadeTime);
                SetAlpha(img, Mathf.Lerp(1f, 0f, progress));
                yield return null;
            }

            Hide(img);
        }

        void Hide(Image img)
        {
            if (img == null) return;
            SetAlpha(img, 1f);
            img.gameObject.SetActive(false);
        }

        void SetAlpha(Image img, float alpha)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }

        Vector2 WorldToCanvasPos(Vector3 worldPos)
        {
            Vector2 screenPos = gameCamera.WorldToScreenPoint(worldPos);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : gameCamera,
                out Vector2 canvasPos
            );

            return canvasPos;
        }

        float EaseOutBack(float x)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}