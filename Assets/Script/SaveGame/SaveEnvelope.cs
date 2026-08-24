using System;
[Serializable]
public class SaveEnvelope
{
    public string payload;

    public string sig;

    public int schemaVersion = 1;
}
