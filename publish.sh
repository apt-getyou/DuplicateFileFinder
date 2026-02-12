#!/bin/bash

# 重复文件扫描器 - 发布脚本

echo "========================================="
echo "  重复文件扫描器 - 发布脚本"
echo "========================================="
echo ""

# 检查 .NET 8.0 是否安装
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未找到 .NET SDK"
    echo "请从以下地址安装 .NET 8.0 SDK:"
    echo "https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

echo "📦 发布为单文件可执行程序..."
echo ""

# 清理之前的发布
rm -rf publish/

# 发布为 Windows x64 单文件
dotnet publish \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o publish/

if [ $? -ne 0 ]; then
    echo "❌ 发布失败"
    exit 1
fi

echo ""
echo "✅ 发布成功！"
echo ""
echo "📂 可执行文件位置:"
echo "   publish/DuplicateFileFinder.exe"
echo ""
echo "📊 文件大小:"
ls -lh publish/DuplicateFileFinder.exe | awk '{print "   " $5}'
echo ""
echo "🎉 现在可以将 publish/ 目录复制到 Windows 电脑上运行！"
