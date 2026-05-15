using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class ChixGameManager : MonoBehaviour
{
    public static ChixGameManager Instance;

    [Header("UI Group Containers")]
    public GameObject heartsGroup;
    public GameObject bossTimerGroup;
    public GameObject bossHitGroup;

    [Header("Static UI")]
    public GameObject backButton;
    public GameObject hammerButton;
    public GameObject freezeButton;

    [Header("Boss Hammer Settings")]
    public GameObject hammerObject;
    private Animator hammerAnim;

    [Header("Normal Hammer Settings")]
    public GameObject normalHammerObject;
    private Animator normalHammerAnim;
    private bool isHammerBusy = false;

    [Header("UI Text References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bossTimerText;
    public TextMeshProUGUI bossHitCountText;
    public TextMeshProUGUI slowMoTimerText;
    public Image[] heartImages;

    [Header("Gameplay Variables")]
    public int hearts = 3;
    public int score = 0;
    public int scoreMultiplier = 1;
    public bool isFrozen = false;
    public bool isBossMode = false;
    public int bossLevel = 0;
    public int bossScoreThreshold = 1000;
    public bool isUltiHammerActive = false; // Persistent state for the upgrade

    [Header("References")]
    public SpawnManager spawnManager;
    public RectTransform[] normalHoles;
    public RectTransform bossHole;
    public GameObject bossPrefab;
    public BossChicken currentBoss;
    public RectTransform shakeRoot;

    [Header("Escalation")]
    public float spawnSpeedMultiplier = 1.0f;
    public int hitsPerLevelIncrement = 5;
    public float timeDecrementPerLevel = 2f;

    [Header("Freeze Settings")]
    public GameObject iceOverlay;
    public bool isBossFrozen = false;

    [Header("Navigation Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("Boss Loss Settings")]
    public GameObject bossOverPrefab;
    public Transform uiCanvasTransform;
    private int lastHitFrame = -1;


    [Header("Game Over Settings")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText; // Add this! Drag the text from inside the panel here.

    [Header("New Ultimate Hammer Settings")]
    public GameObject ultiHamObject; // Drag your UltiHam prefab/object here
    private Animator ultiHamAnim;

    [Header("Game Start")]
    public bool gameStarted = false;



    void Awake()
    {
        if (Instance == null) Instance = this;

        if (heartsGroup != null) heartsGroup.SetActive(true);
        if (bossTimerGroup != null) bossTimerGroup.SetActive(false);
        if (bossHitGroup != null) bossHitGroup.SetActive(false);
        if (slowMoTimerText != null) slowMoTimerText.gameObject.SetActive(false);
        if (hammerButton != null) hammerButton.SetActive(false);
        if (freezeButton != null) freezeButton.SetActive(false);

        if (hammerObject != null)
        {
            hammerObject.SetActive(false);
            hammerAnim = hammerObject.GetComponent<Animator>();
        }

        if (normalHammerObject != null)
        {
            normalHammerObject.SetActive(true);
            normalHammerAnim = normalHammerObject.GetComponent<Animator>();
            StartHammerFloating();
        }
    }

    /*void Start()
    {
        UpdateUI();
        SpawnFirstChicken();
    }*/

    void Start()
    {
        UpdateUI();

        if (spawnManager != null)
        {
            spawnManager.SpawnFirstYellowChicken();
        }
    }


    void SpawnFirstChicken()
    {
        if (spawnManager != null)
        {
            spawnManager.SpawnFirstYellowChicken();
        }
    }

    public void StartGameFromFirstChicken()
    {
        if (gameStarted) return;

        gameStarted = true;

        if (spawnManager != null)
        {
            spawnManager.StartSpawning();
        }
    }

    private void StartHammerFloating()
    {
        normalHammerObject.transform.DOLocalMoveY(20f, 1.5f)
            .SetRelative()
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetId("HammerFloat");
    }

    // --- Hammer Upgrading Logic ---

  
    public void ActivateUltimateHammer()
    {
        if (isUltiHammerActive) return;
        isUltiHammerActive = true;

        // 1. Hide the Upgrade Button so it can't be clicked again
        if (hammerButton != null) hammerButton.SetActive(false);

        // 2. Hide the Normal Hammer AND the regular Boss Hammer
        if (normalHammerObject != null) normalHammerObject.SetActive(false);
        if (hammerObject != null) hammerObject.SetActive(false);

        // 3. Setup the UltiHam
        if (ultiHamObject != null)
        {
            ultiHamObject.SetActive(true);
            ultiHamAnim = ultiHamObject.GetComponent<Animator>();

            // Ensure it is visible on top of other UI elements
            ultiHamObject.transform.SetAsLastSibling();

            // Animation: Pop effect
            ultiHamObject.transform.localScale = Vector3.zero;
            ultiHamObject.transform.DOKill(); // Clean up any existing tweens
            ultiHamObject.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);

            // Animation: Start floating
            ultiHamObject.transform.DOLocalMoveY(25f, 1.8f)
                .SetRelative()
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        // --- THE FIX: Tell the Boss to start taking -2 hits immediately ---
        if (currentBoss != null)
        {
            currentBoss.ActivateUltimateHammer();
        }
    }

    // --- Gameplay Hammer Logic ---

    public void UseNormalHammer(Vector3 worldTarget, Chicken chickenScript)
    {
        // Frame Lock: If we already processed a hit this exact frame, ignore.
        if (Time.frameCount == lastHitFrame) return;

        if (isHammerBusy || isBossMode || normalHammerObject == null) return;

        isHammerBusy = true;
        lastHitFrame = Time.frameCount; // Mark this frame as used

        DOTween.Kill("HammerFloat");
        normalHammerObject.transform.DOKill();

        StartCoroutine(HandleHammerSequence(worldTarget, chickenScript));
    }

    private IEnumerator HandleHammerSequence(Vector3 worldTarget, Chicken chickenScript)
    {
        // isHammerBusy is already set to true in UseNormalHammer, so we are locked.
        normalHammerObject.SetActive(true);
        
        Vector3 hitOffset = new Vector3(120f, 40f, 0f);
        normalHammerObject.transform.DOMove(worldTarget + hitOffset, 0.04f).SetUpdate(true);
        normalHammerObject.transform.DORotate(new Vector3(0, 0, -20f), 0.04f).SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.04f);

        if (normalHammerAnim != null)
        {
            normalHammerAnim.speed = 4f;

            // Use an extension: Reset then Set
            normalHammerAnim.ResetTrigger("Hit");
            normalHammerAnim.SetTrigger("Hit");

            // IMPORTANT: Wait one tiny frame and reset it again 
            // to ensure it doesn't linger in the Animator's memory
            yield return null;
            normalHammerAnim.ResetTrigger("Hit");
        }

        if (chickenScript != null) chickenScript.ExecuteHit();

        yield return new WaitForSecondsRealtime(0.15f);

        normalHammerObject.transform.DORotate(Vector3.zero, 0.1f).SetUpdate(true);
        isHammerBusy = false;
        StartHammerFloating();
    }

    public void PlayHammerHitAnimation()
    {
        // Check if UltiHam is active and has an animator
        if (isUltiHammerActive && ultiHamAnim != null)
        {
            ultiHamAnim.speed = 4f;
            ultiHamAnim.ResetTrigger("Hit");
            ultiHamAnim.SetTrigger("Hit");
        }
        // Fallback to the regular boss hammer if not upgraded
        else if (hammerAnim != null)
        {
            hammerAnim.speed = 4f;
            hammerAnim.ResetTrigger("Hit");
            hammerAnim.SetTrigger("Hit");
        }
    }

    // --- Boss Fight Logic ---

    void StartBossFight()
    {
        isBossMode = true;
        bossLevel++;

        if (heartsGroup != null) heartsGroup.SetActive(false);
        if (bossTimerGroup != null) bossTimerGroup.SetActive(true);
        if (bossHitGroup != null) bossHitGroup.SetActive(true);

        // Only show button if not already upgraded
        if (hammerButton != null) hammerButton.SetActive(!isUltiHammerActive);

        if (freezeButton != null) freezeButton.SetActive(true);
        if (backButton != null) backButton.SetActive(true);

        if (hammerObject != null)
        {
            hammerObject.SetActive(true);
            hammerObject.transform.DOKill();

            // If already upgraded when boss starts, ensure it shows visually
            if (isUltiHammerActive && hammerAnim != null) hammerAnim.SetTrigger("UltiHam");

            hammerObject.transform.DOLocalMoveY(25f, 1.8f)
                .SetRelative()
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        if (normalHammerObject != null) normalHammerObject.SetActive(false);
        if (spawnManager != null) spawnManager.StopSpawning();
        StartCoroutine(TransformToBossArena());
    }


    public void TriggerGameOver()
    {
        if (gameOverPanel == null) return;
        if (gameOverPanel.activeSelf) return;

        PlayerXPManager.SaveScore(score); // ? ADD THIS LINE

        if (finalScoreText != null)
            finalScoreText.text = "Final Score: " + score.ToString();
        if (spawnManager != null) spawnManager.StopSpawning();
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.localScale = Vector3.zero;
        gameOverPanel.transform.DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
        Time.timeScale = 0f;
    }


    public void EndBossFight(bool victory = true)
    {
        isBossMode = false;
        isUltiHammerActive = false;
     //   isBossMode = false;

        // Hide all boss-only items
       /* if (hammerObject != null) hammerObject.SetActive(false);
        if (ultiHamObject != null) ultiHamObject.SetActive(false); // <--- Crucial line*/

        // 1. Clean up Hammer UI/Visuals immediately
        if (ultiHamObject != null)
        {
            ultiHamObject.transform.DOKill();
            ultiHamObject.SetActive(false);
        }
        if (hammerObject != null) hammerObject.SetActive(false);
        if (hammerButton != null) hammerButton.SetActive(false);
        if (freezeButton != null) freezeButton.SetActive(false);

        if (victory)
        {
            score += 100;
            spawnSpeedMultiplier += 0.2f;
        }

        // 2. Transition UI to normal mode
        if (heartsGroup != null) heartsGroup.SetActive(true);
        if (bossTimerGroup != null) bossTimerGroup.SetActive(false);
        if (bossHitGroup != null) bossHitGroup.SetActive(false);

        // 3. Hide the Boss Hole
        if (bossHole != null)
        {
            bossHole.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => bossHole.gameObject.SetActive(false));
        }

        // --- THE FIX: WAIT 1 SECOND BEFORE STARTING NORMAL MODE ---
        // This creates the "breather" period you asked for
        DOVirtual.DelayedCall(1.0f, () =>
        {
            // 4. Bring back normal hammer
            if (normalHammerObject != null)
            {
                normalHammerObject.SetActive(true);
                StartHammerFloating();
            }

            // 5. Start Spawning again
            if (spawnManager != null) spawnManager.StartSpawning();

            // 6. Pop the normal holes back up
            foreach (RectTransform hole in normalHoles)
            {
                hole.localScale = Vector3.zero;
                hole.gameObject.SetActive(true);
                hole.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
            }

            UpdateUI();

        }).SetUpdate(true); // SetUpdate(true) ensures this works even if time is paused
    }

 

    IEnumerator TransformToBossArena()
    {
        foreach (RectTransform hole in normalHoles)
        {
            hole.DOScale(Vector3.zero, 0.6f).SetEase(Ease.InBack).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.8f);

        if (bossHole != null)
        {
            bossHole.gameObject.SetActive(true);
            bossHole.localScale = Vector3.zero;
            bossHole.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.6f);

        if (bossPrefab != null)
        {
            GameObject bossGO = Instantiate(bossPrefab, bossHole);
            bossGO.transform.SetAsFirstSibling();

            currentBoss = bossGO.GetComponent<BossChicken>();
            currentBoss.gameManager = this;
            currentBoss.maxHits = 20 + ((bossLevel - 1) * hitsPerLevelIncrement);
            currentBoss.hitsText = bossHitCountText;
            currentBoss.ultimateHammerActive = isUltiHammerActive;

            RectTransform bossRect = bossGO.GetComponent<RectTransform>();

            // --- FIXED POP UP: Move to height immediately, but scale from zero ---
            bossRect.anchoredPosition = new Vector2(0, 180f); // Set Y higher here
            bossRect.localScale = Vector3.zero;

            bossRect.DOScale(Vector3.one, 0.4f) // Normal chicken "Pop"
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(() => {
                    currentBoss.InitializeBoss();
                });

            float currentTimer = Mathf.Max(5f, 20f - (bossLevel - 1) * timeDecrementPerLevel);
            StartCoroutine(BossTimer(currentTimer));
        }
    }


    IEnumerator BossTimer(float duration)
    {
        float timeLeft = duration;
        while (timeLeft > 0 && isBossMode)
        {
            if (!isBossFrozen)
            {
                if (bossTimerText != null) bossTimerText.text = timeLeft.ToString("F1");
                timeLeft -= 0.1f;
            }
            yield return new WaitForSecondsRealtime(0.1f);
        }

        if (isBossMode && timeLeft <= 0) StartCoroutine(HandleBossLoss());
    }


    private IEnumerator HandleBossLoss()
    {
        isBossMode = false;

        // --- FIXED DESPAWN: Boss retreats first ---
        if (currentBoss != null)
        {
            // Kill floating movement so it doesn't interfere with retreating
            currentBoss.transform.DOKill();

            // Scale down exactly like a normal chicken despawning
            currentBoss.transform.DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .SetUpdate(true);

            yield return new WaitForSecondsRealtime(0.35f);
            currentBoss.gameObject.SetActive(false);
        }

        if (hammerObject != null) hammerObject.SetActive(false);

        // Now play the losing "Boss Over" animation
        if (bossOverPrefab != null && shakeRoot != null)
        {
            GameObject go = Instantiate(bossOverPrefab, shakeRoot);
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            Animator anim = go.GetComponent<Animator>();
            if (anim != null)
            {
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;
                anim.Play("BoosOver");
            }
        }

        yield return new WaitForSecondsRealtime(2.0f);
        TriggerGameOver();
    }
    // --- Scoring and Damage ---

    public void OnChickenHit(Chicken.ChickenType type)
    {
        switch (type)
        {
            case Chicken.ChickenType.Yellow: AddScore(10 * scoreMultiplier); break;
            case Chicken.ChickenType.Blue: StartCoroutine(FreezeTime(10f)); break;
            /*case Chicken.ChickenType.Green:
                StartCoroutine(ActivateMultiplier(5, 5f));
                AddScore(50);
                break;*/

            case Chicken.ChickenType.Green:
                AddScore(50); // ONLY instant bonus
                break;
            case Chicken.ChickenType.Bomb:
                // --- CHANGE THIS PART ---
                CameraShake();
                // We stop using DelayedGameOver and use the new sequence instead
                StartCoroutine(BombGameOverSequence());
                break;
        }
    }


    private IEnumerator BombGameOverSequence()
    {
        // This gives the bomb animation 0.8 seconds to play 
        // so the player sees the explosion before the menu pops up.
        yield return new WaitForSecondsRealtime(0.8f);
        TriggerGameOver();
    }

    public void MissYellow()
    {
        if (isBossMode) return;
        if (hearts > 0)
        {
            hearts--;
            if (hearts < heartImages.Length) heartImages[hearts].gameObject.SetActive(false);
        }
        UpdateUI();

        if (hearts <= 0) TriggerGameOver(); // Replaced GameOver()
    }

    void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
        if (!isBossMode && score >= bossScoreThreshold * (bossLevel + 1)) StartBossFight();
    }

    void UpdateUI() { if (scoreText != null) scoreText.text = score.ToString(); }
    void GameOver() { Time.timeScale = 0f; }

    private IEnumerator DelayedGameOver()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        GameOver();
    }

    // --- Power Ups ---

    public void ToggleBossFreeze()
    {
        if (!isBossMode || isBossFrozen) return;
        StartCoroutine(BossFreezeRoutine(5f));
    }

    private IEnumerator BossFreezeRoutine(float duration)
    {
        isBossFrozen = true; // This flag stops timeLeft in BossTimer()

        if (iceOverlay != null)
            iceOverlay.SetActive(true);

        float timer = duration;

        while (timer > 0)
        {
            if (slowMoTimerText != null)
            {
                slowMoTimerText.gameObject.SetActive(true);
                slowMoTimerText.text = Mathf.CeilToInt(timer).ToString();
            }

            yield return new WaitForSecondsRealtime(0.1f);
            timer -= 0.1f;
        }

        isBossFrozen = false;

        if (iceOverlay != null)
            iceOverlay.SetActive(false);

        if (slowMoTimerText != null)
            slowMoTimerText.gameObject.SetActive(false);
    }

  

    IEnumerator FreezeTime(float duration)
    {
        isFrozen = true;

        // Change 0.6f to 0.8f or 0.85f to make it "faster" during slow-mo
        Time.timeScale = 0.4f;

        if (slowMoTimerText != null) slowMoTimerText.gameObject.SetActive(true);

        float remainingTime = duration;
        while (remainingTime > 0)
        {
            if (slowMoTimerText != null)
                slowMoTimerText.text = Mathf.CeilToInt(remainingTime).ToString();

            // We use WaitForSecondsRealtime so the countdown itself stays fast
            yield return new WaitForSecondsRealtime(0.1f);
            remainingTime -= 0.1f;
        }

        // Reset back to normal speed
        Time.timeScale = 1f;
        isFrozen = false;

        if (slowMoTimerText != null) slowMoTimerText.gameObject.SetActive(false);
    }

    IEnumerator ActivateMultiplier(int mult, float duration)
    {
        scoreMultiplier = mult; yield return new WaitForSeconds(duration); scoreMultiplier = 1;
    }

    public void CameraShake()
    {
        if (isBossMode || shakeRoot == null) return;
        shakeRoot.DOKill();
        shakeRoot.DOShakeAnchorPos(0.4f, 30f, 20, 90f, true).SetUpdate(true);
    }

    public bool IsHammerBusy()
    {
        return isHammerBusy;
    }
  

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);

        // 1. Resume time FIRST
        Time.timeScale = 1f;

        // 2. Restart spawning cleanly
        if (gameStarted && !isBossMode && spawnManager != null)
        {
            spawnManager.StartSpawning(); // ?? THIS FIXES EVERYTHING
        }
    }

}