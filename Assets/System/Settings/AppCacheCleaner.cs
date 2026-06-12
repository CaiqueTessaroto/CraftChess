using System.Collections;
using System.IO;
using UnityEngine;

public class AppCacheCleaner : MonoBehaviour
{

    public static AppCacheCleaner Instance { get; private set; }

    [Header("Limite em MB")]
    public float maxCacheSizeMB = 20f;

    [Header("Opcional - pastas específicas para limpar dentro do persistentDataPath")]
    public string[] foldersToClear = { "Sprites", "Pieces", "Squads" };

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    public void CheckAndClearCache()
    {
        string path = Application.temporaryCachePath;

        if (!Directory.Exists(path))
            return;

        long totalBytes = GetDirectorySize(new DirectoryInfo(path));
        float totalMB = totalBytes / (1024f * 1024f);

        //Debug.Log("Temporary Cache Size: " + totalMB + " MB");

        if (totalMB > maxCacheSizeMB)
        {
            Debug.Log("⚠ Cache excedeu " + maxCacheSizeMB + "MB. Limpando...");
            ClearTemporaryCache();
        }
    }

    long GetDirectorySize(DirectoryInfo dir)
    {
        long size = 0;

        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
            size += file.Length;

        DirectoryInfo[] dirs = dir.GetDirectories();
        foreach (DirectoryInfo subDir in dirs)
            size += GetDirectorySize(subDir);

        return size;
    }

    // 🔹 Limpa somente o cache temporário
    public void ClearTemporaryCache()
    {
        try
        {
            string tempPath = Application.temporaryCachePath;

            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
                Directory.CreateDirectory(tempPath);
            }

            Caching.ClearCache();

            Debug.Log("✔ Temporary cache limpo com sucesso.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro ao limpar temporary cache: " + e.Message);
        }
    }

    // 🔹 Limpa apenas pastas específicas (ideal para seu RewardManager)
    public void ClearRewardFolders()
    {
        try
        {
            foreach (string folder in foldersToClear)
            {
                string fullPath = Path.Combine(Application.persistentDataPath, folder);

                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                    Debug.Log("✔ Pasta removida: " + folder);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro ao limpar rewards: " + e.Message);
        }
    }

    public void ClearAllPersistentDataSprites()
    {

        const string key = "DataCleared_v1";

        if (PlayerPrefs.GetInt(key, 0) == 1)
        {
            Debug.Log("[DataCleared] Dados já foram limpos anteriormente. Pulando...");
            return;
        }

        try
        {
            string path = Application.persistentDataPath;

            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    if (Path.GetFileName(dir) != "Sprites")
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }

            PlayerPrefs.DeleteAll();

            PlayerPrefs.SetInt("DataCleared_v1", 1);
            PlayerPrefs.Save();

            Debug.Log("✔ Dados persistentes apagados (pasta Sprites preservada).");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro ao limpar persistentDataPath: " + e.Message);
        }
    }
    // 🔹 Limpa TUDO do persistentDataPath (CUIDADO)
    public void ClearAllPersistentData()
    {
        try
        {
            string path = Application.persistentDataPath;

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("✔ Todos os dados persistentes foram apagados.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro ao limpar persistentDataPath: " + e.Message);
        }
    }

    // 🔹 Método completo (limpa tudo relacionado a cache)
    public void ClearEverything()
    {
        ClearTemporaryCache();
        ClearRewardFolders();

        Debug.Log("🔥 Limpeza completa executada.");
    }
}