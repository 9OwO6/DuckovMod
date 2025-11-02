# BetterThrowingSystem Mod

更好的投掷物系统 - 为《逃离鸭科夫》游戏添加投掷物背包和快捷操作功能。

## 功能特性

- 投掷物背包：支持携带5个投掷物
- 快捷切换：按 G 键快速切换到手持装备中的投掷物
- 自动扫描：自动检索玩家背包中的投掷物和食品

## 构建说明

### 1. 配置项目路径

在 `BetterThrowingSystem.csproj` 文件中，需要设置 `DuckovPath` 变量指向游戏安装目录。

你可以：
- 在 Visual Studio 中编辑项目属性，添加用户变量
- 或者在构建前设置环境变量
- 或者直接在 csproj 中硬编码路径（不推荐用于发布）

### 2. 构建项目

```bash
dotnet build BetterThrowingSystem.csproj
```

构建完成后，将生成的 `BetterThrowingSystem.dll` 复制到 mod 文件夹中。

### 3. 文件结构

```
BetterThrowingSystem/
├── BetterThrowingSystem.csproj
├── ModBehaviour.cs
├── info.ini
├── BetterThrowingSystem.dll (构建后生成)
└── preview.png (可选)
```

## 注意事项

- 代码中的 `IsThrowableItem()` 方法需要根据实际游戏中的物品类型进行调整
- 可能需要根据实际 API 调整获取玩家角色和库存的方法
- 确保所有引用的 DLL 文件路径正确

## 待实现功能

- [ ] 快捷伙食背包（E 键快捷进食/打药）
- [ ] 长按选择轮盘（类似 GTA 的武器选择）
- [ ] UI 显示当前装备的投掷物

