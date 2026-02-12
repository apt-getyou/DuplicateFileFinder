#!/bin/bash

# 重复文件扫描器 - 编译脚本

echo "========================================="
echo "  重复文件扫描器 - 编译脚本"
echo "========================================="
echo ""

# 检查 .NET 8.0 是否安装
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未找到 .NET SDK"
    echo "请从以下地址安装 .NET 8.0 SDK:"
    echo "https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

echo "🔍 检测 .NET 版本..."
DOTNET_VERSION=$(dotnet --version | cut -d. -f1,2)
echo "当前版本: $DOTNET_VERSION"

echo ""
echo "📦 还原依赖..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "❌ 依赖还原失败"
    exit 1
fi

echo ""
echo "🔨 编译项目..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ 编译失败"
    exit 1
fi

echo ""
echo "✅ 编译成功！"
echo ""
echo "📂 可执行文件位置:"
echo "   bin/Release/net8.0-windows/DuplicateFileFinder.exe"
echo ""
echo "💡 提示: 如需打包为单文件，运行:"
echo "   ./publish.sh"
