using System.Collections;
using UnityEngine;
using TMPro;

namespace StackTower
{
    public class AbilityButton : MonoBehaviour
    {
        [Header("Ability Settings")]
        public int maxUses = 5;   // ✅ total uses available
        public float freezeDuration = 3f;  // ✅ how long countdown freezes

        [Header("References")]
        public TextMeshProUGUI usesText; // ✅ TMP on button showing uses left

        private int usesLeft = 0;
        private bool isFreezing = false;

        void Start()
        {
            usesLeft = maxUses;
            UpdateUI();
        }

        // ── Called by Button OnClick ──────────────────────────────
        public void OnAbilityPressed()
        {
            // Does nothing outside event
            if (AsteroidEvent.Instance == null || !AsteroidEvent.Instance.IsEventActive)
                return;

            // No uses left
            if (usesLeft <= 0)
            {
                Debug.Log("No ability uses left!");
                return;
            }

            // Already freezing
            if (isFreezing)
            {
                Debug.Log("Ability already active!");
                return;
            }

            usesLeft--;
            UpdateUI();

            StartCoroutine(FreezeCountdown());
            Debug.Log("Ability used! Uses left: " + usesLeft);
        }

        IEnumerator FreezeCountdown()
        {
            isFreezing = true;

            // ✅ Freeze the asteroid countdown
            AsteroidEvent.Instance.SetCountdownFrozen(true);
            Debug.Log("Countdown frozen for " + freezeDuration + "s");

            yield return new WaitForSeconds(freezeDuration);

            // ✅ Unfreeze countdown
            AsteroidEvent.Instance.SetCountdownFrozen(false);
            isFreezing = false;
            Debug.Log("Countdown resumed!");
        }

        void UpdateUI()
        {
            if (usesText != null)
                usesText.text = usesLeft.ToString();
            else
                Debug.LogWarning("AbilityButton: usesText not assigned!");
        }
    }
}