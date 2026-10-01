using VideoDownloader.Infrastructure;

namespace VideoDownloader.Models;

public sealed record VideoFormat
{
    public string FormatId { get; init; } = "";
    public string Extension { get; init; } = "";
    public int? Height { get; init; }
    public double? Fps { get; init; }
    public string? VideoCodec { get; init; }
    public string? AudioCodec { get; init; }
    public long? FileSize { get; init; }
    public double? Bitrate { get; init; }
    public string? Note { get; init; }
    public bool IsAudioOnlyChoice { get; init; }

    public bool HasVideo => IsCodec(VideoCodec);
    public bool HasAudio => IsCodec(AudioCodec);

    public string Label
    {
        get
        {
            if (IsAudioOnlyChoice)
                return "Audio only";
            if (Height is int height)
                return Fps >= 50 ? $"{height}p{(int)Math.Round(Fps.Value)}" : $"{height}p";
            return Note ?? FormatId;
        }
    }

    public string CodecText
    {
        get
        {
            var parts = new[] { ShortCodec(VideoCodec), ShortCodec(AudioCodec) }.Where(c => c is not null);
            return string.Join(" + ", parts);
        }
    }

    public string SizeText => SizeFormatter.Format(FileSize);

    private static bool IsCodec(string? codec) => !string.IsNullOrEmpty(codec) && codec != "none";

    private static string? ShortCodec(string? codec)
    {
        if (!IsCodec(codec))
            return null;
        var dot = codec!.IndexOf('.');
        return dot > 0 ? codec[..dot] : codec;
    }
}
