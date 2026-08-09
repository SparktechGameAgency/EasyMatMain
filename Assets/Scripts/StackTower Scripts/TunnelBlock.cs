using UnityEngine;


public class TunnelBlock : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // TUNNEL PATH  (local-space entry & exit points for the alien to follow)
    // -----------------------------------------------------------------------
    [Header("Tunnel Path (Local Space)")]
    [Tooltip("Where the alien enters this block's tunnel (local space).")]
    public Vector3 TunnelEntryLocal = new Vector3(-0.5f, 0f, 0f);

    [Tooltip("Where the alien exits this block's tunnel (local space).")]
    public Vector3 TunnelExitLocal = new Vector3(0.5f, 0f, 0f);

    [Header("Debug Gizmos")]
    public bool showGizmos = true;

    // -----------------------------------------------------------------------
    // TRAP BLOCK
    // -----------------------------------------------------------------------
    /// <summary>True when this block has no tunnel — landing it ends the game.</summary>
    public bool IsTrapBlock { get; private set; } = false;

    [Header("Trap Block Visual")]
    [Tooltip("Optional overlay (e.g. red X sprite) shown only on trap blocks.")]
    public GameObject trapIndicator;

    // -----------------------------------------------------------------------
    // PHYSICS / DROP
    // -----------------------------------------------------------------------
    private Rigidbody2D rb;
    private bool hasDropped = false;
    private bool hasLanded = false;
    private BlockManager manager;
    private int blockNumber;
    private float asteroidForce = 0f;

    /// <summary>Lateral impulse applied on drop during an asteroid event.</summary>
    public float AsteroidForce => asteroidForce;

    private static bool firstBlockGrounded = false;

    // -----------------------------------------------------------------------
    // Awake
    // -----------------------------------------------------------------------
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogError("FATAL: Rigidbody2D missing from TunnelBlock prefab.", this);
    }

   
    public void Initialize(
        BlockManager blockManager,
        int blockCount,
        bool autoDrop = false,
        bool applyAsteroid = false,
        bool isTrap = false)
    {
        manager = blockManager;
        blockNumber = blockCount;
        IsTrapBlock = isTrap;

        if (rb == null) return;

        // Trap indicator visibility
        if (trapIndicator != null)
            trapIndicator.SetActive(isTrap);

        // Asteroid lateral force (not applied to trap blocks)
        asteroidForce = 0f;
        if (manager != null && manager.asteroidEnabled && applyAsteroid && !isTrap)
          //  asteroidForce = (Random.value > 0.5f) ? manager.rightForce : manager.leftForce;

        // Initial kinematic state while riding the holder
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;

        if (manager != null)
            manager.SetBlockZRotationConstraint(this.transform, true);

        if (autoDrop)
            DropBlock();
    }

    // -----------------------------------------------------------------------
    // Update — wait for player tap to drop
    // -----------------------------------------------------------------------
    private void Update()
    {
        if (!hasDropped && rb != null)
        {
            if (Input.GetMouseButtonDown(0))
                DropBlock();
        }
    }

    // -----------------------------------------------------------------------
    // DropBlock
    // -----------------------------------------------------------------------
    private void DropBlock()
    {
        if (hasDropped || rb == null) return;

        hasDropped = true;
        transform.SetParent(null);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;

        // Soft-fall for later blocks
        if (manager != null && blockNumber >= manager.gravityReductionStartBlock)
        {
            rb.gravityScale = manager.reducedGravityScale;
            rb.linearDamping = 5f;
            rb.angularDamping = 10f;
        }
        else
        {
            rb.gravityScale = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
        }

        // Asteroid lateral impulse
        if (Mathf.Abs(asteroidForce) > 0f)
            rb.AddForce(new Vector2(asteroidForce, 0f), ForceMode2D.Impulse);

        // Brief time slow on drop
        Time.timeScale = 0.8f;
        if (manager != null)
            manager.StartCoroutine(manager.ResetTimeScale(0.3f));
    }

    // -----------------------------------------------------------------------
    // Collision
    // -----------------------------------------------------------------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb == null) return;

        // Hit the ground
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (!firstBlockGrounded)
            {
                firstBlockGrounded = true;
                rb.bodyType = RigidbodyType2D.Static;
                if (manager != null)
                    manager.SetBlockZRotationConstraint(this.transform, true);
            }
            else
            {
                if (manager != null) manager.EndGame();
            }
        }

        // Landed on another block
        if (!hasLanded && collision.gameObject.CompareTag("Block"))
        {
            hasLanded = true;
            if (manager != null)
                manager.OnBlockLanded(this.transform);
        }
    }

    // -----------------------------------------------------------------------
    // Gizmos — visualise tunnel path in Scene view
    // -----------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 entry = transform.TransformPoint(TunnelEntryLocal);
        Vector3 exit = transform.TransformPoint(TunnelExitLocal);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(entry, 0.08f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(exit, 0.08f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(entry, exit);
    }
}