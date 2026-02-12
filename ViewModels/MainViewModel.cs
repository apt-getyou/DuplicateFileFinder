using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DuplicateFileFinder.Models;
using DuplicateFileFinder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DuplicateFileFinder.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly FileScanner _scanner;
        private readonly FileOperationService _operationService;
        private readonly ILogger<MainViewModel> _logger;

        private ScanConfig _config;
        private ScanResult? _currentResult;
        private bool _isScanning;
        private int _progress;
        private string _statusMessage = "准备就绪";
        private FileDuplicateGroup? _selectedGroup;

        public MainViewModel(
            FileScanner scanner,
            FileOperationService operationService,
            ILogger<MainViewModel> logger)
        {
            _scanner = scanner;
            _operationService = operationService;
            _logger = logger;

            _config = ScanConfig.Load(GetConfigPath());
            DuplicateGroups = new ObservableCollection<FileDuplicateGroup>();

            // 初始化命令
            AddScanPathCommand = new RelayCommand(AddScanPath);
            RemoveScanPathCommand = new RelayCommand<string>(RemoveScanPath, CanRemoveScanPath);
            BrowseScanPathCommand = new RelayCommand(BrowseScanPath);
            StartScanCommand = new RelayCommand(StartScan, CanStartScan);
            CancelScanCommand = new RelayCommand(CancelScan, CanCancelScan);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, CanDeleteSelected);
            MoveSelectedCommand = new RelayCommand(MoveSelected, CanDeleteSelected);
            ExportReportCommand = new RelayCommand(ExportReport, CanExportReport);
            SaveConfigCommand = new RelayCommand(SaveConfig);
            LoadConfigCommand = new RelayCommand(LoadConfig);

            // 订阅扫描进度
            _scanner.ProgressChanged += (s, e) =>
            {
                Progress = e.Progress;
                StatusMessage = e.Message;
            };
        }

        // ==================== 属性 ====================

        public ScanConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged();
            }
        }

        public ScanResult? CurrentResult
        {
            get => _currentResult;
            set
            {
                _currentResult = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalFilesText));
                OnPropertyChanged(nameof(DuplicateFilesText));
                OnPropertyChanged(nameof(WastedSpaceText));
                OnPropertyChanged(nameof(ElapsedTimeText));
            }
        }

        public ObservableCollection<FileDuplicateGroup> DuplicateGroups { get; }

        public FileDuplicateGroup? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged();
            }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                _isScanning = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public string TotalFilesText => CurrentResult != null
            ? $"{CurrentResult.TotalFiles:N0} 个文件"
            : "-";

        public string DuplicateFilesText => CurrentResult != null
            ? $"{CurrentResult.DuplicateFiles:N0} 个重复"
            : "-";

        public string WastedSpaceText => CurrentResult != null
            ? FormatBytes(CurrentResult.WastedSpace)
            : "-";

        public string ElapsedTimeText => CurrentResult != null
            ? TimeSpan.FromMilliseconds(CurrentResult.ElapsedMilliseconds).ToString(@"hh\:mm\:ss")
            : "-";

        // ==================== 命令 ====================

        public ICommand AddScanPathCommand { get; }
        public ICommand RemoveScanPathCommand { get; }
        public ICommand BrowseScanPathCommand { get; }
        public ICommand StartScanCommand { get; }
        public ICommand CancelScanCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand MoveSelectedCommand { get; }
        public ICommand ExportReportCommand { get; }
        public ICommand SaveConfigCommand { get; }
        public ICommand LoadConfigCommand { get; }

        // ==================== 方法 ====================

        private void AddScanPath()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择要扫描的文件夹",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!Config.ScanPaths.Contains(dialog.SelectedPath))
                {
                    Config.ScanPaths.Add(dialog.SelectedPath);
                    OnPropertyChanged(nameof(Config));
                }
            }
        }

        private bool CanRemoveScanPath(string? path)
        {
            return !string.IsNullOrEmpty(path) && !IsScanning;
        }

        private void RemoveScanPath(string? path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Config.ScanPaths.Remove(path);
                OnPropertyChanged(nameof(Config));
            }
        }

        private void BrowseScanPath()
        {
            AddScanPath();
        }

        private bool CanStartScan()
        {
            return !IsScanning && Config.ScanPaths.Count > 0;
        }

        private async void StartScan()
        {
            // 验证配置
            var (isValid, errors) = Config.Validate();
            if (!isValid)
            {
                MessageBox.Show(
                    string.Join("\n", errors),
                    "配置错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsScanning = true;
            Progress = 0;
            DuplicateGroups.Clear();
            CurrentResult = null;

            try
            {
                _logger.LogInformation("开始扫描...");
                var result = await _scanner.ScanAsync(Config);

                CurrentResult = result;

                if (result.Status == ScanStatus.Completed)
                {
                    foreach (var group in result.Groups)
                    {
                        DuplicateGroups.Add(group);
                    }

                    StatusMessage = $"扫描完成！发现 {result.DuplicateGroups} 组重复文件";
                    MessageBox.Show(
                        $"扫描完成！\n\n发现 {result.DuplicateGroups} 组重复文件\n浪费空间: {FormatBytes(result.WastedSpace)}",
                        "扫描完成",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (result.Status == ScanStatus.Cancelled)
                {
                    StatusMessage = "扫描已取消";
                }
                else
                {
                    StatusMessage = $"扫描出错: {result.StatusMessage}";
                    MessageBox.Show(
                        result.StatusMessage,
                        "扫描错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描失败");
                StatusMessage = $"扫描失败: {ex.Message}";
                MessageBox.Show(
                    $"扫描失败: {ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsScanning = false;
            }
        }

        private bool CanCancelScan()
        {
            return IsScanning;
        }

        private void CancelScan()
        {
            _scanner.Cancel();
            StatusMessage = "正在取消扫描...";
        }

        private bool CanDeleteSelected()
        {
            return !IsScanning && SelectedGroup != null;
        }

        private void DeleteSelected()
        {
            if (SelectedGroup == null)
                return;

            var result = MessageBox.Show(
                $"确定要删除选中的 {SelectedGroup.Files.Count(f => f.IsSelected)} 个文件吗？\n\n这些文件将被移至回收站。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var operationResult = _operationService.DeleteFiles(SelectedGroup, f => f.IsSelected);

                MessageBox.Show(
                    $"操作完成！\n\n已删除: {operationResult.DeletedCount} 个\n跳过: {operationResult.SkippedCount} 个\n错误: {operationResult.ErrorCount} 个\n释放空间: {FormatBytes(operationResult.FreedSpace)}",
                    "删除完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // 刷新列表
                DuplicateGroups.Remove(SelectedGroup);
            }
        }

        private void MoveSelected()
        {
            if (SelectedGroup == null)
                return;

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择目标文件夹",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var operationResult = _operationService.MoveFiles(SelectedGroup, f => f.IsSelected, dialog.SelectedPath);

                MessageBox.Show(
                    $"操作完成！\n\n已移动: {operationResult.MovedCount} 个\n跳过: {operationResult.SkippedCount} 个\n错误: {operationResult.ErrorCount} 个\n释放空间: {FormatBytes(operationResult.FreedSpace)}",
                    "移动完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // 刷新列表
                DuplicateGroups.Remove(SelectedGroup);
            }
        }

        private bool CanExportReport()
        {
            return CurrentResult != null && CurrentResult.Status == ScanStatus.Completed;
        }

        private void ExportReport()
        {
            if (CurrentResult == null)
                return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "文本文件|*.txt|JSON 文件|*.json|CSV 文件|*.csv",
                DefaultExt = "txt",
                FileName = $"重复文件报告_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var format = dialog.FilterIndex switch
                    {
                        1 => ReportFormat.Text,
                        2 => ReportFormat.Json,
                        3 => ReportFormat.Csv,
                        _ => ReportFormat.Text
                    };

                    _operationService.ExportReport(CurrentResult, dialog.FileName, format);
                    MessageBox.Show(
                        $"报告已导出到:\n{dialog.FileName}",
                        "导出成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"导出失败: {ex.Message}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void SaveConfig()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "配置文件|*.json",
                DefaultExt = "json",
                FileName = $"扫描配置_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Config.Save(dialog.FileName);
                    MessageBox.Show(
                        $"配置已保存到:\n{dialog.FileName}",
                        "保存成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"保存失败: {ex.Message}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void LoadConfig()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "配置文件|*.json",
                Title = "选择配置文件"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Config = ScanConfig.Load(dialog.FileName);
                    MessageBox.Show(
                        $"配置已加载:\n{dialog.FileName}",
                        "加载成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"加载失败: {ex.Message}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private string GetConfigPath()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DuplicateFileFinder");

            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);

            return Path.Combine(appData, "config.json");
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 简单的 RelayCommand 实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
