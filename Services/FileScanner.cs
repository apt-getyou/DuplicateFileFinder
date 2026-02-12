using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DuplicateFileFinder.Models;
using Microsoft.Extensions.Logging;

namespace DuplicateFileFinder.Services
{
    /// <summary>
    /// 文件扫描服务
    /// </summary>
    public class FileScanner
    {
        private readonly ILogger<FileScanner> _logger;
        private CancellationTokenSource? _cts;

        public event EventHandler<ScanProgressEventArgs>? ProgressChanged;

        public FileScanner(ILogger<FileScanner> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 扫描重复文件
        /// </summary>
        public async Task<ScanResult> ScanAsync(ScanConfig config, CancellationToken cancellationToken = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var result = new ScanResult
            {
                StartTime = DateTime.Now,
                Status = ScanStatus.Scanning,
                StatusMessage = "正在扫描文件..."
            };

            try
            {
                // 阶段1：收集文件
                var files = await CollectFilesAsync(config, result, _cts.Token);

                if (_cts.Token.IsCancellationRequested)
                {
                    result.Status = ScanStatus.Cancelled;
                    result.StatusMessage = "扫描已取消";
                    return result;
                }

                // 阶段2：按大小分组（快速预筛选）
                result.Status = ScanStatus.Hashing;
                result.StatusMessage = $"正在计算哈希值... (0/{files.Count})";

                var sizeGroups = files.GroupBy(f => f.Size)
                                      .Where(g => g.Count() > 1 && g.Key > 0)
                                      .ToDictionary(g => g.Key, g => g.ToList());

                _logger.LogInformation($"找到 {sizeGroups.Count} 个可能重复的文件组");

                // 阶段3：计算哈希并识别真正的重复
                var duplicateGroups = new List<FileDuplicateGroup>();
                int processedCount = 0;

                foreach (var sizeGroup in sizeGroups.Values)
                {
                    if (_cts.Token.IsCancellationRequested)
                        break;

                    // 计算该组文件的哈希
                    var hashGroups = new ConcurrentDictionary<string, List<FileItem>>();

                    await Task.WhenAll(sizeGroup.Select(async file =>
                    {
                        try
                        {
                            var hash = await ComputeFileHashAsync(file.FullPath, _cts.Token);

                            if (!string.IsNullOrEmpty(hash))
                            {
                                hashGroups.AddOrUpdate(
                                    hash,
                                    new List<FileItem> { file },
                                    (key, list) =>
                                    {
                                        lock (list) list.Add(file);
                                        return list;
                                    });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"计算哈希失败: {file.FullPath}");
                            result.Errors.Add($"计算哈希失败: {file.FullPath} - {ex.Message}");
                        }
                    }));

                    // 添加到结果中
                    foreach (var hashGroup in hashGroups.Where(g => g.Value.Count > 1))
                    {
                        var firstFile = hashGroup.Value.First();
                        duplicateGroups.Add(new FileDuplicateGroup
                        {
                            Hash = hashGroup.Key,
                            Size = firstFile.Size,
                            Extension = Path.GetExtension(firstFile.FileName),
                            Files = hashGroup.Value
                        });
                    }

                    processedCount += sizeGroup.Count;
                    var progress = (int)((double)processedCount / sizeGroups.Sum(g => g.Value.Count) * 100);
                    result.Progress = progress;
                    result.StatusMessage = $"正在计算哈希值... ({processedCount}/{sizeGroups.Sum(g => g.Value.Count)})";

                    OnProgressChanged(new ScanProgressEventArgs
                    {
                        Progress = progress,
                        Message = result.StatusMessage
                    });
                }

                // 统计结果
                result.Groups = duplicateGroups.OrderByDescending(g => g.WastedSpace).ThenByDescending(g => g.Size).ToList();
                result.DuplicateGroups = result.Groups.Count;
                result.DuplicateFiles = result.Groups.Sum(g => g.DuplicateCount);
                result.WastedSpace = result.Groups.Sum(g => g.WastedSpace);

                result.EndTime = DateTime.Now;
                result.Status = ScanStatus.Completed;
                result.StatusMessage = $"扫描完成！发现 {result.DuplicateGroups} 组重复文件";

                _logger.LogInformation($"扫描完成: {result.TotalFiles} 个文件, {result.DuplicateFiles} 个重复, 浪费 {FormatBytes(result.WastedSpace)}");
            }
            catch (OperationCanceledException)
            {
                result.Status = ScanStatus.Cancelled;
                result.StatusMessage = "扫描已取消";
                _logger.LogInformation("扫描已取消");
            }
            catch (Exception ex)
            {
                result.Status = ScanStatus.Error;
                result.StatusMessage = $"扫描出错: {ex.Message}";
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, "扫描失败");
            }

            return result;
        }

