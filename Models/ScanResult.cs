using System.Collections.Generic;

namespace DuplicateFileFinder.Models
{
    /// <summary>
    /// 扫描结果
    /// </summary>
    public class ScanResult
    {
        /// <summary>
        /// 扫描开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 扫描结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 扫描的文件总数
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// 扫描的总大小（字节）
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// 扫描的目录总数
        /// </summary>
        public int TotalDirectories { get; set; }

        /// <summary>
        /// 跳过的文件数
        /// </summary>
        public int SkippedFiles { get; set; }

        /// <summary>
        /// 跳过的总大小（字节）
        /// </summary>
        public long SkippedSize { get; set; }

        /// <summary>
        /// 发现的重复组数
        /// </summary>
        public int DuplicateGroups { get; set; }

        /// <summary>
        /// 重复文件总数
        /// </summary>
        public int DuplicateFiles { get; set; }

        /// <summary>
        /// 浪费的空间（字节）
        /// </summary>
        public long WastedSpace { get; set; }

        /// <summary>
        /// 重复文件组列表
        /// </summary>
        public List<FileDuplicateGroup> Groups { get; set; } = new();

        /// <summary>
        /// 扫描用时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds => (long)(EndTime - StartTime).TotalMilliseconds;

        /// <summary>
        /// 扫描状态
        /// </summary>
        public ScanStatus Status { get; set; } = ScanStatus.Idle;

        /// <summary>
        /// 当前进度（0-100）
        /// </summary>
        public int Progress { get; set; } = 0;

        /// <summary>
        /// 当前状态消息
        /// </summary>
        public string StatusMessage { get; set; } = "准备就绪";

        /// <summary>
        /// 错误消息
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// 扫描状态
    /// </summary>
    public enum ScanStatus
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Idle,

        /// <summary>
        /// 扫描中
        /// </summary>
        Scanning,

        /// <summary>
        /// 计算哈希
        /// </summary>
        Hashing,

        /// <summary>
        /// 完成
        /// </summary>
        Completed,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled,

        /// <summary>
        /// 错误
        /// </summary>
        Error
    }
}
