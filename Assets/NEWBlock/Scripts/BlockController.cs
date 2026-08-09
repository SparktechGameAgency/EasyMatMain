using UnityEngine;

public class BlockController : MonoBehaviour
{
    public TunnelBlockData tunnelData;
    public bool isTrapBlock = false;
    public float fallSpeed = 8f;

    private bool isPlaced = false;
    private bool isFalling = false;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
    }

    public void Drop()
    {
        if (isPlaced || isFalling) return;
        isFalling = true;

        // Enable gravity to fall down
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallSpeed;
    }

    public void Place()
    {
        isPlaced = true;
        isFalling = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        transform.rotation = Quaternion.identity;
    }

    // When block lands on tower
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Tower") || col.gameObject.CompareTag("Ground"))
        {
            if (isTrapBlock)
            {
                GameManagerBlock.Instance.GameOver("Trap block hit tower!");
                return;
            }

            // Snap to tower
            TowerManager tower = FindObjectOfType<TowerManager>();
            tower.TryPlaceBlock(this);
        }
    }
}