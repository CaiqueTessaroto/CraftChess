using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    
    void Awake()
    {
        Application.targetFrameRate = 200;
    }
    


    public void ChangeScene(string sceneName)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        SceneManager.LoadScene(sceneName);
    }
}
