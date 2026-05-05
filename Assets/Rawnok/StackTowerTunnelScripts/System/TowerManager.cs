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
        public AlienClimber alienClimber;           // ✅ drag Alien GO here
        public Transform alienStartPoint;        // ✅ drag StartPoint (child of BaseBlock)
        public int blocksToActivateAlien = 3; // ✅ after X blocks alien appears

        [Header("Difficulty")]
        public float speedIncreasePerBlock = 0.1f;
        public float maxSpeed = 8f;

        private float targetRootY;
        private int landedBlockCount = 0;
        private SpawnerMover spawnerMover;
        private List<GameObject> stackedBlocks = new List<GameObject>();
        private List<Transform> climbPoints = new List<Transform>();

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

            // ✅ Find ClimbPoint on this block
            Transform climbPoint = block.transform.Find("ClimbPoint");
            if (climbPoint != null)
            {
                climbPoints.Add(climbPoint);

                // Always give point to alien queue regardless of active state
                // AlienClimber will hold them until activated
                if (alienClimber != null)
                    alienClimber.AddClimbPoint(climbPoint);
            }
            else
            {
                Debug.LogWarning("Block missing ClimbPoint child: " + block.name);
            }

            // ✅ Activate alien after X blocks
            if (landedBlockCount == blocksToActivateAlien)
            {
                if (alienClimber != null && alienStartPoint != null)
                    alienClimber.Activate(alienStartPoint);
                else
                    Debug.LogWarning("TowerManager: alienClimber or alienStartPoint not assigned!");
            }

            // Scroll world down after threshold
            if (landedBlockCount > blocksBeforeScroll)
                targetRootY -= blockHeight;

            RampDifficulty();
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
                    // Remove its ClimbPoint from list
                    Transform cp = block.transform.Find("ClimbPoint");
                    if (cp != null && climbPoints.Contains(cp))
                        climbPoints.Remove(cp);

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