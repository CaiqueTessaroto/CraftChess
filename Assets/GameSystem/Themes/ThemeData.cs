using UnityEngine;

[CreateAssetMenu(
    fileName = "NewTheme",
    menuName = "Game/Theme",
    order = 1
)]
public class ThemeData : ScriptableObject
{
    [Header("UI Colors")]
    public Color backgroundColor;
    public Color panelColor;
    public Color textColor;
    public Color buttonColor;

    [Header("Sprites")]
    //public Sprite panelSprite;
    public Sprite backgroundSprite;
    public Sprite buttonSprite;

    [Header("Audio")]
    public AudioClip clickSound;
}