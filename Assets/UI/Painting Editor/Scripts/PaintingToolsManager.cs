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

    public Vector2 cursorHotspot = Vector2.zero;
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

                SetCursor(eraserButton, false);
                SetEraserColor();
            });
        }

        if (eraserAllButton != null)
        {
            eraserAllButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.eraseAll = true;

                SetCursor(eraserAllButton, false);
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

                SetCursor(eyedropperButton, false);
                manager.eyedropperMode = true;
            });
        }

        if (paintBucketButton != null)
        {

            paintBucketButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                manager.selectedColor = manager.previewSelectColor.color;

                SetCursor(paintBucketButton, true);
                manager.paintBucketMode = true;
            });
        }

        if (upscaleButton != null)
        {
            upscaleButton.onClick.AddListener(() =>
            {
                manager.DisableTools();

                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

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



    void SetCursor(Button buttonTool, bool bucket)
    {
        // Pega a imagem do botão
        Image buttonImage = buttonTool.GetComponent<Image>();

        if (buttonImage != null && buttonImage.sprite != null)
        {
            // Converte o Sprite para Texture2D
            Texture2D cursorTexture = SpriteToTexture2D(buttonImage.sprite);

            cursorTexture = MakeTextureReadableRGBA(cursorTexture); // garante que é CPU accessible ERGBA

            if (bucket)
                cursorHotspot = new Vector2(cursorTexture.width - 1, cursorTexture.height - 1);
            else
                cursorHotspot = new Vector2(0, cursorTexture.height - 1);

            // Aplica como cursor
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        }
    }

    Texture2D SpriteToTexture2D(Sprite sprite)
    {
        if (sprite.rect.width != sprite.texture.width || sprite.rect.height != sprite.texture.height)
        {
            // Recorta a textura na área do sprite
            Texture2D newText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
            Color[] pixels = sprite.texture.GetPixels((int)sprite.textureRect.x,
                                                      (int)sprite.textureRect.y,
                                                      (int)sprite.textureRect.width,
                                                      (int)sprite.textureRect.height);
            newText.SetPixels(pixels);
            newText.Apply();
            return newText;
        }
        else
        {
            return sprite.texture;
        }
    }


    Texture2D MakeTextureReadableRGBA(Texture2D texture)
    {
        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
        Graphics.Blit(texture, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        //Texture2D readableTexture = new Texture2D(texture.width, texture.height);
        Texture2D newTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        newTex.SetPixels(texture.GetPixels());
        newTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return newTex;
    }

}
