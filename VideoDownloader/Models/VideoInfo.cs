namespace VideoDownloader.Models;

public sealed class VideoInfo
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Uploader { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string WebpageUrl { get; init; } = "";
    public TimeSpan? Duration { get; init; }
    public bool IsPlaylist { get; init; }
    public IReadOnlyList<PlaylistEntry> Entries { get; init; } = Array.Empty<PlaylistEntry>();
    public IReadOnlyList<VideoFormat> Formats { get; init; } = Array.Empty<VideoFormat>();

    public string DurationText => Duration switch
    {
        { TotalHours: >= 1 } d => d.ToString(@"h\:mm\:ss"),
        { } d => d.ToString(@"m\:ss"),
        null => ""
    };

    public string Subtitle
    {
        get
        {
            var parts = new[]
            {
                Uploader,
                DurationText,
                IsPlaylist ? $"{Entries.Count} videos" : null
            };
            return string.Join(" · ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
    }
}

public sealed record PlaylistEntry(string Title, string Url);
