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
        [HideInInspector] public bool isInputLocked = false;

        [Header("References")]
        public Transform blockSpawnPoint;
        public Transform deathZone;
        public TextMeshPro scoreTMPField; // drag ScoreObject here
        public GameObject gameOverPanel;

        [HideInInspector] public int score = 0;
        [HideInInspector] public bool gameActive = true;

        private bool waitingToSpawn = false;
        private BlockController currentRidingBlock = null;
        private TextMeshPro scoreTMP;

        void Awake()
        {
            Instance = this;
            scoreTMP = scoreTMPField;
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

            if (blockSpawnPoint == null) { Debug.LogError("STGameManager: blockSpawnPoint not assigned!"); return; }
            if (deathZone == null) { Debug.LogError("STGameManager: deathZone not assigned!"); return; }
            if (ObjectPool.Instance == null) { Debug.LogError("STGameManager: ObjectPool is null!"); return; }

            GameObject block = ObjectPool.Instance.GetBlock();
            if (block == null) { Debug.LogError("STGameManager: Pool returned null!"); return; }

            BlockController bc = block.GetComponent<BlockController>();
            if (bc == null) { Debug.LogError("Missing BlockController on prefab!"); return; }

            bc.Initialize(blockSpawnPoint, deathZone);
            currentRidingBlock = bc;
        }

        // ─── Normal block landed ─────────────────────────────────
        public void BlockStacked(bool isPerfect)
        {
            int points = isPerfect ? perfectXP : averageXP;
            score += points;
            UpdateUI();

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
            if (!canGameOver)
            {
                ObjectPool.Instance.ReturnBlock(block);
                return;
            }

            // ✅ Register trap in stack + activate alien if first block
            TowerManager.Instance.TrapBlockLanded(block);

            if (LaserAbility.Instance != null && LaserAbility.Instance.HasUsesLeft)
            {
                // belowBlock is the block UNDER the trap (second from top)
                GameObject belowBlock = TowerManager.Instance.GetSecondTopBlock();
                LaserAbility.Instance.OnTrapLanding(block, belowBlock);
            }
            else
            {
                // No laser — trap stays visible, trigger death sequence
                if (AlienClimber.Instance != null)
                    AlienClimber.Instance.TriggerTrapDeath();
                else
                    GameOver();
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
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            Debug.Log("Asteroid Game Over!");
        }

        // ─── Input lock ──────────────────────────────────────────
        public void LockInputPermanent() => isInputLocked = true;
        public void LockInput() => isInputLocked = true;
        public void UnlockInput() => isInputLocked = false;

        // ─── Drop block ──────────────────────────────────────────
        public void TryDropBlock()
        {
            if (!gameActive || isInputLocked) return;
            if (currentRidingBlock == null) return;
            currentRidingBlock.Release();
            currentRidingBlock = null;
        }

        void UpdateUI()
        {
            if (scoreTMP != null)
                scoreTMP.text = score.ToString();
            else
                Debug.LogWarning("STGameManager: scoreTMP is null — check ScoreObject child!");
        }

        void GameOver()
        {
            if (!canGameOver) return;
            gameActive = false;
            Time.timeScale = 0f;
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}