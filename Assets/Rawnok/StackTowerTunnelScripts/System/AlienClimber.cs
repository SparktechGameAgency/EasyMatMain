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

        private Queue<Transform> climbQueue = new Queue<Transform>();
        private Transform currentTarget = null;
        private bool isClimbing = false;

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

        public void AddClimbPointsFromBlock(BlockController block)
        {
            if (block == null || block.climbPoints == null) return;

            // ✅ Loop in reverse — lowest point (last element) queued first
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

        void Update()
        {
            if (!isClimbing || currentTarget == null) return;

            // Smooth move toward target world position
            transform.position = Vector3.MoveTowards(
                transform.position,
                currentTarget.position,
                climbSpeed * Time.deltaTime
            );

            // Check if reached
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
                // ✅ Queue empty → just wait here, no game over
                currentTarget = null;
                Debug.Log("Alien waiting for next block...");
            }
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
            currentTarget = null;
            climbQueue.Clear();
        }
    }
}