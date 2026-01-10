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

    public GameObject dragPrefab;

    public string squad;
    public TMP_Text rulesTmp;

    [Header("Icons Mouse:")]
    public Sprite CrownIcon;
    public Sprite TrashIcon;

    [Header("JSON:")]
    public Squad squadData;
    public List<UnitPieceData> placedPieces = new List<UnitPieceData>();
    public Dictionary<string, Sprite> pieceSprites = new Dictionary<string, Sprite>();

    //public void SelectPiece(string namePieceSquad, string json, Sprite sprite, string rootPath, bool selected = true)
    [Header("Select Piece:")]
    public string currentPieceName;
    public string squadPiece;
    public int currentPiecepower;
    private Sprite spritePiece;
    private SquadPieceData currentPieceData;
    private string currentjson;
    private string currentRootPath;


    [Header("Select King:")]
    public GameObject kingCell;
    public Sprite crownSprite;


    [Header("Preview Piece:")]
    public TMP_Text nameTmp;
    public TMP_Text powerTmp;
    public GameObject piecepanel;
    public Image previewImage;
    public Button deselectBtw;
    public GameObject promotion;
    public GameObject casteling;
    public Transform promotionContent;
    public Transform castelingContent;
    public GameObject viewPiecePrefab;
    public Button moreSpecialBtw;
    public Button clearBtw;
    public GameObject infoGridPanel;

    [Header("Preview Squad:")]
    public GameObject squadpanel;
    public Button removeBtw;
    public Button crownBtw;
    public Image kingView;
    public TMP_Text squadnameTmp;
    public TMP_Text squadpowerTmp;
    public TMP_Text squadgridpowerTmp;
    public GameObject piecesCountPanel;
    public GameObject prefabCounter;

    [Header("Control:")]
    public bool selectedPiece = false;
    public bool removePiece = false;
    public bool setKing = false;
    public bool enabledMode = false;
    public bool editMode = false;
    public bool setCursor = false;

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
            if (editMode)
            {
                infoGridPanel.SetActive(true);

                promotion.SetActive(false);
                casteling.SetActive(false);
                moreSpecialBtw.gameObject.SetActive(true);
                clearBtw.gameObject.SetActive(false);

                SelectPiece(currentPieceName, currentPieceData, currentjson, spritePiece, currentRootPath);
            }
            else
                DeselectPiece();
        });

        crownBtw.onClick.AddListener(() =>
        {
            removePiece = false;
            setKing = true;
            setCursor = true;

            UIHelperUtils.SetCursor(CrownIcon);
        });

        removeBtw.onClick.AddListener(() =>
        {
            setKing = false;
            setCursor = true;

            removePiece = !removePiece;

            UIHelperUtils.SetCursor(TrashIcon);
        });

        clearBtw.onClick.AddListener(() =>
        {

            foreach (Transform child in promotionContent.transform)
                Destroy(child.gameObject);

            foreach (Transform child in castelingContent.transform)
                Destroy(child.gameObject);

            SquadPieceData pieceData = squadData.Pieces.Find(p => p.NameInSquad == currentPieceName);

            int power = pieceData.PromotionPieces.Count * 10;
            power += pieceData.CastlingPieces.Count * 10;
            pieceData.Power -= power;

            pieceData.PromotionPieces.Clear();
            pieceData.CastlingPieces.Clear();

            currentPiecepower = pieceData.Power;

            powerTmp.text = $"Power: {currentPiecepower}";

            UpdateSquadPower();

        });

        moreSpecialBtw.onClick.AddListener(() =>
        {

            infoGridPanel.SetActive(false);

            if (currentPiecepower <= 15)
                promotion.SetActive(true);

            if (currentPiecepower <= 80)
                casteling.SetActive(true);

            moreSpecialBtw.gameObject.SetActive(false);

            clearBtw.gameObject.SetActive(true);

            editMode = true;


        });

    }

    public void Update()
    {
        if (!setKing && !removePiece && setCursor)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            setCursor = false;
        }

    }

    public void DeselectPiece()
    {
        piecepanel.SetActive(false);
        selectedPiece = false;
        currentPieceName = "";
        squadpanel.SetActive(true);
        clearBtw.gameObject.SetActive(false);

        currentPieceData = null;
        currentRootPath = "";
        currentjson = "";
    }

    public void CheckStrategicModeRules()
    {
        bool powerLimit = squadData.Power > 1500;
        bool hasKing = string.IsNullOrEmpty(squadData.King?.Name);

        bool uniqueKing = placedPieces.Count(p => p.Name == squadData.King.Name) > 1;
        bool sameArtPieces = squadData.Pieces.GroupBy(p => p.Sprite)
                            .Any(g => g.Count() > 1);//&& g.Select(p => p.Name).Distinct().Count() > 1)

        string powerLimitTxt = "Squad power must be less than 1500";
        string hasKingTxt = "There must be a King";
        string uniqueKingTxt = "The King's Piece must be unique";
        string sameArtPiecesTxt = "You cannot have different pieces with the same art.";

        enabledMode = !powerLimit && !hasKing && !uniqueKing && !sameArtPieces;

        string enabledTxt = "Strategic mode enabled";

        StringBuilder sb = new StringBuilder();

        if (!enabledMode)
        {
            rulesTmp.color = Color.white;

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

            SquadPieceData pieceData = squadData.Pieces.Find(p => p.NameInSquad == currentPieceName);

            currentPiecepower = pieceData.Power;

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

        int squadPower = CalculateSquadPower(placedPieces, squadData.Pieces);

        squadData.Power = squadPower;
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
            //existingPiece.Power = piece.Power;
            existingPiece.Position = piece.Position;
        }
        else
        {
            // adiciona uma nova peça
            placedPieces.Add(new UnitPieceData(piece.Name, piece.Position));
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


        foreach (SquadPieceData piecedata in squadData.Pieces)
        {
            if (piecedata.PromotionPieces.Remove(piece.Name))
                piecedata.Power -= 10;

            if (piecedata.CastlingPieces.Remove(piece.Name))
                piecedata.Power -= 10;
        }

        UpdateSquadPower();
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
            //existingPiece.Power = currentPiecepower;
            existingPiece.Position = pos;
        }
        else
        {
            // adiciona uma nova peça
            placedPieces.Add(new UnitPieceData(currentPieceName, pos));
        }

        int squadPower = CalculateSquadPower(placedPieces, squadData.Pieces);

        squadData.Power = squadPower;

        squadpowerTmp.text = $"Power: {squadPower}";
        squadgridpowerTmp.text = $"{squadPower}";

        UpdatePieceCountUI();

        if (kingCell != null)
            if (kingCell == cell)
                SetKing(cell, pos);

    }

    public void UpdateSquadPower()
    {
        int squadPower = CalculateSquadPower(placedPieces, squadData.Pieces);

        squadData.Power = squadPower;

        squadpowerTmp.text = $"Power: {squadPower}";
        squadgridpowerTmp.text = $"{squadPower}";
    }

    public int CalculateSquadPower(List<UnitPieceData> units, List<SquadPieceData> pieces)
    {
        if (units == null || pieces == null)
            return 0;

        int total = 0;

        foreach (var piece in pieces)
        {
            if (piece == null || string.IsNullOrEmpty(piece.NameInSquad))
                continue;

            foreach (var unit in units)
            {
                if (unit == null || string.IsNullOrEmpty(unit.Name))
                    continue;

                if (string.Equals(piece.NameInSquad, unit.Name, StringComparison.OrdinalIgnoreCase))
                {
                    total += piece.Power;
                }
            }
        }

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

            float margin = 0f; // margem em pixels

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

        int squadPower = CalculateSquadPower(placedPieces, squadData.Pieces);

        squadData.Power = squadPower;

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

                    SelectPiece(unitPiece.Name, pieceData, fileJson, pieceSprites[unitPiece.Name], rootPath, false);
                }
                else
                {
                    Debug.LogWarning($"Arquivo não encontrado: {fullPath}");
                }

            }
        }

    }



    public void SelectPiece(string namePieceSquad, SquadPieceData pieceData, string json, Sprite sprite, string rootPath, bool selected = true)
    {
        MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);
        //squadManager.spritePiece = sprite;

        if (config.piece.Power > 80)
            moreSpecialBtw.gameObject.SetActive(false);
        else
            moreSpecialBtw.gameObject.SetActive(true);

        clearBtw.gameObject.SetActive(false);

        if (config.piece.Name != currentPieceName || config.piece.Squad != squadPiece || editMode)
        {
            currentjson = json;
            currentRootPath = rootPath;
            currentPieceData = pieceData;

            SetInfoPiece(namePieceSquad, pieceData, config, sprite, selected);
            StartCoroutine(SetPromotionsAndCastelingPieces(currentPieceData, json, rootPath));

            editMode = false;
        }

    }




    public void SetInfoPiece(string namePieceSquad, SquadPieceData pieceData, MovementConfigData config, Sprite sprite, bool selected = true)
    {
        //MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        PieceInfo piece = config.piece;

        squadpanel.SetActive(false);
        piecepanel.SetActive(true);
        infoGridPanel.SetActive(true);

        currentPieceName = namePieceSquad;
        currentPiecepower = pieceData.Power;
        squadPiece = piece.Squad;

        nameTmp.text = currentPieceName;
        powerTmp.text = $"Power: {currentPiecepower}";

        spritePiece = sprite;
        previewImage.sprite = sprite;

        removePiece = false;

        if (selected)
            selectedPiece = true;

    }

    public IEnumerator SetPromotionsAndCastelingPieces(SquadPieceData pieceData, string json, string selectRootPath)
    {

        MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        casteling.SetActive(false);
        promotion.SetActive(false);

        foreach (Transform child in promotionContent.transform)
            Destroy(child.gameObject);

        foreach (Transform child in castelingContent.transform)
            Destroy(child.gameObject);

        if (pieceData.CastlingPieces != null)
        {
            if (pieceData.CastlingPieces.Count > 0)
                casteling.SetActive(true);

            foreach (string name in pieceData.CastlingPieces)
            {
                yield return StartCoroutine(LoadPiecesImage(name, castelingContent));
            }
        }

        if (pieceData.PromotionPieces != null)
        {
            if (pieceData.PromotionPieces.Count > 0)
                promotion.SetActive(true);

            foreach (string name in pieceData.PromotionPieces)
            {
                yield return StartCoroutine(LoadPiecesImage(name, promotionContent));
            }
        }

        yield return null;


        infoGridView.GenerateGridPiece(config);

        yield return null;

    }

    public IEnumerator LoadPiecesImage(string fileName, Transform content)
    {
        //Transform content = panel.transform;

        GameObject clone = Instantiate(viewPiecePrefab, content);

        // Define o nome do objeto (opcional)
        clone.name = "Preview_" + fileName;

        // Acha a imagem dentro do painel
        Image img = clone.GetComponentInChildren<Image>();

        Sprite sprite = null;

        if (pieceSprites.ContainsKey(fileName))
            sprite = pieceSprites[fileName];

        if (img != null)
        {
            img.sprite = sprite;
        }

        // Se quiser simular um carregamento assíncrono, pode colocar um yield
        yield return null;
    }


}
