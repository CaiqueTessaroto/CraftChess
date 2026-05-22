using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerLobbyUI : MonoBehaviour
{
    public GridLobby gridLobby;
    public InteractiveMultiplayerLobby interactiveMultiplayerLobby;

    [Header("Painel Local")]
    public TMP_Text blackSquadName;
    public Transform blackPiecesGrid;

    [Header("Painel Oponente")]
    public TMP_Text whiteSquadName;
    public Transform whitePiecesGrid;

    [Header("Prefabs")]
    public GameObject piece_ImgPrefab;

    public static MultiplayerLobbyUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {

        if (gridLobby == null)
            gridLobby = FindFirstObjectByType<GridLobby>();

        if (interactiveMultiplayerLobby == null)
            interactiveMultiplayerLobby = FindFirstObjectByType<InteractiveMultiplayerLobby>();
    }


    // ───────────────────────────────────────────────────────────────────────
    // Chamado quando o jogador seleciona um squad localmente
    // Chame isso no botão de seleção junto com SelectSquad e SetLocalSquadAndSync
    // ───────────────────────────────────────────────────────────────────────

    public void UpdateLocalPanel(string squadName, MatchSquadData squad)
    {
        if (blackSquadName != null)
            blackSquadName.text = squadName;

        RenderPiecesGrid(blackPiecesGrid, squad.Sprites);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Chamado automaticamente quando ambos os squads chegaram pela rede
    // ───────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        SquadSyncManager.Instance.OnRemoteSquadReady += OnSquadReady;
    }

    private void OnDisable()
    {
        SquadSyncManager.Instance.OnRemoteSquadReady -= OnSquadReady;
    }

    private void OnSquadReady(bool isWhite)
    {
        MatchSquadData squadWhite = MultiplayerLobbyState.WhiteSquad;
        MatchSquadData squadBlack = MultiplayerLobbyState.BlackSquad;

        if (gridLobby == null)
        {
            Debug.LogWarning($"[MultiplayerLobbyUI] gridLobby não atribuído.");
            return;
        }


        gridLobby.posInGrid.Clear();

        if (squadWhite != null)
        {
            if (whiteSquadName != null) whiteSquadName.text = squadWhite.Data.Name;
            RenderPiecesGrid(whitePiecesGrid, squadWhite.Sprites);
            gridLobby.LoadPiecesInGrid(squadWhite.Data, squadWhite.Sprites, false);
        }
        if (squadBlack != null)
        {
            if (blackSquadName != null) blackSquadName.text = squadBlack.Data.Name;
            RenderPiecesGrid(blackPiecesGrid, squadBlack.Sprites);
            gridLobby.LoadPiecesInGrid(squadBlack.Data, squadBlack.Sprites, true);
        }

        gridLobby.ClearGrid(gridLobby.posInGrid);

        //Debug.Log($"[MultiplayerLobbyUI] Painel {(isMySquad ? "local" : "oponente")} atualizado.");
        Debug.Log($"[MultiplayerLobbyUI] Painel atualizado.");
    }

    public void RefreshLocalUI()
    {
        MultiplayerLobbyState.Log("RefreshLocalUI");

        if (this == null || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[MultiplayerLobbyUI] RefreshLocalUI chamado mas objeto inativo.");
            return;
        }

        RefreshSquadPanel(MultiplayerLobbyState.LocalIsWhite);
    }

    private void RefreshSquadPanel(bool isWhite)
    {

        if (gridLobby == null)
        {
            Debug.LogWarning($"[MultiplayerLobbyUI] gridLobby nulo em RefreshSquadPanel.");
            return;
        }

        MatchSquadData squadWhite = MultiplayerLobbyState.WhiteSquad;
        MatchSquadData squadBlack = MultiplayerLobbyState.BlackSquad;

        gridLobby.posInGrid.Clear();

        if (squadWhite != null)
        {
            if (whiteSquadName != null) whiteSquadName.text = squadWhite.Data.Name;
            RenderPiecesGrid(whitePiecesGrid, squadWhite.Sprites);
            gridLobby.LoadPiecesInGrid(squadWhite.Data, squadWhite.Sprites, false);
        }
        if (squadBlack != null)
        {
            if (blackSquadName != null) blackSquadName.text = squadBlack.Data.Name;
            RenderPiecesGrid(blackPiecesGrid, squadBlack.Sprites);
            gridLobby.LoadPiecesInGrid(squadBlack.Data, squadBlack.Sprites, true);
        }

        gridLobby.ClearGrid(gridLobby.posInGrid);

        Debug.Log($"[MultiplayerLobbyUI] Painel {(isWhite ? "White" : "Black")} atualizado localmente.");
    }

    // ───────────────────────────────────────────────────────────────────────

    private void RenderPiecesGrid(Transform grid, Dictionary<string, Sprite> sprites)
    {
        if (grid == null) return;

        // Limpa grid anterior
        foreach (Transform child in grid)
            Destroy(child.gameObject);

        foreach (var kv in sprites)
        {
            GameObject img = Instantiate(piece_ImgPrefab, grid);
            img.name = kv.Key;

            Image imgComp = img.GetComponent<Image>();
            if (imgComp != null)
                imgComp.sprite = kv.Value;

            TextMeshProUGUI text = img.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = kv.Key;
        }
    }

}