using System.Collections.Generic;
using UnityEngine;

public class MatchData : MonoBehaviour
{
    public static MatchData Instance;

    [Header("Lobby")]
    public string blackSquadName;
    public string whiteSquadName;
    public string mapName;
    public StartOption whoStarts; // "User", "Bot" ou "Random"
    public BotDifficulty botDifficulty;

    [Header("Options")]
    public bool noRules = false;
    public bool noTurns = false;
    public bool localGame = false;
    public bool autoSwitchSide = true;
    public bool IAvsIA = false;

    [Header("Data")]
    public List<MatchSquadData> Squads = new List<MatchSquadData>();
    //public MatchSquadData Squad = new MatchSquadData();
    //public MatchSquadData BotSquad = new MatchSquadData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // mantém entre as cenas
        }
        else
        {
            Destroy(gameObject); // impede duplicatas
        }
    }

    void OnDestroy()
    {
        // 🔒 Garante limpeza da instância
        if (Instance == this)
            Instance = null;
    }

}
