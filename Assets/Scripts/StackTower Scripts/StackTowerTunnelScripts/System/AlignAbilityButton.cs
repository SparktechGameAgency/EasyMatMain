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
        private bool isArmed = false;

        void Start()
        {
            usesLeft = maxUses;
            if (button != null) button.interactable = true;
            UpdateUI();
        }

        public void OnAlignDisarmed()
        {
            isArmed = false;
        }

        public void OnAlignChargeUsed(int chargesRemaining) { }

        public void OnAlignArmed(int charges)
        {
            isArmed = true;
            Debug.Log("AlignAbilityButton: armed for " + charges + " blocks.");
        }

        public void OnAbilityPressed()
        {
            if (isArmed) return;
            if (usesLeft <= 0) return;
            if (TowerManager.Instance == null) return;

            usesLeft--;
            UpdateUI();
            TowerManager.Instance.UseAlignAbility();
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