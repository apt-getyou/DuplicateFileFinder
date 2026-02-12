using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace DuplicateFileFinder.Converters
{
    /// <summary>
    /// 字节数转可读字符串
    /// </summary>
    public class BytesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return FormatBytes(bytes);
            }

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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
    /// List&lt;string&gt; 转换为换行分隔的字符串
    /// </summary>
    public class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string> list)
            {
                return string.Join(Environment.NewLine, list);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return new List<string>(str.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
            }

            return new List<string>();
        }
    }
}
