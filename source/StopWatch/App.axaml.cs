using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Threading;
using System.Globalization;

namespace StopWatch;

public partial class App : Application
{
    private static Mutex mutex = new Mutex(true, "{D5597999-20FE-430F-8E5D-8893EBED2599}");
    private static string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jirastopwatch.log");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize localization culture from settings
        Localization.Localizer.InitializeCulture();
        
        // Set thread cultures
        Thread.CurrentThread.CurrentUICulture = Localization.Localizer.Culture;
        Thread.CurrentThread.CurrentCulture = Localization.Localizer.Culture;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                desktop.MainWindow = new MainWindow();
                desktop.Exit += (s, e) => mutex.ReleaseMutex();
            }
            else
            {
                // For single instance, on macOS we might need to handle differently
                // For now, just exit
                desktop.Shutdown();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Add exception handling here if needed

    private void TrayIcon_Clicked(object sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            desktop.MainWindow.Show();
            desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            desktop.MainWindow.Activate();
        }
    }

    private void TrayIcon_Exit(object sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}