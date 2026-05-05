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
        public int pointsPerBlock = 10; // ✅ configurable points per block

        [Header("Spawn Settings")]
        public float spawnDelay = 0.5f;

        [Header("Game Over Settings")]
        public bool canGameOver = true;

        [Header("References")]
        public Transform blockSpawnPoint;
        public Transform deathZone;
        public TextMeshProUGUI scoreText; // ✅ back
        public TextMeshProUGUI livesText;
        public GameObject gameOverPanel;

        [HideInInspector] public int score = 0;
        private int missCount = 0;
        private bool gameActive = true;
        private bool waitingToSpawn = false;

        void Awake() => Instance = this;

        void Start()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            else
                Debug.LogWarning("STGameManager: gameOverPanel not assigned!");

            UpdateUI();
            SpawnNextBlock();
        }

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
        public void BlockStacked()
        {
            score += pointsPerBlock; // ✅ 10, 20, 30...
            UpdateUI();
        }

        // ─── Normal block fell into void ─────────────────────────
        public void BlockMissed(GameObject block)
        {
            ObjectPool.Instance.ReturnBlock(block);
            missCount++;
            UpdateUI();

            if (missCount >= maxMisses)
            {
                GameOver();
                if (!canGameOver)
                    return;
            }
        }

        // ─── Trap landed on tower ────────────────────────────────
        public void TrapLandedOnTower(GameObject block)
        {
            if (canGameOver)
            {
                ObjectPool.Instance.ReturnBlock(block);
                GameOver();
            }
            else
            {
                ObjectPool.Instance.ReturnBlock(block);
            }
        }

        // ─── Trap fell into void ─────────────────────────────────
        public void TrapDodged(GameObject block)
        {
            ObjectPool.Instance.ReturnBlock(block);
        }

        // ─── Alien reached top ───────────────────────────────────
        public void AlienReachedTop()
        {
            if (!gameActive) return;
            GameOver();
        }

        void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = score.ToString(); // ✅ just "10", "20", "30"
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
            Time.timeScale = 0f;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}