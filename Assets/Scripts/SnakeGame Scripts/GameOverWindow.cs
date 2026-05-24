using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;
using TMPro;

public class GameOverWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button playAgainButton;

    [SerializeField] private Text finalScoreText;
    //[SerializeField] private Button exitButton;
    [SerializeField] private Button returnHomeButton;

    [SerializeField] private TextMeshProUGUI stScoreText; // ST game score (TMP UI)

    [SerializeField] private string returnSceneName = "MainMenu";

    private void Start()
    {
        // Make sure the panel is hidden at start
        if (gameOverPanel == null)
            gameOverPanel.SetActive(false);

        // Hook up button events
        if (playAgainButton == null)
            playAgainButton.onClick.AddListener(RestartGame);

        //if (exitButton != null)
        //    exitButton.onClick.AddListener(ExitGame);

        if (returnHomeButton == null)
            returnHomeButton.onClick.AddListener(() => LoadScene(returnSceneName));

        //if (gameOverPanel == null)
        //    gameOverPanel.SetActive(false);
        ////gameObject.SetActive(true);

        //if (playAgainButton != null)
        //    playAgainButton.onClick.AddListener(RestartGame);

        //if (returnHomeButton != null)
        //    returnHomeButton.onClick.AddListener(() => LoadScene(returnSceneName));
    }

    //public void ShowGameOver()
    //{
    //    gameObject.SetActive(true);

    //    if (gameOverPanel != null)
    //    {
    //        gameOverPanel.SetActive(true);
    //        Debug.Log("Game Over Panel Active");
    //    }

    //    if (finalScoreText != null)
    //        finalScoreText.text = GameHandler.GetScore().ToString();

    //    // Pause the game
    //    Time.timeScale = 0f;
    //}
    public void ShowGameOver()
    {
        if (finalScoreText != null)
            finalScoreText.text = "Score: " + GameHandler.GetScore().ToString();

        if (stScoreText != null)
            stScoreText.text = "Score: " + StackTower.STGameManager.Instance.score.ToString();

        if (gameOverPanel != null)
        {
            Vector3 originalScale = new Vector3(0.6829165f, 0.6829165f, 0.6829165f);

            gameOverPanel.SetActive(true);
            gameOverPanel.transform.localScale = Vector3.zero;
            gameOverPanel.transform
                .DOScale(originalScale, 0.4f)
                .SetDelay(1f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(() => Time.timeScale = 0f);
        }
    }

    public void RestartGame()
    {
        Debug.Log("Play Again Clicked!");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Time.timeScale = 1f;

            // ✅ Save auth session
            var user = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
            if (user != null)
            {
                PlayerPrefs.SetString("LoggedInUID", user.UserId);
                PlayerPrefs.SetString("LoggedInEmail", user.Email);
            }

            // ✅ Set static flag
            FirebaseAuthManager.returningFromGame = true;

            // already existed
            PlayerPrefs.SetString("OpenPanel", "AllGamePanel");
            PlayerPrefs.Save();
            SceneManager.LoadScene(sceneName);
        }
    }


}