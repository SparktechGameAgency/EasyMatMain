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

        [Header("Laser Animation")]
        public Image laserImage;
        public Canvas canvas;
        public Sprite[] laserSprites;
        public float frameRate = 0.08f;

        private int usesLeft = 0;
        private bool canUse = false;
        private bool isTrap = false;
        private GameObject targetBlock = null;  // bad block or trap block
        private GameObject belowBlock = null;
        private Coroutine windowCoroutine = null;

        public bool HasUsesLeft => usesLeft > 0;

        void Awake() => Instance = this;

        void Start()
        {
            usesLeft = maxUses;
            UpdateUI();
            RefreshInteractable();

            if (laserImage != null) laserImage.gameObject.SetActive(false);
        }

        // ── Called by TowerManager when a normal block lands bad ──────────
        public void OnBadLanding(GameObject bad, GameObject below)
        {
            if (usesLeft <= 0) return;

            isTrap = false;
            targetBlock = bad;
            belowBlock = below;
            canUse = true;
            RefreshInteractable();

            if (windowCoroutine != null) StopCoroutine(windowCoroutine);
            windowCoroutine = StartCoroutine(WindowTimer(triggerDeathOnExpire: false));

            Debug.Log("Laser window open (bad block) for " + windowDuration + "s");
        }

        // ── Called by STGameManager when a trap block lands ───────────────
        public void OnTrapLanding(GameObject trap, GameObject below)
        {
            if (usesLeft <= 0)
            {
                // Shouldn't reach here (STGameManager checks HasUsesLeft first)
                TriggerDeathSequence();
                return;
            }

            isTrap = true;
            targetBlock = trap;
            belowBlock = below;
            canUse = true;
            RefreshInteractable();

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
            RefreshInteractable();
        }

        // ── Called by laser button OnClick ───────────────────────────────
        public void OnLaserPressed()
        {
            if (!canUse || usesLeft <= 0 || targetBlock == null) return;

            if (STGameManager.Instance != null)
                STGameManager.Instance.LockInput();

            if (windowCoroutine != null) StopCoroutine(windowCoroutine);

            canUse = false;
            usesLeft--;
            UpdateUI();
            RefreshInteractable();

            StartCoroutine(PlayLaserAndReplace());
        }

        IEnumerator PlayLaserAndReplace()
        {
            // ── 1. Capture mid point world position BEFORE touching the block ──
            Vector3 midWorldPos = targetBlock.transform.position; // fallback
            BlockController bc = targetBlock.GetComponent<BlockController>();
            if (bc != null && bc.midPoint != null)
                midWorldPos = bc.midPoint.position;

            // ── 2. Play animation — replace block at the middle frame ──────────
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

                        // ── Spawn replacement at middle frame ─────
                        if (!replaced && i >= midIndex)
                        {
                            TowerManager.Instance.ReplaceWithAlignedBlock(targetBlock, belowBlock, isTrap);
                            targetBlock = null;
                            belowBlock = null;
                            replaced = true;
                        }

                        yield return new WaitForSecondsRealtime(frameRate);
                    }

                    // Fallback — if sprites array was empty somehow
                    if (!replaced)
                    {
                        TowerManager.Instance.ReplaceWithAlignedBlock(targetBlock, belowBlock, isTrap);
                        targetBlock = null;
                        belowBlock = null;
                    }
                }
                else
                {
                    // No sprites — replace immediately
                    TowerManager.Instance.ReplaceWithAlignedBlock(targetBlock, belowBlock, isTrap);
                    targetBlock = null;
                    belowBlock = null;
                }

                laserImage.gameObject.SetActive(false);
            }
            else
            {
                // No image — replace immediately
                TowerManager.Instance.ReplaceWithAlignedBlock(targetBlock, belowBlock, isTrap);
                targetBlock = null;
                belowBlock = null;
            }

            // ── 4. Unlock input after animation ───────────────────────────────
            if (STGameManager.Instance != null)
                STGameManager.Instance.UnlockInput();
        }

        void RefreshInteractable()
        {
            if (laserButton != null)
                laserButton.interactable = canUse && usesLeft > 0;
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