using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

public sealed class YtDlpException : Exception
{
    public YtDlpException(string message) : base(message) { }
}

public sealed class YtDlpService
{
    private readonly string _exePath;
    private readonly string _ffmpegPath;

    public YtDlpService(string toolsFolder)
    {
        _exePath = Path.Combine(toolsFolder, "yt-dlp.exe");
        _ffmpegPath = Path.Combine(toolsFolder, "ffmpeg.exe");
    }

    public bool IsInstalled => File.Exists(_exePath);
    public bool HasFfmpeg => File.Exists(_ffmpegPath);

    public async Task<VideoInfo> GetInfoAsync(string url, bool wholePlaylist, CancellationToken ct)
    {
        var result = await RunAsync(YtDlpArguments.ForInfo(url, wholePlaylist), null, ct);
        if (result.ExitCode != 0)
            throw new YtDlpException(ReadError(result.Error));

        return VideoInfoParser.Parse(result.Output);
    }

    public async Task<string?> DownloadAsync(
        DownloadItem item,
        AppSettings settings,
        IProgress<DownloadProgress> progress,
        CancellationToken ct)
    {
        string? filePath = null;

        void OnLine(string line)
        {
            if (ProgressParser.TryParse(line, out var p))
                progress.Report(p);
            else if (ProgressParser.IsPostProcessing(line))
                progress.Report(new DownloadProgress(100, "", "", DownloadStatus.Processing));
            else if (ProgressParser.TryGetFilePath(line, out var path))
                filePath = path;
        }

        var args = YtDlpArguments.ForDownload(item, settings, _ffmpegPath);
        var result = await RunAsync(args, OnLine, ct);
        if (result.ExitCode != 0)
            throw new YtDlpException(ReadError(result.Error));

        return filePath;
    }

    public async Task<string> UpdateAsync(CancellationToken ct)
    {
        var result = await RunAsync(YtDlpArguments.ForUpdate(), null, ct);
        var text = result.ExitCode == 0 ? result.Output : result.Error;
        return LastLine(text) ?? "Done";
    }

    private async Task<ProcessResult> RunAsync(IEnumerable<string> args, Action<string>? onLine, CancellationToken ct)
    {
        if (!IsInstalled)
            throw new YtDlpException("yt-dlp.exe was not found in the tools folder");

        var startInfo = new ProcessStartInfo(_exePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            if (onLine is null)
                output.AppendLine(e.Data);
            else
                onLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                error.AppendLine(e.Data);
        };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new YtDlpException(ex.Message);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            throw;
        }

        return new ProcessResult(process.ExitCode, output.ToString(), error.ToString());
    }

    private static string ReadError(string stderr)
    {
        var lines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var error = lines.LastOrDefault(l => l.StartsWith("ERROR:", StringComparison.Ordinal));
        if (error is not null)
            return error["ERROR:".Length..].Trim();
        return lines.LastOrDefault() ?? "yt-dlp stopped with an error";
    }

    private static string? LastLine(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
