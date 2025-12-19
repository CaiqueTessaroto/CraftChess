using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;
using System.Collections;

using System.Linq;
using System.Text;



public class SquadManager : MonoBehaviour
{
    public FileManager fileManager;
    public InfoGridView infoGridView;
    public GridSquadManager gridSquadManager;

    public string squad;

    [Header("JSON:")]
    public Squad squadData;
    public List<UnitPieceData> placedPieces = new List<UnitPieceData>();
    public Dictionary<string, Sprite> pieceSprites = new Dictionary<string, Sprite>();

    [Header("Select Piece:")]
    public string currentPieceName;
    public string squadPiece;
    public int currentPiecepower;
    public Sprite spritePiece;

    [Header("Select King:")]
    public GameObject kingCell;
    public Sprite crownSprite;


    [Header("Preview Piece:")]
    public TMP_Text nameTmp;
    public TMP_Text powerTmp;
    public GameObject piecepanel;
    public Image previewImage;
    public Button deselectBtw;
    public GameObject casteling;
    public GameObject promotion;
    public Transform castelingContent;
    public Transform promotionContent;
    public GameObject viewPiecePrefab;

    [Header("Preview Squad:")]
    public GameObject squadpanel;
    public Button removeBtw;
    public Button crownBtw;
    public Image kingView;
    public TMP_Text squadnameTmp;
    public TMP_Text squadpowerTmp;
    public TMP_Text squadgridpowerTmp;
    public TMP_Text rulesTmp;
    public GameObject piecesCountPanel;
    public GameObject prefabCounter;

    [Header("Control:")]
    public bool selectedPiece = false;
    public bool removePiece = false;
    public bool setKing = false;
    public bool enabledMode = false;

    [Header("Save:")]
    public RectTransform gridPanel;




    // Start is called before the first frame update
    void Start()
    {

        //squadData = new SquadConfigData();
        squadData.Units = placedPieces;

        //squadData.power = 0;

        if (fileManager == null)
        {
            fileManager = FindObjectOfType<FileManager>();
        }

        if (gridSquadManager == null)
        {
            gridSquadManager = FindObjectOfType<GridSquadManager>();
        }

        deselectBtw.onClick.AddListener(() =>
        {
            DeselectPiece();
        });

        crownBtw.onClick.AddListener(() =>
        {
            removePiece = false;
            setKing = true;
        });

        removeBtw.onClick.AddListener(() =>
        {
            setKing = false;
            removePiece = !removePiece;
        });

    }

    public void DeselectPiece()
    {
        piecepanel.SetActive(false);
        selectedPiece = false;
        currentPieceName = "";
        squadpanel.SetActive(true);
    }

    public void CheckStrategicModeRules()
    {
        bool powerLimit = squadData.Power > 1600;
        bool hasKing = string.IsNullOrEmpty(squadData.King?.Name);

        bool uniqueKing = placedPieces.Count(p => p.Name == squadData.King.Name) > 1;
        bool sameArtPieces = squadData.Pieces.GroupBy(p => p.Sprite)
                            .Any(g => g.Count() > 1 && g.Select(p => p.Name).Distinct().Count() > 1);

        string powerLimitTxt = "Squad power must be less than 1600";
        string hasKingTxt = "There must be a King";
        string uniqueKingTxt = "The King's Piece must be unique";
        string sameArtPiecesTxt = "You cannot have different pieces with the same art.";

        enabledMode = !powerLimit && !hasKing && !uniqueKing && !sameArtPieces;

        string enabledTxt = "Strategic mode enabled";

        StringBuilder sb = new StringBuilder();

        if (!enabledMode)
        {
            rulesTmp.color = Color.black;

            if (powerLimit)
                sb.AppendLine(powerLimitTxt);

            if (hasKing)
                sb.AppendLine(hasKingTxt);

            if (uniqueKing)
                sb.AppendLine(uniqueKingTxt);

            if (sameArtPieces)
                sb.AppendLine(sameArtPiecesTxt);

            rulesTmp.text = sb.ToString();

        }
        else
        {
            rulesTmp.text = enabledTxt;
            rulesTmp.color = Color.green;
        }

    }
    
