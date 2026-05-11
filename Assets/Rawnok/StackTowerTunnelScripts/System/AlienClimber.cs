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
        public float holdTimeOnFail = 2f;

        [Header("Death Animation")]
        public Sprite[] deathSprites;  // ✅ drag sprites in order here
        public float frameRate = 0.1f; // ✅ seconds per frame
        public SpriteRenderer spriteRenderer;   // ✅ drag alien SpriteRenderer here

        private Queue<Transform> climbQueue = new Queue<Transform>();
        private Transform currentTarget = null;
        private bool isClimbing = false;
        private bool connectionFailed = false;

        void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);

            // Auto find SpriteRenderer if not assigned
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
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

        public void OnConnectionFailed()
        {
            connectionFailed = true;
            Debug.Log("Connection failed! Alien will hold then play death animation.");

            if (isClimbing && currentTarget == null)
                StartCoroutine(HoldThenDie());
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
                    StartCoroutine(HoldThenDie());
                else
                    Debug.Log("Alien waiting for next block...");
            }
        }

        IEnumerator HoldThenDie()
        {
            // ── Hold at current position ─────────────────────────
            Debug.Log("Alien holding for " + holdTimeOnFail + " seconds...");
            yield return new WaitForSeconds(holdTimeOnFail);

            // ── Play death sprite animation ───────────────────────
            if (deathSprites != null && deathSprites.Length > 0 && spriteRenderer != null)
            {
                Debug.Log("Playing death animation...");
                yield return StartCoroutine(PlayDeathAnimation());
            }
            else
            {
                Debug.LogWarning("AlienClimber: deathSprites or spriteRenderer not assigned!");
            }

            // ── Game over immediately after animation ─────────────
            Debug.Log("Death animation done — Game Over!");
            STGameManager.Instance.AlienReachedTop();
        }

        IEnumerator PlayDeathAnimation()
        {
            foreach (Sprite frame in deathSprites)
            {
                spriteRenderer.sprite = frame;
                yield return new WaitForSeconds(frameRate);
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
            connectionFailed = false;
            currentTarget = null;
            climbQueue.Clear();
            StopAllCoroutines();
        }
    }
}