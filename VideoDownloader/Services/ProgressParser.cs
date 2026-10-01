using System.Globalization;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

public static class ProgressParser
{
    public static bool TryParse(string line, out DownloadProgress progress)
    {
        progress = default;
        if (!line.StartsWith(YtDlpArguments.ProgressPrefix, StringComparison.Ordinal))
            return false;

        var parts = line[YtDlpArguments.ProgressPrefix.Length..].Split('|');
        if (parts.Length < 3)
            return false;

        var percentText = parts[0].Trim().TrimEnd('%');
        if (!double.TryParse(percentText, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            return false;

        progress = new DownloadProgress(percent, Clean(parts[1]), Clean(parts[2]), DownloadStatus.Downloading);
        return true;
    }

    public static bool IsPostProcessing(string line) =>
        line.StartsWith(YtDlpArguments.PostProcessPrefix, StringComparison.Ordinal);

    public static bool TryGetFilePath(string line, out string path)
    {
        path = "";
        if (!line.StartsWith(YtDlpArguments.FilePrefix, StringComparison.Ordinal))
            return false;

        path = line[YtDlpArguments.FilePrefix.Length..].Trim();
        return path.Length > 0;
    }

    private static string Clean(string value)
    {
        var text = value.Trim();
        return text is "N/A" or "Unknown" or "NA" ? "" : text;
    }
}
