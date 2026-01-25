using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using System.Linq;

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

    public AudioClip[] musics;

    // Limite em bytes (1 GB)
    private const long RAM_LIMIT = 1L * 1024 * 1024 * 1024;

    // Verifica a cada X segundos (evita custo por frame)
    [SerializeField] private float checkInterval = 2f;

    private float timer;
    // Start is called before the first frame update
    void Start()
    {
        if (musics != null)
            if (musics.Length > 0)
                AudioManager.Instance.PlayMusicPlaylist(musics);

         AudioManager.Instance.PlaySFX(AudioManager.Instance.intro);

         AudioManager.Instance.ApplySoundToAllButtons();

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
        Application.targetFrameRate = 200;
    }



    public void ChangeScene(string sceneName)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        AdsManager.TryShowInterstitial();

        SceneManager.LoadScene(sceneName);
    }

    void CheckRam()
    {
        long usedRam = Profiler.usedHeapSizeLong;

        if (usedRam >= RAM_LIMIT)
        {
            Debug.LogWarning(
                $"RAM excedida: {usedRam / (1024 * 1024)} MB. Resetando cena..."
            );

            ReloadCurrentScene();
        }
    }

    void ReloadCurrentScene()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
