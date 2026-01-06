using UnityEngine;
using UnityEngine.UI;

public class ThemeImage : MonoBehaviour
{
    public enum ImageType { Button, Background, Panel, Background_Icon }
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
                if (!theme.backgroundSprite)
                    img.color = theme.backgroundColor;
                img.sprite = theme.backgroundSprite;
                break;

            case ImageType.Panel:
                img.color = theme.panelColor;
                //img.sprite = theme.panelSprite;
                break;

            case ImageType.Button:
                //img.color = theme.buttonColor;
                img.sprite = theme.buttonSprite;
                break;
            case ImageType.Background_Icon:
                img.color = theme.buttonColor;
                //img.sprite = theme.buttonSprite;
                break;
        }
    }
}
