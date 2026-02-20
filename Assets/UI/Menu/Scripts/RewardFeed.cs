using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RewardFeed : MonoBehaviour
{
    public CreditsManager creditsManager;

    [Header("UI")]
    public TMP_Text textTmp;
    public TMP_Text buttonTextTmp;
    public Image displayImage;

    [Header("Buttons")]
    public Button rewardBtn;
    public Button next;
    public Button previous;

    [Header("Config")]
    public float autoSlideTime = 3f;
    public float swipeThreshold = 50f;

    public int currentIndex = 0;
    Coroutine autoSlideRoutine;


    private Vector2 swipeStartPos;
    private bool isSwiping = false;

    void Start()
    {
        if (creditsManager == null)
        {
            creditsManager = FindFirstObjectByType<CreditsManager>();
        }

        if (RewardManager.Instance.rewards.Length == 0) return;

        rewardBtn.onClick.AddListener(() =>
        {


            RewardManager rewardManager = RewardManager.Instance;

            if (rewardManager.rewards[currentIndex].typeFeed == TypeFeed.Reward)
            {

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR

                if (!AdsManager.Instance.NoAdsEnabled)
                {
                    if (rewardManager.rewards[currentIndex] != null)
                    {
                        rewardManager.rewards[currentIndex].weight = 0.5f;
                    }

                    AdsManager.ShowRewarded();
                }
                else
                    StartCoroutine(RewardManager.Instance.GrantReward(rewardManager.rewards[currentIndex]));

#else
                RewardManager.Instance.GrantReward(rewardManager.rewards[currentIndex]);

#endif


            }
            else if (rewardManager.rewards[currentIndex].typeFeed == TypeFeed.Credits)
                creditsManager.ShowCredits();
            else
            {
                string url = rewardManager.rewards[currentIndex].Content;

                if (!string.IsNullOrEmpty(url))
                {
                    Application.OpenURL(url);
                }

            }


        });

        next.onClick.AddListener(() =>
        {
            Next();
        });

        previous.onClick.AddListener(() =>
        {
            Previous();
        });

        ShowReward(0);
        autoSlideRoutine = StartCoroutine(AutoSlide());
    }

    void Update()
    {
        HandleSwipe();
    }

    void HandleSwipe()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseSwipe();
#else
    HandleTouchSwipe();
#endif
    }

    IEnumerator AutoSlide()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSlideTime);
            Next();
        }
    }

    public void Next()
    {
        currentIndex = (currentIndex + 1) % RewardManager.Instance.rewards.Length;
        ShowReward(currentIndex);
        ResetAutoSlide();
    }

    public void Previous()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = RewardManager.Instance.rewards.Length - 1;

        ShowReward(currentIndex);
        ResetAutoSlide();
    }

    public void ShowReward(int index)
    {
        if (RewardManager.Instance == null)
            return;

        displayImage.sprite = RewardManager.Instance.rewards[index].image;

        if (RewardManager.Instance.rewards[currentIndex].typeFeed == TypeFeed.Reward)
        {
            //displayImage.sprite = RewardManager.Instance.rewards[index].image;

            string text = UIHelperUtils.T("reward.explanation");

            if (string.IsNullOrEmpty(text))
                text = "Watch an advertisement to unlock a random set of pieces.";

            string textBtn = UIHelperUtils.T("Unlock_Reward");

            if (string.IsNullOrEmpty(textBtn))
                textBtn = "Unlock Reward";


            buttonTextTmp.text = textBtn;
            textTmp.text = text;

            bool allunlock = RewardManager.Instance.AllRewardsUnlocked();

            if (allunlock)
                rewardBtn.interactable = false;

        }
        else if (RewardManager.Instance.rewards[currentIndex].typeFeed == TypeFeed.Credits)
        {
            //displayImage.sprite = RewardManager.Instance.rewards[index].image;

            string text = UIHelperUtils.T("credits.explanation");

            if (string.IsNullOrEmpty(text))
                text = "Click here to see the game credits and learn about the people that helped make this project a reality.";


            string textBtn = UIHelperUtils.T("Credits");

            if (string.IsNullOrEmpty(textBtn))
                textBtn = "Credits";


            buttonTextTmp.text = textBtn;
            textTmp.text = text;

            rewardBtn.interactable = true;
        }
        else
        {

            string text = UIHelperUtils.T("cartase.explanation");

            if (string.IsNullOrEmpty(text))
                text = "Contribute to the project on Catarse, our crowdfunding platform, and help make new modes, mechanics, and online multiplayer possible, with exclusive rewards for supporters.";


            string textBtn = UIHelperUtils.T("cartase.enter");

            if (string.IsNullOrEmpty(textBtn))
                textBtn = "Support the Project";


            buttonTextTmp.text = textBtn;
            textTmp.text = text;

            rewardBtn.interactable = true;
        }


    }

    void ResetAutoSlide()
    {
        if (autoSlideRoutine != null)
        {
            StopCoroutine(autoSlideRoutine);
            autoSlideRoutine = StartCoroutine(AutoSlide());
        }
    }

    // ===== SWIPE =====

    void HandleMouseSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPos = Input.mousePosition;
            isSwiping = true;
        }

        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            Vector2 endPos = Input.mousePosition;
            DetectSwipe(endPos);
            isSwiping = false;
        }
    }

    void HandleTouchSwipe()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            swipeStartPos = touch.position;
            isSwiping = true;
        }

        if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && isSwiping)
        {
            DetectSwipe(touch.position);
            isSwiping = false;
        }
    }

    void DetectSwipe(Vector2 endPos)
    {
        Vector2 delta = endPos - swipeStartPos;

        //if (delta.x < 0 && currentIndex >= rewards.Length - 1)
        //    return;

        //if (delta.x > 0 && currentIndex <= 0)
        //    return;

        if (Mathf.Abs(delta.x) < swipeThreshold)
            return;

        if (delta.x < 0)
        {
            // Swipe para esquerda → Próximo
            Next();
        }
        else
        {
            // Swipe para direita → Anterior
            Previous();
        }
    }
}
