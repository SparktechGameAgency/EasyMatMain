using System.Collections.Generic;
using UnityEngine;

namespace StackTower
{
    public class AlienClimber : MonoBehaviour
    {
        public static AlienClimber Instance;

        [Header("Climb Settings")]
        public float climbSpeed = 2f;
        public float reachThreshold = 0.1f; // distance to count as "reached"

        private Queue<Transform> climbQueue = new Queue<Transform>();
        private Transform currentTarget = null;
        private bool isClimbing = false;

        void Awake()
        {
            Instance = this;
            gameObject.SetActive(false); // ✅ hidden at start
        }

        // Called by TowerManager when X blocks have landed
        public void Activate(Transform startPoint)
        {
            // Snap to start point world position
            transform.position = startPoint.position;
            gameObject.SetActive(true);
            isClimbing = true;

            // If points already queued (blocks landed before activation)
            // start moving toward first one
            TryDequeueNext();
        }

        // Called by TowerManager each time a block lands
        public void AddClimbPoint(Transform point)
        {
            climbQueue.Enqueue(point);

            // If alien is active but has no current target, start moving
            if (isClimbing && currentTarget == null)
                TryDequeueNext();
        }

        void Update()
        {
            if (!isClimbing || currentTarget == null) return;

            // Smooth move toward current target world position
            transform.position = Vector3.MoveTowards(
                transform.position,
                currentTarget.position,
                climbSpeed * Time.deltaTime
            );

            // Check if reached current target
            float dist = Vector3.Distance(transform.position, currentTarget.position);
            if (dist <= reachThreshold)
                OnReachedTarget();
        }

        void OnReachedTarget()
        {
            // Snap exactly to target
            transform.position = currentTarget.position;

            if (climbQueue.Count > 0)
            {
                // More points to climb → move to next
                TryDequeueNext();
            }
            else
            {
                // Queue empty → alien reached top block → Game Over
                currentTarget = null;
                STGameManager.Instance.AlienReachedTop();
            }
        }

        void TryDequeueNext()
        {
            if (climbQueue.Count > 0)
                currentTarget = climbQueue.Dequeue();
            else
                currentTarget = null;
        }

        public void Stop()
        {
            isClimbing = false;
            currentTarget = null;
            climbQueue.Clear();
        }
    }
}