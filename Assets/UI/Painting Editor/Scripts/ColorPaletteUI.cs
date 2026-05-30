using UnityEngine;
using UnityEngine.UI;

public class ColorPaletteUI : MonoBehaviour
{

    public PaintingGridManager manager;

    // Start is called before the first frame update
    void Start()
    {

        if (manager == null)
        {
            manager = FindFirstObjectByType<PaintingGridManager>();
        }

        Button button = GetComponent<Button>();
        Image image = GetComponent<Image>();

        if (button != null && image != null && manager != null)
        {
            button.onClick.AddListener(() =>
            {
                manager.DisableTools();
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                manager.SetSelectedColor(image.color);
                manager.selectedButtonColor = gameObject;
            });
        }
        else
        {
            Debug.LogWarning("ColorPaletteUI: Componentes ou manager não atribuídos corretamente.");
        }

    }

}
