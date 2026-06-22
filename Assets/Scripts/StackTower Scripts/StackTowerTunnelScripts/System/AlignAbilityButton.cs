using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StackTower
{
    public class AlignAbilityButton : MonoBehaviour
    {
        [Header("Ability Settings")]
        public int maxUses = 3;

        [Header("References")]
        public TextMeshProUGUI usesText;
        public Button button;

        [Header("Armed Glow")]
        [Tooltip("Optional Image that becomes visible when the power-up is armed (neon purple glow overlay)")]
        public Image glowOverlay;
        public Color armedColor = new Color(0.72f, 0.20f, 1.00f, 1f); // neon purple
        public Color unarmedColor = new Color(1f, 1f, 1f, 1f); // normal white

        private int usesLeft;
        private bool isArmed = false;

        void Start()
        {
            usesLeft = maxUses;
            UpdateUI();
            RefreshInteractable();
            SetGlow(false);
        }

        // ── Called by TowerManager after arm is consumed (all 3 blocks placed) ──
        public void OnAlignDisarmed()
        {
            isArmed = false;
            SetGlow(false);
            RefreshInteractable();
        }

        // ── Called by TowerManager each time one charge is used ───────────────
        public void OnAlignChargeUsed(int chargesRemaining)
        {
            // Optional: update usesText to show remaining charges in current burst.
            // We leave it showing total activations left (usesLeft) instead,
            // but you can swap the line below if you prefer the countdown.
            // usesText.text = chargesRemaining.ToString();
        }

        // ── Called by TowerManager when the power-up is armed ────────────────
        public void OnAlignArmed(int charges)
        {
            isArmed = true;
            SetGlow(true);
            RefreshInteractable();
            Debug.Log("AlignAbilityButton: armed for " + charges + " blocks.");
        }

        // ── Button OnClick ────────────────────────────────────────────────────
        public void OnAbilityPressed()
        {
            if (isArmed || usesLeft <= 0) return;
            if (TowerManager.Instance == null) return;

            usesLeft--;
            UpdateUI();
            TowerManager.Instance.UseAlignAbility();   // arms the power-up in TowerManager
            RefreshInteractable();
        }

        // ─────────────────────────────────────────────────────────────────────

        void RefreshInteractable()
        {
            if (button != null)
                button.interactable = !isArmed && usesLeft > 0;
        }

        void UpdateUI()
        {
            if (usesText != null)
                usesText.text = usesLeft.ToString();
            else
                Debug.LogWarning("AlignAbilityButton: usesText not assigned!");
        }

        void SetGlow(bool on)
        {
            // Tint the button image neon purple while armed
            if (button != null && button.image != null)
                button.image.color = on ? armedColor : unarmedColor;

            // If you have a separate glow overlay sprite, show/hide it
            if (glowOverlay != null)
                glowOverlay.gameObject.SetActive(on);
        }
    }
}