using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RewardFeed : MonoBehaviour
{
    [Header("UI")]
    public Image displayImage;

    [Header("Buttons")]
    public Button rewardBtn;
    public Button next;
    public Button previous;

    [Header("Config")]
    public float autoSlideTime = 3f;
    public float swipeThreshold = 50f;

    int currentIndex = 0;
    Coroutine autoSlideRoutine;


    private Vector2 swipeStartPos;
    private bool isSwiping = false;

    void Start()
    {
        if (RewardManager.Instance.rewards.Length == 0) return;

        rewardBtn.onClick.AddListener(() =>
        {

            if (RewardManager.Instance.rewards[currentIndex] != null)
            {
                RewardManager.Instance.rewards[currentIndex].weight = 0.5f;
            }

            AdsManager.ShowRewarded();
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

    void ShowReward(int index)
    {
        displayImage.sprite = RewardManager.Instance.rewards[index].image;
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
