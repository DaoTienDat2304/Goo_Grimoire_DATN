using UnityEngine;

public static class SlimeNameGenerator
{
    private static readonly string[] Consonants = { "B", "C", "D", "F", "G", "H", "J", "K", "L", "M", "N", "P", "R", "S", "T", "V", "W", "Z", "Ch", "Sh" };
    private static readonly string[] Vowels = { "a", "e", "i", "o", "u", "y", "ee", "oo" };

    public static string GetRandomSlimeName()
    {
        string c1 = Consonants[Random.Range(0, Consonants.Length)];
        string v1 = Vowels[Random.Range(0, Vowels.Length)];
        string c2 = Consonants[Random.Range(0, Consonants.Length)].ToLower();
        string v2 = Vowels[Random.Range(0, Vowels.Length)];

        return $"{c1}{v1}{c2}{v2}";
    }

    public static string GetShortEggId(string fullId)
    {
        if (string.IsNullOrEmpty(fullId)) return Random.Range(100, 999).ToString();
        return fullId.Substring(0, Mathf.Min(4, fullId.Length)).ToUpper();
    }
}
