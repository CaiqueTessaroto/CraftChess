using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class SingleMatchConfig
{
    public string BlackSquadName;
    public string WhiteSquadName;
    public BotDifficulty BotDifficulty;
    public string MapName;
    public StartOption StartOption;
    public bool noRules = false;
    public bool noTurns = false;
    public bool localGame = false;
    public bool switchSide = true;
    public bool IAvsIA = false;
    public bool options = true;
}

public enum BotDifficulty
{
    Easy,
    Medium,
    Hard
}

public enum StartOption
{
    Black,
    White,
    Random
}

public class SingleLobbyManager : MonoBehaviour
{
    public GameManager gameManager;
    public InteractiveLobby interactiveLobby;

    [Header("Buttons")]
    public Button Back;

    void Start()
    {

        if (interactiveLobby == null)
            interactiveLobby = FindObjectOfType<InteractiveLobby>();


        Back.onClick.AddListener(() =>
        {
            SaveMatchConfig(interactiveLobby.currentMatch);
            gameManager.ChangeScene("Menu");
        });

    }

    public void StartMatch(SingleMatchConfig currentMatch, List<MatchSquadData> Squads)
    {
        // preenche os dados da partida
        MatchData.Instance.autoSwitchSide = currentMatch.switchSide;

        MatchData.Instance.blackSquadName = currentMatch.BlackSquadName;
        MatchData.Instance.whiteSquadName = currentMatch.WhiteSquadName;
        MatchData.Instance.mapName = currentMatch.MapName;
        MatchData.Instance.whoStarts = currentMatch.StartOption;
        MatchData.Instance.botDifficulty = currentMatch.BotDifficulty;

        MatchData.Instance.noRules = currentMatch.noRules;
        MatchData.Instance.localGame = currentMatch.localGame;
        MatchData.Instance.noTurns = currentMatch.noTurns;

        MatchData.Instance.IAvsIA = currentMatch.IAvsIA;

        MatchData.Instance.Squads = Squads;

        //MatchData.Instance.Squad = Squad;
        // MatchData.Instance.BotSquad = BotSquad;

        // carrega a próxima cena
        gameManager.ChangeScene("Singleplayer");
    }


    public void SaveMatchConfig(SingleMatchConfig currentMatch)
    {
        MatchConfigManager.Save(currentMatch);
    }

    public static SingleMatchConfig GetMatchConfig()
    {
        SingleMatchConfig currentMatch = MatchConfigManager.Load();

        return currentMatch;
    }
    public static SingleMatchConfig CreateMatch(string userSquad, string botSquad, bool nativeSquad, BotDifficulty difficulty, string map, StartOption start)
    {
        SingleMatchConfig currentMatch = new SingleMatchConfig
        {
            BlackSquadName = userSquad,
            WhiteSquadName = botSquad,
            BotDifficulty = difficulty,
            MapName = map,
            StartOption = start
        };

        return currentMatch;
    }



    public static class MatchConfigManager
    {
        private static string filePath = Application.persistentDataPath + "/User/singleMatch.json";

        // Salvar para JSON
        public static void Save(SingleMatchConfig config)
        {
            string json = JsonUtility.ToJson(config, true); // true = formatado

            string path = Application.persistentDataPath + "/User";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            File.WriteAllText(filePath, json);
            //Debug.Log("Config salva em: " + filePath);
        }

        // Carregar do JSON
        public static SingleMatchConfig Load()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                SingleMatchConfig config = JsonUtility.FromJson<SingleMatchConfig>(json);
                //Debug.Log("Config carregada: " + json);
                return config;
            }
            else
            {
                Debug.LogWarning("Nenhum arquivo de configuração encontrado.");
                return null;
            }
        }

        // Excluir config salva
        public static void Delete()
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                //Debug.Log("Config deletada.");
            }
        }
    }


}
