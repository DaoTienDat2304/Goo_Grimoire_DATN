using System.Globalization;
using UnityEngine;

[System.Serializable]
public enum CurrencyType
{
    Coins,
    Gems
}

public static class CurrencyAmountFormatter
{
    public static string Format(int amount)
    {
        long absoluteAmount = System.Math.Abs((long)amount);
        string sign = amount < 0 ? "-" : string.Empty;

        if (absoluteAmount < 1000)
            return sign + absoluteAmount.ToString(CultureInfo.InvariantCulture);

        if (absoluteAmount < 1000000)
            return sign + FormatScaled(absoluteAmount, 1000, "k");

        if (absoluteAmount < 1000000000)
            return sign + FormatScaled(absoluteAmount, 1000000, "m");

        return sign + FormatScaled(absoluteAmount, 1000000000, "b");
    }

    private static string FormatScaled(long amount, long divisor, string suffix)
    {
        double scaled = (double)amount / divisor;
        int decimalPlaces = scaled >= 100 ? 0 : scaled >= 10 ? 1 : 2;
        double precision = System.Math.Pow(10, decimalPlaces);
        double truncated = System.Math.Floor(scaled * precision) / precision;
        string format = decimalPlaces == 0 ? "0" : decimalPlaces == 1 ? "0.#" : "0.##";

        return truncated.ToString(format, CultureInfo.InvariantCulture) + suffix;
    }
}

[System.Serializable]
public class CurrencyData
{
    public CurrencyType type;
    public int amount;

    public CurrencyData(CurrencyType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}
