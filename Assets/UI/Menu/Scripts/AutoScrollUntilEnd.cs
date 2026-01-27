using UnityEngine;
using UnityEngine.UI;

public class AutoScrollUntilEnd : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollSpeed = 0.075f;

    private bool isScrolling = true;

    void Start()
    {
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>();

        // Começa no topo
        scrollRect.verticalNormalizedPosition = 1f;
    }

    void Update()
    {
        if (!isScrolling) return;

        // Move o scroll para baixo
        scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;

        // Chegou no final
        if (scrollRect.verticalNormalizedPosition <= 0f)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            isScrolling = false;
        }
    }
}
