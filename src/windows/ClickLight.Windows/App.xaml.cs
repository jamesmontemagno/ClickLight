using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ClickLight.Windows;

public sealed class App : Microsoft.UI.Xaml.Application
{
    private MainWindow? _mainWindow;
    private Services.AppController? _controller;

    public App()
    {
        UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("A UI dispatcher queue is required.");

        _controller = new Services.AppController(_mainWindow, dispatcherQueue);
        _controller.Start();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _controller?.Dispose();
    }
}
