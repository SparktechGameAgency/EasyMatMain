using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StackTower
{
    public class TowerManager : MonoBehaviour
    {
        public static TowerManager Instance;

        [Header("World Root")]
        public Transform worldRoot;

        [Header("Block Settings")]
        public float blockHeight = 0.5f;

        [Header("Scroll Settings")]
        public float scrollSpeed = 5f;
        public int blocksBeforeScroll = 3;

        [Header("Alien Settings")]
        public AlienClimber alienClimber;
        public Transform alienStartPoint;
        public int blocksToActivateAlien = 3;

        [Header("Base Block")]
        public Transform baseBlockClimbPoint; // ✅ drag base block's child GO (BoxCollider2D) here

        [Header("Connection Settings")]
        public float minOverlapToConnect = 0.05f;
        public float perfectThreshold = 0.05f;

        [Header("Difficulty")]
        public float speedIncreasePerBlock = 0.1f;
        public float maxSpeed = 8f;

        private float targetRootY;
        private int landedBlockCount = 0;
        private SpawnerMover spawnerMover;
        private List<GameObject> stackedBlocks = new List<GameObject>();

        // ── Align Ability ────────────────────────────────────────
        [Header("Align Ability")]
        public AlignAbilityButton alignAbilityButton; // drag AlignAbilityButton GO here

        private GameObject pendingAlignBlock = null;

        public bool HasPendingAlign => pendingAlignBlock != null;

        void Awake() => Instance = this;

        void Start()
        {
            spawnerMover = FindObjectOfType<SpawnerMover>();
            targetRootY = worldRoot.position.y;
        }

        void Update()
        {
            float newY = Mathf.MoveTowards(
                worldRoot.position.y,
                targetRootY,
                scrollSpeed * Time.deltaTime
            );

            worldRoot.position = new Vector3(
                worldRoot.position.x,
                newY,
                worldRoot.position.z
            );

            RecycleOffScreenBlocks();
        }

        public void BlockLanded(GameObject block)
        {
            // New block just landed — close any open align window
            ClearPendingAlign();

            block.transform.SetParent(worldRoot, true);
            stackedBlocks.Add(block);
            landedBlockCount++;

            // Activate alien after X blocks
            if (landedBlockCount == blocksToActivateAlien)
            {
                if (alienClimber != null && alienStartPoint != null)
                    alienClimber.Activate(alienStartPoint);
                else
                    Debug.LogWarning("TowerManager: alienClimber or alienStartPoint not assigned!");
            }

            if (landedBlockCount > blocksBeforeScroll)
                targetRootY -= blockHeight;

            RampDifficulty();
            StartCoroutine(CheckConnectionNextFrame(block));
        }

        IEnumerator CheckConnectionNextFrame(GameObject block)
        {
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();

            BlockController newBC = block.GetComponent<BlockController>();
            if (newBC == null)
            {
                Debug.LogWarning("Block missing BlockController: " + block.name);
                yield break;
            }

            // ✅ First block — check overlap against base block child collider
            if (stackedBlocks.Count == 1)
            {
                if (baseBlockClimbPoint != null)
                {
                    Transform baseNewLowest = newBC.GetLowestClimbPoint();
                    Collider2D baseCol = baseBlockClimbPoint.GetComponent<Collider2D>();
                    Collider2D baseNewLowestCol = baseNewLowest != null ? baseNewLowest.GetComponent<Collider2D>() : null;

                    if (baseCol != null && baseNewLowestCol != null)
                    {
                        // X overlap check — same system as normal blocks
                        Bounds baseBoundsA = baseNewLowestCol.bounds;
                        Bounds baseBoundsB = baseCol.bounds;

                        float baseXOverlap = Mathf.Max(0f,
                            Mathf.Min(baseBoundsA.max.x, baseBoundsB.max.x) -
                            Mathf.Max(baseBoundsA.min.x, baseBoundsB.min.x));

                        bool baseConnected = baseXOverlap >= minOverlapToConnect;
                        Debug.Log("Base block overlap: " + baseXOverlap.ToString("F3") + " connected: " + baseConnected);

                        if (baseConnected)
                        {
                            // Temporarily unfreeze X to allow slide
                            Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
                            if (rb != null)
                                rb.constraints = RigidbodyConstraints2D.FreezePositionY |
                                                 RigidbodyConstraints2D.FreezeRotation;

                            // ✅ Slide X to align lowest point with base block child
                            float xDiff = baseBlockClimbPoint.position.x - baseNewLowest.position.x;
                            block.transform.position += new Vector3(xDiff, 0f, 0f);

                            if (rb != null)
                                rb.constraints = RigidbodyConstraints2D.FreezeAll;

                            bool isPerfect = Mathf.Abs(xDiff) < perfectThreshold;

                            if (LandingTextEffect.Instance != null)
                            {
                                if (isPerfect) LandingTextEffect.Instance.ShowPerfect(block.transform.position);
                                else LandingTextEffect.Instance.ShowGreat(block.transform.position);
                            }

                            STGameManager.Instance.BlockStacked(isPerfect);

                            if (alienClimber != null)
                                alienClimber.AddClimbPointsFromBlock(newBC);
                        }
                        else
                        {
                            // No overlap with base block
                            if (LandingTextEffect.Instance != null)
                                LandingTextEffect.Instance.ShowBad(block.transform.position);

                            RegisterPendingAlign(block);

                            if (alienClimber != null)
                                alienClimber.OnConnectionFailed();
                        }
                    }
                    else
                    {
                        // Missing colliders — fallback perfect
                        Debug.LogWarning("TowerManager: Missing collider on baseBlockClimbPoint or block!");
                        STGameManager.Instance.BlockStacked(true);
                        if (alienClimber != null) alienClimber.AddClimbPointsFromBlock(newBC);
                    }
                }
                else
                {
                    // No base climb point assigned — fallback perfect
                    Debug.LogWarning("TowerManager: baseBlockClimbPoint not assigned!");
                    if (LandingTextEffect.Instance != null)
                        LandingTextEffect.Instance.ShowPerfect(block.transform.position);
                    STGameManager.Instance.BlockStacked(true);
                    if (alienClimber != null) alienClimber.AddClimbPointsFromBlock(newBC);
                }

                yield break;
            }

            // Get block directly below
            GameObject belowBlock = stackedBlocks[stackedBlocks.Count - 2];
            if (belowBlock == null) yield break;

            BlockController belowBC = belowBlock.GetComponent<BlockController>();
            if (belowBC == null) yield break;

            Transform newLowest = newBC.GetLowestClimbPoint();
            Transform belowHighest = belowBC.GetHighestClimbPoint();

            if (newLowest == null || belowHighest == null)
            {
                Debug.LogWarning("Missing climb points on blocks!");
                yield break;
            }

            Collider2D newLowestCol = newLowest.GetComponent<Collider2D>();
            Collider2D belowHighestCol = belowHighest.GetComponent<Collider2D>();

            if (newLowestCol == null || belowHighestCol == null)
            {
                Debug.LogWarning("Missing Collider2D on climb points!");
                yield break;
            }

            // X overlap check
            Bounds boundsA = newLowestCol.bounds;
            Bounds boundsB = belowHighestCol.bounds;

            float xOverlap = Mathf.Max(0f,
                Mathf.Min(boundsA.max.x, boundsB.max.x) -
                Mathf.Max(boundsA.min.x, boundsB.min.x));

            bool connected = xOverlap >= minOverlapToConnect;
            Debug.Log("X Overlap: " + xOverlap.ToString("F3") + " connected: " + connected);

            if (connected)
            {
                Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.constraints = RigidbodyConstraints2D.FreezePositionY |
                                     RigidbodyConstraints2D.FreezeRotation;

                // Slide X to align
                float xDiff = belowHighest.position.x - newLowest.position.x;
                block.transform.position += new Vector3(xDiff, 0f, 0f);

                if (rb != null)
                    rb.constraints = RigidbodyConstraints2D.FreezeAll;

                bool isPerfect = Mathf.Abs(xDiff) < perfectThreshold;
                Debug.Log("isPerfect: " + isPerfect + " xDiff: " + xDiff.ToString("F3"));

                // ✅ Show Perfect or Great text
                if (LandingTextEffect.Instance != null)
                {
                    if (isPerfect)
                        LandingTextEffect.Instance.ShowPerfect(block.transform.position);
                    else
                        LandingTextEffect.Instance.ShowGreat(block.transform.position);
                }

                // Award XP
                STGameManager.Instance.BlockStacked(isPerfect);

                // Add climb points to alien
                if (alienClimber != null)
                    alienClimber.AddClimbPointsFromBlock(newBC);
            }
            else
            {
                Debug.Log("No connection!");

                // ✅ Show Bad text
                if (LandingTextEffect.Instance != null)
                    LandingTextEffect.Instance.ShowBad(block.transform.position);

                RegisterPendingAlign(block);

                if (alienClimber != null)
                    alienClimber.OnConnectionFailed();
            }
        }

        // ── Align Ability ─────────────────────────────────────────────────────
        void RegisterPendingAlign(GameObject block)
        {
            pendingAlignBlock = block;

            if (alignAbilityButton != null)
                alignAbilityButton.OnAlignAvailable();
        }

        public void ClearPendingAlign()
        {
            pendingAlignBlock = null;

            if (alignAbilityButton != null)
                alignAbilityButton.OnAlignUnavailable();
        }

        // Called by AlignAbilityButton when the player taps it
        public void UseAlignAbility()
        {
            if (pendingAlignBlock == null) return;

            GameObject block = pendingAlignBlock;
            ClearPendingAlign();

            BlockController newBC = block.GetComponent<BlockController>();
            if (newBC == null) return;

            Transform lowest = newBC.GetLowestClimbPoint();
            if (lowest == null) return;

            float xDiff = 0f;

            if (stackedBlocks.Count == 1)
            {
                // Align to base block
                if (baseBlockClimbPoint == null) return;
                xDiff = baseBlockClimbPoint.position.x - lowest.position.x;
            }
            else
            {
                // Align to block directly below
                GameObject belowBlock = stackedBlocks[stackedBlocks.Count - 2];
                if (belowBlock == null) return;

                BlockController belowBC = belowBlock.GetComponent<BlockController>();
                if (belowBC == null) return;

                Transform belowHighest = belowBC.GetHighestClimbPoint();
                if (belowHighest == null) return;

                xDiff = belowHighest.position.x - lowest.position.x;
            }

            // Snap the block to alignment — no score awarded
            Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.constraints = RigidbodyConstraints2D.FreezePositionY |
                                 RigidbodyConstraints2D.FreezeRotation;

            block.transform.position += new Vector3(xDiff, 0f, 0f);

            if (rb != null)
                rb.constraints = RigidbodyConstraints2D.FreezeAll;

            // Cancel the game-over path and let the alien climb normally
            if (alienClimber != null)
            {
                alienClimber.CancelConnectionFailed();
                alienClimber.AddClimbPointsFromBlock(newBC);
            }

            // Flash Perfect text as visual feedback
            if (LandingTextEffect.Instance != null)
                LandingTextEffect.Instance.ShowPerfect(block.transform.position);

            Debug.Log("Align ability used! xDiff: " + xDiff.ToString("F3"));
        }

        void RecycleOffScreenBlocks()
        {
            if (STGameManager.Instance == null) return;

            float deathY = STGameManager.Instance.deathZone.position.y;

            for (int i = stackedBlocks.Count - 1; i >= 0; i--)
            {
                GameObject block = stackedBlocks[i];

                if (block == null)
                {
                    stackedBlocks.RemoveAt(i);
                    continue;
                }

                if (block.transform.position.y < deathY)
                {
                    stackedBlocks.RemoveAt(i);
                    block.transform.SetParent(null);
                    block.tag = "Untagged";
                    ObjectPool.Instance.ReturnBlock(block);
                }
            }
        }

        void RampDifficulty()
        {
            float newSpeed = spawnerMover.baseSpeed +
                (STGameManager.Instance.score * speedIncreasePerBlock);
            newSpeed = Mathf.Min(newSpeed, maxSpeed);
            spawnerMover.SetSpeed(newSpeed);
        }
    }
}