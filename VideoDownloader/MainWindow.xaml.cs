using System.ComponentModel;
using System.Windows;
using VideoDownloader.ViewModels;

namespace VideoDownloader;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.UnicodeText) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm || e.Data.GetData(DataFormats.UnicodeText) is not string text)
            return;

        vm.Url = text.Trim();
        if (vm.AnalyzeCommand.CanExecute(null))
            vm.AnalyzeCommand.Execute(null);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Shutdown();
    }
}
