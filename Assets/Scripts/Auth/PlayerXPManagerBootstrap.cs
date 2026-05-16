using UnityEngine;

public class PlayerXPManagerBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (PlayerXPManager.Instance == null)
        {
            // ✅ Create as a brand new root object
            GameObject go = new GameObject("PlayerXPManager");
            go.AddComponent<PlayerXPManager>();
            Debug.Log("✅ PlayerXPManager bootstrapped from root.");
        }
    }
}