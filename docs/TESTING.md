# 测试指南 / Testing Guide

## 前置要求

1. **安装 .NET SDK**
   - 需要安装 .NET SDK 6.0 或更高版本
   - 下载地址：https://dotnet.microsoft.com/download
   - 验证安装：在命令行运行 `dotnet --version`

2. **确认游戏路径**
   - 游戏路径应为：`D:\SteamLibrary\steamapps\common\Escape from Duckov`
   - 如果路径不同，请修改 `build.bat` 中的 `DUCKOV_PATH` 变量

## 构建步骤

### 方法 1：使用构建脚本（推荐）

1. 打开 PowerShell 或命令提示符
2. 导航到 mod 文件夹：
   ```bash
   cd "D:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Mods\BetterThrowingSystem"
   ```
3. 运行构建脚本：
   ```bash
   .\build.bat
   ```

### 方法 2：手动构建

1. 在项目文件夹中打开 PowerShell
2. 设置游戏路径并构建：
   ```powershell
   $env:DuckovPath = "D:\SteamLibrary\steamapps\common\Escape from Duckov"
   dotnet build BetterThrowingSystem.csproj
   ```
3. 复制 DLL 到 mod 文件夹：
   ```powershell
   Copy-Item "bin\Debug\netstandard2.1\BetterThrowingSystem.dll" -Destination "."
   ```

## 文件结构检查

构建完成后，mod 文件夹应包含以下文件：

```
BetterThrowingSystem/
├── BetterThrowingSystem.dll    ← 必须有（构建后生成）
├── info.ini                    ← 必须有
├── ModBehaviour.cs             ← 源代码（可选，游戏不需要）
├── BetterThrowingSystem.csproj ← 源代码（可选）
└── preview.png                 ← 可选（256x256 预览图）
```

## 在游戏中测试

1. **启动游戏**
   - 启动《逃离鸭科夫》
   - 进入主菜单

2. **启用 Mod**
   - 在主菜单中找到 "Mods" 或 "模组" 选项
   - 找到 "BetterThrowingSystem" 或 "更好的投掷物系统"
   - 确保 mod 已启用（勾选）

3. **开始游戏**
   - 进入游戏世界
   - 确保你有至少一个投掷物在背包中

4. **测试功能**

   **测试 1：扫描投掷物**
   - 查看游戏控制台或日志（如果有）
   - 应该能看到类似 "[BetterThrowingSystem] Found X throwable items" 的消息
   
   **测试 2：G 键切换**
   - 按 `G` 键
   - 应该切换到投掷物模式
   - 再次按 `G` 键退出投掷模式

   **测试 3：检查背包**
   - 按 `G` 键后，应该装备第一个槽位的投掷物
   - 查看你的角色手中是否持有投掷物

## 调试方法

### 查看日志

游戏日志通常位于：
- `%USERPROFILE%\AppData\LocalLow\[游戏公司名]\[游戏名]\Player.log`

或者使用 Unity 日志查看工具。

### 常见问题

**问题 1：构建失败 - 找不到 DLL 文件**
- 解决：检查游戏路径是否正确
- 确保 `Duckov_Data\Managed\` 文件夹存在且包含所需的 DLL

**问题 2：Mod 没有加载**
- 检查 `info.ini` 中的 `name` 是否与 DLL 名称匹配
- 确保 DLL 文件名是 `BetterThrowingSystem.dll`
- 检查游戏中的 mod 列表，确认 mod 已启用

**问题 3：G 键没有反应**
- 检查是否有编译错误
- 查看游戏日志是否有错误信息
- 确认玩家角色已加载（mod 需要等待角色加载）

**问题 4：找不到投掷物**
- 可能需要调整 `IsThrowableItem()` 方法
- 查看日志中 "Found X throwable items" 的数量
- 确认物品名称包含关键词或使用正确的 TypeID

## 下一步调试

如果功能不工作，可以：

1. **检查物品识别**
   - 在游戏中获得一个投掷物
   - 查看日志看是否被识别
   - 可能需要调整 `IsThrowableItem()` 中的关键词

2. **检查 API 调用**
   - `GetPlayerCharacter()` 可能返回 null
   - `GetPlayerInventory()` 可能需要不同的方法
   - 装备物品的 API 可能需要调整

3. **添加更多日志**
   - 在关键位置添加 `Debug.Log()` 语句
   - 重新构建并测试

## 联系和支持

如果遇到问题：
- 检查游戏日志文件
- 查看 Unity 控制台输出
- 根据错误信息调整代码

