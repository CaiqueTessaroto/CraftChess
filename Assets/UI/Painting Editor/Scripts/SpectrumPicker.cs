using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SpectrumPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public Image spectrunImg;
    public RectTransform point;
    public ColorPickerManager colorPickerManager;

    private Texture2D spectrumTexture;
    private RectTransform spectrumRect;

    void Start()
    {

        if (colorPickerManager == null)
            colorPickerManager = FindFirstObjectByType<ColorPickerManager>();

    }

    void Awake()
    {
        spectrumRect = spectrunImg.rectTransform;
        spectrumTexture = spectrunImg.sprite.texture;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PickColor(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        PickColor(eventData);
    }

    void PickColor(PointerEventData eventData)
    {
        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spectrumRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
            return;

        Rect rect = spectrumRect.rect;

        // centro do círculo
        Vector2 center = rect.center;

        // raio do círculo
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;

        // distância do clique ao centro
        float distance = Vector2.Distance(localPoint, center);

        // 🚫 fora do círculo
        if (distance > radius)
            return;

        // normaliza posição (0–1)
        float x = Mathf.Clamp01((localPoint.x - rect.x) / rect.width);
        float y = Mathf.Clamp01((localPoint.y - rect.y) / rect.height);

        int texX = Mathf.RoundToInt(x * spectrumTexture.width);
        int texY = Mathf.RoundToInt(y * spectrumTexture.height);

        Color color = spectrumTexture.GetPixel(texX, texY);

        // move a bolinha
        MovePoint(localPoint);

        // aplica a cor
        colorPickerManager.SetPreviewColor(color);
        colorPickerManager.UpdateSlidersFromColor(color);
        colorPickerManager.UpdateHexFromColor(color);
    }

    void MovePoint(Vector2 localPos)
    {
        point.localPosition = localPos;
    }

}
