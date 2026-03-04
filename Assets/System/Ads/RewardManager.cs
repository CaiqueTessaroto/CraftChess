using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.IO.Compression;
using UnityEngine.UI;
using TMPro;


public class RewardManager : MonoBehaviour
{
    public AppCacheCleaner appCacheCleaner;

    [Header("Data")]
    public RewardData[] rewards;
    public RewardFeed rewardFeed;

    [Header("Unlock Pack Panel")]
    public AudioClip unlock;
    public GameObject unlockPanel;
    public Button okBtn;
    public Image image;
    public TextMeshProUGUI textTmp;

    public static RewardManager Instance;


    void Start()
    {
        if (rewardFeed == null)
            rewardFeed = FindFirstObjectByType<RewardFeed>();

        if (appCacheCleaner == null)
            appCacheCleaner = FindFirstObjectByType<AppCacheCleaner>();


        AllRewardsCheck();

    }
    void Awake()
    {
        Instance = this;
        //DontDestroyOnLoad(gameObject);


        okBtn.onClick.AddListener(() => unlockPanel.SetActive(false));
    }

    public IEnumerator GrantReward(RewardData reward)
    {

        if (this == null || !gameObject)
            yield break; ;

        if (reward.typeFeed != TypeFeed.Reward)
            yield break; ;

        if (PlayerPrefs.GetInt("Reward_" + reward.id, 0) == 1)
        {
            Debug.Log("Reward já desbloqueado: " + reward.id);
            yield break; ;
        }

        string name = UIHelperUtils.T(reward.id);

        if (string.IsNullOrEmpty(name))
            name = reward.id;

        string pack = UIHelperUtils.T("pack", name);

        if (string.IsNullOrEmpty(pack))
            pack = name + " Pack";

        string text = UIHelperUtils.T("unlock.pack", pack);

        if (string.IsNullOrEmpty(text))
            text = "The " + reward.Content + "The has been unlocked and is now available in your library.";

        textTmp.text = text;

        image.sprite = reward.image;

        unlockPanel.SetActive(true);
        AudioManager.Instance.PlaySFX(unlock);

        foreach (RewardData re in rewards)
        {
            re.weight = 1f;
        }

        yield return StartCoroutine(CopyRewardPack(reward.id, "Sprites"));
        yield return StartCoroutine(CopyRewardPack(reward.id, "Pieces"));
        yield return StartCoroutine(CopyRewardPack(reward.id, "Squads"));

        bool exists = FolderExists("Sprites", reward.id);

        if (exists)
        {
            PlayerPrefs.SetInt("Reward_" + reward.id, 1);
            PlayerPrefs.Save();
        }
        else
        {
            PlayerPrefs.SetInt("Reward_" + reward.id, 0);
            PlayerPrefs.Save();
        }

        bool allunlock = AllRewardsUnlocked();

        if (allunlock)
        {
            gameObject.SetActive(false);
            if (rewardFeed != null)
                rewardFeed.rewardBtn.interactable = false;
        }

    }

    public bool FolderExists(string basePath, string folderName)
    {
        string targetDir = Path.Combine(
            Application.persistentDataPath,
            basePath,
            folderName
        );

        return Directory.Exists(targetDir);
    }

    public void ResetRewards()
    {
        for (int i = 0; i < rewards.Length; i++)
        {
            PlayerPrefs.SetInt("Reward_" + rewards[i].id, 0);
        }
    }

    public void AllRewardsCheck()
    {
        for (int i = 0; i < rewards.Length; i++)
        {
            bool exists = FolderExists("Sprites", rewards[i].id);

            if (!exists)
            {
                PlayerPrefs.SetInt("Reward_" + rewards[i].id, 0);
            }
        }

        PlayerPrefs.Save();
    }


    public bool AllRewardsUnlocked()
    {
        for (int i = 0; i < rewards.Length; i++)
        {
            if (PlayerPrefs.GetInt("Reward_" + rewards[i].id, 0) != 1 && rewards[i].typeFeed == TypeFeed.Reward)
            {
                return false; // achou um que ainda não foi salvo
            }
        }

        return true; // todos já estão salvos
    }

    IEnumerator CopyRewardPack(string rewardId, string path)
    {

        string zipName = rewardId + ".zip";

        string zipPath = Path.Combine(
            Application.streamingAssetsPath,
            "Rewards",
            path,
            zipName
        );

        string targetDir = Path.Combine(
            Application.persistentDataPath,
            path
        );

        //if (Directory.Exists(targetDir))
        //{
        //    Debug.Log("Exists: " + targetDir);
        //    yield break;
        //}

        string targetZip = Path.Combine(targetDir, zipName);

        // Garante que a pasta exista
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        using (UnityWebRequest www = UnityWebRequest.Get(zipPath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Erro ao baixar reward {rewardId}/{path}: {www.error}");
                yield break;
            }

            File.WriteAllBytes(targetZip, www.downloadHandler.data);
        }

        // Extrai DENTRO da pasta correta
        ZipFile.ExtractToDirectory(
            targetZip,
            targetDir,
            System.Text.Encoding.UTF8,
            true // overwrite
        );

        File.Delete(targetZip);

        //Debug.Log($"✔ {path}/{rewardId} aplicado");

        yield return new WaitForEndOfFrame();

        appCacheCleaner.ClearTemporaryCache();

        yield return new WaitForEndOfFrame();

    }

    public RewardData GetRandomReward()
    {
        float totalWeight = 0f;

        foreach (var reward in rewards)
            totalWeight += reward.weight;

        float roll = Random.Range(0f, totalWeight);

        foreach (var reward in rewards)
        {
            roll -= reward.weight;
            if (roll <= 0f)
                return reward;
        }

        return rewards[0]; // fallback seguro

    }


}
