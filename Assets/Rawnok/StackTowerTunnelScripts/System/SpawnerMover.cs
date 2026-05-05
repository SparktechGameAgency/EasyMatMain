using UnityEngine;

namespace StackTower
{
    public class SpawnerMover : MonoBehaviour
    {
        [Header("Movement")]
        public float baseSpeed = 3f;
        public float leftBound = -3f;
        public float rightBound = 3f;

        [HideInInspector] public float currentSpeed;
        private float direction = 1f;
        private float lockedZ;  // ← store Z on start, never let it change

        void Start()
        {
            currentSpeed = baseSpeed;
            lockedZ = transform.position.z; // ← lock whatever Z you set in editor
        }

        void Update()
        {
            float newX = transform.position.x + direction * currentSpeed * Time.deltaTime;

            // Bounce off boundaries
            if (newX >= rightBound)
            {
                newX = rightBound;
                direction = -1f;
            }
            else if (newX <= leftBound)
            {
                newX = leftBound;
                direction = 1f;
            }

            // Always enforce locked Z — Z can never drift
            transform.position = new Vector3(newX, transform.position.y, lockedZ);
        }

        public void SetSpeed(float newSpeed)
        {
            currentSpeed = newSpeed;
        }
    }
}