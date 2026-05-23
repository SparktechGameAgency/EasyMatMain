using System.Collections.Generic;
using UnityEngine;

namespace StackTower
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance;

        [Header("Block Prefabs (add all variants here)")]
        public List<GameObject> blockPrefabs;

        [Header("Pool Settings")]
        public int poolSizePerPrefab = 5;

        private List<GameObject> allPooledObjects = new List<GameObject>();

        void Awake()
        {
            Instance = this;

            foreach (GameObject prefab in blockPrefabs)
            {
                for (int i = 0; i < poolSizePerPrefab; i++)
                {
                    GameObject obj = Instantiate(prefab);
                    obj.SetActive(false);
                    allPooledObjects.Add(obj);
                }
            }
        }

        public GameObject GetBlock()
        {
            List<GameObject> available = new List<GameObject>();

            foreach (GameObject obj in allPooledObjects)
            {
                if (!obj.activeInHierarchy)
                    available.Add(obj);
            }

            if (available.Count > 0)
            {
                int randomIndex = Random.Range(0, available.Count);
                GameObject chosen = available[randomIndex];
                chosen.SetActive(true);
                return chosen;
            }

            // Pool exhausted — create new
            GameObject fallback = blockPrefabs[Random.Range(0, blockPrefabs.Count)];
            GameObject newObj = Instantiate(fallback);
            allPooledObjects.Add(newObj);
            return newObj;
        }

        // ✅ Same as GetBlock but never returns a trap block — used by laser ability
        public GameObject GetNonTrapBlock()
        {
            List<GameObject> available = new List<GameObject>();

            foreach (GameObject obj in allPooledObjects)
            {
                if (obj.activeInHierarchy) continue;
                BlockController bc = obj.GetComponent<BlockController>();
                if (bc != null && bc.IsTrap) continue; // skip traps
                available.Add(obj);
            }

            if (available.Count > 0)
            {
                int randomIndex = Random.Range(0, available.Count);
                GameObject chosen = available[randomIndex];
                chosen.SetActive(true);
                return chosen;
            }

            // Pool exhausted — create from a non-trap prefab
            List<GameObject> nonTrapPrefabs = new List<GameObject>();
            foreach (GameObject prefab in blockPrefabs)
            {
                BlockController bc = prefab.GetComponent<BlockController>();
                if (bc == null || !bc.IsTrap)
                    nonTrapPrefabs.Add(prefab);
            }

            if (nonTrapPrefabs.Count == 0)
            {
                Debug.LogError("ObjectPool: No non-trap prefabs found!");
                return null;
            }

            GameObject fallback = nonTrapPrefabs[Random.Range(0, nonTrapPrefabs.Count)];
            GameObject newObj = Instantiate(fallback);
            allPooledObjects.Add(newObj);
            return newObj;
        }

        public void ReturnBlock(GameObject obj)
        {
            obj.transform.SetParent(null);          // ✅ unparent from WorldRoot
            obj.transform.localScale = Vector3.one; // reset scale
            obj.SetActive(false);
            obj.tag = "Untagged";
        }
    }
}