using UnityEngine;

[System.Serializable]
public enum ResourceType
{
    Marshmallow
}

[System.Serializable]
public class ResourceData
{
    public ResourceType type;
    public int amount;

    public ResourceData(ResourceType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}









