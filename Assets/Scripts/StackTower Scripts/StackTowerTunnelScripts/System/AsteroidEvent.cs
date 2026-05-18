using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StackTower
{
    public class AsteroidEvent : MonoBehaviour
    {
        public static AsteroidEvent Instance;

        [Header("Event Timing")]
        public float minInterval = 30f;
        public float maxInterval = 60f;

        [Header("Block Requirement")]
        public int minBlocksRequired = 3;
        public int maxBlocksRequired = 6;
        public float countdownDuration = 15f;

        [Header("Warning Settings")]
        public Image warningImage;
        public Sprite[] warningSprites;          // ✅ warning animation frames
        public float warningFrameRate = 0.1f;    // ✅ warning animation speed
        public float warningDuration = 3f;
        public float warningFadeSpeed = 2f;

        [Header("Event UI")]
        public TextMeshProUGUI countdownText;
        public TextMeshProUGUI blockRequirementText;

        [Header("Asteroid Animation")]
        public Image asteroidImage;
        public Sprite[] asteroidSprites;
        public float frameRate = 0.08f;

        // ── Private State ────────────────────────────────────────
        private bool isEventActive = false;
        private int blocksRequired = 0;
        private int blocksPlaced = 0;
        private float countdownTimer = 0f;

        void Awake() => Instance = this;

        void Start()
        {
            HideAll();
            StartCoroutine(WaitThenWarn());
        }

        void HideAll()
        {
            SetImageAlpha(warningImage, 0f);
            SetImageAlpha(asteroidImage, 0f);

            if (warningImage != null) warningImage.gameObject.SetActive(false);
            if (asteroidImage != null) asteroidImage.gameObject.SetActive(false);

            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (blockRequirementText != null) blockRequirementText.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!isEventActive) return;

            countdownTimer -= Time.deltaTime;
            UpdateEventUI();

            if (countdownTimer <= 0f)
            {
                countdownTimer = 0f;
                isEventActive = false;
                StartCoroutine(AsteroidImpact());
            }
        }

        // ── Step 1: Wait random interval ─────────────────────────
        IEnumerator WaitThenWarn()
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            Debug.Log("Next asteroid event in: " + waitTime.ToString("F1") + "s");
            yield return new WaitForSeconds(waitTime);

            if (STGameManager.Instance != null && !STGameManager.Instance.gameActive)
                yield break;

            // ── Step 2: Play warning flash ────────────────────────
            yield return StartCoroutine(PlayWarning());

            // ── Step 3: Start event ───────────────────────────────
            TriggerEvent();
        }

        // ── Warning fade in/out + sprite animation ───────────────
        IEnumerator PlayWarning()
        {
            if (warningImage == null) yield break;

            warningImage.gameObject.SetActive(true);
            SetImageAlpha(warningImage, 0f);

            // ✅ Set first frame immediately
            if (warningSprites != null && warningSprites.Length > 0)
                warningImage.sprite = warningSprites[0];

            float elapsed = 0f;
            float frameTimer = 0f;
            int frameIndex = 0;

            while (elapsed < warningDuration)
            {
                // Sine wave for smooth fade in/out
                float alpha = (Mathf.Sin(elapsed * warningFadeSpeed * Mathf.PI) + 1f) / 2f;
                SetImageAlpha(warningImage, alpha);

                // ✅ Cycle warning sprites while fading simultaneously
                if (warningSprites != null && warningSprites.Length > 0)
                {
                    frameTimer += Time.deltaTime;
                    if (frameTimer >= warningFrameRate)
                    {
                        frameTimer = 0f;
                        frameIndex = (frameIndex + 1) % warningSprites.Length;
                        warningImage.sprite = warningSprites[frameIndex];
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Hide warning before event starts
            SetImageAlpha(warningImage, 0f);
            warningImage.gameObject.SetActive(false);

            Debug.Log("Warning done — event starting!");
        }

        // ── Trigger the actual event ─────────────────────────────
        void TriggerEvent()
        {
            isEventActive = true;
            blocksPlaced = 0;
            blocksRequired = Random.Range(minBlocksRequired, maxBlocksRequired + 1);
            countdownTimer = countdownDuration;

            if (countdownText != null) countdownText.gameObject.SetActive(true);
            if (blockRequirementText != null) blockRequirementText.gameObject.SetActive(true);

            UpdateEventUI();

            Debug.Log("Asteroid event! Need " + blocksRequired + " blocks in " + countdownDuration + "s");
        }

        // ── Called by STGameManager on every block stacked ───────
        public void OnBlockPlaced()
        {
            if (!isEventActive) return;

            blocksPlaced++;
            UpdateEventUI();
            Debug.Log("Blocks placed: " + blocksPlaced + "/" + blocksRequired);

            if (blocksPlaced >= blocksRequired)
                EventSuccess();
        }

        // ── Player succeeded ─────────────────────────────────────
        void EventSuccess()
        {
            isEventActive = false;
            Debug.Log("Asteroid event SUCCESS!");

            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (blockRequirementText != null) blockRequirementText.gameObject.SetActive(false);

            StartCoroutine(WaitThenWarn());
        }

        // ── Player failed — asteroid hits ────────────────────────
        IEnumerator AsteroidImpact()
        {
            Debug.Log("Asteroid impact!");

            // Hide event UI
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (blockRequirementText != null) blockRequirementText.gameObject.SetActive(false);

            // Lock all input
            if (STGameManager.Instance != null)
                STGameManager.Instance.isInputLocked = true;

            // Show asteroid image
            if (asteroidImage != null)
            {
                asteroidImage.gameObject.SetActive(true);
                SetImageAlpha(asteroidImage, 1f);
            }

            // Play sprite animation (realtime so freeze doesn't block it)
            if (asteroidSprites != null && asteroidSprites.Length > 0
                && asteroidImage != null)
            {
                foreach (Sprite frame in asteroidSprites)
                {
                    asteroidImage.sprite = frame;
                    yield return new WaitForSecondsRealtime(frameRate);
                }
            }

            // Game Over
            if (STGameManager.Instance != null)
                STGameManager.Instance.AsteroidGameOver();
        }

        // ── Update event UI ──────────────────────────────────────
        void UpdateEventUI()
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(countdownTimer).ToString();

            if (blockRequirementText != null)
                blockRequirementText.text = Mathf.Max(0, blocksRequired - blocksPlaced).ToString();
        }

        // ── Helper ───────────────────────────────────────────────
        void SetImageAlpha(Image img, float alpha)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }
}