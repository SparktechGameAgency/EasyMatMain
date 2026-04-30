using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public TowerManager towerManager;
    public float smoothSpeed = 3f;
    public float yOffset = 4f;   // how far above the tower top to sit

    void LateUpdate()
    {
        Vector3 topPos = towerManager.GetTopPosition();
        Vector3 target = new Vector3(
            transform.position.x,
            topPos.y + yOffset,
            transform.position.z
        );
        transform.position = Vector3.Lerp(transform.position, target, smoothSpeed * Time.deltaTime);
    }
}