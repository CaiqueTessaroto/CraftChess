using UnityEngine;
//using Newtonsoft.Json;

using UnityEngine.UI;
using System.Reflection;

using TMPro;

using System.IO;
using System.Collections;
using System.Collections.Generic;

using System;
//using UnityEngine.WSA;



[System.Serializable]
public class Presets
{

    [Header("Predefinidos:")]
    public bool King = false;
    public bool Queen = false;
    public bool Bishop = false;
    public bool Knight = false;
    public bool Rook = false;
    public bool Pawn = false;

}

[System.Serializable]
public class PieceWrapper
{
    public PieceInfo piece;
}

[System.Serializable]
public class MoveData
{
    public int x;
    public int y;
}


public class MovementCreation : MonoBehaviour
{
    public Image resultPreview;
    public TMP_Text powerPreview;
    public PieceInfo piece;
    public Presets presets;
    public Movement straight;
    public Movement diagonal;
    public PersonalizedMove custom;
    public Special special;

    [Header("Toggles:")]
    public GameObject presetsObject; // Objeto pai dos toggles (ex: PanelPresets)
    public GameObject straightObject; // O objeto que contém todos os Toggles
    public GameObject diagonalObject; // O objeto que contém todos os Toggles
    public GameObject customObject; // O objeto que contém todos os Toggles
    public GameObject specialObject; // O objeto que contém todos os Toggles
    public GameObject promotionObject; // O objeto que contém todos os Toggles

    [Header("Grid:")]
    public EditingGridManager customUIGridMove;
    public EditingGridManager specialUIGridMove;

    [Header("Script:")]
    public GridViewManager gridView; // Referência ao script que gerencia a UI do grid
    public NavigationManage_Create navigationManage;
    private bool isLoading = false;

    void Start()
    {

        if (navigationManage == null)
        {
            navigationManage = FindObjectOfType<NavigationManage_Create>();
        }

        GetToggles(straight, straightObject);
        GetDropdown(straight, straightObject);

        GetToggles(diagonal, diagonalObject);
        GetDropdown(diagonal, diagonalObject);

        GetTogglesCustom(custom);

        GetTogglesSpecial(special);

        GetPresetToggles(presets);

        powerPreview.text = UIHelperUtils.SetPowerText(0);
    }


    public bool AddPiece(string pieceName, List<string> targetList)
    {
        if (!targetList.Contains(pieceName))
        {
            targetList.Add(pieceName);
            Debug.Log($"Peça adicionada: {pieceName}");
            return true;
        }
        else
        {
            Debug.LogWarning($"A peça '{pieceName}' já existe na lista!");
            return false;
        }
    }

    public bool RemovePiece(string pieceName, List<string> targetList)
    {
        if (targetList.Contains(pieceName))
        {
            targetList.Remove(pieceName);
            Debug.Log($"Peça removida: {pieceName}");
            return true;
        }
        else
        {
            Debug.LogWarning($"A peça '{pieceName}' não existe na lista!");
            return false;
        }
    }


    public String CreateJson()
    {
        bool isAnyActive = straight.Active || diagonal.Active || special.Active || custom.Active;

        if (isAnyActive)
        {
            if (this.piece.Art != "" || true)
            {

                MovementConfigData config = new MovementConfigData
                {
                    piece = this.piece,
                    straight = this.straight,
                    diagonal = this.diagonal,
                    custom = this.custom,
                    special = this.special,
                };

                return JsonUtility.ToJson(config, true);

            }
            else
                Debug.Log("Arte de peça não selecionado.");
        }
        else
            Debug.Log("Nenhum movimento configurado.");

        return null;
    }


