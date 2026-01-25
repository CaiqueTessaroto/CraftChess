using UnityEngine;


[CreateAssetMenu(fileName = "RewardData", menuName = "Rewards/Reward")]
public class RewardData : ScriptableObject
{
    public Sprite image;
    public string id;
    public string displayName;
    public float weight = 1f;
    
}
