using System.Collections;
using UnityEngine;
using TMPro;

namespace StackTower
{
    public class STGameManager : MonoBehaviour
    {
        public static STGameManager Instance;

        [Header("XP Settings")]
        public int perfectXP = 10;
        public int averageXP = 5;

        [Header("Spawn Settings")]
        public float spawnDelay = 0.5f;

        [Header("Game Over Settings")]
        public bool canGameOver = true;

        [Header("Input Lock")]
        private int inputUnlockFrame = -1;
        public bool isInputLocked => Time.frameCount < inputUnlockFrame;

        [Header("References")]
        public Transform blockSpawnPoint;
        public Transform deathZone;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI blocksSpawnedText;
        public GameObject gameOverPanel;

        [HideInInspector] public int score = 0;
        [HideInInspector] public int blocksSpawned = 0;
        [HideInInspector] public bool gameActive = true; // ✅ public for AsteroidEvent

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

            blocksSpawned++;
            UpdateUI();
            bc.Initialize(blockSpawnPoint, deathZone);
        }

        // ─── Normal block landed ─────────────────────────────────
        public void BlockStacked(bool isPerfect)
        {
            int points = isPerfect ? perfectXP : averageXP;
            score += points;
            UpdateUI();

            // ✅ Notify asteroid event
            if (AsteroidEvent.Instance != null)
                AsteroidEvent.Instance.OnBlockPlaced();

            Debug.Log("XP: +" + points + " isPerfect: " + isPerfect);
        }

        // ─── Normal block fell into void ─────────────────────────
        public void BlockMissed(GameObject block)
        {
            ObjectPool.Instance.ReturnBlock(block);
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

        // ─── Asteroid hit ────────────────────────────────────────
        public void AsteroidGameOver()
        {
            if (!gameActive) return;

            gameActive = false;
            Time.timeScale = 0f;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            Debug.Log("Asteroid Game Over!");
        }

        // ─── Unlock input with delay ─────────────────────────────
        public void LockInputForFrames(int frames = 2)
        {
            inputUnlockFrame = Time.frameCount + frames;
        }

        // Permanent lock (e.g. asteroid impact) — stays locked until scene reload
        public void LockInputPermanent()
        {
            inputUnlockFrame = int.MaxValue;
        }

        void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = score.ToString();
            else
                Debug.LogWarning("STGameManager: scoreText not assigned!");

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