using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ProfileImageManager : MonoBehaviour
{

    public FileManager fileManager;
    public FolderNavigation folderNavigation;
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static ProfileImageManager Instance { get; private set; }

    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("UI")]
    [Tooltip("RawImage ou Image que exibe o avatar do jogador.")]
    [SerializeField] private RawImage profileRawImage;   // prefira RawImage para Texture2D
    [SerializeField] private Image profileImage;     // alternativa se usar Image/Sprite
    public Button setAvatarButton; // Botão para abrir o seletor de avatar

    [Header("Padrão")]
    [Tooltip("Sprite exibido quando nenhuma imagem foi salva ainda.")]
    [SerializeField] private Sprite defaultSprite;

    // ── Constantes ─────────────────────────────────────────────────────────────
    private const string FILE_NAME = "profile_image.png";

    // ── Propriedade pública ────────────────────────────────────────────────────
    /// <summary>Textura atualmente em uso como avatar.</summary>
    public Texture2D CurrentTexture { get; private set; }

    // ── Caminho completo no disco ──────────────────────────────────────────────
    private string SavePath => Path.Combine(Application.persistentDataPath, FILE_NAME);

    // ══════════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Singleton seguro
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {

        if (fileManager == null)
            fileManager = FindFirstObjectByType<FileManager>();

        if (folderNavigation == null)
            folderNavigation = FindFirstObjectByType<FolderNavigation>();

        setAvatarButton.onClick.AddListener(() =>
        {

            folderNavigation.panelFolders.SetActive(true);

            folderNavigation.StartCreatingFolderButtons(fileManager.basePath_Sprite, folderNavigation.panelFolders);
        });

        LoadProfileImage();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MÉTODO PÚBLICO — chame este ao confirmar a seleção no botão do ScrollView
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Chame este método ao clicar em "Confirmar" / "Selecionar" no seu ScrollView.
    /// Ele salva a textura no disco e atualiza a UI imediatamente.
    /// </summary>
    /// <param name="selectedTexture">Texture2D escolhida pelo jogador.</param>
    public void OnImageSelected(Texture2D selectedTexture)
    {
        if (selectedTexture == null)
        {
            Debug.LogWarning("[ProfileImageManager] OnImageSelected: textura nula recebida.");
            return;
        }

        SaveProfileImage(selectedTexture);
        ApplyTextureToUI(selectedTexture);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Salvar
    // ══════════════════════════════════════════════════════════════════════════

    private void SaveProfileImage(Texture2D texture)
    {
        try
        {
            // Garante que a textura seja legível antes de encodar
            Texture2D readable = EnsureReadable(texture);
            byte[] pngBytes = readable.EncodeToPNG();

            File.WriteAllBytes(SavePath, pngBytes);
            Debug.Log($"[ProfileImageManager] Imagem salva em: {SavePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ProfileImageManager] Erro ao salvar imagem: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Carregar (chamado automaticamente no Start)
    // ══════════════════════════════════════════════════════════════════════════

    private void LoadProfileImage()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[ProfileImageManager] Nenhuma imagem salva encontrada. Usando padrão.");
            ApplyDefault();
            return;
        }

        try
        {
            byte[] pngBytes = File.ReadAllBytes(SavePath);

            // Cria textura temporária; LoadImage preenche largura/altura automaticamente
            Texture2D loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (loaded.LoadImage(pngBytes))
            {
                CurrentTexture = loaded;
                ApplyTextureToUI(loaded);
                Debug.Log($"[ProfileImageManager] Imagem carregada de: {SavePath}");
            }
            else
            {
                Debug.LogWarning("[ProfileImageManager] Falha ao decodificar PNG salvo. Usando padrão.");
                ApplyDefault();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ProfileImageManager] Erro ao carregar imagem: {ex.Message}");
            ApplyDefault();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helpers de UI
    // ══════════════════════════════════════════════════════════════════════════

    private void ApplyTextureToUI(Texture2D texture)
    {
        CurrentTexture = texture;

        if (profileRawImage != null)
        {
            profileRawImage.texture = texture;
        }

        if (profileImage != null)
        {
            // Converte Texture2D → Sprite para Image do Unity
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            profileImage.sprite = Sprite.Create(texture, rect, pivot);
            NetworkLobbyManager.Instance.CurrentSprite = profileImage.sprite;
        }
    }

    private void ApplyDefault()
    {
        CurrentTexture = null;

        if (profileRawImage != null)
            profileRawImage.texture = defaultSprite != null ? defaultSprite.texture : null;

        if (profileImage != null)
        {
            profileImage.sprite = defaultSprite;
            CurrentTexture = defaultSprite.texture;

            if (NetworkLobbyManager.Instance != null)
                NetworkLobbyManager.Instance.CurrentSprite = defaultSprite;
        }

    }

    // ══════════════════════════════════════════════════════════════════════════
    // Utilitário: torna textura legível se não for (ex: sprite importado)
    // ══════════════════════════════════════════════════════════════════════════

    private Texture2D EnsureReadable(Texture2D source)
    {
        // Se já for legível, retorna direto
        if (source.isReadable) return source;

        // Blit via RenderTexture para contornar a restrição de leitura
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0,
                                                      RenderTextureFormat.Default,
                                                      RenderTextureReadWrite.Default);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readable;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Opcional: deletar imagem salva (ex: botão "Resetar avatar")
    // ══════════════════════════════════════════════════════════════════════════

    public void DeleteSavedImage()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[ProfileImageManager] Imagem salva removida.");
        }
        ApplyDefault();
    }
}