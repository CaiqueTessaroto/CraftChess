using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class SingleMatchConfig
{
    public string UserSquadName;
    public string BotSquadName;
    public BotDifficulty BotDifficulty;
    public string MapName;
    public StartOption StartOption;
    public bool noRules = false;
    public bool noTurns = false;
    public bool localGame = false;
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

    [Header("Buttons")]
    public Button Back;

    void Start()
    {


        Back.onClick.AddListener(() => gameManager.ChangeScene("Menu"));

    }

    public void StartMatch(SingleMatchConfig currentMatch, List<MatchSquadData> Squads)
    {
        // preenche os dados da partida
        MatchData.Instance.userSquadName = currentMatch.UserSquadName;
        MatchData.Instance.botSquadName = currentMatch.BotSquadName;
        MatchData.Instance.mapName = currentMatch.MapName;
        MatchData.Instance.whoStarts = currentMatch.StartOption;
        MatchData.Instance.botDifficulty = currentMatch.BotDifficulty;

        MatchData.Instance.noRules = currentMatch.noRules;
        MatchData.Instance.localGame = currentMatch.localGame;
        MatchData.Instance.noTurns = currentMatch.noTurns;

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
    public static SingleMatchConfig CreateMatch(string userSquad, string botSquad, BotDifficulty difficulty, string map, StartOption start)
    {
        SingleMatchConfig currentMatch = new SingleMatchConfig
        {
            UserSquadName = userSquad,
            BotSquadName = botSquad,
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