    public void SavePresetJson()
    {
        bool isAnyActive = straight.Active || diagonal.Active || special.Active || custom.Active;

        if (isAnyActive)
        {
            if (this.piece.Art != "" || true)
            {

                MovementConfigData config = new MovementConfigData
                {
                    piece = this.piece,
                    straight = this.straight,
                    diagonal = this.diagonal,
                    custom = this.custom,
                    special = this.special,
                };

                string json = JsonUtility.ToJson(config, true);

                //Bishop King Knight Pawn Queen Rook 
                string modelName = "Pawn";
                string directoryPath = Path.Combine(UnityEngine.Application.dataPath, "Resources/Presets");

                //string folderPath = "Assets/Resources/Models/Squads";   //StreamingAssets
                //string modelName = this.piece.Model;
                //string squadName = null;


                string fileName = modelName + ".json";

                //string directoryPath = Path.Combine(Application.dataPath, "StreamingAssets/Models/Squads", squadName, "Configs"); //Resources
                string filePath = Path.Combine(directoryPath, fileName);

                //if (!Directory.Exists(directoryPath))
                //    Directory.CreateDirectory(directoryPath);


                File.WriteAllText(filePath, json);
                Debug.Log("Configurações salvas em: " + filePath);
            }
            else
                Debug.Log("Modelo de peça não selecionado.");
        }
        else
            Debug.Log("Nenhum movimento configurado.");
    }

    //public IEnumerator LoadJson(string fileName){}

    public IEnumerator LoadJson(string filePath)
    {

        if (!File.Exists(filePath))
        {
            Debug.LogError("Arquivo JSON não encontrado em: " + filePath);
            yield break;
        }

        isLoading = true;

        //string resourcePath = Path.Combine("Presets", fileName);

        string json = File.ReadAllText(filePath);

        MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        if (config != null)
        {
            this.piece.Power = config.piece.Power;
            this.piece.NativeSprite = false;//config.piece.NativeSprite;

            this.straight = config.straight;
            this.diagonal = config.diagonal;
            this.special = config.special;
            this.custom = config.custom;

            // Atualiza a UI com base nos dados carregados
            //uISelectList.ClearCastlingAndPromotion();
            specialUIGridMove.DeselectAllCells();
            customUIGridMove.DeselectAllCells();
            gridView.ClearHighlights();

            // Gera os grids primeiro
            if (config.special?.Moves != null)
                specialUIGridMove.GenerateGrid();

            if (config.custom?.Moves != null)
                customUIGridMove.GenerateGrid();

            // Agora aplica os movimentos
            if (config.special?.Moves != null)
            {
                foreach (MoveData moveData in config.special.Moves)
                    specialUIGridMove.ToggleCellLoadSelection(new Vector2Int(moveData.x, moveData.y));
            }

            if (config.custom?.Moves != null)
            {
                foreach (MoveData moveData in config.custom.Moves)
                    customUIGridMove.ToggleCellLoadSelection(new Vector2Int(moveData.x, moveData.y));
            }

            // Destaca células
            gridView.HighlightValidMoves();

            ApplyDataToUI();

            //powerPreview.text = $"Power: {piece.Power}";
            powerPreview.text = UIHelperUtils.SetPowerText(piece.Power);

            isLoading = false;

            //Debug.Log("Configuração carregada de Resources: " + resourcePath);
        }
        else
        {
            Debug.LogError("Falha ao desserializar o JSON.");
        }
    }



