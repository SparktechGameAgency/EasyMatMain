using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AlienRunner : MonoBehaviour
{
    public float speed = 2f;
    public TowerManager towerManager;

    private int currentBlockIndex = 0;
    private int currentWaypointIndex = 0;
    private bool isRunning = false;

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
            Transform[] waypoints = block.tunnelWaypoints;

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