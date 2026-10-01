using System.Text.Json;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

public static class VideoInfoParser
{
    public static VideoInfo Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var isPlaylist = GetString(root, "_type") == "playlist";

        return new VideoInfo
        {
            Id = GetString(root, "id") ?? "",
            Title = GetString(root, "title") ?? "Untitled",
            Uploader = GetString(root, "uploader") ?? GetString(root, "channel"),
            ThumbnailUrl = GetString(root, "thumbnail") ?? LastThumbnail(root),
            WebpageUrl = GetString(root, "webpage_url") ?? "",
            Duration = GetDouble(root, "duration") is double seconds ? TimeSpan.FromSeconds(seconds) : null,
            IsPlaylist = isPlaylist,
            Entries = isPlaylist ? ParseEntries(root) : Array.Empty<PlaylistEntry>(),
            Formats = isPlaylist ? Array.Empty<VideoFormat>() : BuildChoices(ParseFormats(root))
        };
    }

    private static List<PlaylistEntry> ParseEntries(JsonElement root)
    {
        var result = new List<PlaylistEntry>();
        if (!root.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in entries.EnumerateArray())
        {
            var url = GetString(entry, "webpage_url") ?? GetString(entry, "url");
            if (string.IsNullOrEmpty(url))
                continue;
            result.Add(new PlaylistEntry(GetString(entry, "title") ?? url, url));
        }
        return result;
    }

    private static List<VideoFormat> ParseFormats(JsonElement root)
    {
        var result = new List<VideoFormat>();
        if (!root.TryGetProperty("formats", out var formats) || formats.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var f in formats.EnumerateArray())
        {
            var ext = GetString(f, "ext") ?? "";
            if (ext == "mhtml")
                continue;

            result.Add(new VideoFormat
            {
                FormatId = GetString(f, "format_id") ?? "",
                Extension = ext,
                Height = GetInt(f, "height"),
                Fps = GetDouble(f, "fps"),
                VideoCodec = GetString(f, "vcodec"),
                AudioCodec = GetString(f, "acodec"),
                FileSize = GetLong(f, "filesize") ?? GetLong(f, "filesize_approx"),
                Bitrate = GetDouble(f, "tbr"),
                Note = GetString(f, "format_note")
            });
        }
        return result;
    }

    private static List<VideoFormat> BuildChoices(List<VideoFormat> formats)
    {
        var bestAudio = formats
            .Where(f => f.HasAudio && !f.HasVideo)
            .OrderByDescending(f => f.Bitrate ?? 0)
            .FirstOrDefault();

        var videos = formats
            .Where(f => f.HasVideo && f.Height is not null)
            .GroupBy(f => f.Height!.Value)
            .Select(g => g.OrderByDescending(f => f.Bitrate ?? 0).First())
            .OrderByDescending(f => f.Height)
            .Select(f => f.HasAudio || bestAudio is null ? f : f with
            {
                AudioCodec = bestAudio.AudioCodec,
                FileSize = f.FileSize + bestAudio.FileSize
            })
            .ToList();

        if (bestAudio is not null)
            videos.Add(bestAudio with { IsAudioOnlyChoice = true });

        return videos;
    }

    private static string? LastThumbnail(JsonElement root)
    {
        if (!root.TryGetProperty("thumbnails", out var thumbs) || thumbs.ValueKind != JsonValueKind.Array)
            return null;
        return thumbs.EnumerateArray().Select(t => GetString(t, "url")).LastOrDefault(u => u is not null);
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? GetDouble(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;

    private static int? GetInt(JsonElement element, string name) =>
        GetDouble(element, name) is double d ? (int)d : null;

    private static long? GetLong(JsonElement element, string name) =>
        GetDouble(element, name) is double d ? (long)d : null;
}
