using UnityEngine;
using System;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;

    public ThemeData currentTheme;

    public static Action<ThemeData> OnThemeChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetTheme(ThemeData theme)
    {
        currentTheme = theme;
        OnThemeChanged?.Invoke(theme);
    }
}
