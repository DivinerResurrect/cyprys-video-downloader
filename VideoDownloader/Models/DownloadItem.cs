using VideoDownloader.Infrastructure;

namespace VideoDownloader.Models;

public enum DownloadStatus
{
    Queued,
    Downloading,
    Processing,
    Done,
    Failed,
    Canceled
}

public sealed class DownloadItem : ObservableObject
{
    private DownloadStatus _status = DownloadStatus.Queued;
    private double _progress;
    private string _speed = "";
    private string _eta = "";
    private string? _error;
    private string? _filePath;

    public DownloadItem(string title, string url)
    {
        Title = title;
        Url = url;
    }

    public string Title { get; }
    public string Url { get; }
    public string? FormatSelector { get; init; }
    public string FormatLabel { get; init; } = "";
    public bool AudioOnly { get; init; }

    public CancellationTokenSource? Cancellation { get; set; }

    public DownloadStatus Status
    {
        get => _status;
        set
        {
            if (!SetProperty(ref _status, value))
                return;
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsFinished));
            OnPropertyChanged(nameof(IsDone));
            OnPropertyChanged(nameof(CanCancel));
            OnPropertyChanged(nameof(CanRetry));
        }
    }

    public double Progress
    {
        get => _progress;
        set
        {
            if (SetProperty(ref _progress, value))
                OnPropertyChanged(nameof(StatusText));
        }
    }

    public string Speed
    {
        get => _speed;
        set
        {
            if (SetProperty(ref _speed, value))
                OnPropertyChanged(nameof(StatusText));
        }
    }

    public string Eta
    {
        get => _eta;
        set
        {
            if (SetProperty(ref _eta, value))
                OnPropertyChanged(nameof(StatusText));
        }
    }

    public string? Error
    {
        get => _error;
        set
        {
            if (SetProperty(ref _error, value))
                OnPropertyChanged(nameof(StatusText));
        }
    }

    public string? FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public bool IsFinished => Status is DownloadStatus.Done or DownloadStatus.Failed or DownloadStatus.Canceled;
    public bool IsDone => Status == DownloadStatus.Done;
    public bool CanCancel => Status is DownloadStatus.Queued or DownloadStatus.Downloading or DownloadStatus.Processing;
    public bool CanRetry => Status is DownloadStatus.Failed or DownloadStatus.Canceled;

    public string StatusText => Status switch
    {
        DownloadStatus.Queued => $"Queued · {FormatLabel}",
        DownloadStatus.Downloading => string.Join("   ", new[]
        {
            $"{Progress:0.0}%",
            Speed,
            Eta.Length > 0 ? $"{Eta} left" : ""
        }.Where(s => s.Length > 0)),
        DownloadStatus.Processing => "Processing",
        DownloadStatus.Done => $"Done · {FormatLabel}",
        DownloadStatus.Failed => Error ?? "Failed",
        DownloadStatus.Canceled => "Canceled",
        _ => ""
    };

    public void Apply(DownloadProgress progress)
    {
        if (IsFinished)
            return;

        Progress = progress.Percent;
        Speed = progress.Speed;
        Eta = progress.Eta;
        Status = progress.Status;
    }

    public void Reset()
    {
        Progress = 0;
        Speed = "";
        Eta = "";
        Error = null;
        FilePath = null;
        Status = DownloadStatus.Queued;
    }
}

public readonly record struct DownloadProgress(double Percent, string Speed, string Eta, DownloadStatus Status);
