using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using System.Linq;
using TMPro;

public static class GameModeManager
{
    public static GameMode SelectedMode;
}

public enum GameMode
{
    PlayerVsAI,
    PlayerVsPlayerLocal,
    AIVsAI
}

public class GameManager : MonoBehaviour
{

    // Limite em bytes (1 GB)
    private const long RAM_LIMIT = 1L * 1024 * 1024 * 1024;

    // Verifica a cada X segundos (evita custo por frame)
    [SerializeField] private float checkInterval = 2f;

    private float timer;


    // Start is called before the first frame update
    void Start()
    {

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.intro);
            AudioManager.Instance.ApplySoundToAllButtons();
        }

        SetupAllInputFields();

    }

    public void SetupAllInputFields()
    {
        TMP_InputField[] inputFields = FindObjectsOfType<TMP_InputField>(true);

        foreach (TMP_InputField tmp in inputFields)
        {
            tmp.characterLimit = 50;
            tmp.lineType = TMP_InputField.LineType.SingleLine;
        }
    }


    // Update is called once per frame
    void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckRam();
        }
    }

    void Awake()
    {
        Application.targetFrameRate = 30;
    }



    public void ChangeScene(string sceneName)
    {
        if (sceneName == "Menu")
            if (MatchData.Instance != null)
                MatchData.Instance.Reset();

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        AdsManager.TryShowInterstitial();

        SceneManager.LoadScene(sceneName);
    }

    void CheckRam()
    {
        long usedRam = Profiler.usedHeapSizeLong;

        if (usedRam >= RAM_LIMIT)
        {

            //Debug.LogWarning($"RAM excedida: {usedRam / (1024 * 1024)} MB. Resetando cena...");

#if !UNITY_EDITOR

        ReloadCurrentScene();

#endif

        }
    }

    void ReloadCurrentScene()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
