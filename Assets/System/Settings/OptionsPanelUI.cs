using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System;

public class OptionsPanelUI : MonoBehaviour
{
    [Header("Painel")]
    public Button close;
    public Button restart;

    public Button discord;


    DateTime dataLimite = new DateTime(2026, 2, 28);
    DateTime dataAtual = DateTime.Now;

    void Start()
    {

        close.onClick.AddListener(() =>
        {
            CloseSettings();
        });

        restart.onClick.AddListener(() =>
        {

            if (dataAtual <= dataLimite)
                if (Existskey())
                {
                    PlayerPrefs.SetInt("noAd", 1);
                    PlayerPrefs.Save();
                }

            //Debug.Log("Existskey: " + Existskey());
            //Debug.Log("noAd: " + AdsManager.Instance.NoAdsEnabled);

            ResetTutorial();

        });

        discord.onClick.AddListener(() =>
        {
            string url = "https://discord.gg/MBSXMyCuRG";

            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }

        });



    }

    string[] folderskeys = { "noAdkey",
    "gabriellinhasapequinha",
    "tikudopautorto",
    "jonatassafadinho",
    "teteudelas",
    "renanmagicplays",
    "davidsanjidelas",
    "paranhaeszinhosapeca",
    "hamaueotaka",
    "jeanhunter"
      };

    bool Existskey()
    {
        return folderskeys.Any(folder =>
            Directory.Exists(Path.Combine(Application.persistentDataPath, "Sprites"))
        );
    }

    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialSeen");
        PlayerPrefs.DeleteKey("TutorialSeenMenu");
        PlayerPrefs.DeleteKey("TutorialSeenPainting");
        PlayerPrefs.DeleteKey("TutorialSeenPiece");
        PlayerPrefs.DeleteKey("TutorialSeenSquad");
        PlayerPrefs.DeleteKey("TutorialSeenLobby");
        PlayerPrefs.Save();

        ShowTutorialBody();
    }

    public void CloseSettings()
    {
        SettingsManager.Instance.SendMessage("ToggleSettingsPanel");
    }

    public void ShowTutorialBody()
    {
        GameObject tutorialPanel = GameObject.Find("TutorialPanel");

        if (tutorialPanel == null)
        {
            Debug.LogWarning("TutorialPanel não encontrado");
            return;
        }

        Transform body = tutorialPanel.transform.Find("Body");

        if (body == null)
        {
            Debug.LogWarning("Filho 'Body' não encontrado em TutorialPanel");
            return;
        }

        body.gameObject.SetActive(true);
    }

}
