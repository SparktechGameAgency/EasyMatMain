using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
        public Button button;

        [Header("Freeze Effect")]
        public Image freezeImage;
        public float fadeDuration = 0.5f;

        private int usesLeft = 0;
        private bool isFreezing = false;

        void Start()
        {
            usesLeft = maxUses;
            if (button != null) button.interactable = true;
            UpdateUI();
            gameObject.SetActive(false);

            if (freezeImage != null)
                SetImageAlpha(0f);
        }

        public void OnAbilityPressed()
        {
            if (AsteroidEvent.Instance == null || !AsteroidEvent.Instance.IsEventActive) return;
            if (usesLeft <= 0 || isFreezing) return;

            usesLeft--;
            UpdateUI();
            StartCoroutine(FreezeCountdown());
        }

        IEnumerator FreezeCountdown()
        {
            isFreezing = true;
            AsteroidEvent.Instance.SetCountdownFrozen(true);

            yield return StartCoroutine(FadeImage(0f, 1f));
            yield return new WaitForSeconds(freezeDuration);

            AsteroidEvent.Instance.SetCountdownFrozen(false);
            isFreezing = false;

            yield return StartCoroutine(FadeImage(1f, 0f));
        }

        IEnumerator FadeImage(float from, float to)
        {
            if (freezeImage == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                SetImageAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
                yield return null;
            }
            SetImageAlpha(to);
        }

        void SetImageAlpha(float alpha)
        {
            if (freezeImage == null) return;
            Color c = freezeImage.color;
            c.a = alpha;
            freezeImage.color = c;
        }

        void UpdateUI()
        {
            if (usesText != null)
                usesText.text = usesLeft.ToString();
            else
                Debug.LogWarning("FrezzAbilityButton: usesText not assigned!");
        }
    }
}