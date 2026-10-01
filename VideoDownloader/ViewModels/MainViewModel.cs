using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using VideoDownloader.Infrastructure;
using VideoDownloader.Models;
using VideoDownloader.Services;

namespace VideoDownloader.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly YtDlpService _ytDlp;
    private readonly SettingsService _settingsService;
    private CancellationTokenSource? _analyzeCts;
    private string _url = "";
    private VideoInfo? _video;
    private VideoFormat? _selectedFormat;
    private bool _isAnalyzing;
    private string _status = "";

    public MainViewModel(YtDlpService ytDlp, SettingsService settingsService)
    {
        _ytDlp = ytDlp;
        _settingsService = settingsService;

        Settings = settingsService.Load();
        Settings.PropertyChanged += (_, _) => _settingsService.Save(Settings);

        Queue = new DownloadQueue(ytDlp, () => Settings);
        Queue.ItemFinished += OnItemFinished;

        PasteCommand = new AsyncRelayCommand(_ => PasteAndAnalyzeAsync());
        AnalyzeCommand = new AsyncRelayCommand(_ => AnalyzeAsync(), _ => !string.IsNullOrWhiteSpace(Url));
        DownloadCommand = new RelayCommand(_ => Download(), _ => Video is not null && !IsAnalyzing);
        CancelItemCommand = new RelayCommand(p => { if (p is DownloadItem item) Queue.Cancel(item); });
        RetryItemCommand = new RelayCommand(p => { if (p is DownloadItem item) Queue.Retry(item); });
        ShowInFolderCommand = new RelayCommand(p => ShowInFolder(p as DownloadItem));
        ClearFinishedCommand = new RelayCommand(_ => Queue.ClearFinished());
        BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());
        OpenFolderCommand = new RelayCommand(_ => OpenFolder());
        UpdateEngineCommand = new AsyncRelayCommand(_ => UpdateEngineAsync());

        Status = !_ytDlp.IsInstalled
            ? "yt-dlp.exe not found in the tools folder"
            : !_ytDlp.HasFfmpeg
                ? "ffmpeg.exe not found: 1080p and above, MP3 and subtitles may not work"
                : "Ready";
    }

    public AppSettings Settings { get; }
    public DownloadQueue Queue { get; }
    public ObservableCollection<VideoFormat> Formats { get; } = new();

    public ICommand PasteCommand { get; }
    public ICommand AnalyzeCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand CancelItemCommand { get; }
    public ICommand RetryItemCommand { get; }
    public ICommand ShowInFolderCommand { get; }
    public ICommand ClearFinishedCommand { get; }
    public ICommand BrowseFolderCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand UpdateEngineCommand { get; }

    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    public VideoInfo? Video
    {
        get => _video;
        private set
        {
            if (!SetProperty(ref _video, value))
                return;

            Formats.Clear();
            foreach (var format in value?.Formats ?? Array.Empty<VideoFormat>())
                Formats.Add(format);
            SelectedFormat = PickDefaultFormat();

            OnPropertyChanged(nameof(HasVideo));
            OnPropertyChanged(nameof(ShowEmptyHint));
            OnPropertyChanged(nameof(DownloadButtonText));
        }
    }

    public VideoFormat? SelectedFormat
    {
        get => _selectedFormat;
        set => SetProperty(ref _selectedFormat, value);
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set
        {
            if (SetProperty(ref _isAnalyzing, value))
                OnPropertyChanged(nameof(ShowEmptyHint));
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool HasVideo => Video is not null;
    public bool ShowEmptyHint => Video is null && !IsAnalyzing;

    public string DownloadButtonText => Video is { IsPlaylist: true } playlist
        ? $"Download {playlist.Entries.Count} videos"
        : "Download";

    public void Shutdown()
    {
        _analyzeCts?.Cancel();
        Queue.CancelAll();
    }

    private async Task PasteAndAnalyzeAsync()
    {
        string text;
        try
        {
            text = Clipboard.GetText().Trim();
        }
        catch (COMException)
        {
            text = "";
        }

        if (text.Length == 0)
        {
            Status = "The clipboard is empty";
            return;
        }

        Url = text;
        await AnalyzeAsync();
    }

    private async Task AnalyzeAsync()
    {
        var url = Url.Trim();
        if (!IsWebLink(url))
        {
            Status = "This does not look like a link";
            return;
        }

        _analyzeCts?.Cancel();
        _analyzeCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var token = _analyzeCts.Token;

        Video = null;
        IsAnalyzing = true;
        Status = "Reading the page...";

        try
        {
            var info = await _ytDlp.GetInfoAsync(url, Settings.WholePlaylist, token);
            Video = info;
            Status = info.IsPlaylist
                ? $"Playlist found: {info.Entries.Count} videos"
                : $"{info.Formats.Count} formats found";
        }
        catch (OperationCanceledException)
        {
            Status = "Stopped";
        }
        catch (YtDlpException ex)
        {
            Status = ex.Message;
        }
        catch (JsonException)
        {
            Status = "Could not read the page information";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private void Download()
    {
        if (Video is null)
            return;

        var audioDefault = Settings.DefaultQuality == QualityPreset.AudioOnly;

        if (Video.IsPlaylist)
        {
            foreach (var entry in Video.Entries)
            {
                Queue.Enqueue(new DownloadItem(entry.Title, entry.Url)
                {
                    AudioOnly = audioDefault,
                    FormatLabel = QualityLabel(Settings.DefaultQuality)
                });
            }
            Status = $"{Video.Entries.Count} videos added to the queue";
            return;
        }

        var format = SelectedFormat;
        var audioOnly = format?.IsAudioOnlyChoice ?? audioDefault;

        Queue.Enqueue(new DownloadItem(Video.Title, Video.WebpageUrl.Length > 0 ? Video.WebpageUrl : Url.Trim())
        {
            AudioOnly = audioOnly,
            FormatSelector = format is null || audioOnly ? null : YtDlpArguments.SelectorFor(format),
            FormatLabel = audioOnly ? Settings.AudioFormat.ToUpperInvariant() : format?.Label ?? QualityLabel(Settings.DefaultQuality)
        });
        Status = "Added to the queue";
    }

    private VideoFormat? PickDefaultFormat()
    {
        if (Formats.Count == 0)
            return null;

        int? maxHeight = Settings.DefaultQuality switch
        {
            QualityPreset.P2160 => 2160,
            QualityPreset.P1080 => 1080,
            QualityPreset.P720 => 720,
            _ => null
        };

        if (Settings.DefaultQuality == QualityPreset.AudioOnly)
            return Formats.FirstOrDefault(f => f.IsAudioOnlyChoice) ?? Formats[0];

        return maxHeight is null
            ? Formats[0]
            : Formats.FirstOrDefault(f => !f.IsAudioOnlyChoice && f.Height <= maxHeight) ?? Formats[0];
    }

    private void OnItemFinished(object? sender, DownloadItem item)
    {
        Status = item.Status switch
        {
            DownloadStatus.Done => $"Saved: {item.Title}",
            DownloadStatus.Failed => $"Failed: {item.Title}",
            DownloadStatus.Canceled => $"Canceled: {item.Title}",
            _ => Status
        };

        if (item.Status == DownloadStatus.Done && !Queue.Items.Any(i => i.CanCancel))
            SystemSounds.Asterisk.Play();
    }

    private async Task UpdateEngineAsync()
    {
        Status = "Checking for engine updates...";
        try
        {
            Status = await _ytDlp.UpdateAsync(CancellationToken.None);
        }
        catch (YtDlpException ex)
        {
            Status = ex.Message;
        }
    }

    private void BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose where to save videos",
            InitialDirectory = Directory.Exists(Settings.OutputFolder) ? Settings.OutputFolder : ""
        };

        if (dialog.ShowDialog() == true)
            Settings.OutputFolder = dialog.FolderName;
    }

    private void OpenFolder()
    {
        Directory.CreateDirectory(Settings.OutputFolder);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{Settings.OutputFolder}\"") { UseShellExecute = true });
    }

    private static void ShowInFolder(DownloadItem? item)
    {
        if (item?.FilePath is not string path || !File.Exists(path))
            return;

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
    }

    private static bool IsWebLink(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string QualityLabel(QualityPreset preset) => preset switch
    {
        QualityPreset.P2160 => "4K",
        QualityPreset.P1080 => "1080p",
        QualityPreset.P720 => "720p",
        QualityPreset.AudioOnly => "Audio",
        _ => "Best"
    };
}
