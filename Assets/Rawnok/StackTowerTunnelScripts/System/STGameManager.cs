using System.Collections;
using UnityEngine;
using TMPro;

namespace StackTower
{
    public class STGameManager : MonoBehaviour
    {
        public static STGameManager Instance;

        [Header("XP Settings")]
        public int perfectXP = 10;  // ✅ perfect land = +10
        public int averageXP = 5;   // ✅ adjusted/slid land = +5

        [Header("Input Lock")]
        [HideInInspector] public bool isInputLocked = false;

        [Header("Spawn Settings")]
        public float spawnDelay = 0.5f;

        [Header("Game Over Settings")]
        public bool canGameOver = true;

        [Header("References")]
        public Transform blockSpawnPoint;
        public Transform deathZone;
        public TextMeshProUGUI scoreText;        // shows XP
        public TextMeshProUGUI blocksSpawnedText; // shows block count
        public GameObject gameOverPanel;

        [HideInInspector] public int score = 0; // total XP
        [HideInInspector] public int blocksSpawned = 0; // total blocks spawned

        private bool gameActive = true;
        private bool waitingToSpawn = false;
        // Call these from your Settings Panel open/close
        public void LockInput() => isInputLocked = true;
        public void UnlockInput() => StartCoroutine(UnlockAfterDelay());

        void Awake() => Instance = this;

        IEnumerator UnlockAfterDelay()
        {
            // ✅ Wait one frame so the closing tap doesn't drop the block
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame(); // two frames to be safe
            isInputLocked = false;
        }

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

            // ✅ Count every block spawned
            blocksSpawned++;
            UpdateUI();

            bc.Initialize(blockSpawnPoint, deathZone);
        }

        // ─── Normal block landed ─────────────────────────────────
        // Called by TowerManager after connection check
        public void BlockStacked(bool isPerfect)
        {
            // ✅ Perfect = +10, Adjusted = +5
            score += isPerfect ? perfectXP : averageXP;
            UpdateUI();
            Debug.Log("XP awarded: " + (isPerfect ? perfectXP : averageXP) +
                      " | isPerfect: " + isPerfect);
        }

        // ─── Normal block fell into void ─────────────────────────
        // No lives system — just return block to pool
        public void BlockMissed(GameObject block)
        {
            ObjectPool.Instance.ReturnBlock(block);
            // ✅ No lives deduction — just continue
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
            // ✅ XP display
            if (scoreText != null)
                scoreText.text = score.ToString();
            else
                Debug.LogWarning("STGameManager: scoreText not assigned!");

            // ✅ Block count display
            if (blocksSpawnedText != null)
                blocksSpawnedText.text = blocksSpawned.ToString();
            else
                Debug.LogWarning("STGameManager: blocksSpawnedText not assigned!");
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