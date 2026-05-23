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

        private int usesLeft;
        private bool alignAvailable = false;

        void Start()
        {
            usesLeft = maxUses;
            UpdateUI();
            RefreshInteractable();
        }

        public void OnAlignAvailable()
        {
            alignAvailable = true;
            RefreshInteractable();
        }

        public void OnAlignUnavailable()
        {
            alignAvailable = false;
            RefreshInteractable();
        }

        public void OnAbilityPressed()
        {
            if (!alignAvailable || usesLeft <= 0) return;
            if (TowerManager.Instance == null) return;

            // ✅ Frame-based lock: blocks same-frame GetMouseButtonDown in BlockController
            if (STGameManager.Instance != null)

                usesLeft--;
            UpdateUI();
            TowerManager.Instance.UseAlignAbility();
            RefreshInteractable();
        }

        void RefreshInteractable()
        {
            if (button != null)
                button.interactable = alignAvailable && usesLeft > 0;
        }

        void UpdateUI()
        {
            if (usesText != null)
                usesText.text = usesLeft.ToString();
            else
                Debug.LogWarning("AlignAbilityButton: usesText not assigned!");
        }
    }
}