        /// <summary>
        /// 收集文件
        /// </summary>
        private async Task<List<FileItem>> CollectFilesAsync(ScanConfig config, ScanResult result, CancellationToken cancellationToken)
        {
            var files = new ConcurrentBag<FileItem>();
            var totalDirs = 0;

            await Task.Run(() =>
            {
                foreach (var scanPath in config.ScanPaths)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        _logger.LogInformation($"开始扫描: {scanPath}");

                        // 并行扫描子目录
                        var dirs = Directory.GetDirectories(scanPath, "*", SearchOption.AllDirectories);
                        totalDirs += dirs.Length + 1;

                        Parallel.ForEach(new[] { scanPath }.Concat(dirs), new ParallelOptions
                        {
                            CancellationToken = cancellationToken,
                            MaxDegreeOfParallelism = Environment.ProcessorCount
                        }, dir =>
                        {
                            try
                            {
                                // 检查是否应该排除此目录
                                var dirName = new DirectoryInfo(dir).Name;
                                if (config.ExcludeDirectories.Any(excl =>
                                    dirName.Contains(excl, StringComparison.OrdinalIgnoreCase)))
                                {
                                    _logger.LogDebug($"跳过目录: {dir}");
                                    return;
                                }

                                // 获取目录中的文件
                                var dirFiles = Directory.GetFiles(dir);
                                foreach (var filePath in dirFiles)
                                {
                                    try
                                    {
                                        var fileInfo = new FileInfo(filePath);

                                        // 检查是否应该跳过此文件
                                        if (!ShouldIncludeFile(fileInfo, config))
                                        {
                                            Interlocked.Increment(ref result.SkippedFiles);
                                            Interlocked.Add(ref result.SkippedSize, fileInfo.Length);
                                            continue;
                                        }

                                        var fileItem = new FileItem
                                        {
                                            FullPath = fileInfo.FullName,
                                            FileName = fileInfo.Name,
                                            Directory = fileInfo.DirectoryName ?? "",
                                            Size = fileInfo.Length,
                                            LastModified = fileInfo.LastWriteTime,
                                            Created = fileInfo.CreationTime
                                        };

                                        files.Add(fileItem);
                                        Interlocked.Increment(ref result.TotalFiles);
                                        Interlocked.Add(ref result.TotalSize, fileInfo.Length);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning($"跳过文件: {filePath} - {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"跳过目录: {dir} - {ex.Message}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"扫描路径失败: {scanPath}");
                        result.Errors.Add($"扫描路径失败: {scanPath} - {ex.Message}");
                    }
                }

                result.TotalDirectories = totalDirs;
                _logger.LogInformation($"文件收集完成: {files.Count} 个文件");
            }, cancellationToken);

            return files.ToList();
        }

        /// <summary>
        /// 检查是否应该包含此文件
        /// </summary>
        private bool ShouldIncludeFile(FileInfo fileInfo, ScanConfig config)
        {
            // 检查隐藏文件
            if (!config.IncludeHiddenFiles && (fileInfo.Attributes & FileAttributes.Hidden) != 0)
                return false;

            // 检查系统文件
            if (!config.IncludeSystemFiles && (fileInfo.Attributes & FileAttributes.System) != 0)
                return false;

            // 检查文件大小
            if (config.MinFileSize > 0 && fileInfo.Length < config.MinFileSize)
                return false;

            if (config.MaxFileSize > 0 && fileInfo.Length > config.MaxFileSize)
                return false;

            var extension = fileInfo.Extension.ToLowerInvariant();

            // 检查排除扩展名
            if (config.ExcludeExtensions.Any(excl =>
                extension.Equals(excl.ToLowerInvariant())))
                return false;

            // 检查包含扩展名
            if (config.IncludeExtensions.Count > 0 &&
                !config.IncludeExtensions.Any(inc =>
                    extension.Equals(inc.ToLowerInvariant())))
                return false;

            return true;
        }

        /// <summary>
        /// 计算文件哈希（SHA-256）
        /// </summary>
        private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                using var sha256 = SHA256.Create();
                using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 8192, // 8KB 缓冲区
                    options: FileOptions.SequentialScan);

                var hash = sha256.ComputeHash(stream);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }, cancellationToken);
        }

        /// <summary>
        /// 取消扫描
        /// </summary>
        public void Cancel()
        {
            _cts?.Cancel();
        }

        protected virtual void OnProgressChanged(ScanProgressEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
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
    }

    /// <summary>
    /// 扫描进度事件参数
    /// </summary>
    public class ScanProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
// Trigger rebuild
