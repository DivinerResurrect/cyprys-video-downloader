using VideoDownloader.Models;

namespace VideoDownloader.Services;

public static class YtDlpArguments
{
    public const string ProgressPrefix = "[vd-dl]";
    public const string PostProcessPrefix = "[vd-pp]";
    public const string FilePrefix = "[vd-file]";

    public static IEnumerable<string> ForInfo(string url, bool wholePlaylist)
    {
        return new[]
        {
            "--dump-single-json",
            "--no-warnings",
            "--encoding", "utf-8",
            wholePlaylist ? "--flat-playlist" : "--no-playlist",
            "--",
            url
        };
    }

    public static IEnumerable<string> ForDownload(DownloadItem item, AppSettings settings, string ffmpegPath)
    {
        var args = new List<string>
        {
            "--newline",
            "--progress",
            "--no-warnings",
            "--no-playlist",
            "--encoding", "utf-8",
            "--color", "no_color",
            "--windows-filenames",
            "--progress-template", $"download:{ProgressPrefix}%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s",
            "--progress-template", $"postprocess:{PostProcessPrefix}%(progress.postprocessor)s",
            "-P", settings.OutputFolder,
            "-o", "%(title).150B [%(id)s].%(ext)s"
        };

        if (File.Exists(ffmpegPath))
            args.AddRange(new[] { "--ffmpeg-location", ffmpegPath });

        if (item.AudioOnly)
        {
            args.AddRange(new[]
            {
                "-f", "bestaudio/best",
                "-x",
                "--audio-format", settings.AudioFormat,
                "--audio-quality", "0"
            });
        }
        else
        {
            args.AddRange(new[]
            {
                "-f", item.FormatSelector ?? SelectorFor(settings.DefaultQuality),
                "--merge-output-format", settings.VideoContainer
            });

            if (settings.DownloadSubtitles)
            {
                args.AddRange(new[]
                {
                    "--write-subs",
                    "--sub-langs", settings.SubtitleLanguages,
                    "--embed-subs"
                });
            }
        }

        if (settings.EmbedThumbnail)
            args.AddRange(new[] { "--embed-thumbnail", "--embed-metadata" });

        args.AddRange(new[] { "--print", $"after_move:{FilePrefix}%(filepath)s" });
        args.AddRange(new[] { "--", item.Url });
        return args;
    }

    public static IEnumerable<string> ForUpdate() => new[] { "-U" };

    public static string SelectorFor(QualityPreset preset) => preset switch
    {
        QualityPreset.P2160 => "bv*[height<=2160]+ba/b[height<=2160]/b",
        QualityPreset.P1080 => "bv*[height<=1080]+ba/b[height<=1080]/b",
        QualityPreset.P720 => "bv*[height<=720]+ba/b[height<=720]/b",
        QualityPreset.AudioOnly => "bestaudio/best",
        _ => "bv*+ba/b"
    };

    public static string SelectorFor(VideoFormat format)
    {
        if (format.IsAudioOnlyChoice)
            return "bestaudio/best";
        return format.HasAudio ? format.FormatId : $"{format.FormatId}+bestaudio/best";
    }
}
