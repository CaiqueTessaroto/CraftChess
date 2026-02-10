using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SquadButtonsManager : MonoBehaviour
{
    public GameManager gameManager;

    [Header("Buttons:")]
    public Button menuBtn;

    public Button resetBtn;
    bool isResetting = true;

    // Start is called before the first frame update
    void Start()
    {

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        menuBtn.onClick.AddListener(() => gameManager.ChangeScene("Menu"));

        resetBtn.onClick.AddListener(() =>
        {
            if (isResetting) return;

            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
            
        });


        StartCoroutine(ResetSceneWithDelay());



    }

    IEnumerator ResetSceneWithDelay()
    {
        yield return new WaitForSeconds(1);

        isResetting = false;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
