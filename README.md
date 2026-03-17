# StS2 Mod Manager / 杀戮尖塔2 Mod 管理器

[中文](./README.md) | [English](./README.en.md)

一个面向《杀戮尖塔2（Slay the Spire 2）》的桌面 Mod 管理工具，提供目录管理、批量生效、存档互拷、备份恢复和一键启动能力。

---

## 中文文档

### 1. 项目定位

- 自动检测游戏目录，也支持手动指定目录。
- 管理工具目录和游戏目录两侧的 Mod，并支持“应用/取出”。
- 支持存档在 modded/非 modded 目录之间复制，复制前自动备份。
- 支持直接启动和 Steam 启动，并提供无 Mod 启动参数。

### 2. 主要功能

#### 2.1 路径管理

- 自动扫描常见安装路径。
- 可手动浏览并选择游戏目录。
- 快捷打开：游戏目录、游戏 Mods、待生效 Mods、工具 Mods。

#### 2.2 Mod 管理

- 管理器默认工具目录：`<工具目录>/Mods`
- 游戏 Mods 目录：`<游戏目录>/mods`（小写）
- 支持勾选生效、取消生效、全选生效、全部取消。
- 支持“取出”操作（从游戏 Mods 复制回工具 Mods）。
- 支持按关键字、标签、作者筛选。
- 支持统一编辑 Mod 元信息（名称、标签、作者、备注、链接等）。

#### 2.3 存档管理

- 存档基础目录：`%AppData%/SlayTheSpire2/steam`
- 默认使用最新的 Steam 数字 ID 目录。
- 支持在以下路径间复制：
  - `modded/profileX`
  - `profileX`
- 支持按栏位选择（如 profile1/profile2/profile3 或全部）。
- 复制前自动备份到：`<工具目录>/Backup/Saves/时间戳`
- 支持备份列表恢复，恢复前会自动生成“恢复前备份”。

#### 2.4 启动方式

- 直接启动：`SlayTheSpire2.exe`
- 直接无 Mod：`SlayTheSpire2.exe --nomods`
- Steam 启动：`steam://rungameid/2868840`
- Steam 无 Mod：`steam://run/2868840//--nomods`

### 3. 下载与发布

#### 3.1 常规下载

- GitHub Releases：<https://github.com/liwenhao0427/StS2ModManager/releases>

#### 3.2 整合包（中文用户）

- 夸克网盘：<https://pan.quark.cn/s/b89dbac25ba4>
- 口令：`/~88063M0Ul6~:/`

> 整合包通常包含：管理器 + `Mods` 目录，可开箱即用。

### 4. 使用说明（推荐流程）

1. 首次启动后，确认游戏路径是否正确。
2. 在 Mod 列表勾选需要生效的 Mod。
3. 如需筛选，使用搜索/标签/作者条件快速定位。
4. 点击启动按钮进入游戏（可选无 Mod 启动）。
5. 在进行存档互拷前，先确认方向与栏位，避免误覆盖。

### 5. 本地开发

#### 5.1 技术栈

- .NET 9（WPF）
- CommunityToolkit.Mvvm

#### 5.2 构建

```bash
dotnet restore
dotnet build
```

#### 5.3 发布（双版本）

项目约定“发布”默认同时产出两个单文件 exe：

- 依赖 .NET 运行时版本：`StS2ModManager.依赖.Net环境版本.exe`
- 自包含版本：`StS2ModManager.exe`

固定输出目录：

`C:\Users\temp\项目\杀戮尖塔2Mod\Slay the Spire 2\StS2ModManager\ReleaseSingle`

示例命令：

```bash
dotnet publish "StS2ModManager.csproj" -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:EnableCompressionInSingleFile=false -o "ReleaseSingle/FrameworkDependent"
dotnet publish "StS2ModManager.csproj" -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o "ReleaseSingle/SelfContained"
```

#### 5.4 GitHub Release 规范

- 仅上传两个 exe 文件，不上传 pdb/zip/目录。
- 本地输出命名保持：
  - `StS2ModManager.依赖.Net环境版本.exe`
  - `StS2ModManager.exe`
- GitHub 资产中，依赖版可能显示为 `StS2ModManager.Net.exe`（与本地依赖版对应）。

### 6. 常见问题

- 启动失败：先检查游戏目录是否包含 `SlayTheSpire2.exe`。
- Steam 启动失败：确认 Steam 已安装并登录。
- 链接无法打开：确认链接格式为完整 URL（包含 `http://` 或 `https://`）。
- 存档复制失败：优先检查源栏位是否存在，再查看备份目录权限。

---

## License

This repository currently has no explicit open-source license file. If you plan to redistribute or modify it publicly, please add a `LICENSE` file first.
