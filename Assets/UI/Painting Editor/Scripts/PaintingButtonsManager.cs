using UnityEngine;
using UnityEngine.UI;

public class PaintingButtonsManager : MonoBehaviour
{
    public PaintingGridManager manager;
    public GameManager gameManager;

    [Header("Buttons:")]
    public Button colorPickerButton;
    public Button menu;

    [Header("Panels:")]
    public GameObject colorPickerPanel;

    // Start is called before the first frame update
    void Start()
    {

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        if (manager == null)
        {
            manager = FindObjectOfType<PaintingGridManager>();
        }

        menu.onClick.AddListener(() =>
        {
            gameManager.ChangeScene("Menu");
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        });




        if (colorPickerButton != null)
        {
            colorPickerButton.onClick.AddListener(() =>
            {

                if (manager.eyedropperMode || manager.paintBucketMode || manager.eraserMode || manager.circleMode || manager.lineMode || manager.rectMode || manager.shadowMode || manager.eraseAll || manager.OnSelecting)
                {
                    manager.DisableTools();

                    manager.selectedColor = manager.previewSelectColor.color;
                }
                else
                    colorPickerPanel.SetActive(true);

            });
        }

    }



}
