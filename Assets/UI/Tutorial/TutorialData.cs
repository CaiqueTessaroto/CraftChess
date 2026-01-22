using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "TutorialData",
    menuName = "Game/Tutorial/Tutorial Data"
)]
public class TutorialData : ScriptableObject
{
    public List<TutorialPage> pages;
}