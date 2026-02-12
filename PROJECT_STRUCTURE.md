# 项目结构

```
DuplicateFileFinder/
├── Models/                          # 数据模型
│   ├── FileDuplicateGroup.cs       # 重复文件组模型
│   ├── FileItem.cs                 # 文件项模型（在 FileDuplicateGroup.cs 中）
│   ├── ScanConfig.cs               # 扫描配置模型
│   └── ScanResult.cs               # 扫描结果模型
│
├── Services/                        # 业务服务
│   ├── FileScanner.cs              # 文件扫描服务
│   └── FileOperationService.cs     # 文件操作服务
│
├── ViewModels/                      # 视图模型
│   └── MainViewModel.cs            # 主窗口视图模型
│
├── Views/                           # 视图
│   ├── MainWindow.xaml             # 主窗口 UI 定义
│   └── MainWindow.xaml.cs          # 主窗口代码后置
│
├── Converters/                      # 值转换器
│   └── ValueConverters.cs          # 数据绑定转换器
│
├── App.xaml                         # 应用程序资源
├── App.xaml.cs                      # 应用程序入口
├── DuplicateFileFinder.csproj      # 项目文件
│
├── README.md                        # 项目说明
├── build.sh                         # 编译脚本
├── publish.sh                       # 发布脚本
└── .gitignore                       # Git 忽略文件

```

## 核心组件说明

### Models 层

- **FileDuplicateGroup**: 表示一组重复文件，包含哈希值、大小、扩展名和文件列表
- **FileItem**: 单个文件的信息，包含路径、大小、时间戳等
- **ScanConfig**: 扫描配置，包含扫描路径、过滤规则等
- **ScanResult**: 扫描结果，包含统计数据和重复文件组列表

### Services 层

- **FileScanner**: 核心扫描服务
  - 并行文件收集
  - 大小预筛选
  - SHA-256 哈希计算
  - 实时进度报告

- **FileOperationService**: 文件操作服务
  - 删除文件（支持回收站）
  - 移动文件
  - 导出报告（TXT/JSON/CSV）

### ViewModels 层

- **MainViewModel**: 主窗口视图模型
  - 绑定扫描配置
  - 管理扫描流程
  - 处理用户操作
  - 提供 UI 命令

### Views 层

- **MainWindow**: 主窗口界面
  - 扫描配置面板
  - 进度显示
  - 重复文件列表
  - 操作按钮

## 技术栈

- **.NET 8.0**: 最新 .NET 框架
- **WPF**: Windows Presentation Foundation
- **MVVM**: Model-View-ViewModel 架构模式
- **DI**: Microsoft.Extensions.DependencyInjection
- **Logging**: Microsoft.Extensions.Logging

## 数据流

```
用户操作 (MainWindow)
    ↓
ViewModel (MainViewModel)
    ↓
Service (FileScanner)
    ↓
扫描结果 (ScanResult)
    ↓
ViewModel 更新
    ↓
UI 自动刷新
```

## 扫描流程

```
1. 用户配置扫描路径和过滤规则
        ↓
2. 点击"开始扫描"按钮
        ↓
3. FileScanner 收集文件（并行）
        ↓
4. 按文件大小预筛选
        ↓
5. 计算 SHA-256 哈希（并行）
        ↓
6. 生成重复文件组
        ↓
7. 返回 ScanResult
        ↓
8. UI 显示结果
        ↓
9. 用户选择操作（删除/移动/导出）
```

## 性能优化

1. **并行扫描**: 使用 Parallel.ForEach 并行处理目录
2. **大小预筛选**: 只计算大小相同的文件的哈希
3. **异步哈希**: 使用 Task.WhenAll 并行计算哈希
4. **流式计算**: 使用 FileStream 直接计算哈希，不加载全部内容
5. **进度报告**: 实时更新进度，避免 UI 冻结

## 安全特性

1. **回收站删除**: 默认移至回收站，可恢复
2. **操作确认**: 所有危险操作都有确认对话框
3. **默认排除**: 预设排除系统目录和代码仓库
4. **错误处理**: 完善的异常捕获和错误报告
