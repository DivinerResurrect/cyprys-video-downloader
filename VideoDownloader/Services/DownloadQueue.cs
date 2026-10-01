using System.Collections.ObjectModel;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

public sealed class DownloadQueue
{
    private readonly YtDlpService _ytDlp;
    private readonly Func<AppSettings> _settings;
    private bool _isRunning;

    public DownloadQueue(YtDlpService ytDlp, Func<AppSettings> settings)
    {
        _ytDlp = ytDlp;
        _settings = settings;
    }

    public ObservableCollection<DownloadItem> Items { get; } = new();

    public event EventHandler<DownloadItem>? ItemFinished;

    public void Enqueue(DownloadItem item)
    {
        Items.Add(item);
        _ = RunAsync();
    }

    public void Cancel(DownloadItem item)
    {
        if (item.Status == DownloadStatus.Queued)
            item.Status = DownloadStatus.Canceled;
        else
            item.Cancellation?.Cancel();
    }

    public void CancelAll()
    {
        foreach (var item in Items.Where(i => i.CanCancel).ToList())
            Cancel(item);
    }

    public void Retry(DownloadItem item)
    {
        if (!item.CanRetry)
            return;
        item.Reset();
        _ = RunAsync();
    }

    public void ClearFinished()
    {
        foreach (var item in Items.Where(i => i.IsFinished).ToList())
            Items.Remove(item);
    }

    private async Task RunAsync()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        try
        {
            while (Items.FirstOrDefault(i => i.Status == DownloadStatus.Queued) is { } next)
                await DownloadOneAsync(next);
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task DownloadOneAsync(DownloadItem item)
    {
        item.Cancellation = new CancellationTokenSource();
        item.Status = DownloadStatus.Downloading;
        var progress = new Progress<DownloadProgress>(item.Apply);

        try
        {
            var settings = _settings();
            Directory.CreateDirectory(settings.OutputFolder);
            item.FilePath = await _ytDlp.DownloadAsync(item, settings, progress, item.Cancellation.Token);
            item.Progress = 100;
            item.Status = DownloadStatus.Done;
        }
        catch (OperationCanceledException)
        {
            item.Status = DownloadStatus.Canceled;
        }
        catch (YtDlpException ex)
        {
            item.Error = ex.Message;
            item.Status = DownloadStatus.Failed;
        }
        catch (IOException ex)
        {
            item.Error = ex.Message;
            item.Status = DownloadStatus.Failed;
        }
        finally
        {
            item.Cancellation.Dispose();
            item.Cancellation = null;
            ItemFinished?.Invoke(this, item);
        }
    }
}
