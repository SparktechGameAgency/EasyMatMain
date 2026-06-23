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

        [Header("Active Particle")]
        [Tooltip("World Space particle prefab — spawned behind the button while armed")]
        public ParticleSystem activeParticlePrefab;

        private int usesLeft;
        private bool isArmed = false;
        private ParticleSystem spawnedParticle;

        void Start()
        {
            usesLeft = maxUses;
            if (button != null) button.interactable = true;
            UpdateUI();
        }

        public void OnAlignDisarmed()
        {
            isArmed = false;
            StopParticle();
        }

        public void OnAlignChargeUsed(int chargesRemaining) { }

        public void OnAlignArmed(int charges)
        {
            isArmed = true;
            SpawnParticle();
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

        void SpawnParticle()
        {
            if (activeParticlePrefab == null) return;
            StopParticle(); // clear any leftover

            spawnedParticle = Instantiate(
                activeParticlePrefab,
                transform.position,
                Quaternion.identity,
                transform          // parent to button so it follows if layout shifts
            );
            spawnedParticle.Play();
        }

        void StopParticle()
        {
            if (spawnedParticle == null) return;
            spawnedParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(spawnedParticle.gameObject);
            spawnedParticle = null;
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