    public IEnumerator LoadPresetFromJson(string fileName)
    {
        isLoading = true;

        string resourcePath = Path.Combine("Presets", fileName);

        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

        if (jsonAsset == null)
        {
            Debug.LogError("Arquivo de configuração não encontrado em Resources: " + resourcePath);
            yield break;
        }

        string json = jsonAsset.text;

        MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        if (config != null)
        {
            //this.piece.Power = config.piece.Power;
            this.straight = config.straight;
            this.diagonal = config.diagonal;

            this.special.Active = config.special.Active;
            this.special.Move = config.special.Move;
            this.special.Capture = config.special.Capture;
            this.special.Jump = config.special.Jump;
            this.special.Moves = config.special.Moves;

            //this.promotion = config.promotion;

            this.custom = config.custom;

            // Atualiza a UI com base nos dados carregados
            //uISelectList.ClearCastlingAndPromotion();
            specialUIGridMove.DeselectAllCells();
            customUIGridMove.DeselectAllCells();
            gridView.ClearHighlights();

            // Gera os grids primeiro
            if (config.special?.Moves != null)
                specialUIGridMove.GenerateGrid();

            if (config.custom?.Moves != null)
                customUIGridMove.GenerateGrid();

            // Agora aplica os movimentos
            if (config.special?.Moves != null)
            {
                foreach (MoveData moveData in config.special.Moves)
                    specialUIGridMove.ToggleCellLoadSelection(new Vector2Int(moveData.x, moveData.y));
            }

            if (config.custom?.Moves != null)
            {
                foreach (MoveData moveData in config.custom.Moves)
                    customUIGridMove.ToggleCellLoadSelection(new Vector2Int(moveData.x, moveData.y));
            }

            // Destaca células
            gridView.HighlightValidMoves();

            //createSelectionManager.ClearSelectPieces();

            ApplyDataToUI();

            //powerPreview.text = $"Power: {piece.Power}";
            powerPreview.text = UIHelperUtils.SetPowerText(piece.Power);

            isLoading = false;

            CalcularPoderTotal();

            //Debug.Log("Configuração carregada de Resources: " + resourcePath);
        }
        else
        {
            Debug.LogError("Falha ao desserializar o JSON.");
        }
    }


    void ApplyDataToUI()
    {
        GetToggles(straight, straightObject);
        GetDropdown(straight, straightObject);

        GetToggles(diagonal, diagonalObject);
        GetDropdown(diagonal, diagonalObject);

        GetTogglesCustom(custom);

        GetTogglesSpecial(special);

        // Aqui você pode atualizar sliders, campos, toggles individuais, etc.
        // Exemplo:
        // someToggle.isOn = straight.someBool;
        // someSlider.value = custom.someValue;
    }





    public List<Vector2Int> GetValidMoves(Vector2Int currentPosition)
    {

        List<Vector2Int> validMoves = new List<Vector2Int>();

        if (straight.Active)
            validMoves.AddRange(GetStraightMoves(currentPosition)); // Adiciona os movimentos retos

        if (diagonal.Active)
            validMoves.AddRange(GetDiagonalMoves(currentPosition)); // Adiciona os movimentos diagonais

        if (custom.Active)
            validMoves.AddRange(GetCustomMoves(currentPosition)); // Adiciona os movimentos retos

        return validMoves;

    }

    public List<Vector2Int> GetCustomMoves(Vector2Int currentPosition, bool isRecursive = false)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        List<Vector2Int> greenCells = customUIGridMove.GetGreenCells();

        List<Vector2Int> specialMoves = new List<Vector2Int>();
        if (special.Active && !isRecursive)
            specialMoves = GetspecialMoves(currentPosition, true); // evita recursão

        // soma posição atual às células verdes
        foreach (Vector2Int cell in greenCells)
        {
            validMoves.Add(new Vector2Int(cell.x + currentPosition.x, cell.y + currentPosition.y));
        }

        // filtra movimentos válidos
        validMoves = FilterGreenCells(validMoves, specialMoves, currentPosition, "custom");

        // cria lista relativa de células verdes convertida para MoveData
        List<MoveData> relativeGreenMoves = new List<MoveData>();
        foreach (Vector2Int cell in validMoves)
        {
            Vector2Int relative = new Vector2Int(cell.x - currentPosition.x, cell.y - currentPosition.y);
            if (greenCells.Contains(relative))
                relativeGreenMoves.Add(new MoveData { x = relative.x, y = relative.y });
        }

        // atribui ao JSON-friendly
        custom.Moves = relativeGreenMoves;

