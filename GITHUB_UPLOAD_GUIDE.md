# GitHub 上传指南 🚀

## 📦 当前状态

✅ **代码已提交到本地 Git 仓库**
- 20 个文件已提交
- 3539+ 行代码
- 完整的项目文档

## 🎯 下一步操作

### 方法 1: 使用 GitHub 网页界面（最简单）

#### 步骤 1: 创建 GitHub 仓库

1. 访问 https://github.com/new
2. 填写仓库信息：
   - **仓库名称**: `DuplicateFileFinder`
   - **描述**: `一款高效的 Windows 重复文件扫描工具，基于 C# + WPF + .NET 8.0`
   - **可见性**: Public（公开）或 Private（私有）
   - **不要**勾选 "Add a README file"（我们已经有了）
   - **不要**勾选 "Add .gitignore"（我们已经有了）
3. 点击 **"Create repository"**

#### 步骤 2: 推送代码到 GitHub

创建仓库后，GitHub 会显示推送命令。在终端中运行：

```bash
# 添加远程仓库（替换 YOUR_USERNAME 为你的 GitHub 用户名）
git remote add origin https://github.com/YOUR_USERNAME/DuplicateFileFinder.git

# 推送代码到 GitHub
git push -u origin master
```

**提示：** 如果需要密码，请使用 GitHub **Personal Access Token**（不是账号密码）

---

### 方法 2: 使用 GitHub CLI（如果已安装）

```bash
# 1. 登录 GitHub
gh auth login

# 2. 创建仓库并推送
gh repo create DuplicateFileFinder --public --source=. --remote=origin --push
```

---

### 方法 3: 在 Windows 上操作（推荐）

如果您在 Windows 上更熟悉操作：

1. **安装 Git for Windows**
   - 下载：https://git-scm.com/download/win

2. **复制项目到 Windows**
   ```
   复制整个 /home/wuying/clawd/projects/DuplicateFileFinder 文件夹
   ```

3. **在 Windows PowerShell 中操作**
   ```powershell
   cd DuplicateFileFinder
   git init
   git add .
   git commit -m "Initial commit"
   git remote add origin https://github.com/YOUR_USERNAME/DuplicateFileFinder.git
   git push -u origin master
   ```

---

## 🔑 获取 GitHub Personal Access Token

如果推送时需要认证：

1. 访问 https://github.com/settings/tokens
2. 点击 **"Generate new token"** → **"Generate new token (classic)"**
3. 设置：
   - **Note**: `DuplicateFileFinder`
   - **Expiration**: 选择过期时间
   - **Scopes**: 勾选 `repo`（完整仓库访问权限）
4. 点击 **"Generate token"**
5. **复制 token**（只显示一次！）
6. 推送时用 token 代替密码

---

## 🎨 上传后的后续操作

### 1. 启用 GitHub Actions（自动编译）

在仓库中创建 `.github/workflows/build.yml`：

```yaml
name: Build and Test

on:
  push:
    branches: [ master, main ]
  pull_request:
    branches: [ master, main ]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --no-build --verbosity normal
```

### 2. 添加仓库标签

- `csharp`
- `wpf`
- `dotnet`
- `duplicate-files`
- `file-scanner`
- `windows`
- `desktop-application`
- `chinese`

### 3. 设置仓库描述

```
一款高效的 Windows 重复文件扫描工具，基于 C# + WPF + .NET 8.0

✨ 特性:
- 内容级重复检测（SHA-256）
- 灵活的文件过滤系统
- 程序员友好的默认规则
- 配置保存/加载
- 多格式报告导出
- 完整中文界面

🚀 快速开始: 查看 README.md
```

### 4. 添加 About 页面

在 GitHub 仓库设置中添加 About 信息，包含：
- 项目简介
- 快速开始链接
- 截图（后续添加）

---

## 📊 仓库创建后检查清单

- [ ] 代码已成功推送到 GitHub
- [ ] README.md 正常显示
- [ ] 所有文件都已上传
- [ ] 设置仓库描述和标签
- [ ] （可选）启用 GitHub Actions
- [ ] （可选）添加 Releases
- [ ] （可选）设置 GitHub Pages（展示文档）

---

## 🔍 验证编译

### 方法 1: 使用 GitHub Actions

1. 在 GitHub 仓库页面点击 **"Actions"**
2. 查看 **"Build and Test"** 工作流
3. 检查编译是否成功

### 方法 2: 本地编译测试

```bash
# 在项目目录中运行
./build.sh

# 查看编译结果
ls -la bin/Release/net8.0-windows/
```

### 方法 3: 在线编译（暂时不适用）

.NET 项目可以使用以下在线编译服务：
- GitHub Actions（推荐）
- Azure DevOps
- AppVeyor

---

## 💡 提示

1. **第一次推送可能需要认证**
   - 使用 Personal Access Token
   - 不要使用账号密码（GitHub 已弃用）

2. **如果推送失败**
   - 检查远程仓库 URL 是否正确
   - 确认 token 是否有 `repo` 权限
   - 尝试 `git push -u origin master --force`（慎用）

3. **保护主分支**
   - 在 GitHub 设置中启用分支保护
   - 要求 PR 审查（如果是团队项目）

---

## 🎉 完成后

您的项目将托管在：
```
https://github.com/YOUR_USERNAME/DuplicateFileFinder
```

您可以分享给其他人，或者：
- 📦 创建 Release 发布可执行文件
- 📝 添加 Wiki 完善文档
- 🐛 设置 Issues 跟踪问题
- 💬 启用 Discussions 讨论功能

---

需要帮助？请告诉我！🦞
