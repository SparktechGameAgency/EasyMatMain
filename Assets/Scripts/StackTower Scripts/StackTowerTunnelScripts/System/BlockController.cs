using UnityEngine;

namespace StackTower
{
    public class BlockController : MonoBehaviour
    {
        [Header("Trap Setting (tick ON for trap prefab only)")]
        [SerializeField] private bool isTrap = false;
        public bool IsTrap => isTrap;

        [Header("Alien Climb Points (drag S1, S2, S3 etc.)")]
        public Transform[] climbPoints;

        [Header("Mid Point (drag the mid child object here)")]
        public Transform midPoint;

        [Header("Physics Settings")]
        [SerializeField] public float fallGravityScale = 3f;
        [SerializeField] public float maxFallSpeed = 10f;
        [SerializeField] private float settleVelocity = 0.15f;
        [SerializeField] private float settleAngular = 10f;
        [SerializeField] private float settleTime = 0.25f;
        [Header("Spawn Settings")]
        public float spawnZ = 3.34f; // ✅ control Z from Inspector
        public Vector3 spawnScale = new Vector3(0.7f, 0.7f, 1f);

        [Header("Death Effect")]
        public ParticleSystem deathParticle;

        private bool isRiding = true;
        private bool hasLanded = false;
        private bool hasResolved = false;
        private float contactTimer = 0f;

        // Auto Align slide — SmoothStep over estimated fall duration, stops on hasResolved
        private bool isAligning = false;
        private float alignStartX = 0f;
        private float alignTargetX = 0f;
        private float alignDuration = 0f;
        private float alignElapsed = 0f;

        private Transform spawnPoint;
        private Transform deathZone;
        private Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void Initialize(Transform spawnPointRef, Transform deathZoneRef)
        {
            isRiding = true;
            hasLanded = false;
            hasResolved = false;
            contactTimer = 0f;
            spawnPoint = spawnPointRef;
            deathZone = deathZoneRef;

            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX;

            transform.rotation = Quaternion.identity;
            transform.localScale = spawnScale;
            transform.SetParent(null);

            Vector3 spawnPos = spawnPointRef.position;
            transform.position = new Vector3(spawnPos.x, spawnPos.y, spawnZ); // ✅
        }

        void Update()
        {
            if (isRiding)
            {
                if (spawnPoint == null) return; // not initialized yet, skip this frame

                Vector3 pos = spawnPoint.position;
                transform.position = new Vector3(pos.x, pos.y, spawnZ);
                return;
            }

            // Auto Align slide — SmoothStep toward alignTargetX over alignDuration
            if (isAligning && !hasResolved)
            {
                alignElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(alignDuration > 0f ? alignElapsed / alignDuration : 1f);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                float newX = Mathf.Lerp(alignStartX, alignTargetX, smoothT);

                Vector3 cur = transform.position;
                transform.position = new Vector3(newX, cur.y, cur.z);

                if (t >= 1f)
                {
                    isAligning = false;
                    // Lock X for the remainder of the fall so it drops perfectly straight
                    rb.constraints = RigidbodyConstraints2D.FreezePositionX |
                                     RigidbodyConstraints2D.FreezeRotation;
                }
            }

            if (maxFallSpeed > 0 && rb.velocity.y < -maxFallSpeed)
                rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);

            if (!hasResolved && deathZone != null && transform.position.y < deathZone.position.y)
                ResolveVoid();
        }
        public void Release()
        {
            if (!isRiding) return;
            isRiding = false;
            rb.gravityScale = fallGravityScale;
            rb.constraints = RigidbodyConstraints2D.None;
            STGameManager.Instance.OnPlayerTapped();
            TowerManager.Instance.OnBlockReleased(gameObject); // notify: block is now falling
        }

        // Called by TowerManager when the Auto Align power-up fires.
        // duration = estimated time until the block reaches the stack (from kinematic calc).
        public void StartAlignSlide(float targetX, float duration)
        {
            isAligning = true;
            alignStartX = transform.position.x;
            alignTargetX = targetX;
            alignDuration = Mathf.Max(duration, 0.05f); // never zero
            alignElapsed = 0f;
            // Lock rotation immediately; X stays free until SmoothStep finishes
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        void OnCollisionStay2D(Collision2D col)
        {
            if (hasResolved || isRiding) return;

            if (col.gameObject.CompareTag("Block") || col.gameObject.CompareTag("BaseBlock"))
            {
                bool velocitySettled = rb.velocity.magnitude < settleVelocity;
                bool rotationSettled = Mathf.Abs(rb.angularVelocity) < settleAngular;

                if (velocitySettled && rotationSettled)
                {
                    contactTimer += Time.deltaTime;
                    if (contactTimer >= settleTime)
                        ResolveLanded();
                }
                else
                {
                    contactTimer = 0f;
                }
            }
        }

        void OnCollisionExit2D(Collision2D col)
        {
            contactTimer = 0f;
        }

        void ResolveLanded()
        {
            if (hasResolved) return;

            hasResolved = true;
            hasLanded = true;

            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;

            if (isTrap)
            {
                STGameManager.Instance.TrapLandedOnTower(gameObject);
            }
            else
            {
                gameObject.tag = "Block";
                TowerManager.Instance.BlockLanded(gameObject); // ✅ TowerManager handles score now
            }
        }

        void ResolveVoid()
        {
            if (hasResolved) return;
            hasResolved = true;

            if (deathParticle != null)
            {
                ParticleSystem ps = Instantiate(deathParticle, transform.position, Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }

            if (isTrap)
                STGameManager.Instance.TrapDodged(gameObject);
            else
                STGameManager.Instance.BlockMissed(gameObject);
        }
        // ✅ Returns climb point with lowest Y on this block
        public Transform GetLowestClimbPoint()
        {
            if (climbPoints == null || climbPoints.Length == 0) return null;

            Transform lowest = climbPoints[0];
            foreach (Transform cp in climbPoints)
                if (cp != null && cp.position.y < lowest.position.y)
                    lowest = cp;

            return lowest;
        }

        // ✅ Returns climb point with highest Y on this block
        public Transform GetHighestClimbPoint()
        {
            if (climbPoints == null || climbPoints.Length == 0) return null;

            Transform highest = climbPoints[0];
            foreach (Transform cp in climbPoints)
                if (cp != null && cp.position.y > highest.position.y)
                    highest = cp;

            return highest;
        }
    }
}