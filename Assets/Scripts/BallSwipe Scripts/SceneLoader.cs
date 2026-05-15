using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Loads a scene by its name
    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f; // Make sure game isn’t paused
        SceneManager.LoadScene(sceneName);
    }

    // Loads a scene by its build index (optional)
    public void LoadSceneByIndex(int sceneIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    // Reloads the current scene
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // Quits the game (works in build only)
    public void QuitGame()
    {
        Debug.Log("Quit Game called");
        Application.Quit();
    }
}
