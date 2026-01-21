using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class TutorialPage
{
    public Sprite image;
    [TextArea(3, 6)]
    public string description;
}

public class TutorialController : MonoBehaviour
{
    [Header("Data")]
    public TutorialData tutorialData;

    [Header("UI")]
    public Image image;
    public TextMeshProUGUI text;
    public TextMeshProUGUI pagination;
    public Button nextButton;
    public Button prevButton;
    public Button closeButton;

    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 100f;

    private Vector2 swipeStartPos;
    private bool isSwiping = false;

    private int currentIndex = 0;

    void Start()
    {
        nextButton.onClick.AddListener(() => Next());

        prevButton.onClick.AddListener(() => Previous());

        closeButton.onClick.AddListener(() => CloseTutorial());

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

    void OnEnable()
    {
        ShowPage(0);
    }

    void ShowPage(int index)
    {
        currentIndex = index;

        var page = tutorialData.pages[index];

        image.sprite = page.image;
        text.text = page.description;

        prevButton.interactable = index > 0;
        //nextButton.interactable = index < tutorialData.pages.Count - 1;
        
        nextButton.gameObject.SetActive(index < tutorialData.pages.Count - 1);

        closeButton.gameObject.SetActive(index == tutorialData.pages.Count - 1);

        UpdatePagination();
    }

    public void Next()
    {
        if (currentIndex < tutorialData.pages.Count - 1)
            ShowPage(currentIndex + 1);
    }

    public void Previous()
    {
        if (currentIndex > 0)
            ShowPage(currentIndex - 1);
    }

    public void CloseTutorial()
    {
        gameObject.SetActive(false);
        PlayerPrefs.SetInt("TutorialSeen", 1);
    }

    void UpdatePagination()
    {
        pagination.gameObject.SetActive(tutorialData.pages.Count > 1);

        int currentPage = currentIndex + 1;        // humano (1-based)
        int totalPages = tutorialData.pages.Count;

        pagination.text = $"{currentPage} / {totalPages}";
    }



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

        if (delta.x < 0 && currentIndex >= tutorialData.pages.Count - 1)
            return;

        if (delta.x > 0 && currentIndex <= 0)
            return;

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