    public void LoadSquadData(string name, string rootPath)
    {

        string squadFolder = Path.Combine(rootPath, fileManager.basePath_SquadData, name);
        string dataFolder = Path.Combine(squadFolder);

        string path = Path.Combine(dataFolder, name + ".json");

        if (!File.Exists(path))
        {
            Debug.LogWarning("Arquivo de squad não encontrado em: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        Squad loadedData = JsonUtility.FromJson<Squad>(json);

        //Debug.Log("Squad carregado de: " + path);
        squadData = loadedData;
    }

    public void LoadPiecesInGrid()
    {
        DeselectPiece();

        gridSquadManager.RegenerateGrid();

        //formationData = LoadSquadData(name, rootPath);

        if (squadData == null)
        {
            Debug.LogWarning("Nenhum squad carregado.");
            squadData = new Squad();
            squadpowerTmp.text = $"Power: {0}";
            squadgridpowerTmp.text = $"{0}";
            placedPieces.Clear();
            UpdatePieceCountUI();
            return;
        }


        foreach (var piece in squadData.Units)
        {
            // pega a célula do grid na posição da peça
            GameObject cell = gridSquadManager.GetCellAtPosition(piece.Position);

            // seta variáveis globais (usadas no SetPieceToCell)
            currentPieceName = piece.Name;
            currentPiecepower = piece.Power;

            // coloca a peça
            SetPieceToCellFromJson(cell, piece);
        }

        // 🔹 Se tiver rei salvo, marca ele
        if (!string.IsNullOrEmpty(squadData.King.Name))
        {
            GameObject kingCellObj = gridSquadManager.GetCellAtPosition(squadData.King.Position);
            if (kingCellObj != null)
            {
                SetKing(kingCellObj, squadData.King.Position);
            }
        }

        int squadPower = CalculateTotalPower(placedPieces);

        squadpowerTmp.text = $"Power: {squadPower}";
        squadgridpowerTmp.text = $"{squadPower}";
        squadData.Units = new List<UnitPieceData>(placedPieces);

        UpdatePieceCountUI();
        CheckStrategicModeRules();
    }


    public void SetPieceToCellFromJson(GameObject cell, UnitPieceData piece)
    {
        // coloca o sprite na célula
        if (!pieceSprites.ContainsKey(piece.Name))
        {
            //squadData.Units.RemoveAll(p => p.Name == piece.Name);
            placedPieces.RemoveAll(p => p.Name == piece.Name);
            return;
        }

        SetSpriteFromJson(cell, piece);

        // 🔹 procura se já existe uma peça nessa posição
        UnitPieceData existingPiece = placedPieces.Find(p => p.Position == piece.Position);

        if (existingPiece != null)
        {
            // atualiza os dados da peça existente
            existingPiece.Name = piece.Name;
            existingPiece.Power = piece.Power;
            existingPiece.Position = piece.Position;
        }
        else
        {
            // adiciona uma nova peça
            placedPieces.Add(new UnitPieceData(piece.Name, piece.Power, piece.Position));
        }
    }

    public void SetSpriteFromJson(GameObject cell, UnitPieceData piece)
    {
        // procura se já existe um filho chamado "Piece"
        Transform pieceTransform = cell.transform.Find("Piece");
        Image pieceImage;

        if (pieceTransform == null)
        {
            // cria um novo GameObject dentro da célula
            GameObject pieceGO = new GameObject("Piece", typeof(RectTransform), typeof(Image));

            // define como filho da célula
            pieceGO.transform.SetParent(cell.transform, false);

            float margin = 5f; // margem em pixels

            // ajusta o RectTransform para ocupar toda a célula
            RectTransform rt = pieceGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);       // distância da borda inferior/esquerda
            rt.offsetMax = new Vector2(-margin, -margin);     // distância da borda superior/direita

            // pega o componente Image recém-criado
            pieceImage = pieceGO.GetComponent<Image>();
        }
        else
        {
            // se já existe, só pega o Image
            pieceImage = pieceTransform.GetComponent<Image>();
        }
        if (pieceSprites.ContainsKey(piece.Name))
            pieceImage.sprite = pieceSprites[piece.Name];


        pieceImage.preserveAspect = true;
    }
























    public UnitPieceData GetPieceAtPosition(Vector2Int pos)
    {
        return placedPieces.FirstOrDefault(p => p.Position == pos);
    }

    public void SetKing(GameObject cell, Vector2Int cellPos)
    {
        UnitPieceData piece = GetPieceAtPosition(cellPos);

        if (piece == null)
            return;

        if (kingCell != null && kingCell != cell)
        {
            Transform kingTransform = kingCell.transform.Find("Crown");

            if (kingTransform != null)
            {
                // destrói o GameObject "Piece" (junto com o componente Image)
                GameObject.Destroy(kingTransform.gameObject);
            }
        }

        squadData.King.Name = piece.Name;
        squadData.King.Position = cellPos;
        /*
        Transform pieceTransform = cell.transform.Find("Crown");
        Image image;
        
        if (pieceTransform == null)
        {
            // cria um novo GameObject dentro da célula
            GameObject pieceGO = new GameObject("Crown", typeof(RectTransform), typeof(Image));

            // define como filho da célula
            pieceGO.transform.SetParent(cell.transform, false);

            float margin = 10f; // margem em pixels

            // ajusta o RectTransform para ocupar toda a célula
            RectTransform rt = pieceGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);       // distância da borda inferior/esquerda
            rt.offsetMax = new Vector2(-margin, -margin);     // distância da borda superior/direita

            // pega o componente Image recém-criado
            image = pieceGO.GetComponent<Image>();
        }
        else
        {
            // se já existe, só pega o Image
            image = pieceTransform.GetComponent<Image>();
        }

                // aplica o sprite da peça
        image.sprite = crownSprite;
        image.preserveAspect = true;
        */

        SetPieceKingToButton(cell);

        kingCell = cell;

        setKing = false;

    }

    public void SetPieceKingToButton(GameObject cell)
    {
        Transform pieceTransform = cell.transform.Find("Piece");

        if (pieceTransform != null)
        {
            // Pega a imagem do Piece
            Image pieceImage = pieceTransform.GetComponent<Image>();

            // Copia o sprite
            if (pieceImage != null)
            {
                kingView.sprite = pieceImage.sprite;
            }
        }
    }




    public void SetPieceToCell(GameObject cell, Vector2Int pos)
    {
        // procura se já existe um filho chamado "Piece"
        SetSprite(cell);

        // 🔹 procura se já existe uma peça nessa posição
        UnitPieceData existingPiece = placedPieces.Find(p => p.Position == pos);

        if (existingPiece != null)
        {
            // atualiza os dados da peça existente
            existingPiece.Name = currentPieceName;
            existingPiece.Power = currentPiecepower;
            existingPiece.Position = pos;
        }
        else
        {
            // adiciona uma nova peça
            placedPieces.Add(new UnitPieceData(currentPieceName, currentPiecepower, pos));
        }

        int squadPower = CalculateTotalPower(placedPieces);

        squadpowerTmp.text = $"Power: {squadPower}";
        squadgridpowerTmp.text = $"{squadPower}";

        UpdatePieceCountUI();

        if (kingCell != null)
            if (kingCell == cell)
                SetKing(cell, pos);

    }



    public int CalculateTotalPower(List<UnitPieceData> pieces)
    {
        int total = 0;

        foreach (var piece in pieces)
        {
            total += piece.Power;
        }

        squadData.Power = total;

        return total;
    }



    public void SetSprite(GameObject cell)
    {
        // procura se já existe um filho chamado "Piece"
        Transform pieceTransform = cell.transform.Find("Piece");
        Image pieceImage;

        if (pieceTransform == null)
        {
            // cria um novo GameObject dentro da célula
            GameObject pieceGO = new GameObject("Piece", typeof(RectTransform), typeof(Image));

            // define como filho da célula
            pieceGO.transform.SetParent(cell.transform, false);

            float margin = 5f; // margem em pixels

            // ajusta o RectTransform para ocupar toda a célula
            RectTransform rt = pieceGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);       // distância da borda inferior/esquerda
            rt.offsetMax = new Vector2(-margin, -margin);     // distância da borda superior/direita

            // pega o componente Image recém-criado
            pieceImage = pieceGO.GetComponent<Image>();
        }
        else
        {
            // se já existe, só pega o Image
            pieceImage = pieceTransform.GetComponent<Image>();
        }

        // aplica o sprite da peça
        pieceImage.sprite = spritePiece;
        pieceImage.preserveAspect = true;


    }

