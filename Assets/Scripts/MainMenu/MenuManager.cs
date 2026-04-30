using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public List<GameObject> allPanels;

    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    [Header("UFO Settings")]
    public Transform ufo, ufoParticle;
    public float ufoFlyDuration = 1.2f;
    public Ease ufoFlyEase = Ease.InOutQuad;

    private bool isUFOFlying = false;
    private Vector3 ufoStartPosition;
    private Vector3 ufoStartParScale;
    private Vector3 ufoStartScale;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            //Destroy(gameObject);
        }

        if (ufo != null)
        {
            ufoStartPosition = ufo.GetComponent<RectTransform>().anchoredPosition;
            ufoStartScale = ufo.localScale;
            ufoStartParScale = ufoParticle.localScale;
        }

        ShowPanel(mainMenuPanel);
    }

    private void ResetUFOPosition()
    {
        if (ufo != null)
        {
            RectTransform ufoRect = ufo.GetComponent<RectTransform>();
            ufoRect.anchoredPosition = ufoStartPosition;
            ufo.localScale = ufoStartScale;
            ufoParticle.localScale = ufoStartParScale;
            ufo.gameObject.SetActive(true);
        }
    }

    private void AnimatePanelFade(GameObject panel)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        panel.transform.localScale = Vector3.one * 0.4f;
        canvasGroup.alpha = 0f;
        panel.SetActive(true);

        // Animate both scale & fade
        panel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        //panel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutExpo);
        canvasGroup.DOFade(1f, 0.3f);
    }

    public void ShowPanel(GameObject panelToShow)
    {
        foreach (GameObject panel in allPanels)
            panel.SetActive(false);

        if (panelToShow != null)
        {
            AnimatePanelFade(panelToShow);
            panelHistory.Push(panelToShow);
        }

        // When you show the leaderboard panel, also call:
        //leaderboardManager.LoadLeaderboard();
    }

    public void UFOFlyIntoButton(GameObject targetPanel)
    {
        if (isUFOFlying) return;
        if (ufo == null || targetPanel == null) return;

        isUFOFlying = true;

        // Detect clicked button
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        if (clickedButton == null) return;

        Transform targetButton = clickedButton.transform;

        // FIND THE GLOW OBJECT
        Transform glow = targetButton.Find("GlowSuper");

        // Play glow appear animation
        if (glow != null)
        {
            glow.gameObject.SetActive(true);
            glow.localScale = Vector3.zero;  // start small

            glow.DOScale(80f, 0.65f)
                .SetEase(Ease.OutFlash);
        }

        // UFO Animation
        ufo.GetComponent<Animator>().enabled = true;
        ufo.gameObject.SetActive(true);
        ufo.DOKill();

        Sequence seq = DOTween.Sequence();

        RectTransform ufoRect = ufo.GetComponent<RectTransform>();
        RectTransform buttonRect = targetButton.GetComponent<RectTransform>();

        seq.Append(ufoRect.DOAnchorPos(buttonRect.anchoredPosition, ufoFlyDuration).SetEase(ufoFlyEase));
        seq.Join(ufo.DOScale(0.3f, ufoFlyDuration * 0.8f));
        seq.Join(ufoParticle.DOScale(100f, ufoFlyDuration * 0.8f));

        seq.OnComplete(() =>
        {
            ufo.localScale = Vector3.one;
            ufo.gameObject.SetActive(false);

            // Show the next panel
            ShowPanel(targetPanel);

            // Fade glow out after panel appears
            if (glow != null)
            {
                glow.DOScale(0f, 0.25f)
                    .SetEase(Ease.InBack)
                    .SetDelay(0.3f)
                    .OnComplete(() => glow.gameObject.SetActive(false));
            }

            isUFOFlying = false;
        });
    }



    public void CloseCurrentPanel()
    {
        if (panelHistory.Count > 0)
        {
            GameObject closedPanel = panelHistory.Pop();

            // Animate closing before deactivating
            closedPanel.transform.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack)
                .OnComplete(() => closedPanel.SetActive(false));

            if (panelHistory.Count > 0)
            {
                GameObject previousPanel = panelHistory.Peek();
                AnimatePanelFade(previousPanel);
            }
            else
            {
                AnimatePanelFade(mainMenuPanel);
                panelHistory.Push(mainMenuPanel);
            }
        }
    }

    public void GoBack()
    {
        if (panelHistory.Count > 1)
        {
            GameObject current = panelHistory.Pop();
            GameObject previous = panelHistory.Peek();

            // Animate current panel closing
            CanvasGroup currentGroup = current.GetComponent<CanvasGroup>();
            if (currentGroup == null)
                currentGroup = current.AddComponent<CanvasGroup>();

            current.transform.DOScale(0.8f, 0.25f)
                .SetEase(Ease.InBack);

            currentGroup.DOFade(0f, 0.25f)
                .OnComplete(() =>
                {
                    current.SetActive(false);

                    // Animate the previous panel back in
                    AnimatePanelFade(previous);

                    // If returning to main menu or a root panel, reset UFO
                    if (previous == mainMenuPanel)
                    {
                        ResetUFOPosition();
                        ufo.GetComponent<Animator>().enabled = false;
                    }
                });
        }
    }


    // Load another scene
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
