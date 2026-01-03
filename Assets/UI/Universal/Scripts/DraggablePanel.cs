using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform panelRect;
    private RectTransform canvasRect;
    private Vector2 offset;

    void Awake()
    {
        panelRect = GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect,
            eventData.position,
            eventData.pressEventCamera,
            out offset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
            return;

        Vector2 targetPos = localPoint - offset;
        panelRect.localPosition = ClampToWindow(targetPos);
    }

    private Vector2 ClampToWindow(Vector2 targetPos)
    {
        Vector2 panelSize = panelRect.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;

        Vector2 min = new Vector2(
            -canvasSize.x / 2 + panelSize.x / 2,
            -canvasSize.y / 2 + panelSize.y / 2
        );

        Vector2 max = new Vector2(
            canvasSize.x / 2 - panelSize.x / 2,
            canvasSize.y / 2 - panelSize.y / 2
        );

        return new Vector2(
            Mathf.Clamp(targetPos.x, min.x, max.x),
            Mathf.Clamp(targetPos.y, min.y, max.y)
        );
    }
}
