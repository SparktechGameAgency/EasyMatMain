using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SnapAndScaleCarousel : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public ScrollRect scrollRect;
    
    [Header("Snap Settings")]
    public float snapSpeed = 10f;

    [Header("Scale Settings")]
    public float centerScale = 1.15f;    // Size of the centered card
    public float edgeScale = 0.8f;       // Size of the side cards
    public float distanceToScale = 300f; // Pixels from center before fully shrinking

    private float[] pagePositions;
    private float targetPosition;
    private bool isDragging;
    private RectTransform[] cards;

    void Start()
    {
        int cardCount = scrollRect.content.childCount;
        pagePositions = new float[cardCount];
        cards = new RectTransform[cardCount];

        // Automatically grab all the cards inside the Content object
        for (int i = 0; i < cardCount; i++)
        {
            cards[i] = scrollRect.content.GetChild(i) as RectTransform;
        }

        if (cardCount > 1)
        {
            // Calculate percentage positions for snapping
            for (int i = 0; i < cardCount; i++)
            {
                pagePositions[i] = (float)i / (cardCount - 1);
            }

            // Start directly on the middle card
            int middleIndex = cardCount / 2;
            targetPosition = pagePositions[middleIndex];
            scrollRect.horizontalNormalizedPosition = targetPosition;
        }
        else
        {
            targetPosition = 0f;
        }
    }

    void Update()
    {
        // 1. Snapping Logic
        if (!isDragging)
        {
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                scrollRect.horizontalNormalizedPosition, 
                targetPosition, 
                Time.deltaTime * snapSpeed
            );
        }

        // 2. Scaling Logic
        Vector2 centerPosition = scrollRect.transform.position; // Get the absolute screen center

        foreach (RectTransform card in cards)
        {
            if (card == null) continue;

            // Check how far this specific card is from the center of the scroll view
            float distance = Mathf.Abs(centerPosition.x - card.transform.position.x);
            float normalizedDistance = Mathf.Clamp01(distance / distanceToScale);

            // Interpolate scale and apply it
            float targetScale = Mathf.Lerp(centerScale, edgeScale, normalizedDistance);
            card.localScale = new Vector3(targetScale, targetScale, 1f);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // Find closest page to snap to when user lifts their finger
        float currentPos = scrollRect.horizontalNormalizedPosition;
        float closestDistance = float.MaxValue;

        foreach (float pos in pagePositions)
        {
            float distance = Mathf.Abs(currentPos - pos);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetPosition = pos;
            }
        }
    }
}