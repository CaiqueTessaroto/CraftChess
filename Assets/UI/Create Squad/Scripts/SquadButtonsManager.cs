using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SquadButtonsManager : MonoBehaviour
{
    public GameManager gameManager;

    [Header("Buttons:")]
    public Button menuBtn;

    // Start is called before the first frame update
    void Start()
    {

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        menuBtn.onClick.AddListener(() => gameManager.ChangeScene("Menu"));



    }

    // Update is called once per frame
    void Update()
    {

    }
}