    public void RemovePieceFromCell(GameObject cell, Vector2Int pos)
    {

        if (kingCell != null)
        {
            Transform kingTransform = cell.transform.Find("Crown");

            if (kingTransform != null)
            {
                // destrói o GameObject "Piece" (junto com o componente Image)
                GameObject.Destroy(kingTransform.gameObject);

                squadData.King.Name = "";
                squadData.King.Position = new Vector2Int();
                kingCell = null;
            }
        }

        RemoveSprite(cell);

        UnitPieceData pieceToRemove = placedPieces.Find(p => p.Position == pos);

        if (pieceToRemove != null)
        {
            placedPieces.Remove(pieceToRemove);
        }

        int squadPower = CalculateTotalPower(placedPieces);

        squadpowerTmp.text = $"Power: {squadPower}";
        squadgridpowerTmp.text = $"{squadPower}";

        UpdatePieceCountUI();

    }

    public void RemoveSprite(GameObject cell)
    {
        // procura se existe um filho chamado "Piece"
        Transform pieceTransform = cell.transform.Find("Piece");

        if (pieceTransform != null)
        {
            // destrói o GameObject "Piece" (junto com o componente Image)
            GameObject.Destroy(pieceTransform.gameObject);
        }
    }




    public void UpdatePieceCountUI()
    {

        // limpa contadores antigos
        foreach (Transform child in piecesCountPanel.transform)
        {
            Destroy(child.gameObject);
        }

        List<string> uniquePieces = new List<string>();

        // agrupa por nome de peça e conta
        var grouped = placedPieces
            .GroupBy(p => p.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() });

