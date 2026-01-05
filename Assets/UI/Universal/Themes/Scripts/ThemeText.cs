using UnityEngine;
using TMPro;

public class ThemeText : MonoBehaviour
{
    TMP_Text text;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        ApplyTheme(ThemeManager.Instance.currentTheme);
        ThemeManager.OnThemeChanged += ApplyTheme;
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
}
