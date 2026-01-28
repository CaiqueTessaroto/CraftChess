using UnityEngine;
using TMPro;

public class ThemeText : MonoBehaviour
{
    public string key;
    private TMP_Text text;

    void Start()
    {
        if (!ThemeManager.Instance)
            return;

        ApplyTheme(ThemeManager.Instance.currentTheme);
        ThemeManager.OnThemeChanged += ApplyTheme;
    }

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        if (LocalizationManager.Instance != null && SettingsManager.Instance != null)
        {
            LocalizationManager.Instance.ApplyFontToAllTMP(SettingsManager.Instance.Settings.language);
            UpdateText();
        }
    }


    void OnDestroy()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    void ApplyTheme(ThemeData theme)
    {
        if (theme == null) return;
        text.color = theme.textColor;
    }

    public void UpdateText()
    {
        if (LocalizationManager.Instance == null)
            return;

        if (text == null)
            text = GetComponent<TMP_Text>();

        if (text == null)
            return;

        string txt = LocalizationManager.Instance.Get(key);
        if (!string.IsNullOrEmpty(txt))
        {
            //text.font = font;
            text.text = txt;
        }
    }
}
