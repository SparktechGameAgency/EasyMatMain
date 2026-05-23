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

        [Header("Waiting Settings")]
        [Tooltip("Alien stops and waits at this element index")]
        public int waitingIndex = 1;

        [Header("Connection Fail Settings")]
        public float holdTimeOnFail = 2f;

        [Header("Death Animation")]
        public Sprite[] deathSprites;
        public float frameRate = 0.1f;
        public SpriteRenderer spriteRenderer;

        [Header("Fly Effect Settings")]
        public float bobSpeed = 2f;
        public float bobAmount = 0.08f;
        public float wobbleSpeed = 1.5f;
        public float wobbleAngle = 8f;
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.04f;
        public float leanSmoothing = 5f;
        public float maxLeanAngle = 15f;

        // ── Climb variables ──────────────────────────────────────
        private Queue<Transform> climbQueue = new Queue<Transform>();
        private Transform currentTarget = null;
        private bool isClimbing = false;
        private bool connectionFailed = false;
        private Transform[] lastBlockClimbPoints = null;

        // ── Fly effect variables ─────────────────────────────────
        private Transform visualTransform;  // child visual transform
        private bool hasChildVisual;   // true if sprite is on child GO
        private Vector3 visualBaseLocalPos;
        private Vector3 visualBaseScale;
        private float timeOffset;
        private Vector3 lastWorldPos;
        private float currentLean;

        void Awake()
        {
            Instance = this;
            // ✅ Alien stays visible from start — removed SetActive(false)

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            // ✅ Check if sprite is on child or same GO
            if (spriteRenderer != null && spriteRenderer.transform != transform)
            {
                // Sprite is on a CHILD — safe to bob local position
                visualTransform = spriteRenderer.transform;
                hasChildVisual = true;
            }
            else
            {
                // Sprite is on SAME GO — only rotate/scale, never touch localPosition
                visualTransform = transform;
                hasChildVisual = false;
            }
        }

        void Start()
        {
            InitFlyEffect();
        }

        void InitFlyEffect()
        {
            visualBaseLocalPos = visualTransform.localPosition;
            visualBaseScale = visualTransform.localScale;
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
            lastWorldPos = transform.position;
        }

        public void Activate(Transform startPoint)
        {
            // ✅ Alien already visible — no SetActive needed
            transform.position = startPoint.position;
            isClimbing = true;

            InitFlyEffect();

            Debug.Log("Alien activated at: " + startPoint.position);
            TryDequeueNext();
        }

        public void AddClimbPointsFromBlock(BlockController block)
        {
            if (block == null || block.climbPoints == null) return;

            lastBlockClimbPoints = block.climbPoints;

            for (int i = block.climbPoints.Length - 1; i >= waitingIndex; i--)
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
            Debug.Log("Connection failed! Alien climbing to top then game over.");

            if (lastBlockClimbPoints != null)
            {
                for (int i = waitingIndex - 1; i >= 0; i--)
                {
                    Transform point = lastBlockClimbPoints[i];
                    if (point != null)
                        climbQueue.Enqueue(point);
                }
            }

            if (isClimbing && currentTarget == null)
                TryDequeueNext();
        }

        void Update()
        {
            // ── Climbing ─────────────────────────────────────────
            if (isClimbing && currentTarget != null)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    currentTarget.position,
                    climbSpeed * Time.deltaTime
                );

                float dist = Vector3.Distance(transform.position, currentTarget.position);
                if (dist <= reachThreshold)
                    OnReachedTarget();
            }

            // ── Fly Effect ───────────────────────────────────────
            ApplyFlyEffect();
        }

        void ApplyFlyEffect()
        {
            // Movement lean calculation
            Vector3 velocity = (transform.position - lastWorldPos) / Time.deltaTime;
            lastWorldPos = transform.position;

            float targetLean = Mathf.Clamp(-velocity.x * 2f, -maxLeanAngle, maxLeanAngle);
            currentLean = Mathf.Lerp(currentLean, targetLean, leanSmoothing * Time.deltaTime);

            // ✅ Bob — ONLY if sprite is on a child GO
            // Prevents conflict with MoveTowards world position
            if (hasChildVisual)
            {
                float bobOffset = Mathf.Sin(Time.time * bobSpeed + timeOffset) * bobAmount;
                visualTransform.localPosition = new Vector3(
                    visualBaseLocalPos.x,
                    visualBaseLocalPos.y + bobOffset,
                    visualBaseLocalPos.z
                );
            }

            // ✅ Rotation — safe on both child and same GO
            float wobble = Mathf.Sin(Time.time * wobbleSpeed + timeOffset) * wobbleAngle;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, wobble + currentLean);

            // ✅ Scale pulse — safe on both child and same GO
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + timeOffset) * pulseAmount;
            visualTransform.localScale = visualBaseScale * pulse;
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
                    Debug.Log("Alien waiting at index " + waitingIndex + " for next block...");
            }
        }

        // ✅ Called by STGameManager when trap block lands
        // Reuses EXACT same flow as connection fail:
        // alien climbs to top → holds → plays animation → GameOver
        public void TriggerTrapDeath()
        {
            connectionFailed = true;

            // If alien has never climbed yet (trap was first block),
            // there are no climb points — go straight to death
            if (lastBlockClimbPoints == null)
            {
                if (isClimbing && currentTarget == null)
                    StartCoroutine(HoldThenDie());
                return;
            }

            // Otherwise reuse the normal connection fail path
            OnConnectionFailed();
        }

        IEnumerator HoldThenDie()
        {
            Debug.Log("Alien holding for " + holdTimeOnFail + " seconds...");
            yield return new WaitForSeconds(holdTimeOnFail);

            if (deathSprites != null && deathSprites.Length > 0 && spriteRenderer != null)
            {
                Debug.Log("Playing death animation...");
                yield return StartCoroutine(PlayDeathAnimation());
            }
            else
            {
                Debug.LogWarning("AlienClimber: deathSprites or spriteRenderer not assigned!");
            }

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

        // ── Called by TowerManager / LaserAbility — cancels fail sequence ──
        public void CancelConnectionFailed() => CancelConnectionFail(); // alias

        public void CancelConnectionFail()
        {
            if (!connectionFailed) return;

            connectionFailed = false;
            StopAllCoroutines();  // stop HoldThenDie if already started
            climbQueue.Clear();   // remove the die-path points added by OnConnectionFailed

            Debug.Log("Alien connection fail cancelled — laser used!");

            // Resume normal climbing if there are points waiting
            if (isClimbing && currentTarget == null)
                TryDequeueNext();
        }

        public void Stop()
        {
            isClimbing = false;
            connectionFailed = false;
            currentTarget = null;
            lastBlockClimbPoints = null;
            climbQueue.Clear();
            StopAllCoroutines();
        }
    }
}