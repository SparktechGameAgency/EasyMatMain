using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public BallSpawner spawner;
    public Transform topRight, topLeft, bottomLeft, bottomRight;
    public Transform centerPoint;
    public Transform midLeft;
    public Transform midRight;

    [Header("Unlock Settings")]
    public int unlockMiddleAtScore = 20;
    private bool middleUnlocked = false;

    [Header("Gameplay Settings")]
    public float centerRadius = 2f;
    public float minSwipeDistance = 0.2f;
    public int maxMisses = 3;

    public Text finalScoreText;

    [Header("Ball Sprites (Assign 6 images)")]
    public Sprite redBall;
    public Sprite greenBall;
    public Sprite blueBall;
    public Sprite yellowBall;
    public Sprite purpleBall;
    public Sprite cyanBall;

    [Header("UI")]
    public Text scoreText;
    public Text livesText;
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button exitButton;

    [HideInInspector] public BallController currentBall;

    private Vector2 swipeStartWorld;
    private bool swipeStarted = false;
    public int score = 0;
    private int missCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Start with middle balls hidden
        if (midLeft) midLeft.gameObject.SetActive(false);
        if (midRight) midRight.gameObject.SetActive(false);

        if (gameOverPanel) gameOverPanel.SetActive(false);

        if (restartButton) restartButton.onClick.AddListener(RestartGame);
        if (exitButton) exitButton.onClick.AddListener(ExitGame);

        UpdateUI();
    }

    private void Update()
    {
        if (currentBall == null || missCount >= maxMisses) return;

        Vector2 center = centerPoint != null ? (Vector2)centerPoint.position : Vector2.zero;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            swipeStartWorld = world;
            swipeStarted = true;
        }

        if (swipeStarted && Input.GetMouseButtonUp(0))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            Vector2 swipe = (Vector2)world - swipeStartWorld;
            swipeStarted = false;

            if (swipe.magnitude < minSwipeDistance) return;

            if (Vector2.Distance(currentBall.transform.position, center) <= centerRadius)
            {
                Corner corner = GetCornerFromDirection(swipe);

                // ❌ Block mid-left & mid-right swipes before unlock
                if (!middleUnlocked &&
                    (corner == Corner.MidLeft || corner == Corner.MidRight))
                {
                    return;
                }

                Transform cornerTransform = GetCornerTransform(corner);

                if (cornerTransform != null && currentBall != null)
                {
                    currentBall.MoveToCorner(cornerTransform, corner, 12f);
                }
            }
        }
    }

    public void OnBallArrived(BallController ball, Corner corner)
    {
        BallColor expected = BallColor.Red;

        switch (corner)
        {
            case Corner.TopRight: expected = BallColor.Red; break;
            case Corner.TopLeft: expected = BallColor.Blue; break;
            case Corner.BottomLeft: expected = BallColor.Green; break;
            case Corner.BottomRight: expected = BallColor.Yellow; break;
            case Corner.MidLeft: expected = BallColor.Purple; break;
            case Corner.MidRight: expected = BallColor.Cyan; break;
        }

        if (ball.ballColor == expected)
        {
            score++;
            audiomanager.Instance.PlayCorrect();

            // ✅ ✅ SUCCESS POP
            if (ball != null)
                ball.SuccessPopAndDestroy();
        }
        else
        {
            missCount++;
            audiomanager.Instance.PlayWrong();

            // ❌ ❌ WRONG POP
            if (ball != null)
                ball.WrongPopAndDestroy();

            if (CameraShake.Instance != null)
                CameraShake.Instance.Shake();

            if (missCount >= maxMisses)
            {
                GameOver();
                return;
            }
        }

        UpdateUI();
        CheckUnlock();
        spawner.SpawnBall();
    }


    public void OnBallMissed(BallController ball)
    {
        missCount++;
        audiomanager.Instance.PlayWrong();

        // ❌ ❌ MISS POP
        if (ball != null)
            ball.WrongPopAndDestroy();

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake();

        if (missCount >= maxMisses)
        {
            GameOver();
            return;
        }

        UpdateUI();
        CheckUnlock();
        spawner.SpawnBall();
    }


    private void GameOver()
    {
        Debug.Log("🎮 GameOver Score = " + score); // ← ADD THIS
        //PlayerXPManager.SaveScore(score); // ✅ ADD THIS LINE
        PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Snake);

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (scoreText) scoreText.gameObject.SetActive(false);
        if (livesText) livesText.gameObject.SetActive(false);
        if (finalScoreText)
            finalScoreText.text = "Final Score: " + score;
        Time.timeScale = 0f;
        if (audiomanager.Instance != null)
            audiomanager.Instance.StopMusic();
    }

    private void UpdateUI()
    {
        if (scoreText) scoreText.text = $"{score}";
        if (livesText) livesText.text = $"{maxMisses - missCount}";
    }

    private void RestartGame()
    {
        score = 0;
        missCount = 0;

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (scoreText) scoreText.gameObject.SetActive(true);
        if (livesText) livesText.gameObject.SetActive(true);

        if (currentBall != null)
            Destroy(currentBall.gameObject);

        Time.timeScale = 1f;

        if (audiomanager.Instance != null)
            audiomanager.Instance.ResumeMusic();

        if (finalScoreText)
            finalScoreText.text = "";

        // Reset unlock state
        middleUnlocked = false;
        if (midLeft) midLeft.gameObject.SetActive(false);
        if (midRight) midRight.gameObject.SetActive(false);
        if (spawner) spawner.middleColorsUnlocked = false;

        spawner.SpawnBall();
        UpdateUI();
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private Corner GetCornerFromDirection(Vector2 dir)
    {
        // Horizontal swipe → mid corners
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y) * 1.2f)
        {
            if (dir.x > 0) return Corner.MidRight;
            else return Corner.MidLeft;
        }

        // Diagonal corners
        if (dir.x >= 0f && dir.y >= 0f) return Corner.TopRight;
        if (dir.x < 0f && dir.y >= 0f) return Corner.TopLeft;
        if (dir.x < 0f && dir.y < 0f) return Corner.BottomLeft;
        return Corner.BottomRight;
    }

    private Transform GetCornerTransform(Corner c)
    {
        switch (c)
        {
            case Corner.TopRight: return topRight;
            case Corner.TopLeft: return topLeft;
            case Corner.BottomLeft: return bottomLeft;
            case Corner.BottomRight: return bottomRight;
            case Corner.MidLeft: return midLeft;
            case Corner.MidRight: return midRight;
        }
        return null;
    }

    public Sprite GetSprite(BallColor c)
    {
        switch (c)
        {
            case BallColor.Red: return redBall;
            case BallColor.Blue: return blueBall;
            case BallColor.Green: return greenBall;
            case BallColor.Yellow: return yellowBall;
            case BallColor.Purple: return purpleBall;
            case BallColor.Cyan: return cyanBall;
        }
        return redBall;
    }

    private void CheckUnlock()
    {
        if (!middleUnlocked && score >= unlockMiddleAtScore)
        {
            middleUnlocked = true;

            if (midLeft) midLeft.gameObject.SetActive(true);
            if (midRight) midRight.gameObject.SetActive(true);

            if (spawner) spawner.middleColorsUnlocked = true;
        }
    }

}
