using UnityEngine;
using System.IO;
using UnityEngine.UI;

#if UNITY_STANDALONE || UNITY_EDITOR
using SFB; // StandaloneFileBrowser
#endif

#if UNITY_ANDROID || UNITY_IOS
#endif
public class ImageImporter : MonoBehaviour
{
    public FileManager fileManager;
    public PaintingGridManager gridManager;
    public NavigationManage_Painting painting;

    [Header("Buttons:")]
    public Button import;

    // Start is called before the first frame update
    void Start()
    {

        if (fileManager == null)
            fileManager = FindFirstObjectByType<FileManager>();


        if (gridManager == null)
            gridManager = FindFirstObjectByType<PaintingGridManager>();

        if (painting == null)
            painting = FindFirstObjectByType<NavigationManage_Painting>();

        import.onClick.AddListener(() =>
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            ImportImageButton();

        });
    }


    public void ImportImageButton(bool direct = false)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        // PC/Mac/Linux
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
        };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Selecione uma imagem", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            if (direct)
                DirectImportImage(paths[0], 408, 408);
            else
                ImportImage(paths[0], 34, 34);

        }


#elif UNITY_ANDROID || UNITY_IOS
        // Mobile
        if (NativeFilePicker.IsFilePickerBusy()) return;

        // 1. Verifica a permissão (Nesta versão o retorno é bool)
        // O parâmetro 'false' geralmente é para permissão de leitura
        bool hasPermission = NativeFilePicker.CheckPermission(false);

        if (!hasPermission)
        {
            // Em versões que não têm 'RequestPermission', 
            // basta chamar o PickFile que ele solicita a permissão automaticamente.
            Debug.Log("Permissão ainda não concedida ou pendente.");
        }

        // 2. Tenta abrir o seletor diretamente
        // O plugin cuidará de pedir a permissão se necessário
        string[] mimeTypes = { "image/png", "image/jpeg" };

        NativeFilePicker.PickFile((path) =>
        {
            if (path != null)
            {
                if(direct)
                    DirectImportImage(path,408,408);
                else
                    ImportImage(path, 34, 34);
            }
            else
            {
                Debug.Log("Usuário cancelou a seleção ou permissão negada.");
            }
        }, mimeTypes);
#endif
    }


    public void DirectImportImage(string filePath, int targetWidth, int targetHeight)
    {
        // 1. Carrega os dados brutos
        byte[] fileData = System.IO.File.ReadAllBytes(filePath);
        Texture2D originalTexture = new Texture2D(2, 2);

        if (!originalTexture.LoadImage(fileData)) return;

        // 2. Calcula as proporções para evitar distorção (Aspect Fill/Crop)
        float scale = Mathf.Max((float)targetWidth / originalTexture.width, (float)targetHeight / originalTexture.height);
        int widthAfterScale = Mathf.RoundToInt(originalTexture.width * scale);
        int heightAfterScale = Mathf.RoundToInt(originalTexture.height * scale);

        // 3. Redimensiona temporariamente (ainda pode estar retangular)
        RenderTexture rt = RenderTexture.GetTemporary(widthAfterScale, heightAfterScale);
        RenderTexture.active = rt;
        Graphics.Blit(originalTexture, rt);

        // 4. Cria a textura final 408x408 e faz o Crop centralizado
        Texture2D finalTexture = new Texture2D(targetWidth, targetHeight);

        // Calcula o offset para centralizar o corte
        int offsetX = (widthAfterScale - targetWidth) / 2;
        int offsetY = (heightAfterScale - targetHeight) / 2;

        finalTexture.ReadPixels(new Rect(offsetX, offsetY, targetWidth, targetHeight), 0, 0);
        finalTexture.Apply();

        // 5. Limpeza de memória
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        Destroy(originalTexture);

        // Use a finalTexture aqui (ex: atribuir a uma Image de UI)
        painting.finalTexture = finalTexture;

        Debug.Log($"Imagem importada e cortada para: {finalTexture.width}x{finalTexture.height}");
    }

    public void ImportImage(string filePath, int targetWidth, int targetHeight)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);

        // Redimensiona para 34x34
        Texture2D resized = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        for (int x = 0; x < targetWidth; x++)
        {
            for (int y = 0; y < targetHeight; y++)
            {
                float u = (float)x / targetWidth;
                float v = (float)y / targetHeight;
                Color c = texture.GetPixelBilinear(u, v);
                resized.SetPixel(x, y, c);
            }
        }
        resized.Apply();

        //if (!gridManager.isUpscaled)
        //    gridManager.UpdateScale();

        gridManager.UpscaleGrid();
        gridManager.ClearGrid();

        // Aplica direto no grid
        for (int x = 0; x < targetWidth; x++)
        {
            for (int y = 0; y < targetHeight; y++)
            {
                Color c = resized.GetPixel(x, y);

                Vector2Int pos = new Vector2Int(x, y);
                if (gridManager.gridCells.TryGetValue(pos, out GameObject cell))
                {
                    Image img = cell.GetComponent<Image>();
                    if (c.a <= 0.01f)
                        img.color = gridManager.baseGridColor;
                    else
                        img.color = c;
                }
            }
        }

        gridManager.UpdatePreview();
        gridManager.SaveStateForUndo();

        if (fileNameWithoutExtension.Length <= 20)
            painting.namePiece.text = fileNameWithoutExtension;

        Debug.Log("Imagem importada e aplicada no grid!");

        AdsManager.TryShowInterstitial();
    }

}
