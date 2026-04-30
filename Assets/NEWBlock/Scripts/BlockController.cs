using UnityEngine;

public class BlockController : MonoBehaviour
{


    public TunnelBlockData tunnelData; // ← shows up in Inspector!
    public bool isTrapBlock = false;
    public float swingSpeed = 60f;       // degrees per second
    public float swingAmplitude = 45f;   // max angle
    public float fallSpeed = 5f;
  //  public bool isTrapBlock = false;

    // Tunnel connection points (set in Inspector)
    public Transform tunnelEntry;
    public Transform tunnelExit;
    public Transform[] tunnelWaypoints;

    private bool isPlaced = false;
    private bool isFalling = false;
    private float swingTime = 0f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isPlaced || isFalling) return;

        // Swing left and right like a pendulum
        swingTime += Time.deltaTime;
        float angle = Mathf.Sin(swingTime * swingSpeed * Mathf.Deg2Rad) * swingAmplitude;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Drop()
    {
        if (isPlaced) return;
        isFalling = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallSpeed;
    }

    public void Place()
    {
        isPlaced = true;
        isFalling = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
        transform.rotation = Quaternion.identity; // snap straight
    }
}