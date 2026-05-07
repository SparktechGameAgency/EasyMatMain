using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StackTower
{
    public class AlienClimber : MonoBehaviour
    {
        public static AlienClimber Instance;

        [Header("Climb Settings")]
        public float climbSpeed = 2f;
        public float reachThreshold = 0.1f;

        [Header("Connection Fail Settings")]
        public float holdTimeOnFail = 2f; // ✅ hold time before game over

        private Queue<Transform> climbQueue = new Queue<Transform>();
        private Transform currentTarget = null;
        private bool isClimbing = false;
        private bool connectionFailed = false;

        void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        public void Activate(Transform startPoint)
        {
            gameObject.SetActive(true);
            transform.position = startPoint.position;
            isClimbing = true;

            Debug.Log("Alien activated at: " + startPoint.position);
            Debug.Log("Points in queue: " + climbQueue.Count);

            TryDequeueNext();
        }

        // Called by TowerManager when connection IS made
        public void AddClimbPointsFromBlock(BlockController block)
        {
            if (block == null || block.climbPoints == null) return;

            // Reverse order — lowest point first
            for (int i = block.climbPoints.Length - 1; i >= 0; i--)
            {
                Transform point = block.climbPoints[i];
                if (point != null)
                {
                    climbQueue.Enqueue(point);
                    Debug.Log("Added climb point: " + point.name);
                }
            }

            if (isClimbing && currentTarget == null)
                TryDequeueNext();
        }

        // Called by TowerManager when connection FAILS
        public void OnConnectionFailed()
        {
            connectionFailed = true;
            Debug.Log("Connection failed! Alien will hold then game over.");

            // If alien already has no target (already at top of queue)
            // start hold timer immediately
            if (isClimbing && currentTarget == null)
                StartCoroutine(HoldThenGameOver());
        }

        void Update()
        {
            if (!isClimbing || currentTarget == null) return;

            transform.position = Vector3.MoveTowards(
                transform.position,
                currentTarget.position,
                climbSpeed * Time.deltaTime
            );

            float dist = Vector3.Distance(transform.position, currentTarget.position);
            if (dist <= reachThreshold)
                OnReachedTarget();
        }

        void OnReachedTarget()
        {
            transform.position = currentTarget.position;
            Debug.Log("Alien reached: " + currentTarget.name);

            if (climbQueue.Count > 0)
            {
                TryDequeueNext();
            }
            else
            {
                currentTarget = null;

                if (connectionFailed)
                {
                    // ✅ Hold for holdTime then game over
                    StartCoroutine(HoldThenGameOver());
                }
                else
                {
                    // Normal wait — new block will resume climbing
                    Debug.Log("Alien waiting for next block...");
                }
            }
        }

        IEnumerator HoldThenGameOver()
        {
            Debug.Log("Alien holding for " + holdTimeOnFail + " seconds...");
            yield return new WaitForSeconds(holdTimeOnFail);
            Debug.Log("Alien held long enough — Game Over!");
            STGameManager.Instance.AlienReachedTop();
        }

        void TryDequeueNext()
        {
            if (climbQueue.Count > 0)
            {
                currentTarget = climbQueue.Dequeue();
                Debug.Log("Alien targeting: " + currentTarget.name);
            }
            else
            {
                currentTarget = null;
            }
        }

        public void Stop()
        {
            isClimbing = false;
            connectionFailed = false;
            currentTarget = null;
            climbQueue.Clear();
            StopAllCoroutines();
        }
    }
}