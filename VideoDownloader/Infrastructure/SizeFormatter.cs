using System.Globalization;

namespace VideoDownloader.Infrastructure;

public static class SizeFormatter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    public static string Format(long? bytes)
    {
        if (bytes is not long value || value <= 0)
            return "?";

        double size = value;
        var unit = 0;
        while (size >= 1024 && unit < Units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        var pattern = unit >= 2 ? "0.0" : "0";
        return size.ToString(pattern, CultureInfo.InvariantCulture) + " " + Units[unit];
    }
}