        foreach (var group in grouped)
        {
            // instancia o prefab
            GameObject panel = Instantiate(prefabCounter, piecesCountPanel.transform);

            if (!uniquePieces.Contains(group.Name)) //!uniquePieces.Contains(group.Name)
            {
                Image img = panel.GetComponentInChildren<Image>();
                //img.sprite = spritePiece;

                // seta sprite da peça
                if (pieceSprites.ContainsKey(group.Name))
                    img.sprite = pieceSprites[group.Name];

                // adiciona ao dicionário para futuras atualizações
                uniquePieces.Add(group.Name);
            }

            // define texto da quantidade
            TMP_Text text = panel.GetComponentInChildren<TMP_Text>();
            text.text = $"- {group.Count.ToString()}";
        }





    }




















    public void GetPieceOnCell(Vector2Int cellPos)
    {

        UnitPieceData unitPiece = placedPieces.Find(p => p.Position == cellPos);

        if (unitPiece != null)
        {
            if (pieceSprites.ContainsKey(unitPiece.Name))
            {
                string fullPath;
                string rootPath;
                SquadPieceData pieceData = squadData.Pieces.Find(p => p.NameInSquad == unitPiece.Name);

                if (pieceData.NativePiece)
                    rootPath = Application.streamingAssetsPath;
                else
                    rootPath = Application.persistentDataPath;

                fullPath = Path.Combine(rootPath, fileManager.basePath_PieceData, pieceData.Squad, pieceData.Name + ".json");

                if (File.Exists(fullPath))
                {
                    string fileJson = File.ReadAllText(fullPath);

                    SelectPiece(unitPiece.Name, fileJson, pieceSprites[unitPiece.Name], rootPath, false);
                }
                else
                {
                    Debug.LogWarning($"Arquivo não encontrado: {fullPath}");
                }

            }
        }

    }

    public void SelectPiece(string namePieceSquad, string json, Sprite sprite, string rootPath, bool selected = true)
    {
        MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);
        //squadManager.spritePiece = sprite;

        if (config.piece.Name != currentPieceName || config.piece.Squad != squadPiece)
        {
            SetInfoPiece(namePieceSquad, config, sprite, selected);
            StartCoroutine(SetPromotionsAndCastelingPieces(config, rootPath));
        }
    }





    public void SetInfoPiece(string namePieceSquad, MovementConfigData config, Sprite sprite, bool selected = true)
    {
        //MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        PieceInfo piece = config.piece;

        squadpanel.SetActive(false);
        piecepanel.SetActive(true);

        currentPieceName = namePieceSquad;
        currentPiecepower = piece.Power;
        squadPiece = piece.Squad;

        nameTmp.text = currentPieceName;
        powerTmp.text = $"Power: {currentPiecepower}";

        spritePiece = sprite;
        previewImage.sprite = sprite;

        removePiece = false;

        if (selected)
            selectedPiece = true;


    }

    public IEnumerator SetPromotionsAndCastelingPieces(MovementConfigData config, string selectRootPath)
    {

        //MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        casteling.SetActive(false);
        promotion.SetActive(false);

        foreach (Transform child in promotionContent.transform)
            Destroy(child.gameObject);

        foreach (Transform child in castelingContent.transform)
            Destroy(child.gameObject);

        if (config.special?.Pieces != null)
        {
            if (config.special.Pieces.Count > 0)
                casteling.SetActive(true);

            foreach (string name in config.special.Pieces)
            {
                yield return StartCoroutine(LoadPiecesImage(name, config.piece.Squad, castelingContent, selectRootPath));
            }
        }

        if (config.promotion?.Pieces != null)
        {
            if (config.promotion.Pieces.Count > 0)
                promotion.SetActive(true);

            foreach (string name in config.promotion.Pieces)
            {
                yield return StartCoroutine(LoadPiecesImage(name, config.piece.Squad, promotionContent, selectRootPath));
            }
        }

        yield return null;


        infoGridView.GenerateGridPiece(config);

        yield return null;

    }

    public IEnumerator LoadPiecesImage(string fileName, string squadPiece, Transform content, string selectRootPath)
    {
        //Transform content = panel.transform;

        string jsonPath = Path.Combine(selectRootPath, fileManager.basePath_PieceData, squadPiece, fileName + ".json");


        string json = File.ReadAllText(jsonPath);
        PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);
        PieceInfo piece = wrapper.piece;

        // Instancia o prefab do painel
        GameObject clone = Instantiate(viewPiecePrefab, content);

        // Define o nome do objeto (opcional)
        clone.name = "Preview_" + piece.Name;

        // Acha a imagem dentro do painel
        Image img = clone.GetComponentInChildren<Image>();

        Texture2D tex = fileManager.LoadTextureFromFile(piece.FolderSprite, piece.Art, fileManager.basePath_Sprite, selectRootPath);
        Sprite sprite = fileManager.ConvertTextureToSprite(tex);

        if (img != null)
        {
            img.sprite = sprite;
        }

        // Se quiser simular um carregamento assíncrono, pode colocar um yield
        yield return null;
    }


}
