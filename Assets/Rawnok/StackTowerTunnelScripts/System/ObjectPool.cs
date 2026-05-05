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

        public void ReturnBlock(GameObject obj)
        {
            obj.transform.SetParent(null);          // ✅ unparent from WorldRoot
            obj.transform.localScale = Vector3.one; // reset scale
            obj.SetActive(false);
            obj.tag = "Untagged";
        }
    }
}