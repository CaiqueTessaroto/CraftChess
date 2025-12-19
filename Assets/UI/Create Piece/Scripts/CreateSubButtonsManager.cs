using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreateSubButtonsManager : MonoBehaviour
{


    [Header("Sub Options Button:")]
    public Button straightButton;
    public Button diagonalButton;
    public Button customButton;

    [Header("Sub Options Panels:")]
    public GameObject straightPanel;
    public GameObject diagonalPanel;
    public GameObject customPanel;

    // Start is called before the first frame update
    void Start()
    {
        straightButton.onClick.AddListener(() => SelectSubOptionsPanels(straightPanel));
        diagonalButton.onClick.AddListener(() => SelectSubOptionsPanels(diagonalPanel));
        customButton.onClick.AddListener(() => SelectSubOptionsPanels(customPanel));

    }



    private void SelectSubOptionsPanels(GameObject subPanel)
    {
        straightPanel.SetActive(false);
        diagonalPanel.SetActive(false);
        customPanel.SetActive(false);

        subPanel.SetActive(true);
    }
    
}
