using UnityEngine;

namespace StackTower
{
    public class InputBlocker : MonoBehaviour
    {
        public static InputBlocker Instance;

        [Header("Blocked UI Areas (drag RectTransforms here)")]
        public RectTransform[] blockedAreas;

        [Header("Canvas")]
        public Canvas canvas;

        void Awake()
        {
            Instance = this;
        }

        public bool IsTapBlocked(Vector2 screenPos)
        {
            if (blockedAreas == null) return false;

            foreach (RectTransform area in blockedAreas)
            {
                if (area == null) continue;

                // ✅ Check if tap is inside this RectTransform
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    area,
                    screenPos,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay
                        ? null
                        : Camera.main))
                {
                    Debug.Log("Tap blocked by: " + area.name);
                    return true;
                }
            }

            return false;
        }
    }
}