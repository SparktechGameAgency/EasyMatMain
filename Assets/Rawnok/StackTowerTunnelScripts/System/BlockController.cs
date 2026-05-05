using UnityEngine;

namespace StackTower
{
    public class BlockController : MonoBehaviour
    {
        [Header("Trap Setting (tick ON for trap prefab only)")]
        [SerializeField] private bool isTrap = false;

        [Header("Physics Settings")]
        [SerializeField] public float fallGravityScale = 3f;
        [SerializeField] public float maxFallSpeed = 10f;
        [SerializeField] private float settleVelocity = 0.15f;
        [SerializeField] private float settleAngular = 10f;
        [SerializeField] private float settleTime = 0.25f;

        private bool isRiding = true;
        private bool hasLanded = false;
        private bool hasResolved = false;
        private float contactTimer = 0f;

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
            transform.SetParent(null);

            Vector3 spawnPos = spawnPointRef.position;
            transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
        }

        void Update()
        {
            if (isRiding)
            {
                Vector3 pos = spawnPoint.position;
                transform.position = new Vector3(pos.x, pos.y, 0f);

                if (Input.GetMouseButtonDown(0))
                    Release();

                return;
            }

            // Cap fall speed
            if (maxFallSpeed > 0 && rb.velocity.y < -maxFallSpeed)
                rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);

            // Void check
            if (!hasResolved && deathZone != null && transform.position.y < deathZone.position.y)
                ResolveVoid();
        }

        void Release()
        {
            isRiding = false;
            rb.gravityScale = fallGravityScale;
            rb.constraints = RigidbodyConstraints2D.None;

            // ✅ Notify game manager — triggers spawn delay timer from tap
            STGameManager.Instance.OnPlayerTapped();
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
                // ✅ Trap: don't touch the block — just notify game manager
                STGameManager.Instance.TrapLandedOnTower(gameObject);
            }
            else
            {
                gameObject.tag = "Block";
                TowerManager.Instance.BlockLanded(gameObject);
                STGameManager.Instance.BlockStacked();
            }
        }

        void ResolveVoid()
        {
            if (hasResolved) return;
            hasResolved = true;

            if (isTrap)
                STGameManager.Instance.TrapDodged(gameObject);
            else
                STGameManager.Instance.BlockMissed(gameObject);
        }
    }
}