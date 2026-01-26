using UnityEngine;


public enum TypeFeed
{
    Reward,
    URL,
    Credits
}

[CreateAssetMenu(fileName = "RewardData", menuName = "Feed/Data")]
public class RewardData : ScriptableObject
{
    public Sprite image;
    public string id;
    public string displayName;
    public float weight = 1f;
    public TypeFeed typeFeed = TypeFeed.Reward;
    
}
