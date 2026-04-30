using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    public GameObject[] tunnelBlockPrefabs; // your different tunnel shapes
    public GameObject trapBlockPrefab;
    public Transform spawnPoint;            // top of screen
    public float trapChance = 0.1f;

    private BlockController currentBlock;

    void Start()
    {
        SpawnNextBlock();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            DropCurrentBlock();
        }
    }

    public void SpawnNextBlock()
    {
        // Occasionally spawn a trap block
        GameObject prefab;
        if (Random.value < trapChance)
        {
            prefab = trapBlockPrefab;
        }
        else
        {
            prefab = tunnelBlockPrefabs[Random.Range(0, tunnelBlockPrefabs.Length)];
        }

        GameObject block = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        currentBlock = block.GetComponent<BlockController>();
    }

    void DropCurrentBlock()
    {
        if (currentBlock == null) return;
        currentBlock.Drop();
        currentBlock = null;
        // Spawn next after short delay
        Invoke(nameof(SpawnNextBlock), 0.5f);
    }
}