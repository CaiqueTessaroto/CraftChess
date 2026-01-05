using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThemeButton : MonoBehaviour
{
    public ThemeData theme;

    public bool setTheme = false;

    void Update()
    {
        if(setTheme == true)
        {
            SelectTheme();
            setTheme = false;
        }
        
    }

    public void SelectTheme()
    {
        ThemeManager.Instance.SetTheme(theme);
        PlayerPrefs.SetString("THEME", theme.name);
    }
}

