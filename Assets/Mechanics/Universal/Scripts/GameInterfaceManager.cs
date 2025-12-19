using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameInterfaceManager : MonoBehaviour
{
    public BoardChessManager boardChessManager;
    public Button switchSide;
    // Start is called before the first frame update
    void Start()
    {

        if (boardChessManager == null)
            boardChessManager = FindObjectOfType<BoardChessManager>();

        switchSide.onClick.AddListener(() =>
        {
            boardChessManager.SwitchSide();

        });

    }

    // Update is called once per frame
    void Update()
    {

    }
}
