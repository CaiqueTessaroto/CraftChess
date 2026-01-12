using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PaitingToolsManager : MonoBehaviour
{
    public PaintingGridManager manager;

    [Header("Cursor Sprite")]
    public Sprite eraserSprite;
    public Sprite eyedropperSprite;
    public Sprite paintBucketSprite;
    public Sprite circleSprite;
    public Sprite lineSprite;
    public Sprite rectSprite;
    public Sprite shadowSprite;
    public Sprite eraserAllSprite;
    public Sprite selectSprite;
    public Sprite setaSprite;

    [Header("Buttons")]
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

                UIHelperUtils.SetCursor(shadowSprite, CursorHotspot.Center);
            });
        }

        if (rectButton != null)
        {
            rectButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.rectMode = true;

                UIHelperUtils.SetCursor(rectSprite, CursorHotspot.TopLeft);
            });
        }

        if (lineButton != null)
        {
            lineButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.lineMode = true;

                UIHelperUtils.SetCursor(lineSprite, CursorHotspot.TopLeft);
            });
        }


        if (circleButton != null)
        {
            circleButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.circleMode = true;

                UIHelperUtils.SetCursor(circleSprite, CursorHotspot.TopLeft);
            });
        }

        if (eraserButton != null)
        {
            eraserButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.eraserMode = true;

                UIHelperUtils.SetCursor(eraserSprite);
                SetEraserColor();
            });
        }

        if (eraserAllButton != null)
        {
            eraserAllButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.eraseAll = true;

                UIHelperUtils.SetCursor(eraserAllSprite);
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

                UIHelperUtils.SetCursor(selectSprite, CursorHotspot.TopLeft);

                //SetCursor(selectButton, false);
            });
        }

        if (eyedropperButton != null)
        {
            eyedropperButton.onClick.AddListener(() =>
            {
                manager.DisableTools();


                UIHelperUtils.SetCursor(eyedropperSprite);

                manager.eyedropperMode = true;
            });
        }

        if (paintBucketButton != null)
        {

            paintBucketButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.selectedColor = manager.previewSelectColor.color;

                UIHelperUtils.SetCursor(paintBucketSprite, CursorHotspot.BottomRight);

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



    public void SetEraserColor()
    {
        //float alpha = 60f / 255f;
        //Color color = new Color(1, 1, 1, alpha);
        manager.selectedColor = manager.baseGridColor;
    }







}
