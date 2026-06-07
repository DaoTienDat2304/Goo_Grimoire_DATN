using UnityEngine;
using System.Collections.Generic;

public enum WildSlimeType
{
    Friendly,
    Aggressive
}

[CreateAssetMenu(fileName = "WildSlimes", menuName = "WildSlimeDatas/WildSlimes")]
public class WildSlimes : ScriptableObject
{
    [System.Serializable]
    public class WildSlimeTraits
    {
        public int slimeID;
        public TraitSO[] wildSlimeTraits = new TraitSO[3];
        public WildSlimeType slimeType = WildSlimeType.Friendly;
    }
    public List<WildSlimeTraits> slimes;
    public List<WildSlimeTraits> tamedSlimes;
}
