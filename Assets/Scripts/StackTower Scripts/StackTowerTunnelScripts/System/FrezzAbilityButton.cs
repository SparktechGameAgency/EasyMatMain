using System.Collections;
using UnityEngine;
using TMPro;

namespace StackTower
{
    public class FrezzAbilityButton : MonoBehaviour
    {
        [Header("Ability Settings")]
        public int maxUses = 5;
        public float freezeDuration = 3f;

        [Header("References")]
        public TextMeshProUGUI usesText;

        private int usesLeft = 0;
        private bool isFreezing = false;

        void Start()
        {
            usesLeft = maxUses;
            UpdateUI();
            gameObject.SetActive(false);
        }

        public void OnAbilityPressed()
        {
            if (AsteroidEvent.Instance == null || !AsteroidEvent.Instance.IsEventActive)
                return;

            if (usesLeft <= 0 || isFreezing)
                return;

            // ✅ Frame-based lock: blocks same-frame GetMouseButtonDown in BlockController
            if (STGameManager.Instance != null)

                usesLeft--;
            UpdateUI();

            StartCoroutine(FreezeCountdown());
        }

        IEnumerator FreezeCountdown()
        {
            isFreezing = true;
            AsteroidEvent.Instance.SetCountdownFrozen(true);

            yield return new WaitForSeconds(freezeDuration);

            AsteroidEvent.Instance.SetCountdownFrozen(false);
            isFreezing = false;
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