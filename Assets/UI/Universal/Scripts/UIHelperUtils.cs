using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class SpriteData
{
    public string Name { get; set; }
    public Sprite Sprite { get; set; }
    public string JsonPath { get; set; }
    public string PngPath { get; set; }
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

    // Start is called before the first frame update
    void Start()
    {





    }


    public void ResetAllControlBooleans()
    {
        save = false;
        delete = false;
    }


    public bool setAll()
    {
        if (onLibrary && onMy)
            return false;

        onMy = true;
        onLibrary = true;
        return true;
    }

    public bool setMy()
    {
        if (!onLibrary)
            return false;

        onMy = true;
        onLibrary = false;
        return true;
    }

    public bool setLibrary()
    {
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












    public List<SpriteData> LoadJsonSpritesFromPath(string pathJsons, string pathSprites)
    {

        List<SpriteData> list = new List<SpriteData>();

        if (!Directory.Exists(pathJsons))
            return list;

        // Lista temporária de PNGs válidos
        List<string> pngValids = new List<string>();

        string[] arquivosJson = Directory.GetFiles(pathJsons, "*.json");
        foreach (string arquivoJson in arquivosJson)
        {
            string nameFile = Path.GetFileNameWithoutExtension(arquivoJson);
            string pathImage = Path.Combine(pathSprites, nameFile + ".png");

            pngValids.Add(nameFile + ".png");

            Sprite sprite = GetSpriteFromPath(pathImage);

            list.Add(new SpriteData
            {
                Name = nameFile,
                Sprite = sprite,
                JsonPath = arquivoJson,
                PngPath = pathImage
            });
        }
        // Excluir PNGs órfãos
        if (Directory.Exists(pathSprites))
        {
            string[] filesPng = Directory.GetFiles(pathSprites, "*.png");
            foreach (string filePng in filesPng)
            {
                string namePng = Path.GetFileName(filePng);
                if (!pngValids.Contains(namePng))
                {
                    File.Delete(filePng);
                    Debug.LogWarning("PNG excluded because it has no corresponding JSON: " + filePng);
                }
            }
        }

        return list;
    }




}
