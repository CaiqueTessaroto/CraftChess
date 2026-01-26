using UnityEngine;
using UnityEngine.UI;

public class CreditsManager : MonoBehaviour
{

    public Image creditsImage;
    public Sprite ptCredits;
    public Sprite egCredits;

    //public Button openCredits;
    public Button returnMenu;


    public GameObject creditsPanel;
    // Start is called before the first frame update
    void Start()
    {

        //openCredits.onClick.AddListener(() =>
        //{
        //});

        returnMenu.onClick.AddListener(() =>
        {
            creditsPanel.SetActive(false);
        });

        //SettingsManager.Instance.Settings.language

    }

    public void ShowCredits()
    {
        creditsPanel.SetActive(true);

        if (SettingsManager.Instance != null)
            if (SettingsManager.Instance.Settings.language != Language.PortugueseBR)
            {
                creditsImage.sprite = egCredits;
                return;
            }

        creditsImage.sprite = ptCredits;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
