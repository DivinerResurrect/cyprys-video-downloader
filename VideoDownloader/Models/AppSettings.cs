using VideoDownloader.Infrastructure;

namespace VideoDownloader.Models;

public enum QualityPreset
{
    Best,
    P2160,
    P1080,
    P720,
    AudioOnly
}

public sealed class AppSettings : ObservableObject
{
    private string _outputFolder = "";
    private QualityPreset _defaultQuality = QualityPreset.P1080;
    private string _videoContainer = "mp4";
    private string _audioFormat = "mp3";
    private bool _downloadSubtitles;
    private string _subtitleLanguages = "en";
    private bool _embedThumbnail = true;
    private bool _wholePlaylist = true;

    public string OutputFolder
    {
        get => _outputFolder;
        set => SetProperty(ref _outputFolder, value);
    }

    public QualityPreset DefaultQuality
    {
        get => _defaultQuality;
        set => SetProperty(ref _defaultQuality, value);
    }

    public string VideoContainer
    {
        get => _videoContainer;
        set => SetProperty(ref _videoContainer, value);
    }

    public string AudioFormat
    {
        get => _audioFormat;
        set => SetProperty(ref _audioFormat, value);
    }

    public bool DownloadSubtitles
    {
        get => _downloadSubtitles;
        set => SetProperty(ref _downloadSubtitles, value);
    }

    public string SubtitleLanguages
    {
        get => _subtitleLanguages;
        set => SetProperty(ref _subtitleLanguages, value);
    }

    public bool EmbedThumbnail
    {
        get => _embedThumbnail;
        set => SetProperty(ref _embedThumbnail, value);
    }

    public bool WholePlaylist
    {
        get => _wholePlaylist;
        set => SetProperty(ref _wholePlaylist, value);
    }

    public static AppSettings CreateDefault() => new()
    {
        OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Downloads")
    };
}
