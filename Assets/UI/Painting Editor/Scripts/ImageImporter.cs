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
            fileManager = FindObjectOfType<FileManager>();


        if (gridManager == null)
            gridManager = FindObjectOfType<PaintingGridManager>();

        if (painting == null)
            painting = FindObjectOfType<NavigationManage_Painting>();

        import.onClick.AddListener(() =>
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            ImportImageButton();

        });
    }


    public void ImportImageButton()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        // PC/Mac/Linux
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
        };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Selecione uma imagem", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            ImportImage(paths[0], 34, 34);

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
                ImportImage(path, 34, 34);
            }
            else
            {
                Debug.Log("Usuário cancelou a seleção ou permissão negada.");
            }
        }, mimeTypes);
#endif
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
    }

}
