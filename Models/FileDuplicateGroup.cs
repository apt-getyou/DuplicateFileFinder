using System.Collections.Generic;

namespace DuplicateFileFinder.Models
{
    /// <summary>
    /// 重复文件组
    /// </summary>
    public class FileDuplicateGroup
    {
        /// <summary>
        /// 哈希值（SHA-256）
        /// </summary>
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 文件信息列表
        /// </summary>
        public List<FileItem> Files { get; set; } = new();

        /// <summary>
        /// 冗余数量（副本数 - 1）
        /// </summary>
        public int DuplicateCount => Files.Count - 1;

        /// <summary>
        /// 浪费空间（字节）
        /// </summary>
        public long WastedSpace => Size * DuplicateCount;

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string Extension { get; set; } = string.Empty;
    }

    /// <summary>
    /// 文件项
    /// </summary>
    public class FileItem
    {
        /// <summary>
        /// 完整路径
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 目录路径
        /// </summary>
        public string Directory { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// 是否选中（用于删除操作）
        /// </summary>
        public bool IsSelected { get; set; } = false;
    }
}
