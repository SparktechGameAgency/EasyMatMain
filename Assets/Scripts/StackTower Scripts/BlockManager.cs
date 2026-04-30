using Cinemachine;
using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class BlockManager : MonoBehaviour
{
    [Header("References")]
    public GameObject blockPrefab;         // Block with tunnel path carved into it
    public GameObject trapBlockPrefab;     // Trap block (no tunnel) - used during asteroid challenges
    public Transform spawnPoint;
    public float spawnHeightOffset = 3f;
    public CameraTarget cameraTarget;

    [Header("Score Settings")]
    public int score = 0;
    public TMP_Text scoreText;

    [Header("Holder Settings")]
    public float holderStepUp = 0.5f;

    [Header("Holder")]
    public Transform holder;

    private int blockCount = 0;
    private bool canSpawn = true;
    private Transform topBlock;

    [Header("UI Panels")]
    public GameObject newPanel;     // Game Over Panel
    public GameObject gamePanel;

    [Header("Timer Settings")]
    public float challengeTimeLimit = 30f;
    private float timer;
    private bool isTimerRunning = false;
    public TMP_Text timerText;

    // ----- CHALLENGE VARIABLES -----
    [Header("Challenge Settings")]
    public int blocksToFirstChallenge = 15;
    public int blocksToNextChallenge = 10;
    public int blocksGoalInChallenge = 7;
    private int blocksLandedInCurrentChallenge = 0;
    private int totalBlocksLanded = 0;
    private int blocksSinceLastChallengeTrigger = 0;
    // --------------------------------

    [Header("Challenge UI")]
    public TMP_Text challengePopupText;

    // ------------------------------------------------------------------
    // ASTEROID SETTINGS (replaces Wind)
    // ------------------------------------------------------------------
    [Header("Asteroid Settings")]
    public bool asteroidEnabled = false;
    public int asteroidStartAfter = 10;          // Blocks before asteroid events can begin
    public int asteroidFrequency = 5;            // Asteroid event every X blocks after start
    private int blocksSinceLastAsteroid = 0;
    private bool isCurrentBlockAsteroidEvent = false;

    [Header("Asteroid Animation")]
    public GameObject asteroidAnimatorObject;    // Asteroid visual / animator
    public string asteroidAnimationName = "AsteroidPlay";

    [Header("Asteroid Impact")]
    public GameObject asteroidImpactObject;      // The asteroid-hitting-tower object/animator
    public Animator asteroidImpactAnimator;
    public float asteroidImpactAnimationDuration = 2.0f;
    public float asteroidImpactVerticalOffset = 0f;
    // ------------------------------------------------------------------

    // ------------------------------------------------------------------
    // TRAP BLOCK SETTINGS (used during asteroid challenges)
    // ------------------------------------------------------------------
    [Header("Trap Block Settings")]
    [Tooltip("Probability (0-1) that a block spawned during an asteroid challenge is a trap block.")]
    public float trapBlockChance = 0.3f;         // 30% chance per block during challenge
    private bool isNextBlockTrap = false;
    // ------------------------------------------------------------------

    // ------------------------------------------------------------------
    // TIME FREEZER POWER-UP
    // ------------------------------------------------------------------
    [Header("Time Freezer Power-Up")]
    public GameObject timeFreezerUI;             // Button / icon to activate Time Freezer
    public float timeFreezerDuration = 5f;       // How long the freeze lasts
    public int timeFreezerUsesPerChallenge = 1;  // Uses allowed per asteroid event
    private int timeFreezerUsesRemaining = 0;
    private bool isTimeFrozen = false;
    public TMP_Text timeFreezerCountText;        // Optional: shows remaining uses
    // ------------------------------------------------------------------

    // ------------------------------------------------------------------
    // ALIEN SETTINGS
    // ------------------------------------------------------------------
    [Header("Alien Settings")]
    public GameObject alienObject;               // The alien that flies through the tunnel
    // The alien movement is driven externally by an AlienTunnelFollower script
    // that reads connected tunnel paths on placed blocks.
    // BlockManager just enables/disables it and notifies it on block placement.
    // ------------------------------------------------------------------

    [Header("Settings Panel")]
    public GameObject settingsPanel;

    public CinemachineVirtualCamera virtualCamera;
    public float moveSpeed = 1f;
    public float moveRange = 2f;

    private Vector3 initialHolderPos;
    private float moveTimer = 0f;

    [Header("Final Score UI")]
    public TMP_Text finalScoreText;

    [Header("Sound Settings")]
    public AudioSource fallSound;
    public AudioSource gameOverSound;
    public AudioSource asteroidSound;            // Replaces windSound
    public AudioSource timeFreezeSound;

    [Header("Perfect Placement Settings")]
    public float snapThreshold = 0.4f;
    public float failThreshold = 1.2f;

    [Header("Camera Settings")]
    public float cameraStepOffset = 0.85f;

    // Slow fall (kept from original)
    [Header("Slow Fall Settings")]
    public int gravityReductionStartBlock = 18;
    public float reducedGravityScale = 0.5f;

    // -----------------------------------------------------------------------
    // Private state
    // -----------------------------------------------------------------------
    private void Start()
    {
        initialHolderPos = holder.position;

        timer = challengeTimeLimit;
        isTimerRunning = false;
        timerText.text = "";

        totalBlocksLanded = 0;
        blocksSinceLastChallengeTrigger = 0;
        blocksLandedInCurrentChallenge = 0;
        blocksSinceLastAsteroid = 0;

        if (asteroidImpactObject != null)
            asteroidImpactObject.SetActive(false);

        if (asteroidAnimatorObject != null)
            asteroidAnimatorObject.SetActive(false);

        if (challengePopupText != null)
            challengePopupText.gameObject.SetActive(false);

        // Hide Time Freezer UI until an asteroid challenge starts
        if (timeFreezerUI != null)
            timeFreezerUI.SetActive(false);

        // Alien starts hidden; the AlienTunnelFollower enables it when
        // there are enough stacked blocks to travel through.
        if (alienObject != null)
            alienObject.SetActive(false);

        SpawnBlock(autoDrop: true);
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (scoreText != null)
            scoreText.text = "" + score;
    }

    public IEnumerator ResetTimeScale(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // ---- CHALLENGE TIMER ----
        if (isTimerRunning && !isTimeFrozen)
        {
            timer -= Time.deltaTime;

            if (timerText != null)
                timerText.text = Mathf.Ceil(timer).ToString();

            if (timer <= 0)
            {
                isTimerRunning = false;
                timerText.text = "Time: 0";

                if (blocksLandedInCurrentChallenge < blocksGoalInChallenge)
                {
                    Debug.Log("Asteroid Challenge Failed: Goal not met in time.");
                    EndGame();
                    StartCoroutine(HandleAsteroidImpact());
                    canSpawn = false;
                }
                else
                {
                    EndChallengeSuccess();
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // ASTEROID IMPACT (replaces flood HandleTimeOut)
    // -----------------------------------------------------------------------
    private IEnumerator HandleAsteroidImpact()
    {
        // Show asteroid hitting the tower
        if (asteroidImpactObject != null)
            asteroidImpactObject.SetActive(true);

        if (asteroidImpactAnimator != null)
            asteroidImpactAnimator.Play("AsteroidImpact");

        if (asteroidSound != null)
            asteroidSound.Play();

        yield return new WaitForSeconds(asteroidImpactAnimationDuration);

        CleanupGameObjects();
        ShowGameOverPanel();
    }

    // -----------------------------------------------------------------------
    // CHALLENGE POPUP
    // -----------------------------------------------------------------------
    private IEnumerator ShowChallengePopup(int goalBlocks, float duration)
    {
        if (challengePopupText != null)
        {
            challengePopupText.text = $"Drop {goalBlocks} Blocks before impact!";
            challengePopupText.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            challengePopupText.gameObject.SetActive(false);
        }
    }

    // -----------------------------------------------------------------------
    // CHALLENGE SUCCESS
    // -----------------------------------------------------------------------
    private void EndChallengeSuccess()
    {
        Debug.Log("Asteroid Challenge Succeeded!");
        isTimerRunning = false;
        timer = challengeTimeLimit;
        timerText.text = "";
        blocksSinceLastChallengeTrigger = 0;

        // Hide Time Freezer during non-challenge phase
        if (timeFreezerUI != null)
            timeFreezerUI.SetActive(false);

        // Hide asteroid animator
        if (asteroidAnimatorObject != null)
            asteroidAnimatorObject.SetActive(false);

        // Stop asteroid sound
        if (asteroidSound != null)
            asteroidSound.Stop();
    }

    // -----------------------------------------------------------------------
    // HOLDER MOVEMENT
    // -----------------------------------------------------------------------
    private void LateUpdate()
    {
        UpdateHolderMovement();
    }

    private void UpdateHolderMovement()
    {
        if (holder == null || topBlock == null) return;

        moveTimer += Time.deltaTime * moveSpeed;
        float offsetX = Mathf.Sin(moveTimer) * moveRange;

        float desiredY = topBlock.position.y + (spawnHeightOffset - 0.5f);
        float targetY = holder.position.y;

        if (desiredY > holder.position.y)
            targetY = Mathf.Lerp(holder.position.y, desiredY, Time.deltaTime * 5f);

        holder.position = new Vector3(initialHolderPos.x + offsetX, targetY, holder.position.z);
    }

    // -----------------------------------------------------------------------
    // ROTATION CONSTRAINT HELPER
    // -----------------------------------------------------------------------
    public void SetBlockZRotationConstraint(Transform blockTransform, bool freeze)
    {
        Rigidbody2D rb = blockTransform.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (freeze)
                rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
            else
                rb.constraints &= ~RigidbodyConstraints2D.FreezeRotation;
        }
    }

    // -----------------------------------------------------------------------
    // BLOCK LANDED CALLBACK
    // -----------------------------------------------------------------------
    public void OnBlockLanded(Transform landedBlock)
    {
        if (fallSound != null)
            fallSound.Play();

        // Check if this was a trap block — instant game over
        TunnelBlock blockScript = landedBlock.GetComponent<TunnelBlock>();
        if (blockScript != null && blockScript.IsTrapBlock)
        {
            Debug.Log("Trap block landed on tower! Game over.");
            EndGame();
            StartCoroutine(HandleAsteroidImpact());
            return;
        }

        totalBlocksLanded++;

        // Notify the alien follower a new tunnel block was placed
        NotifyAlienOfNewBlock(landedBlock);

        if (isTimerRunning)
        {
            blocksLandedInCurrentChallenge++;

            if (blocksLandedInCurrentChallenge >= blocksGoalInChallenge)
                EndChallengeSuccess();
        }
        else
        {
            blocksSinceLastChallengeTrigger++;

            int triggerThreshold = totalBlocksLanded <= blocksToFirstChallenge
                ? blocksToFirstChallenge
                : blocksToNextChallenge;

            if (blocksSinceLastChallengeTrigger >= triggerThreshold)
            {
                Debug.Log($"Starting asteroid challenge: Goal = {blocksGoalInChallenge}, Time = {challengeTimeLimit}s");
                isTimerRunning = true;
                timer = challengeTimeLimit;
                blocksLandedInCurrentChallenge = 1;

                // Give the player Time Freezer uses
                timeFreezerUsesRemaining = timeFreezerUsesPerChallenge;
                UpdateTimeFreezerUI();
                if (timeFreezerUI != null)
                    timeFreezerUI.SetActive(true);

                StartCoroutine(ShowChallengePopup(blocksGoalInChallenge, 2.5f));
                ShowAsteroidApproachAnimation();
            }
        }

        // Game over if block placement is too far off
        if (topBlock != null)
        {
            float xDiff = landedBlock.position.x - topBlock.position.x;

            if (Mathf.Abs(xDiff) > failThreshold)
            {
                Rigidbody2D rb2d = landedBlock.GetComponent<Rigidbody2D>();
                if (rb2d != null) rb2d.bodyType = RigidbodyType2D.Dynamic;
                EndGame();
                return;
            }

            if (Mathf.Abs(xDiff) <= snapThreshold)
            {
                landedBlock.position = new Vector3(
                    topBlock.position.x,
                    landedBlock.position.y,
                    landedBlock.position.z
                );
            }

            SetBlockZRotationConstraint(topBlock, true);
        }

        AddScore(10);
        topBlock = landedBlock;
        SetBlockZRotationConstraint(topBlock, false);

        StartCoroutine(MoveHolderUp(holderStepUp, 0.3f));
        UpdateVisualElementsPosition();

        // --- ASTEROID FREQUENCY LOGIC ---
        if (!asteroidEnabled && blockCount >= asteroidStartAfter)
        {
            asteroidEnabled = true;
            blocksSinceLastAsteroid = 0;
        }
        else if (asteroidEnabled)
        {
            blocksSinceLastAsteroid++;
            if (blocksSinceLastAsteroid >= asteroidFrequency)
                blocksSinceLastAsteroid = 0;
        }

        if (blocksSinceLastAsteroid > 0)
        {
            if (asteroidSound != null) asteroidSound.Stop();
            if (asteroidAnimatorObject != null) asteroidAnimatorObject.SetActive(false);
        }
        // --- END ASTEROID FREQUENCY LOGIC ---

        if (canSpawn)
            StartCoroutine(SpawnNextBlock());
    }

    // -----------------------------------------------------------------------
    // ALIEN TUNNEL FOLLOWER NOTIFICATION
    // -----------------------------------------------------------------------
    private void NotifyAlienOfNewBlock(Transform newBlock)
    {
        if (alienObject == null) return;

        // Enable the alien once we have at least 2 stacked blocks with tunnels
        if (totalBlocksLanded >= 2 && !alienObject.activeSelf)
            alienObject.SetActive(true);

        // If an AlienTunnelFollower component exists, notify it
        AlienTunnelFollower follower = alienObject.GetComponent<AlienTunnelFollower>();
        if (follower != null)
            follower.OnNewBlockPlaced(newBlock);
    }

    // -----------------------------------------------------------------------
    // TIME FREEZER POWER-UP (called by UI button)
    // -----------------------------------------------------------------------
    public void ActivateTimeFreezer()
    {
        if (!isTimerRunning || timeFreezerUsesRemaining <= 0 || isTimeFrozen) return;

        timeFreezerUsesRemaining--;
        UpdateTimeFreezerUI();
        StartCoroutine(TimeFreezerCoroutine());
    }

    private IEnumerator TimeFreezerCoroutine()
    {
        isTimeFrozen = true;
        if (timeFreezeSound != null) timeFreezeSound.Play();

        // Visual feedback: you can trigger a freeze animation here
        Debug.Log($"Time frozen for {timeFreezerDuration}s!");
        yield return new WaitForSeconds(timeFreezerDuration);

        isTimeFrozen = false;
        Debug.Log("Time freeze ended.");
    }

    private void UpdateTimeFreezerUI()
    {
        if (timeFreezerCountText != null)
            timeFreezerCountText.text = timeFreezerUsesRemaining > 0
                ? $"Freeze x{timeFreezerUsesRemaining}"
                : "No Freezes";

        // Disable button when out of uses
        if (timeFreezerUI != null)
        {
            var btn = timeFreezerUI.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn != null) btn.interactable = timeFreezerUsesRemaining > 0;
        }
    }

    // -----------------------------------------------------------------------
    // ASTEROID APPROACH ANIMATION
    // -----------------------------------------------------------------------
    private void ShowAsteroidApproachAnimation()
    {
        if (asteroidAnimatorObject != null)
        {
            asteroidAnimatorObject.SetActive(true);
            asteroidAnimatorObject.GetComponent<Animator>()?.Play(asteroidAnimationName);
        }

        if (asteroidSound != null && !asteroidSound.isPlaying)
            asteroidSound.Play();
    }

    // -----------------------------------------------------------------------
    // SPAWN LOGIC
    // -----------------------------------------------------------------------
    private IEnumerator SpawnNextBlock()
    {
        canSpawn = false;
        yield return new WaitForSeconds(0.6f);
        SpawnBlock(autoDrop: false);
        canSpawn = true;
    }

    private void SpawnBlock(bool autoDrop)
    {
        blockCount++;
        Camofsetadd();

        // Determine if asteroid event applies to this block
        bool applyAsteroid = asteroidEnabled && blocksSinceLastAsteroid == 0;
        isCurrentBlockAsteroidEvent = applyAsteroid;

        // Determine if this block should be a trap block
        // Trap blocks only appear during an active timed challenge
        isNextBlockTrap = isTimerRunning && (Random.value < trapBlockChance);

        float yOffset = -0.7f;
        spawnPoint.position = new Vector3(holder.position.x, holder.position.y + yOffset, holder.position.z);

        // Choose prefab: trap block (no tunnel) or normal tunnel block
        GameObject prefabToSpawn = (isNextBlockTrap && trapBlockPrefab != null) ? trapBlockPrefab : blockPrefab;

        GameObject newBlock = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);
        newBlock.transform.SetParent(holder);

        TunnelBlock blockScript = newBlock.GetComponent<TunnelBlock>();
        if (blockScript != null)
        {
            // Pass asteroid state instead of wind; IsTrapBlock is set inside Block.Initialize
            blockScript.Initialize(this, blockCount, autoDrop, applyAsteroid, isNextBlockTrap);
        }

        if (cameraTarget != null)
            cameraTarget.SetTopBlock(newBlock.transform);

        if (applyAsteroid)
            ShowAsteroidApproachAnimation();
    }

    // -----------------------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------------------
    private IEnumerator MoveHolderUp(float step, float delay)
    {
        yield return new WaitForSeconds(delay);
        holder.position += new Vector3(0, step, 0);
    }

    void Camofsetadd()
    {
        if (virtualCamera != null)
            virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>().m_TrackedObjectOffset.y += cameraStepOffset;
    }

    // -----------------------------------------------------------------------
    // VISUAL ELEMENT POSITION UPDATE
    // -----------------------------------------------------------------------
    private void UpdateVisualElementsPosition()
    {
        // Update Asteroid Animator position relative to holder
        if (asteroidAnimatorObject != null)
        {
            Vector3 targetPos = new Vector3(
                asteroidAnimatorObject.transform.position.x,
                holder.position.y,
                asteroidAnimatorObject.transform.position.z
            );
            asteroidAnimatorObject.transform.position = targetPos;
        }

        // Update Asteroid Impact Object position
        if (asteroidImpactObject != null && topBlock != null)
        {
            Vector3 targetImpactPos = new Vector3(
                asteroidImpactObject.transform.position.x,
                topBlock.position.y + asteroidImpactVerticalOffset,
                asteroidImpactObject.transform.position.z
            );
            asteroidImpactObject.transform.position = targetImpactPos;
        }
    }

    // -----------------------------------------------------------------------
    // CLEANUP & GAME OVER
    // -----------------------------------------------------------------------
    private void CleanupGameObjects()
    {
        if (holder != null)
            holder.gameObject.SetActive(false);

        TunnelBlock[] allBlocks = FindObjectsOfType<TunnelBlock>();
        foreach (var block in allBlocks)
            Destroy(block.gameObject);

        if (asteroidAnimatorObject != null) asteroidAnimatorObject.SetActive(false);
        if (asteroidImpactObject != null) asteroidImpactObject.SetActive(false);
        if (asteroidSound != null) asteroidSound.Stop();

        if (challengePopupText != null)
            challengePopupText.gameObject.SetActive(false);

        if (timeFreezerUI != null)
            timeFreezerUI.SetActive(false);

        if (alienObject != null)
            alienObject.SetActive(false);
    }

    private void ShowGameOverPanel()
    {
        if (newPanel != null)
            newPanel.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = "" + score;
    }

    public void EndGame()
    {
        if (gameOverSound != null)
            gameOverSound.Play();

        if (gamePanel != null)
            gamePanel.SetActive(false);

        canSpawn = false;
        isTimerRunning = false;
        isTimeFrozen = false;
        timerText.text = "";

        if (timer > 0)
        {
            CleanupGameObjects();
            ShowGameOverPanel();
        }
        // If timer ran out, HandleAsteroidImpact() handles cleanup.
    }

    // -----------------------------------------------------------------------
    // SETTINGS
    // -----------------------------------------------------------------------
    public void OpenSettings()
    {
        canSpawn = false;
        StopAllCoroutines();
        isTimerRunning = false;
        isTimeFrozen = false;

        CleanupGameObjects();

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(true);

        if (holder != null)
            holder.gameObject.SetActive(true);

        canSpawn = true;
    }

    public void RestartGame()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}