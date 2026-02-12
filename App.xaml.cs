using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DuplicateFileFinder.Services;
using DuplicateFileFinder.ViewModels;
using DuplicateFileFinder.Views;
using DuplicateFileFinder.Converters;

namespace DuplicateFileFinder
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 配置依赖注入
            var services = new ServiceCollection();

            // 添加日志
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // 添加服务
            services.AddSingleton<FileScanner>();
            services.AddSingleton<FileOperationService>();

            // 添加 ViewModel
            services.AddSingleton<MainViewModel>();

            // 添加窗口
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            // 显示主窗口
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
