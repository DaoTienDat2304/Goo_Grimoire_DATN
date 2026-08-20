using System;

/// <summary>
/// </summary>
[Serializable]
public class SaveEnvelope
{
    public string payload;

    public string sig;

    public int schemaVersion = 1;
}
