using UnityEngine;
using System.Collections.Generic;

public class TowerManager : MonoBehaviour
{
    public float blockHeight = 1.28f;
    public List<BlockController> tower = new List<BlockController>();
    private Vector3 nextPlacePosition;

    void Start()
    {
        nextPlacePosition = Vector3.zero;
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

        // Tag it as Tower for collision detection
        block.gameObject.tag = "Tower";

        // Check tunnel connection with block below
        if (tower.Count > 1)
        {
            BlockController lower = tower[tower.Count - 2];
            BlockController upper = tower[tower.Count - 1];

            bool connected = CheckTunnelConnection(lower, upper);

            if (!connected)
            {
                GameManagerBlock.Instance.GameOver("Tunnel broken!");
                return false;
            }
        }

        nextPlacePosition += Vector3.up * blockHeight;
        GameManagerBlock.Instance.AddScore();
        return true;
    }

    // ✅ Fixed — uses tunnelData not tunnelExit directly
    bool CheckTunnelConnection(BlockController lower, BlockController upper)
    {
        TunnelExit lowerExit = lower.tunnelData.exitPoint;
        TunnelExit upperEntry = upper.tunnelData.entryPoint;

        return lowerExit == upperEntry;
    }

    public Vector3 GetTopPosition()
    {
        return nextPlacePosition;
    }
}