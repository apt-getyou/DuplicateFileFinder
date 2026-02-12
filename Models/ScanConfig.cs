using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DuplicateFileFinder.Models
{
    /// <summary>
    /// 扫描配置
    /// </summary>
    public class ScanConfig
    {
        /// <summary>
        /// 扫描路径列表
        /// </summary>
        public List<string> ScanPaths { get; set; } = new();

        /// <summary>
        /// 包含的扩展名（空列表=全部）
        /// </summary>
        public List<string> IncludeExtensions { get; set; } = new();

        /// <summary>
        /// 排除的扩展名
        /// </summary>
        public List<string> ExcludeExtensions { get; set; } = new();

        /// <summary>
        /// 排除的目录名（支持部分匹配）
        /// </summary>
        public List<string> ExcludeDirectories { get; set; } = new()
        {
            "$Recycle.Bin",
            "$RECYCLE.BIN",
            "System Volume Information",
            "Windows",
            "Program Files",
            "Program Files (x86)",
            "ProgramData",
            "node_modules",
            ".git",
            "venv",
            ".venv",
            "env",
            "__pycache__",
            ".vs",
            "bin",
            "obj",
            "debug",
            "release",
            ".idea",
            ".vscode"
        };

        /// <summary>
        /// 最小文件大小（字节，0=无限制）
        /// </summary>
        public long MinFileSize { get; set; } = 0;

        /// <summary>
        /// 最大文件大小（字节，0=无限制）
        /// </summary>
        public long MaxFileSize { get; set; } = 0;

        /// <summary>
        /// 是否包含隐藏文件
        /// </summary>
        public bool IncludeHiddenFiles { get; set; } = false;

        /// <summary>
        /// 是否包含系统文件
        /// </summary>
        public bool IncludeSystemFiles { get; set; } = false;

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void Save(string filePath)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static ScanConfig Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new ScanConfig();
            }

            var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            return JsonConvert.DeserializeObject<ScanConfig>(json) ?? new ScanConfig();
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        public (bool IsValid, List<string> Errors) Validate()
        {
            var errors = new List<string>();

            if (ScanPaths.Count == 0)
            {
                errors.Add("请至少选择一个扫描路径");
            }

            foreach (var path in ScanPaths)
            {
                if (!Directory.Exists(path))
                {
                    errors.Add($"路径不存在: {path}");
                }
            }

            return (errors.Count == 0, errors);
        }
    }
}
