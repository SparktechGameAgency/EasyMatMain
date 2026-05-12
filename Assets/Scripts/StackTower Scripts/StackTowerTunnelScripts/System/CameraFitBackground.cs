using UnityEngine;

[ExecuteAlways] // Works in Editor too, live preview
[RequireComponent(typeof(SpriteRenderer))]
public class CameraFitBackground : MonoBehaviour
{
    private SpriteRenderer sr;
    private Camera cam;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;
    }

    void Start()
    {
        FitToScreen();
    }

    void FitToScreen()
    {
        if (cam == null || sr == null || sr.sprite == null) return;

        // Get world size of camera view
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect; // aspect = width/height ratio

        // Get sprite's natural world size
        float spriteHeight = sr.sprite.bounds.size.y;
        float spriteWidth = sr.sprite.bounds.size.x;

        // Scale sprite to FILL camera on BOTH axes (no black bars ever)
        float scaleX = camWidth / spriteWidth;
        float scaleY = camHeight / spriteHeight;

        // Use the LARGER scale so it always covers the screen
        float finalScale = Mathf.Max(scaleX, scaleY);

        transform.localScale = new Vector3(finalScale, finalScale, 1f);

        // Always center it on camera
        transform.position = new Vector3(
            cam.transform.position.x,
            cam.transform.position.y,
            transform.position.z
        );
    }

#if UNITY_EDITOR
    void Update()
    {
        FitToScreen(); // Live-updates in editor when resizing Game view
    }
#endif
}