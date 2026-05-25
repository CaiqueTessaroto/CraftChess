using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popula um ScrollView com os sprites de Resources/Sprites/Avatar
/// e chama ProfileImageManager.OnImageSelected ao confirmar.
///
/// SETUP:
///   1. Adicione este script ao GameObject do painel/janela de seleção de avatar.
///   2. Atribua 'content'         → o Transform filho do ScrollView (Viewport > Content).
///   3. Atribua 'itemPrefab'      → prefab de botão (precisa de Button + Image).
///   4. Atribua 'confirmButton'   → botão "Confirmar" (opcional; pode confirmar no clique).
///   5. Atribua 'previewImage'    → RawImage de preview (opcional).
///   6. Defina 'avatarFolderPath' se quiser uma subpasta diferente.
/// </summary>
public class DefaultAvatarScrollView : MonoBehaviour
{
    public Button openScrollViewButton; // Botão que abre o painel de seleção (opcional)
    public Button closeScrollViewButton; // Botão que fecha o painel de seleção (opcional)
    public GameObject scrollViewPanel; // Painel que contém o ScrollView (para ativar/desativar)
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("ScrollView")]
    [Tooltip("Content do ScrollView onde os botões serão instanciados.")]
    [SerializeField] private Transform content;

    [Tooltip("Prefab de cada item (precisa ter Button + Image no root).")]
    [SerializeField] private GameObject itemPrefab;

    [Header("Seleção")]
    [Tooltip("(Opcional) Botão de confirmação — confirma o avatar selecionado.")]
    [SerializeField] private Button confirmButton;

    [Tooltip("(Opcional) RawImage para preview do avatar destacado.")]
    [SerializeField] private RawImage previewImage;

    [Header("Caminho")]
    [Tooltip("Caminho relativo dentro de Resources (sem 'Resources/').")]
    [SerializeField] private string avatarFolderPath = "Sprites/Avatar";

    // ── Estado interno ─────────────────────────────────────────────────────────
    private Sprite _selectedSprite;
    private Image  _selectedItemImage;   // para destacar visualmente o item ativo

    [Header("Visual de seleção")]
    [SerializeField] private Color selectedTint   = new Color(0.6f, 0.9f, 1f, 1f);
    [SerializeField] private Color deselectedTint = Color.white;

    // ══════════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSelection);
    }

    private void OnEnable()
    {
        PopulateGrid();
    }

    void Start()
    {
        // Configura botões de abrir/fechar, se existirem
        if (openScrollViewButton != null && scrollViewPanel != null)
            openScrollViewButton.onClick.AddListener(() => scrollViewPanel.SetActive(true));

        if (closeScrollViewButton != null && scrollViewPanel != null)
            closeScrollViewButton.onClick.AddListener(() => scrollViewPanel.SetActive(false));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // População
    // ══════════════════════════════════════════════════════════════════════════

    private void PopulateGrid()
    {
        // Limpa itens anteriores
        foreach (Transform child in content)
            Destroy(child.gameObject);

        _selectedSprite    = null;
        _selectedItemImage = null;

        // Carrega todos os sprites da pasta
        Sprite[] avatars = Resources.LoadAll<Sprite>(avatarFolderPath);

        if (avatars == null || avatars.Length == 0)
        {
            Debug.LogWarning($"[DefaultAvatarScrollView] Nenhum sprite encontrado em Resources/{avatarFolderPath}");
            return;
        }

        foreach (Sprite avatar in avatars)
        {
            Sprite spriteCopy = avatar;   // captura local para o closure

            GameObject item  = Instantiate(itemPrefab, content);
            Image      img   = item.GetComponent<Image>();
            Button     btn   = item.GetComponent<Button>();

            if (img != null) img.sprite = spriteCopy;

            btn.onClick.AddListener(() => OnItemClicked(spriteCopy, img));
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Clique em um item
    // ══════════════════════════════════════════════════════════════════════════

    private void OnItemClicked(Sprite sprite, Image itemImage)
    {
        // Remove destaque do item anterior
        if (_selectedItemImage != null)
            _selectedItemImage.color = deselectedTint;

        // Aplica destaque no item clicado
        _selectedSprite    = sprite;
        _selectedItemImage = itemImage;
        if (itemImage != null) itemImage.color = selectedTint;

        // Atualiza preview, se houver
        if (previewImage != null)
            previewImage.texture = sprite.texture;

        // Se não há botão de confirmação separado, confirma direto no clique
        if (confirmButton == null)
            ConfirmSelection();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Confirmação — chama o ProfileImageManager
    // ══════════════════════════════════════════════════════════════════════════

    public void ConfirmSelection()
    {
        if (_selectedSprite == null)
        {
            Debug.LogWarning("[DefaultAvatarScrollView] Nenhum avatar selecionado.");
            return;
        }

        scrollViewPanel.SetActive(false); // Fecha o painel de seleção

        ProfileImageManager.Instance.OnImageSelected(_selectedSprite.texture);

        //Debug.Log($"[DefaultAvatarScrollView] Avatar confirmado: {_selectedSprite.name}");
    }
}