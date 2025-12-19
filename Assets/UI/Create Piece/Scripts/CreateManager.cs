using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
public class CreateManager : MonoBehaviour
{

    public GameManager gameManager;

    [Header("Buttons:")]
    public Button menuBtw;
    public Button backBtn;

    [Header("Options Button:")]
    public Button presetsButton;
    public Button movesButton;
    public Button specialButton;
    public Button promotionButton;

    [Header("Options Panels:")]
    public GameObject presetsPanel;
    public GameObject movesPanel;
    public GameObject specialPanel;
    public GameObject promotionPanel;

    // Start is called before the first frame update
    void Start()
    {

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        

        backBtn.onClick.AddListener(() => gameManager.ChangeScene("Menu"));

        presetsButton.onClick.AddListener(() => SelectOptionsPanels(presetsPanel));
        movesButton.onClick.AddListener(() => SelectOptionsPanels(movesPanel));
        specialButton.onClick.AddListener(() => SelectOptionsPanels(specialPanel));
        promotionButton.onClick.AddListener(() => SelectOptionsPanels(promotionPanel));



    }


    private void SelectOptionsPanels(GameObject panel)
    {
        presetsPanel.SetActive(false);
        movesPanel.SetActive(false);
        specialPanel.SetActive(false);
        promotionPanel.SetActive(false);

        panel.SetActive(true);
    }



}
