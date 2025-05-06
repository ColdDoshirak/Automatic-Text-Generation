using System;
using System.Windows;
using ConsoleApp8.Configuration;
using ConsoleApp8.Services;
using ConsoleApp8.ViewModels;
using ConsoleApp8.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace ConsoleApp8;

public partial class App : Application
{
    private ServiceProvider _serviceProvider;
    private static Mutex _singleInstanceMutex;

    public App()
    {
        _singleInstanceMutex = new Mutex(true, "ConsoleApp8_UrfuAssistantSingleInstance", out bool createdNew);
        
        if (!createdNew)
        {
            MessageBox.Show("Приложение уже запущено!", "Предупреждение", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<ApiService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start the application: {ex.Message}", "Startup Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
}