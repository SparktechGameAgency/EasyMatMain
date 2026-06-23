using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StackTower
{
    public class LaserAbility : MonoBehaviour
    {
        public static LaserAbility Instance;

        [Header("Ability Settings")]
        public int maxUses = 3;
        public float windowDuration = 3f;

        [Header("UI")]
        public Button laserButton;
        public TextMeshProUGUI usesText;

        [Header("Active Particle")]
        [Tooltip("World Space particle prefab — spawned behind the button while the laser window is open")]
        public ParticleSystem activeParticlePrefab;

        [Header("Laser Animation")]
        public Image laserImage;
        public Canvas canvas;
        public Sprite[] laserSprites;
        public float frameRate = 0.08f;

        private int usesLeft = 0;
        private bool canUse = false;
        private bool isTrap = false;
        private GameObject targetBlock = null;
        private GameObject belowBlock = null;
        private Coroutine windowCoroutine = null;
        private ParticleSystem spawnedParticle;

        public bool HasUsesLeft => usesLeft > 0;

        void Awake() => Instance = this;

        void Start()
        {
            usesLeft = maxUses;
            if (laserButton != null) laserButton.interactable = true;
            UpdateUI();

            if (laserImage != null) laserImage.gameObject.SetActive(false);
        }

        public void OnBadLanding(GameObject bad, GameObject below)
        {
            if (usesLeft <= 0) return;

            isTrap = false;
            targetBlock = bad;
            belowBlock = below;
            canUse = true;
            SpawnParticle();

            if (windowCoroutine != null) StopCoroutine(windowCoroutine);
            windowCoroutine = StartCoroutine(WindowTimer(triggerDeathOnExpire: false));

            Debug.Log("Laser window open (bad block) for " + windowDuration + "s");
        }

        public void OnTrapLanding(GameObject trap, GameObject below)
        {
            if (usesLeft <= 0)
            {
                TriggerDeathSequence();
                return;
            }

            isTrap = true;
            targetBlock = trap;
            belowBlock = below;
            canUse = true;
            SpawnParticle();

            if (windowCoroutine != null) StopCoroutine(windowCoroutine);
            windowCoroutine = StartCoroutine(WindowTimer(triggerDeathOnExpire: true));

            Debug.Log("Laser window open (trap block) for " + windowDuration + "s");
        }

        IEnumerator WindowTimer(bool triggerDeathOnExpire)
        {
            yield return new WaitForSeconds(windowDuration);
            CloseWindow();

            if (triggerDeathOnExpire)
            {
                Debug.Log("Laser window expired — trap triggers death");
                TriggerDeathSequence();
            }
            else
            {
                Debug.Log("Laser window expired");
            }
        }

        void TriggerDeathSequence()
        {
            if (AlienClimber.Instance != null)
                AlienClimber.Instance.TriggerTrapDeath();
            else
                STGameManager.Instance.AlienReachedTop();
        }

        void CloseWindow()
        {
            canUse = false;
            targetBlock = null;
            belowBlock = null;
            StopParticle();
        }

        public void OnLaserPressed()
        {
            if (!canUse || usesLeft <= 0 || targetBlock == null) return;

            if (STGameManager.Instance != null)
                STGameManager.Instance.LockInput();

            if (windowCoroutine != null) StopCoroutine(windowCoroutine);

            canUse = false;
            usesLeft--;
            UpdateUI();
            StopParticle();

            StartCoroutine(PlayLaserAndReplace());
        }

        IEnumerator PlayLaserAndReplace()
        {
            Vector3 midWorldPos = targetBlock.transform.position;
            BlockController bc = targetBlock.GetComponent<BlockController>();
            if (bc != null && bc.midPoint != null)
                midWorldPos = bc.midPoint.position;

            if (laserImage != null && canvas != null)
            {
                Vector2 screenPos = Camera.main.WorldToScreenPoint(midWorldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.GetComponent<RectTransform>(),
                    screenPos,
                    canvas.worldCamera,
                    out Vector2 localPos
                );
                laserImage.rectTransform.anchoredPosition = localPos;
                laserImage.gameObject.SetActive(true);

                if (laserSprites != null && laserSprites.Length > 0)
                {
                    int midIndex = laserSprites.Length / 2;
                    bool replaced = false;

                    for (int i = 0; i < laserSprites.Length; i++)
                    {
                        laserImage.sprite = laserSprites[i];

                        if (!replaced && i >= midIndex)
                        {
                            TowerManager.Instance.ReplaceWithAlignedBlock(targetBlock, belowBlock, isTrap);
                            targetBlock = null;
                            belowBlock = null;
                            replaced = true;
                        }

                        yield return new WaitForSecondsRealtime(frameRate);
                    }

                    if (!replaced)
                    {
                        TowerManager.Instance.ReplaceWithAlignedBlock(targetBlock, belowBlock, isTrap);
                        targetBlock = null;
                        belowBlock = null;
                    }
                }
                else
                {
                    TowerManager.Instance.ReplaceWithAlignedBlock(targetBlock, belowBlock, isTrap);
                    targetBlock = null;
                    belowBlock = null;
                }

                laserImage.gameObject.SetActive(false);
            }
            else
            {
                TowerManager.Instance.ReplaceWithAlignedBlock(targetBlock, belowBlock, isTrap);
                targetBlock = null;
                belowBlock = null;
            }

            if (STGameManager.Instance != null)
                STGameManager.Instance.UnlockInput();
        }

        void SpawnParticle()
        {
            if (activeParticlePrefab == null) return;
            StopParticle();

            spawnedParticle = Instantiate(
                activeParticlePrefab,
                laserButton != null ? laserButton.transform.position : transform.position,
                Quaternion.identity,
                laserButton != null ? laserButton.transform : transform
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
                Debug.LogWarning("LaserAbility: usesText not assigned!");
        }
    }
}