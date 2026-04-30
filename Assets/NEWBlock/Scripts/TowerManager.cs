using UnityEngine;
using System.Collections.Generic;

public class TowerManager : MonoBehaviour
{
    public float blockHeight = 1.28f; // height of each block in world units
    public List<BlockController> tower = new List<BlockController>();
    private Vector3 nextPlacePosition;

    void Start()
    {
        nextPlacePosition = Vector3.zero; // base of tower
    }

    public bool TryPlaceBlock(BlockController block)
    {
        if (block.isTrapBlock)
        {
           
            GameManagerBlock.Instance.GameOver("Trap block hit the tower!");
            return false;
        }

        // Snap block into position
        block.transform.position = nextPlacePosition;
        block.Place();
        tower.Add(block);

        // Check tunnel connectivity
        if (tower.Count > 1)
        {
            bool connected = CheckTunnelConnection(
                tower[tower.Count - 2],
                tower[tower.Count - 1]
            );
            if (!connected)
            {
                GameManagerBlock.Instance.GameOver("Tunnel broken!");
                return false;
            }
        }

        nextPlacePosition += Vector3.up * blockHeight;
        return true;
    }

    bool CheckTunnelConnection(BlockController lower, BlockController upper)
    {
        // The exit of the lower block must match the entry of the upper block
        // Compare exit world position of lower to entry world position of upper
        float tolerance = 0.3f;
        Vector3 lowerExit = lower.tunnelExit.position;
        Vector3 upperEntry = upper.tunnelEntry.position;
        return Mathf.Abs(lowerExit.x - upperEntry.x) < tolerance;
    }

    public Vector3 GetTopPosition()
    {
        return nextPlacePosition;
    }
}