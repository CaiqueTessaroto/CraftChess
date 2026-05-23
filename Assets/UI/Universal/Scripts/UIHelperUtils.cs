using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System;


public class SpriteData
{
    public string Name { get; set; }
    public Sprite Sprite { get; set; }
    public string PngPath { get; set; }
}

public enum CursorHotspot
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}

public class UIHelperUtils : MonoBehaviour
{

    [Header("Control Options:")]
    public bool onMy = false;
    public bool onLibrary = false;

    [Header("Control Actions:")]
    public bool back = false;
    public bool delete = false;
    public bool save = false;
    public bool change = true;

    [Header("Control Panels:")]
    public bool OnFiles = false;
    public bool OnFolder = true;

    [Header("Icons Mouse:")]
    public Sprite TrashIcon;

    // Start is called before the first frame update
    void Start()
    {





    }

    public static string Translate(string key, params object[] args)
    {
        if (LocalizationManager.Instance != null)
        {
            try
            {
                return string.Format(LocalizationManager.Instance.Get(key), args);
            }
            catch
            {
                return key;
            }
        }

        return key;
    }


    public static string T(string key, params object[] args)
    {
        if (LocalizationManager.Instance != null)
        {
            try
            {
                return string.Format(LocalizationManager.Instance.Get(key), args);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public static string SetPowerText(int power)
    {
        string Tpower = T("power", power);
        if (string.IsNullOrEmpty(Tpower))
            Tpower = $"Power: {power}";

        return Tpower;

        string T(string key, params object[] args)
        {
            if (LocalizationManager.Instance != null)
            {
                try
                {
                    return string.Format(LocalizationManager.Instance.Get(key), args);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }

    public void ResetAllControlBooleans()
    {
        save = false;
        delete = false;
    }


    public bool setAll()
    {
        delete = false;

        if (onLibrary && onMy)
            return false;

        onMy = true;
        onLibrary = true;
        return true;
    }

    public bool setMy()
    {
        delete = false;

        if (!onLibrary)
            return false;

        onMy = true;
        onLibrary = false;
        return true;
    }

    public bool setLibrary()
    {
        delete = false;

        if (!onMy)
            return false;

        onMy = false;
        onLibrary = true;
        return true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static GameObject CreateImage(Transform parent, float width, float height, Sprite sprite)
    {
        if (parent == null)
        {
            Debug.LogWarning("Nenhum parent foi atribuído!");
            return null;
        }

        // Cria um novo GameObject para a imagem
        GameObject imageGO = new GameObject("DynamicImage", typeof(RectTransform), typeof(Image));

        // Define o parent (Canvas, painel, etc)
        imageGO.transform.SetParent(parent, false);

        // Obtém o componente Image e define o sprite
        Image img = imageGO.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true; // mantém a proporção do sprite (opcional)

        // Configura o tamanho e posição
        RectTransform rt = imageGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero; // centraliza (pode mudar depois)

        return imageGO;
    }


    public static void SetSizeScrollView(GameObject painel)
    {
        if (painel == null || painel.transform == null)
        {
            Debug.LogWarning("SetSizeScrollView: painel é nulo!");
            return;
        }

        GridLayoutGroup grid = painel.transform.Find("Scroll View/Viewport/Content").GetComponent<GridLayoutGroup>();
        RectTransform content = painel.transform.Find("Scroll View/Viewport/Content").GetComponent<RectTransform>(); ;
        ScrollRect scrollRect = grid.GetComponentInParent<ScrollRect>();

        if (scrollRect != null)
        {
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.content = content;
        }

        int elementCount = content.childCount;

        // Calcula o número de linhas
        int lineCount = Mathf.CeilToInt((float)elementCount / grid.constraintCount);

        // Altura total = linhas * cell size + (linhas - 1) * spacing + margem extra
        float totalHeight = (lineCount * grid.cellSize.y) + (lineCount * grid.spacing.y) + grid.spacing.y + 25;
        //float totalHeight = lineCount * grid.cellSize.y + Mathf.Max(0, lineCount - 1) * grid.spacing.y;

        // Ajusta o Content
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);

        // Ajusta pivot e anchors para topo
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);

        // Força atualização do layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    public static bool CheckTranslationFile(string rootPath, string selectBasePath, string pasta)
    {
        // Combina os caminhos e adiciona o nome do arquivo com a extensão
        string caminhoCompleto = Path.Combine(rootPath, selectBasePath, pasta, "translate.txt");

        // Retorna true se o arquivo existir, caso contrário, false
        return File.Exists(caminhoCompleto);
    }


    public static Sprite GetSpriteFromPath(string pathSprite) //GameObject buttonObj, string pathSprite, Vector2? size = null new Vector2(90, 90)
    {
        Sprite sprite = null;

        if (File.Exists(pathSprite))
        {
            // Carrega a textura do arquivo
            byte[] bytes = File.ReadAllBytes(pathSprite);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);

            sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        else
        {
            //Debug.LogWarning($"Arquivo não encontrado: {pathSprite}. Tentando carregar sprite padrão do Resources...");

            // Tenta carregar de Resources (ex.: Assets/Resources/Sprites/Default.png)
            sprite = Resources.Load<Sprite>("Sprites/Default/Piece_Default");

            if (sprite == null)
            {
                Debug.LogError("Sprite padrão não encontrado em Resources/Sprites/Piece_Default");
                return null;
            }
        }

        return sprite;
    }





    public static void SetCursor(Sprite sprite, CursorHotspot hotspotType = CursorHotspot.BottomLeft)
    {
        if (sprite == null) return;

        // Converte o Sprite para Texture2D
        Texture2D cursorTexture = SpriteToTexture2D(sprite);
        cursorTexture = MakeTextureReadableRGBA(cursorTexture);

        //MakeTextureBlack(cursorTexture);

        Vector2 cursorHotspot = GetHotspot(cursorTexture, hotspotType);

        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);


        Vector2 GetHotspot(Texture2D tex, CursorHotspot type)
        {
            switch (type)
            {
                case CursorHotspot.TopLeft:
                    return new Vector2(0, 0);

                case CursorHotspot.TopRight:
                    return new Vector2(tex.width - 1, 0);

                case CursorHotspot.BottomLeft:
                    return new Vector2(0, tex.height - 1);

                case CursorHotspot.BottomRight:
                    return new Vector2(tex.width - 1, tex.height - 1);

                case CursorHotspot.Center:
                    return new Vector2(tex.width / 2f, tex.height / 2f);

                default:
                    return Vector2.zero;
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





    public IEnumerator LoadSpritesFromPathCoroutine(
        string pathSprites,
        List<SpriteData> sprites
    )
    {
        sprites.Clear();

        if (!Directory.Exists(pathSprites))
            yield break;

        string[] filesPng = Directory.GetFiles(pathSprites, "*.png");

        foreach (string filePng in filesPng)
        {
            string nameFile = Path.GetFileNameWithoutExtension(filePng);

            Sprite sprite = GetSpriteFromPath(filePng);

            sprites.Add(new SpriteData
            {
                Name = nameFile,
                Sprite = sprite,
                PngPath = filePng
            });

            // 🔹 evita travar o frame
            yield return null;
        }
    }







}
