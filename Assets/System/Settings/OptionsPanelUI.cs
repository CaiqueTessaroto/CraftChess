using UnityEngine;
using UnityEngine.UI;

public class OptionsPanelUI : MonoBehaviour
{
    [Header("Painel")]
    public Button close;
    public Button restart;

    public Button discord;


    void Start()
    {

        close.onClick.AddListener(() =>
        {
            CloseSettings();
        });

        restart.onClick.AddListener(() =>
        {
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
