using UnityEngine;
using UnityEngine.UI;

public class ThemeImage : MonoBehaviour
{
    public enum ImageType { Background, Panel, Button }
    public ImageType type;

    Image img;

    void Start()
    {
        img = GetComponent<Image>();
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

        switch (type)
        {
            case ImageType.Background:
                //img.color = theme.backgroundColor;
                break;

            case ImageType.Panel:
                //img.color = theme.panelColor;
                img.sprite = theme.panelSprite;
                break;

            case ImageType.Button:
                //img.color = theme.buttonColor;
                img.sprite = theme.buttonSprite;
                break;
        }
    }
}
