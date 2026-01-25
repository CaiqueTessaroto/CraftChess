using UnityEngine;
using UnityEngine.UI;

public class MenuButtonsManager : MonoBehaviour
{
    public GameManager gameManager;
    public RewardFeed rewardFeed;

    public Button ButtonRewards;
    public GameObject RewardsPanel;

    [Header("Main:")]
    public Button playBtn;
    public Button createBtn;
    public Button inventoryBtn;
    public Button settingsBtn;
    public Button exitBtn;
    public GameObject settingsPanel;

    [Header("Play:")]
    public Button playIA;
    public Button local;
    public Button simulation;
    public GameObject playPanel;

    [Header("Create:")]
    public Button createSquadBtn;
    public Button paintingEditorBtn;
    public Button createPieceBtn;
    public GameObject CreationPanel;


    void Start()
    {

        //testBtn.onClick.AddListener(() => AdsManager.ShowRewarded());
        //testBtn2.onClick.AddListener(() => AdsManager.ShowInterstitial());

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (rewardFeed == null)
        {
            rewardFeed = FindObjectOfType<RewardFeed>();
        }

        bool allunlock = RewardManager.Instance.AllRewardsUnlocked();

        if (allunlock){
            RewardsPanel.SetActive(false);
            rewardFeed.rewardBtn.interactable = false;
        }

        ButtonRewards.onClick.AddListener(() =>
        {
            RewardsPanel.SetActive(!RewardsPanel.activeSelf);
        });

        //playBtn.onClick.AddListener(() => gameManager.ChangeScene("Single Lobby"));

        playBtn.onClick.AddListener(() =>
        {
            if (playPanel.activeSelf)
                playPanel.SetActive(false);
            else
                SwitchPainelTo(playPanel);
        });

        playIA.onClick.AddListener(() =>
        {
            GameModeManager.SelectedMode = GameMode.PlayerVsAI;
            gameManager.ChangeScene("Single Lobby");
        });
        local.onClick.AddListener(() =>
        {
            GameModeManager.SelectedMode = GameMode.PlayerVsPlayerLocal;
            gameManager.ChangeScene("Single Lobby");

        });
        simulation.onClick.AddListener(() =>
        {
            GameModeManager.SelectedMode = GameMode.AIVsAI;
            gameManager.ChangeScene("Single Lobby");

        });


        createBtn.onClick.AddListener(() =>
        {
            if (CreationPanel.activeSelf)
                CreationPanel.SetActive(false);
            else
                SwitchPainelTo(CreationPanel);
        });

        settingsBtn.onClick.AddListener(() =>
        {
            if (settingsPanel.activeSelf)
                settingsPanel.SetActive(false);
            else
                SwitchPainelTo(settingsPanel);
        });

        paintingEditorBtn.onClick.AddListener(() => gameManager.ChangeScene("Painting Editor"));

        createPieceBtn.onClick.AddListener(() => gameManager.ChangeScene("Create Piece"));

        createSquadBtn.onClick.AddListener(() => gameManager.ChangeScene("Create Squad"));

        exitBtn.onClick.AddListener(() => QuitGame());

    }

    public void SwitchPainelTo(GameObject painel)
    {
        CreationPanel.SetActive(false);
        playPanel.SetActive(false);
        settingsPanel.SetActive(false);


        painel.SetActive(true);
    }







    public void QuitGame()
    {
        // Salva dados se necessário (ex: PlayerPrefs.Save())
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Fecha no Editor
#else
                            Application.Quit(); // Fecha na build executável
#endif
    }


}
