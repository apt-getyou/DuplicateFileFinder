using System;
using System.IO;
using System.Linq;
using DuplicateFileFinder.Models;
using Microsoft.Extensions.Logging;

namespace DuplicateFileFinder.Services
{
    /// <summary>
    /// 文件操作服务
    /// </summary>
    public class FileOperationService
    {
        private readonly ILogger<FileOperationService> _logger;

        public FileOperationService(ILogger<FileOperationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 删除选中的文件
        /// </summary>
        public FileOperationResult DeleteFiles(FileDuplicateGroup group, Func<FileItem, bool> selector)
        {
            var result = new FileOperationResult();
            var filesToDelete = group.Files.Where(selector).ToList();

            foreach (var file in filesToDelete)
            {
                try
                {
                    if (File.Exists(file.FullPath))
                    {
                        // 先移动到回收站（软删除）
                        if (!MoveToRecycleBin(file.FullPath))
                        {
                            // 如果回收站失败，永久删除
                            File.Delete(file.FullPath);
                            result.DeletedCount++;
                            result.FreedSpace += file.Size;
                            _logger.LogInformation($"已删除: {file.FullPath}");
                        }
                        else
                        {
                            result.DeletedCount++;
                            result.FreedSpace += file.Size;
                            _logger.LogInformation($"已移至回收站: {file.FullPath}");
                        }
                    }
                    else
                    {
                        result.SkippedCount++;
                        result.Errors.Add($"文件不存在: {file.FullPath}");
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"删除失败: {file.FullPath} - {ex.Message}");
                    _logger.LogError(ex, $"删除失败: {file.FullPath}");
                }
            }

            return result;
        }

        /// <summary>
        /// 移动文件到指定目录
        /// </summary>
        public FileOperationResult MoveFiles(FileDuplicateGroup group, Func<FileItem, bool> selector, string targetDirectory)
        {
            var result = new FileOperationResult();
            var filesToMove = group.Files.Where(selector).ToList();

            // 确保目标目录存在
            if (!Directory.Exists(targetDirectory))
            {
                try
                {
                    Directory.CreateDirectory(targetDirectory);
                    _logger.LogInformation($"创建目标目录: {targetDirectory}");
                }
                catch (Exception ex)
                {
                    result.ErrorCount = filesToMove.Count;
                    result.Errors.Add($"无法创建目标目录: {targetDirectory} - {ex.Message}");
                    return result;
                }
            }

            foreach (var file in filesToMove)
            {
                try
                {
                    if (File.Exists(file.FullPath))
                    {
                        // 构建目标路径
                        var relativePath = GetRelativePath(file.FullPath);
                        var destPath = Path.Combine(targetDirectory, relativePath);
                        var destDir = Path.GetDirectoryName(destPath);

                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        // 移动文件
                        File.Move(file.FullPath, destPath, overwrite: true);
                        result.MovedCount++;
                        result.FreedSpace += file.Size;
                        _logger.LogInformation($"已移动: {file.FullPath} -> {destPath}");
                    }
                    else
                    {
                        result.SkippedCount++;
                        result.Errors.Add($"文件不存在: {file.FullPath}");
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"移动失败: {file.FullPath} - {ex.Message}");
                    _logger.LogError(ex, $"移动失败: {file.FullPath}");
                }
            }

            return result;
        }

        /// <summary>
        /// 导出报告
        /// </summary>
        public void ExportReport(ScanResult result, string filePath, ReportFormat format)
        {
            try
            {
                switch (format)
                {
                    case ReportFormat.Text:
                        ExportTextReport(result, filePath);
                        break;
                    case ReportFormat.Json:
                        ExportJsonReport(result, filePath);
                        break;
                    case ReportFormat.Csv:
                        ExportCsvReport(result, filePath);
                        break;
                }

                _logger.LogInformation($"报告已导出: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出报告失败: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// 导出文本报告
        /// </summary>
        private void ExportTextReport(ScanResult result, string filePath)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

            writer.WriteLine("═══════════════════════════════════════════════════════");
            writer.WriteLine("           重复文件扫描报告");
            writer.WriteLine("═══════════════════════════════════════════════════════");
            writer.WriteLine();
            writer.WriteLine($"📊 扫描统计");
            writer.WriteLine($"─────────────────────────────────────────────────────");
            writer.WriteLine($"扫描时间: {result.StartTime:yyyy-MM-dd HH:mm:ss} - {result.EndTime:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"用时: {TimeSpan.FromMilliseconds(result.ElapsedMilliseconds):hh\\:mm\\:ss}");
            writer.WriteLine();
            writer.WriteLine($"扫描文件总数: {result.TotalFiles:N0}");
            writer.WriteLine($"扫描文件大小: {FormatBytes(result.TotalSize)}");
            writer.WriteLine($"扫描目录总数: {result.TotalDirectories:N0}");
            writer.WriteLine($"跳过文件数: {result.SkippedFiles:N0}");
            writer.WriteLine($"跳过文件大小: {FormatBytes(result.SkippedSize)}");
            writer.WriteLine();
            writer.WriteLine($"🔍 重复文件统计");
            writer.WriteLine($"─────────────────────────────────────────────────────");
            writer.WriteLine($"发现重复组数: {result.DuplicateGroups:N0}");
            writer.WriteLine($"重复文件总数: {result.DuplicateFiles:N0}");
            writer.WriteLine($"浪费空间: {FormatBytes(result.WastedSpace)}");
            writer.WriteLine();
            writer.WriteLine($"═══════════════════════════════════════════════════════");
            writer.WriteLine($"           重复文件详情");
            writer.WriteLine($"═══════════════════════════════════════════════════════");
            writer.WriteLine();

            int groupNumber = 1;
            foreach (var group in result.Groups)
            {
                writer.WriteLine($"📦 组 {groupNumber}: {group.Extension} - {FormatBytes(group.Size)} × {group.Files.Count}");
                writer.WriteLine($"   哈希: {group.Hash.Substring(0, 16)}...");
                writer.WriteLine($"   浪费空间: {FormatBytes(group.WastedSpace)}");
                writer.WriteLine();

                foreach (var file in group.Files)
                {
                    writer.WriteLine($"   📄 {file.FullPath}");
                    writer.WriteLine($"      修改: {file.LastModified:yyyy-MM-dd HH:mm:ss}");
                }

                writer.WriteLine();
                writer.WriteLine($"─────────────────────────────────────────────────────");
                groupNumber++;
            }

            if (result.Errors.Count > 0)
            {
                writer.WriteLine();
                writer.WriteLine($"⚠️  错误信息");
                writer.WriteLine($"─────────────────────────────────────────────────────");
                foreach (var error in result.Errors)
                {
                    writer.WriteLine($"   {error}");
                }
            }
        }

        /// <summary>
        /// 导出 JSON 报告
        /// </summary>
        private void ExportJsonReport(ScanResult result, string filePath)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 导出 CSV 报告
        /// </summary>
        private void ExportCsvReport(ScanResult result, string filePath)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

            // 写入表头
            writer.WriteLine("组编号,哈希,文件大小,扩展名,浪费空间,文件路径,修改时间");

            int groupNumber = 1;
            foreach (var group in result.Groups)
            {
                foreach (var file in group.Files)
                {
                    writer.WriteLine($"{groupNumber},{group.Hash},{group.Size},{group.Extension},{group.WastedSpace},\"{file.FullPath}\",{file.LastModified:yyyy-MM-dd HH:mm:ss}");
                }

                groupNumber++;
            }
        }

        /// <summary>
        /// 移动文件到回收站
        /// </summary>
        private bool MoveToRecycleBin(string filePath)
        {
            try
            {
                // 使用 Windows API 移动到回收站
                var fileOp = new NativeMethods.SHFILEOPSTRUCT
                {
                    wFunc = NativeMethods.FO_DELETE,
                    pFrom = filePath + '\0', // 双空结尾
                    fFlags = NativeMethods.FOF_ALLOWUNDO | NativeMethods.FOF_NOCONFIRMATION | NativeMethods.FOF_SILENT
                };

                int result = NativeMethods.SHFileOperation(ref fileOp);
                return result == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取相对路径（简化版）
        /// </summary>
        private string GetRelativePath(string fullPath)
        {
            // 简单提取文件名和父目录
            var fileInfo = new FileInfo(fullPath);
            var parentDir = fileInfo.Directory?.Name ?? "root";
            return Path.Combine(parentDir, fileInfo.Name);
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
    /// 文件操作结果
    /// </summary>
    public class FileOperationResult
    {
        public int DeletedCount { get; set; }
        public int MovedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public long FreedSpace { get; set; }
        public System.Collections.Generic.List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// 报告格式
    /// </summary>
    public enum ReportFormat
    {
        Text,
        Json,
        Csv
    }

    /// <summary>
    /// Windows API 原生方法
    /// </summary>
    internal static class NativeMethods
    {
        public const int FO_DELETE = 0x0003;
        public const int FOF_ALLOWUNDO = 0x0040;
        public const int FOF_NOCONFIRMATION = 0x0010;
        public const int FOF_SILENT = 0x0004;

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public int wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public int fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }
    }
}
