using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WinOptimizer.Optimizations;
using WinOptimizer.Optimizations.Bloatware;
using WinOptimizer.Optimizations.Cleanup;
using WinOptimizer.Optimizations.Network;
using WinOptimizer.Optimizations.Performance;
using WinOptimizer.Optimizations.Privacy;
using WinOptimizer.Optimizations.Win11;
using WinOptimizer.Services;
using WinOptimizer.ViewModels;
using WinOptimizer.Optimizations.Bloatware;
using WinOptimizer.Optimizations.Startup;
using WinOptimizer.Optimizations.Display;
using WinOptimizer.Optimizations.Startup;

namespace WinOptimizer;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Servisler
        services.AddSingleton<SystemScanner>();
        services.AddSingleton<RestorePointManager>();
        services.AddSingleton<LoggingService>();

        // Performans
        services.AddTransient<IOptimization, PowerPlanOptimization>();
        services.AddTransient<IOptimization, VisualEffectsOptimization>();
        services.AddTransient<IOptimization, GameModeOptimization>();

        // Gizlilik
        services.AddTransient<IOptimization, TelemetryOptimization>();
        services.AddTransient<IOptimization, CortanaOptimization>();

        // Ağ
        services.AddTransient<IOptimization, NagleOptimization>();

        // Disk
        services.AddTransient<IOptimization, TempFilesOptimization>();

        // Windows 11
        services.AddTransient<IOptimization, ClassicContextMenuOptimization>();
        services.AddTransient<IOptimization, TaskbarAlignOptimization>();

        // ViewModel
        services.AddSingleton<MainViewModel>();

        // Bloatware
        services.AddTransient<IOptimization, AppxRemover>();
        services.AddTransient<IOptimization, OneDriveRemover>();

        // Başlangıç
        services.AddTransient<IOptimization, StartupManagerOptimization>();

        // Ekran
        services.AddTransient<IOptimization, RefreshRateOptimization>();

        
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Kaydedilmiş dil ayarını yükle
        LanguageManager.LoadSettings();

        var vm = _serviceProvider.GetRequiredService<MainViewModel>();
        var window = new MainWindow(vm);
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "WinOptimizer");
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, recursive: true);
        }
        catch { }

        _serviceProvider.Dispose();
        base.OnExit(e);
    }

    public static T GetService<T>() where T : class
    {
        var app = (App)Current;
        return app._serviceProvider.GetRequiredService<T>();
    }
}