using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    public GameObject[] tunnelBlockPrefabs;
    public GameObject trapBlockPrefab;
    public Transform spawnPoint;      // child of Holder
    public Transform holder;          // drag Holder GO here
    public float trapChance = 0.1f;

    private BlockController currentBlock;
    private bool canDrop = true;

    // Alternate between S and Z block
    private int lastExitSide = 0;

    void Start()
    {
        SpawnNextBlock();
    }

    void Update()
    {
        // Any click or tap to drop
        if (canDrop && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            DropCurrentBlock();
        }
    }

    public void SpawnNextBlock()
    {
        canDrop = false;

        GameObject prefab;

        // Trap block chance
        if (Random.value < trapChance)
        {
            prefab = trapBlockPrefab;
        }
        else
        {
            // Alternate S and Z so tunnel always connects
            if (lastExitSide == 0)
            {
                prefab = tunnelBlockPrefabs[0]; // S block
                lastExitSide = 1;
            }
            else
            {
                prefab = tunnelBlockPrefabs[1]; // Z block
                lastExitSide = 0;
            }
        }

        // Spawn as CHILD of holder so it moves with it
        GameObject block = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        block.transform.SetParent(holder);          // ← key line
        block.transform.localPosition = Vector3.zero; // centered on holder

        currentBlock = block.GetComponent<BlockController>();
        canDrop = true;
    }

    void DropCurrentBlock()
    {
        if (currentBlock == null) return;

        canDrop = false;

        // Detach from holder so it falls independently
        currentBlock.transform.SetParent(null);     // ← unparent before drop
        currentBlock.Drop();
        currentBlock = null;

        Invoke(nameof(SpawnNextBlock), 0.8f);
    }
}