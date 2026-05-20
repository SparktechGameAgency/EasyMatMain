using UnityEngine;
using UnityEngine.EventSystems;

namespace StackTower
{
    public class BlockController : MonoBehaviour
    {
        [Header("Trap Setting (tick ON for trap prefab only)")]
        [SerializeField] private bool isTrap = false;

        [Header("Alien Climb Points (drag S1, S2, S3 etc.)")]
        public Transform[] climbPoints;

        [Header("Physics Settings")]
        [SerializeField] public float fallGravityScale = 3f;
        [SerializeField] public float maxFallSpeed = 10f;
        [SerializeField] private float settleVelocity = 0.15f;
        [SerializeField] private float settleAngular = 10f;
        [SerializeField] private float settleTime = 0.25f;
        [Header("Spawn Settings")]
        public float spawnZ = 3.34f; // ✅ control Z from Inspector

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
            transform.position = new Vector3(spawnPos.x, spawnPos.y, spawnZ); // ✅
        }

        void Update()
        {
            if (isRiding)
            {
                Vector3 pos = spawnPoint.position;
                transform.position = new Vector3(pos.x, pos.y, spawnZ); // ✅ not 0f anymore

                if (Input.GetMouseButtonDown(0))
                    Release();

                return;
            }

            if (maxFallSpeed > 0 && rb.velocity.y < -maxFallSpeed)
                rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);

            if (!hasResolved && deathZone != null && transform.position.y < deathZone.position.y)
                ResolveVoid();
        }

        void Release()
        {
            // Block if input is locked (settings panel, ability button)
            if (STGameManager.Instance != null && STGameManager.Instance.isInputLocked)
                return;

            // Block if tap is over any UI element (mouse)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // ✅ Block if tap is over any UI element (touch/mobile)
            if (Input.touchCount > 0)
            {
                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    return;
            }

            isRiding = false;
            rb.gravityScale = fallGravityScale;
            rb.constraints = RigidbodyConstraints2D.None;
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