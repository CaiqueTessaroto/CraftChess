using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameInterfaceManager : MonoBehaviour
{
    public BoardChessManager boardChessManager;
    public Button MenuBtn;
    public Button switchSide;

    [Header("Panel")]
    public GameObject panel;
    public TMP_Text tmpEnd;
    public Button ContinueBtn;
    // Start is called before the first frame update
    void Start()
    {

        if (boardChessManager == null)
            boardChessManager = FindObjectOfType<BoardChessManager>();

        switchSide.onClick.AddListener(() =>
        {
            boardChessManager.SwitchSide();
            boardChessManager.UpdateBoardControl();
        });

        MenuBtn.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Menu");
        });


        ContinueBtn.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Single Lobby");
        });

    }

    public void EndGame(string result)
    {
        tmpEnd.text = result;
        panel.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
