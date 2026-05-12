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

            // ✅ First block — always perfect
            if (stackedBlocks.Count == 1)
            {
                if (LandingTextEffect.Instance != null)
                    LandingTextEffect.Instance.ShowPerfect(block.transform.position);

                STGameManager.Instance.BlockStacked(true);

                if (alienClimber != null)
                    alienClimber.AddClimbPointsFromBlock(newBC);

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

                if (alienClimber != null)
                    alienClimber.OnConnectionFailed();
            }
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