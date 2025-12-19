using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuButtonsManager : MonoBehaviour
{
    public GameManager gameManager;

    [Header("Main:")]
    public Button playBtn;
    public Button createBtn;
    public Button inventoryBtn;
    public Button exitBtn;

    [Header("Play:")]
    public Button singleBtn;
    public Button multiplayerBtn;
    public GameObject playPanel;

    [Header("Create:")]
    public Button createSquadBtn;
    public Button paintingEditorBtn;
    public Button createPieceBtn;
    public GameObject CreationPanel;


    void Start()
    {

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        playBtn.onClick.AddListener(() =>
        {
            if (playPanel.activeSelf)
                playPanel.SetActive(false);
            else
                SwitchPainelTo(playPanel);
        });


        createBtn.onClick.AddListener(() =>
        {
            if (CreationPanel.activeSelf)
                CreationPanel.SetActive(false);
            else
                SwitchPainelTo(CreationPanel);
        });

        paintingEditorBtn.onClick.AddListener(() => gameManager.ChangeScene("Painting Editor"));

        createPieceBtn.onClick.AddListener(() => gameManager.ChangeScene("Create Piece"));

        createSquadBtn.onClick.AddListener(() => gameManager.ChangeScene("Create Squad"));

        singleBtn.onClick.AddListener(() => gameManager.ChangeScene("Single Lobby"));

        exitBtn.onClick.AddListener(() => QuitGame());

    }

    public void SwitchPainelTo(GameObject painel)
    {
        CreationPanel.SetActive(false);
        playPanel.SetActive(false);


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
