namespace RaceOps.Infrastructure.Live;

/// <summary>Conversão dos formatos de tempo da Race Monitor ("hh:mm:ss.fff", "mm:ss.fff") para segundos.</summary>
public static class TimeParsing
{
    public static double ToSeconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var parts = value.Trim().Split(':');
        try
        {
            return parts.Length switch
            {
                3 => int.Parse(parts[0]) * 3600 + int.Parse(parts[1]) * 60 + ParseSeconds(parts[2]),
                2 => int.Parse(parts[0]) * 60 + ParseSeconds(parts[1]),
                1 => ParseSeconds(parts[0]),
                _ => 0
            };
        }
        catch (FormatException)
        {
            return 0;
        }
    }

    private static double ParseSeconds(string value) =>
        double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
