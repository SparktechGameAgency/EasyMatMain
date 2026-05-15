using UnityEngine;

public class PlayerXPManagerBootstrap : MonoBehaviour
{
    void Awake()
    {
        // ? Create PlayerXPManager if it doesn't exist yet
        if (PlayerXPManager.Instance == null)
        {
            GameObject go = new GameObject("PlayerXPManager");
            go.AddComponent<PlayerXPManager>();
            // DontDestroyOnLoad is handled inside PlayerXPManager.Awake()
            Debug.Log("? PlayerXPManager bootstrapped.");
        }
    }
}