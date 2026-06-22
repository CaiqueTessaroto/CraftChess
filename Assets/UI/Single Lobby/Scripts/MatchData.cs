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

    [Header("Multiplayer")]
    public bool isMultiplayer = false;
    public bool HostIsWhite = false;
    public Sprite HostProfileSprite;
    public Sprite ClientProfileSprite;

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

    public void Reset()
    {
        blackSquadName = string.Empty;
        whiteSquadName = string.Empty;
        mapName = string.Empty;
        whoStarts = default;
        botDifficulty = default;

        noRules = false;
        noTurns = false;
        localGame = false;
        autoSwitchSide = true;
        IAvsIA = false;

        Squads.Clear();

        isMultiplayer = false;
        HostIsWhite = false;
        HostProfileSprite = null;
        ClientProfileSprite = null;
    }

}
