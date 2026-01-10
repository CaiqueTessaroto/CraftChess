using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PaitingToolsManager : MonoBehaviour
{
    public PaintingGridManager manager;



    public Button eraserButton;
    public Button eyedropperButton;
    public Button paintBucketButton;

    public Button circleButton;
    public Button lineButton;
    public Button rectButton;
    public Button shadowButton;
    public Button eraserAllButton;
    public Button selectButton;



    public Button upscaleButton;

    // Start is called before the first frame update
    void Start()
    {

        if (manager == null)
        {
            manager = FindObjectOfType<PaintingGridManager>();
        }

        if (shadowButton != null)
        {
            shadowButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.shadowMode = true;

                //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            });
        }

        if (rectButton != null)
        {
            rectButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.rectMode = true;

                //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            });
        }

        if (lineButton != null)
        {
            lineButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.lineMode = true;

                //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            });
        }


        if (circleButton != null)
        {
            circleButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.circleMode = true;

                //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            });
        }

        if (eraserButton != null)
        {
            eraserButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.eraserMode = true;

                Image buttonImage = eraserButton.GetComponent<Image>();

                UIHelperUtils.SetCursor(buttonImage.sprite);
                SetEraserColor();
            });
        }

        if (eraserAllButton != null)
        {
            eraserAllButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.eraseAll = true;

                Image buttonImage = eraserAllButton.GetComponent<Image>();

                UIHelperUtils.SetCursor(buttonImage.sprite);
                SetEraserColor();
            });
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.SaveStateForUndo();

                manager.OnSelecting = true;

                //SetCursor(selectButton, false);
            });
        }

        if (eyedropperButton != null)
        {
            eyedropperButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                Image buttonImage = eyedropperButton.GetComponent<Image>();

                UIHelperUtils.SetCursor(buttonImage.sprite);
                manager.eyedropperMode = true;
            });
        }

        if (paintBucketButton != null)
        {

            paintBucketButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.selectedColor = manager.previewSelectColor.color;

                Image buttonImage = paintBucketButton.GetComponent<Image>();

                UIHelperUtils.SetCursor(buttonImage.sprite, true);
                manager.paintBucketMode = true;
            });
        }

        if (upscaleButton != null)
        {
            upscaleButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

                manager.SaveStateForUndo();
                manager.UpdateScale();
            });
        }



    }

    // Update is called once per frame
    void Update()
    {

        //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);


    }


    public void SetEraserColor()
    {
        //float alpha = 60f / 255f;
        //Color color = new Color(1, 1, 1, alpha);
        manager.selectedColor = manager.baseGridColor;
    }







}
