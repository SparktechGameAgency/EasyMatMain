using UnityEngine;
using System.Collections;

public class AlienRunner : MonoBehaviour
{
    public float speed = 2f;
    public TowerManager towerManager;

    private int currentBlockIndex = 0;
    private bool isRunning = false;

    void Start()
    {
        StartRunning();
    }

    public void StartRunning()
    {
        isRunning = true;
        StartCoroutine(RunThroughTunnel());
    }

    IEnumerator RunThroughTunnel()
    {
        while (true)
        {
            if (currentBlockIndex >= towerManager.tower.Count)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            BlockController block = towerManager.tower[currentBlockIndex];

            // ✅ Fixed — go through tunnelData to get waypoints
            Transform[] waypoints = block.tunnelData.waypoints;

            // Move through each waypoint in the block
            foreach (Transform wp in waypoints)
            {
                while (Vector3.Distance(transform.position, wp.position) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        wp.position,
                        speed * Time.deltaTime
                    );
                    yield return null;
                }
            }

            currentBlockIndex++;
            yield return null;
        }
    }
}