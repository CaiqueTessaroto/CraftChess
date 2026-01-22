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

    [Header("Select Colors:")]
    public Color32 corPadrao = new Color32(255, 255, 255, 100);
    public Color32 corDestaque = new Color32(240, 75, 79, 255);

    // Start is called before the first frame update
    void Start()
    {
        straightButton.onClick.AddListener(() =>
        {
            SelecionarPainel(straightButton.gameObject);
            SelectSubOptionsPanels(straightPanel);
        });
        diagonalButton.onClick.AddListener(() =>
        {
            SelecionarPainel(diagonalButton.gameObject);
            SelectSubOptionsPanels(diagonalPanel);
        });
        customButton.onClick.AddListener(() =>
        {
            SelecionarPainel(customButton.gameObject);
            SelectSubOptionsPanels(customPanel);
        });

    }



    private void SelectSubOptionsPanels(GameObject subPanel)
    {

        straightPanel.SetActive(false);
        diagonalPanel.SetActive(false);
        customPanel.SetActive(false);

        subPanel.SetActive(true);
    }

    public void SelecionarPainel(GameObject painelAtivo)
    {
        MudarCor(straightButton.gameObject, corPadrao);
        MudarCor(diagonalButton.gameObject, corPadrao);
        MudarCor(customButton.gameObject, corPadrao);

        MudarCor(painelAtivo, corDestaque);
    }

    private void MudarCor(GameObject obj, Color32 cor)
    {
        if (obj != null && obj.TryGetComponent<Image>(out Image img))
        {
            img.color = cor;
        }
    }

}
