/*using UnityEngine;
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
*/


using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // ? Helper — saves Firebase auth session before scene switch
    void SaveAuthSession()
    {
        var user = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
        {
            PlayerPrefs.SetString("LoggedInUID", user.UserId);
            PlayerPrefs.SetString("LoggedInEmail", user.Email);
            PlayerPrefs.Save();
            Debug.Log("? Auth session saved for: " + user.Email);
        }
    }

    // ? Loads a scene by name and opens HomePanel
   

    // ? Go to main menu and open AllGamePanel
    public void LoadSceneAndOpenPanel(string sceneName)
    {
        Time.timeScale = 1f;
        SaveAuthSession();
        PlayerPrefs.SetString("OpenPanel", "AllGamePanel");
        PlayerPrefs.Save();
        FirebaseAuthManager.returningFromGame = true; // ? set static flag
        SceneManager.LoadScene(sceneName);
    }
    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;
        SaveAuthSession();
        PlayerPrefs.SetString("OpenPanel", "HomePanel");
        PlayerPrefs.Save();
        FirebaseAuthManager.returningFromGame = true; // ? set static flag
        SceneManager.LoadScene(sceneName);
    }

    // ? Go to main menu and open any specific panel by name
    public void LoadSceneAndOpenSpecificPanel(string sceneName, string panelName)
    {
        Time.timeScale = 1f;
        SaveAuthSession();
        PlayerPrefs.SetString("OpenPanel", panelName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneName);
    }

    // ? Loads a scene by build index and opens HomePanel
    public void LoadSceneByIndex(int sceneIndex)
    {
        Time.timeScale = 1f;
        SaveAuthSession();
        PlayerPrefs.SetString("OpenPanel", "HomePanel");
        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneIndex);
    }

    // ? Reloads the current scene
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ? Quits the game (works in build only)
    public void QuitGame()
    {
        Debug.Log("Quit Game called");
        Application.Quit();
    }
}