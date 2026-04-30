using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this component to the Alien GameObject.
/// It reads the tunnel entry/exit points on each placed Block and
/// smoothly animates the alien through the connected path.
///
/// SETUP:
///   1. Add this script to the alien GameObject (the one assigned in BlockManager.alienObject).
///   2. Each Block prefab must have a TunnelBlock component that exposes:
///        - Vector3 TunnelEntryLocal  (local-space entry point of the carved tunnel)
///        - Vector3 TunnelExitLocal   (local-space exit point of the carved tunnel)
///   3. Assign alienMoveSpeed in the Inspector.
/// </summary>
public class AlienTunnelFollower : MonoBehaviour
{
    [Header("Movement")]
    public float alienMoveSpeed = 2f;       // World-units per second through the tunnel
    public float pauseBetweenBlocks = 0.1f; // Brief pause before entering the next block

    // Internal path queue – world-space waypoints the alien must travel through
    private Queue<Vector3> waypointQueue = new Queue<Vector3>();
    private bool isMoving = false;

    // -----------------------------------------------------------------
    // Called by BlockManager every time a tunnel block is successfully placed
    // -----------------------------------------------------------------
    public void OnNewBlockPlaced(Transform newBlock)
    {
        TunnelBlock tunnel = newBlock.GetComponent<TunnelBlock>();
        if (tunnel == null) return; // Trap block or block without tunnel data → skip

        // Convert local tunnel points to world space
        Vector3 entry = newBlock.TransformPoint(tunnel.TunnelEntryLocal);
        Vector3 exit = newBlock.TransformPoint(tunnel.TunnelExitLocal);

        waypointQueue.Enqueue(entry);
        waypointQueue.Enqueue(exit);

        if (!isMoving)
            StartCoroutine(TravelThroughTunnel());
    }

    // -----------------------------------------------------------------
    // Continuously drain the waypoint queue
    // -----------------------------------------------------------------
    private IEnumerator TravelThroughTunnel()
    {
        isMoving = true;

        while (waypointQueue.Count > 0)
        {
            Vector3 target = waypointQueue.Dequeue();

            // Move toward the next waypoint
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    alienMoveSpeed * Time.deltaTime
                );

                // Face the direction of travel (2D: rotate sprite)
                Vector3 dir = (target - transform.position).normalized;
                if (dir.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                }

                yield return null;
            }

            transform.position = target;

            // Small pause between entry and exit waypoints of the same block
            if (waypointQueue.Count > 0)
                yield return new WaitForSeconds(pauseBetweenBlocks);
        }

        isMoving = false;
    }

    // -----------------------------------------------------------------
    // Reset when the game restarts
    // -----------------------------------------------------------------
    private void OnDisable()
    {
        StopAllCoroutines();
        waypointQueue.Clear();
        isMoving = false;
    }
}