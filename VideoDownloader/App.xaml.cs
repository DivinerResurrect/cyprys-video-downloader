using System.Windows;
using System.Windows.Threading;
using VideoDownloader.Services;
using VideoDownloader.ViewModels;

namespace VideoDownloader;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        var baseFolder = AppContext.BaseDirectory;
        var ytDlp = new YtDlpService(System.IO.Path.Combine(baseFolder, "tools"));
        var settings = new SettingsService(baseFolder);

        var window = new MainWindow { DataContext = new MainViewModel(ytDlp, settings) };
        MainWindow = window;
        window.Show();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "Video Downloader", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
