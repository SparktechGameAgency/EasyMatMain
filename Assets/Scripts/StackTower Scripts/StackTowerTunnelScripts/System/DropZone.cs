using UnityEngine;
using UnityEngine.EventSystems;

namespace StackTower
{
    // Attach to a full-screen transparent UI Image (Raycast Target ON).
    // Place it BEHIND all other UI buttons in the hierarchy
    // so ability buttons naturally block it when tapped.
    public class DropZone : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            if (STGameManager.Instance != null)
                STGameManager.Instance.TryDropBlock();
        }
    }
}
