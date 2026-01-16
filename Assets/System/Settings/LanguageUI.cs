using System;
using System.Linq;
using TMPro;
using UnityEngine;

public enum Language
{
    PortugueseBR,
    EnglishUS,
    SpanishES
}

public class LanguageUI : MonoBehaviour
{
    public TMP_Dropdown dropdown;


    void Awake()
    {
        //dropdown = GetComponent<TMP_Dropdown>();
        Populate();
    }

    void Populate()
    {
        dropdown.ClearOptions();

        var options = Enum.GetValues(typeof(Language))
            .Cast<Language>()
            .Select(LanguageHelper.ToDisplayName)
            .ToList();

        dropdown.AddOptions(options);

        dropdown.onValueChanged.AddListener(OnValueChanged);

        dropdown.value = (int)SettingsManager.Instance.Settings.language;
        dropdown.RefreshShownValue();
    }

    void OnValueChanged(int index)
    {
        LocalizationManager.Instance.ApplyLanguage((Language)index);
    }


}
