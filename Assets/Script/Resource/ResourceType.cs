using UnityEngine;

[System.Serializable]
public enum ResourceType
{
    Marshmallow    // Kẹo dẻo - dùng để bắt slime
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