        return validMoves;
    }

    public List<Vector2Int> GetspecialMoves(Vector2Int currentPosition, bool isRecursive = false)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        List<Vector2Int> greenCells = specialUIGridMove.GetGreenCells();

        List<Vector2Int> customMoves = new List<Vector2Int>();
        if (custom.Active && !isRecursive)
            customMoves = GetCustomMoves(currentPosition, true); // evita recursão

        // soma posição atual às células verdes
        foreach (Vector2Int cell in greenCells)
        {
            validMoves.Add(new Vector2Int(cell.x + currentPosition.x, cell.y + currentPosition.y));
        }

        // filtra movimentos válidos
        validMoves = FilterGreenCells(validMoves, customMoves, currentPosition, "special");

        // cria lista relativa de células verdes
        List<MoveData> relativeGreenMoves = new List<MoveData>();
        foreach (Vector2Int cell in validMoves)
        {
            Vector2Int relative = new Vector2Int(cell.x - currentPosition.x, cell.y - currentPosition.y);
            if (greenCells.Contains(relative))
                relativeGreenMoves.Add(new MoveData { x = relative.x, y = relative.y });
        }

        // atribui ao JSON-friendly
        special.Moves = relativeGreenMoves;

        return validMoves;
    }

    public List<Vector2Int> FilterGreenCells(List<Vector2Int> greenCells, List<Vector2Int> personalizedMoves, Vector2Int currentPosition, String type)
    {
        List<Vector2Int> straightMoves = new List<Vector2Int>();
        List<Vector2Int> diagonalMoves = new List<Vector2Int>();
        List<Vector2Int> allCells = new List<Vector2Int>();

        //if (type == "special")
        //{
        //    if (!special.Move && !special.Capture && !special.Jump)
        //        special.Move = true;
        //}
        //else
        //{
        //    if (!custom.Move && !custom.Capture && !custom.Jump)
        //        custom.Move = true;
        //}

        if (straight.Active)
        {
            if (type == "special")
            {
                if (straight.Move == special.Move && straight.Capture == special.Capture && straight.Jump == special.Jump)
                {
                    straightMoves = GetStraightMoves(currentPosition);
                    allCells.AddRange(straightMoves);
                }
                else if (!special.Move && !special.Capture && !special.Jump)
                {
                    straightMoves = GetStraightMoves(currentPosition);
                    allCells.AddRange(straightMoves);
                }
            }
            else
            {
                if (straight.Move == custom.Move && straight.Capture == custom.Capture && straight.Jump == custom.Jump)
                {
                    straightMoves = GetStraightMoves(currentPosition);
                    allCells.AddRange(straightMoves);
                }
                else if (!custom.Move && !custom.Capture && !custom.Jump)
                {
                    straightMoves = GetStraightMoves(currentPosition);
                    allCells.AddRange(straightMoves);
                }
            }
        }

        if (diagonal.Active)
        {
            if (type == "special")
            {
                if (diagonal.Move == special.Move && diagonal.Capture == special.Capture && diagonal.Jump == special.Jump)
                {
                    diagonalMoves = GetDiagonalMoves(currentPosition);
                    allCells.AddRange(diagonalMoves);
                }
                else if (!special.Move && !special.Capture && !special.Jump)
                {
                    diagonalMoves = GetStraightMoves(currentPosition);
                    allCells.AddRange(diagonalMoves);
                }
            }
            else
            {
                if (diagonal.Move == custom.Move && diagonal.Capture == custom.Capture && diagonal.Jump == custom.Jump)
                {
                    diagonalMoves = GetDiagonalMoves(currentPosition);
                    allCells.AddRange(diagonalMoves);
                }
                else if (!custom.Move && !custom.Capture && !custom.Jump)
                {
                    diagonalMoves = GetStraightMoves(currentPosition);
                    allCells.AddRange(diagonalMoves);
                }
            }
        }

        if (personalizedMoves != null)
        {
            if (type == "special")
            {
                if (custom.Move == special.Move && custom.Capture == special.Capture && custom.Jump == special.Jump)
                {
                    allCells.AddRange(personalizedMoves);
                }
                else if (!special.Move && !special.Capture && !special.Jump)
                {
                    allCells.AddRange(personalizedMoves);
                }
            }
            else
            {
                if (!custom.Move && !custom.Capture && !custom.Jump)
                {
                    allCells.AddRange(personalizedMoves);
                }

            }
        }


        List<Vector2Int> filteredGreenCells = new List<Vector2Int>();

        foreach (var cell in greenCells)
        {
            if (!allCells.Contains(cell) && !filteredGreenCells.Contains(cell))
            {
                filteredGreenCells.Add(cell);
            }
        }

        return filteredGreenCells;
    }


    public List<Vector2Int> GetStraightMoves(Vector2Int currentPosition)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        int[] directionsX = { 0, 1, -1 }; // Movimentos horizontais
        int[] directionsY = { 0, 1, -1 }; // Movimentos verticais

        for (int i = 1; i <= straight.Range; i++)
        {
            foreach (int dx in directionsX)
            {
                foreach (int dy in directionsY)
                {
                    if (dx == 0 && dy == 0) continue; // Ignora a posição atual

                    bool isStraight = dx == 0 || dy == 0;
                    //bool isDiagonal = Mathf.Abs(dx) == Mathf.Abs(dy);

                    if (isStraight)
                    {
                        Vector2Int newPos = new Vector2Int(currentPosition.x + dx * i, currentPosition.y + dy * i);

                        if (!straight.All)
                        {
                            // Para frente (Y+)
                            if (dy > 0 && !straight.Front) continue;
                            // Para trás (Y-)
                            if (dy < 0 && !straight.Back) continue;
                            // Para direita (X+)
                            if (dx > 0 && !straight.Right) continue;
                            // Para esquerda (X-)
                            if (dx < 0 && !straight.Left) continue;
                        }

                        validMoves.Add(newPos);
                    }
                }
            }
        }

        return validMoves;
    }

    public List<Vector2Int> GetDiagonalMoves(Vector2Int currentPosition)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        int[] directionsX = { 0, 1, -1 }; // Movimentos horizontais
        int[] directionsY = { 0, 1, -1 }; // Movimentos verticais

        for (int i = 1; i <= diagonal.Range; i++)
        {
            foreach (int dx in directionsX)
            {
                foreach (int dy in directionsY)
                {
                    if (dx == 0 && dy == 0) continue; // Ignora a posição atual

                    //bool isStraight = dx == 0 || dy == 0;
                    bool isDiagonal = Mathf.Abs(dx) == Mathf.Abs(dy);

                    if (isDiagonal)
                    {
                        Vector2Int newPos = new Vector2Int(currentPosition.x + dx * i, currentPosition.y + dy * i);

                        // Filtrando direções específicas
                        if (!diagonal.All)
                        {

                            if (diagonal.Front && !diagonal.Right && !diagonal.Left)
                            {
                                if (dy < 0 && !diagonal.Back) continue;
                            }
                            else if (diagonal.Back && !diagonal.Right && !diagonal.Left)
                            {
                                if (dy > 0 && !diagonal.Front) continue;
                            }
                            else if (diagonal.Right && !diagonal.Front && !diagonal.Back)
                            {
                                if (dx < 0 && !diagonal.Left) continue;
                            }
                            else if (diagonal.Left && !diagonal.Front && !diagonal.Back)
                            {
                                if (dx > 0 && !diagonal.Right) continue;
                            }
                            else
                            {
                                // Para frente (Y+)
                                if (dy > 0 && !diagonal.Front) continue;
                                // Para trás (Y-)
                                if (dy < 0 && !diagonal.Back) continue;
                                // Para direita (X+)
                                if (dx > 0 && !diagonal.Right) continue;
                                // Para esquerda (X-)
                                if (dx < 0 && !diagonal.Left) continue;
                            }

                        }

                        validMoves.Add(newPos);
                    }
                }
            }
        }

        return validMoves;
    }


    void GetPresetToggles(Presets presets)
    {
        Toggle[] toggles = presetsObject.GetComponentsInChildren<Toggle>(true);



        foreach (Toggle toggle in toggles)
        {
            //Debug.Log(toggle.gameObject.name);

            FieldInfo field = typeof(Presets).GetField(toggle.gameObject.name, BindingFlags.Public | BindingFlags.Instance);

            if (field == null)
            {
                //Debug.LogWarning($"Nenhum campo correspondente encontrado para: {toggle.gameObject.name}");
                return;
            }

            if (field != null && field.FieldType == typeof(bool))
            {
                toggle.isOn = (bool)field.GetValue(presets);

                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        // Desmarca todos os outros
                        ResetAllPresets(toggle);
                        field.SetValue(presets, true);
                        toggle.isOn = true;

                        //if (!string.IsNullOrEmpty(toggle.gameObject.name))
                        StartCoroutine(LoadPresetFromJson(toggle.gameObject.name));
                        //ApplyPreset(toggle.gameObject.name);
                    }
                });
            }
            else
            {
                Debug.LogWarning("Campo de preset não encontrado para: " + toggle.gameObject.name);
            }
        }
    }



    void ResetAllPresets(Toggle selectedToggle)
    {
        presets.King = false;
        presets.Queen = false;
        presets.Bishop = false;
        presets.Knight = false;
        presets.Rook = false;
        presets.Pawn = false;

        // Atualiza os toggles visualmente
        Toggle[] toggles = presetsObject.GetComponentsInChildren<Toggle>(true);
        foreach (Toggle toggle in toggles)
        {
            if (toggle != selectedToggle)
                toggle.isOn = false;
        }
    }

    void GetTogglesSpecial(Special movement)
    {

        Toggle[] toggles = specialObject.GetComponentsInChildren<Toggle>(true); // Inclui objetos inativos

        foreach (Toggle toggle in toggles)
        {
            //Debug.Log("Toggle encontrado: " + toggle.gameObject.name);

            // Procura um campo na classe Movement com o mesmo nome do Toggle
            FieldInfo field = typeof(Special).GetField(toggle.gameObject.name, BindingFlags.Public | BindingFlags.Instance);

            if (field != null && field.FieldType == typeof(bool))
            {
                // Atualiza o Toggle com o valor atual da configuração
                toggle.isOn = (bool)field.GetValue(movement);

                // Adiciona um listener para atualizar a classe Movement e chamar a atualização da UI
                toggle.onValueChanged.AddListener((value) =>
                {
                    field.SetValue(movement, value);
                    //Debug.Log(toggle.gameObject.name + " atualizado para: " + value);
                    gridView.HighlightValidMoves(); // Atualiza os movimentos válidos na UI
                    CalcularPoderTotal();
                });
            }
            else
            {
                Debug.LogWarning("Nenhum campo correspondente encontrado para: " + toggle.gameObject.name);
            }
        }
    }


    void GetTogglesCustom(PersonalizedMove movement)
    {

        Toggle[] toggles = customObject.GetComponentsInChildren<Toggle>(true); // Inclui objetos inativos

        foreach (Toggle toggle in toggles)
        {
            //Debug.Log("Toggle encontrado: " + toggle.gameObject.name);

            // Procura um campo na classe Movement com o mesmo nome do Toggle
            FieldInfo field = typeof(PersonalizedMove).GetField(toggle.gameObject.name, BindingFlags.Public | BindingFlags.Instance);

            if (field != null && field.FieldType == typeof(bool))
            {
                // Atualiza o Toggle com o valor atual da configuração
                toggle.isOn = (bool)field.GetValue(movement);

                // Adiciona um listener para atualizar a classe Movement e chamar a atualização da UI
                toggle.onValueChanged.AddListener((value) =>
                {
                    field.SetValue(movement, value);
                    //Debug.Log(toggle.gameObject.name + " atualizado para: " + value);
                    gridView.HighlightValidMoves(); // Atualiza os movimentos válidos na UI
                    CalcularPoderTotal();
                });
            }
            else
            {
                Debug.LogWarning("Nenhum campo correspondente encontrado para: " + toggle.gameObject.name);
            }
        }
    }




    void GetToggles(Movement movement, GameObject movesObject)
    {

        Toggle[] toggles = movesObject.GetComponentsInChildren<Toggle>(true); // Inclui objetos inativos

        foreach (Toggle toggle in toggles)
        {
            //Debug.Log("Toggle encontrado: " + toggle.gameObject.name);

            // Procura um campo na classe Movement com o mesmo nome do Toggle
            FieldInfo field = typeof(Movement).GetField(toggle.gameObject.name, BindingFlags.Public | BindingFlags.Instance);

            if (field != null && field.FieldType == typeof(bool))
            {
                // Atualiza o Toggle com o valor atual da configuração
                toggle.isOn = (bool)field.GetValue(movement);

                // Adiciona um listener para atualizar a classe Movement e chamar a atualização da UI
                toggle.onValueChanged.AddListener((value) =>
                {
                    field.SetValue(movement, value);
                    //Debug.Log(toggle.gameObject.name + " atualizado para: " + value);
                    gridView.HighlightValidMoves(); // Atualiza os movimentos válidos na UI
                    CalcularPoderTotal();
                });
            }
            else
            {
                //    Debug.LogWarning("Nenhum campo correspondente encontrado para: " + toggle.gameObject.name);
            }
        }
    }

    void GetDropdown(Movement movement, GameObject movesObject)
    {
        // Encontra o GameObject "Range" dentro de movesObject
        Transform columnObject = movesObject.transform.Find("Column (1)");
        Transform rangeObject = columnObject.Find("Range");

        if (rangeObject == null)
        {
            Debug.LogError("GameObject 'Range' não encontrado dentro de movesObject!");
            return;
        }

        // Obtém o TMP_Dropdown dentro do GameObject "Range"
        TMP_Dropdown rangeDropdown = rangeObject.GetComponentInChildren<TMP_Dropdown>(true);

        if (rangeDropdown == null)
        {
            Debug.LogError("TMP_Dropdown não encontrado dentro de 'Range'!");
            return;
        }

        // Obtém o campo da classe Movement baseado no nome do GameObject
        FieldInfo field = typeof(Movement).GetField(rangeObject.gameObject.name, BindingFlags.Public | BindingFlags.Instance);

        if (field == null)
        {
            Debug.LogError($"Campo '{rangeObject.gameObject.name}' não encontrado na classe Movement!");
            return;
        }

        // Pega o valor atual da classe Movement
        object value = field.GetValue(movement);

        // Define o valor inicial do dropdown
        if (value != null)
        {
            int intValue = (int)value;

            // Procura a opção cujo texto corresponde ao valor
            int optionIndex = rangeDropdown.options.FindIndex(opt => opt.text == intValue.ToString());

            if (optionIndex >= 0)
                rangeDropdown.value = optionIndex; // Seleciona a opção encontrada
            else
                rangeDropdown.value = 0; // fallback se não encontrou
        }
        else
        {
            rangeDropdown.value = 0; // padrão se não houver valor
        }

        // Listener para capturar alterações no TMP_Dropdown
        rangeDropdown.onValueChanged.AddListener((selectedIndex) =>
        {
            string selectedText = rangeDropdown.options[selectedIndex].text;

            if (int.TryParse(selectedText, out int intValue))
            {
                field.SetValue(movement, intValue);
                gridView.HighlightValidMoves(); // Atualiza os movimentos válidos na UI
                CalcularPoderTotal();
            }
            else
            {
                Debug.LogWarning($"Valor inválido para '{rangeObject.gameObject.name}': {selectedText}");
            }
        });
    }



    public void CalcularPoderTotal()
    {
        if (isLoading) return;

        int poderTotal = 0;
        int poder = 0;

        // Função local para calcular com base nos flags
        int CalcularPoder(bool move, bool capture, bool jump, int range)
        {
            int poder = 0;
            if (move) poder += 2 * range;
            if (capture) poder += 3 * range;
            if (jump) poder += 2 * range;
            return poder;
        }

        // Movimento em linha reta
        if (straight != null && straight.Active)
        {
            int rangePorDirecao = GetAccumulatedPowerByDirection(straight);
            poder = CalcularPoder(straight.Move, straight.Capture, straight.Jump, rangePorDirecao);
            poderTotal = poderTotal + poder;
        }

        // Movimento diagonal
        if (diagonal != null && diagonal.Active)
        {
            int rangePorDirecao = GetAccumulatedPowerByDirection(diagonal);
            poder = CalcularPoder(diagonal.Move, diagonal.Capture, diagonal.Jump, rangePorDirecao);
            poder = Mathf.CeilToInt(poder * 0.5f);
            poderTotal = poderTotal + poder;
        }

        if (straight.Active && diagonal.Active)
        {
            if (straight.All && diagonal.All)
                poderTotal = poderTotal + 30;
            else if (straight.Front && straight.Back && diagonal.Front && diagonal.Back)
            {
                poderTotal = poderTotal + 20;
            }
            else if (straight.Front && straight.Back && straight.Left && straight.Right && diagonal.Front && diagonal.Back && straight.Left && straight.Right)
            {
                poderTotal = poderTotal + 30;
            }

        }

        // Movimento customizado
        if (custom != null && custom.Active)
        {
            poder = CalcularPoder(custom.Move, custom.Capture, custom.Jump, custom.Moves.Count);
            poderTotal = poderTotal + poder;

            if (custom.Moves.Count >= 4)
            {
                poderTotal = poderTotal + 20;
            }

            if (custom.Moves.Count > 8)
                poderTotal = poderTotal + poder;
        }

        // Movimento especial
        if (special != null)
        {
            if (special.Active)
            {
                poder = CalcularPoder(special.Move, special.Capture, special.Jump, special.Moves.Count);
                poderTotal = poderTotal + poder;

                if (special.Moves.Count > 8)
                {
                    poderTotal = poderTotal + poder;
                }

            }
        }

        bool bloqueavel = Canbeblock();

        //Debug.Log("bloqueavel:" + bloqueavel);

        if (bloqueavel)
        {
            poderTotal = Mathf.CeilToInt(poderTotal * 0.7f);
            poderTotal = poderTotal + 1;
        }
        else
        {
            poderTotal = poderTotal + 20;
        }




        // Atribui o poder à peça
        piece.Power = poderTotal;
        //powerPreview.text = $"Power: {piece.Power}";
        powerPreview.text = UIHelperUtils.SetPowerText(piece.Power);
    }

    private int GetAccumulatedPowerByDirection(Movement movement)
    {
        if (movement == null || !movement.Active)
            return 0;

        int poderPorDirecao = movement.Range;
        int direcoes = 0;

        if (movement.All)
        {
            direcoes = movement.Range * 4; // Front, Back, Left, Right
        }
        else
        {
            if (movement.Front) direcoes += poderPorDirecao;
            if (movement.Back) direcoes += poderPorDirecao;
            if (movement.Left) direcoes += poderPorDirecao;
            if (movement.Right) direcoes += poderPorDirecao;
        }

        if (direcoes == 0) return 0;

        return direcoes;
    }


    private bool Canbeblock()
    {

        // Verifica straight
        if (straight != null && straight.Active)
            if (straight.Range != 0)
                if (straight.Jump || (straight.Move == straight.Capture))
                    return false;

        // Verifica diagonal
        if (diagonal != null && diagonal.Active)
            if (diagonal.Range != 0)
                if (diagonal.Jump || (diagonal.Move == diagonal.Capture))
                    return false;

        // Verifica custom
        if (custom != null && custom.Active)
            if (custom.Moves.Count != 0)
                if (custom.Jump || custom.Capture)
                    return false;

        // Verifica special
        if (special != null && special.Active)
            if (special.Moves.Count != 0)
                if (special.Jump || special.Capture)
                    return false;

        return true;
    }




}





