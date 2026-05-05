using System.Collections;
using UnityEngine;
using TMPro;

namespace StackTower
{
    public class STGameManager : MonoBehaviour
    {
        public static STGameManager Instance;

        [Header("Settings")]
        public int maxMisses = 3;

        [Header("Spawn Settings")]
        public float spawnDelay = 0.5f; // ✅ seconds after tap before next block spawns

        [Header("Game Over Settings")]
        public bool canGameOver = true;

        [Header("References")]
        public Transform blockSpawnPoint;
        public Transform deathZone;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI livesText;
        public GameObject gameOverPanel;

        [HideInInspector] public int score = 0;
        private int missCount = 0;
        private bool gameActive = true;
        private bool waitingToSpawn = false; // prevents double spawn

        void Awake() => Instance = this;

        void Start()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            else
                Debug.LogWarning("STGameManager: gameOverPanel is not assigned!");

            UpdateUI();
            SpawnNextBlock(); // first block, no delay
        }

        // ✅ Called by BlockController the moment player taps
        public void OnPlayerTapped()
        {
            if (!gameActive || waitingToSpawn) return;
            StartCoroutine(SpawnAfterDelay());
        }

        IEnumerator SpawnAfterDelay()
        {
            waitingToSpawn = true;
            yield return new WaitForSeconds(spawnDelay);
            waitingToSpawn = false;
            SpawnNextBlock();
        }

        public void SpawnNextBlock()
        {
            if (!gameActive) return;

            if (blockSpawnPoint == null)
            {
                Debug.LogError("STGameManager: blockSpawnPoint not assigned!");
                return;
            }
            if (deathZone == null)
            {
                Debug.LogError("STGameManager: deathZone not assigned!");
                return;
            }
            if (ObjectPool.Instance == null)
            {
                Debug.LogError("STGameManager: ObjectPool is null!");
                return;
            }

            GameObject block = ObjectPool.Instance.GetBlock();
            if (block == null)
            {
                Debug.LogError("STGameManager: Pool returned null!");
                return;
            }

            BlockController bc = block.GetComponent<BlockController>();
            if (bc == null)
            {
                Debug.LogError("Missing BlockController on prefab!");
                return;
            }

            bc.Initialize(blockSpawnPoint, deathZone);
        }

        // ─── Normal block landed ─────────────────────────────────
        // ✅ Only updates score — spawn is triggered by tap, not landing
        public void BlockStacked()
        {
            score++;
            UpdateUI();
        }

        // ─── Normal block fell into void ─────────────────────────
        // ✅ Only updates lives — spawn is triggered by tap, not void
        public void BlockMissed(GameObject block)
        {
            ObjectPool.Instance.ReturnBlock(block);
            missCount++;
            UpdateUI();

            if (missCount >= maxMisses)
            {
                GameOver();
                if (!canGameOver)
                    return; // keep playing, next spawn comes from tap
            }
        }

        // ─── Trap landed on tower ────────────────────────────────
        public void TrapLandedOnTower(GameObject block)
        {
            // ✅ Do NOT return block to pool — stays visible on tower
            // ✅ Do NOT destroy — just freeze + show panel
            GameOver();

            if (!canGameOver)
            {
                // canGameOver false → treat as normal miss, pool the trap
                ObjectPool.Instance.ReturnBlock(block);
            }
        }

        // ─── Trap fell into void → dodged ───────────────────────
        // ✅ Only pools the block — spawn comes from tap
        public void TrapDodged(GameObject block)
        {
            ObjectPool.Instance.ReturnBlock(block);
        }

        void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = "Score: " + score;
            else
                Debug.LogWarning("STGameManager: scoreText not assigned!");

            if (livesText != null)
                livesText.text = "Lives: " + (maxMisses - missCount);
            else
                Debug.LogWarning("STGameManager: livesText not assigned!");
        }

        void GameOver()
        {
            if (!canGameOver) return;

            gameActive = false;
            Time.timeScale = 0f; // ✅ full freeze — physics, update, everything stops

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f; // ✅ unfreeze before reload
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}