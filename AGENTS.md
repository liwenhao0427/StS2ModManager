# StS2 Mod Manager 会话整理

本文件由当前会话整理生成，用于后续迭代时快速理解上下文。

## 项目目标
构建一个可打包为单文件exe的杀戮尖塔2 Mod管理器，界面简洁易用，主要功能包含：
- 自动检测游戏目录 + 支持手动选择
- Mod管理（工具目录/游戏目录）与备份
- 存档管理（基于steam目录、支持modded/非mod切换）
- 一键启动游戏（含无Mod参数）

## 技术栈
- WPF (.NET 9.0)
- CommunityToolkit.Mvvm
- 单文件发布（win-x64）

## 关键路径/约定
- 游戏exe：`SlayTheSpire2.exe`
- 游戏Mods目录：`<游戏目录>/mods`（小写）
- 工具Mods目录：`<工具目录>/Mods`
- 备份目录：`<工具目录>/Backup`
- 配置目录：`<工具目录>/Config`
- 存档基础目录：`%AppData%/SlayTheSpire2/steam`

## 存档逻辑（当前理解）
- 在最新的steam数字ID目录下执行存档复制（按目录更新时间挑选）
- modded 存档目录：`<steamId>/modded/profileX`
- 普通存档目录：`<steamId>/profileX`
- 支持复选profile1/2/3... 进行复制替换
- 复制前先备份到 `Backup/Saves/时间戳`

## 启动方式
- 直接启动：调用游戏exe（可带 `--nomods`）
- Steam启动：`steam://rungameid/2868840`（无Mod可走 `steam://run/2868840//--nomods`）

## UI 要点
- 亮色主题
- 可打开游戏目录/游戏Mods目录/工具Mods目录
- Mod“取出”按钮：从游戏Mods复制到工具Mods目录
- 存档操作提示需写清楚风险

## 已完成
- 亮色主题
- Steam启动按钮
- Mod管理（应用/取出）
- 存档复制功能（待最终按profile复选实现）

## 待确认/待调整
- 存档界面改为profile复选框模式
- 最新steamId目录的选择策略是否需要手动选择

## 会话规则（新增）
- 每次会话修改完成后，助手需执行一次 git 提交。
- 当用户说“发布”时，默认指同时产出两个版本：
  - 依赖 .NET 运行时版本（framework-dependent）
  - 包含依赖版本（self-contained）
- 发 GitHub Release 时仅上传以上两个 exe 文件，不上传其他产物（如 pdb、zip、目录等）。
- 发布输出目录固定为：`C:\Users\temp\项目\杀戮尖塔2Mod\Slay the Spire 2\StS2ModManager\ReleaseSingle`
- 发布文件命名约定：
  - 依赖 .NET 运行时版本命名为：`StS2ModManager.依赖.Net环境版本.exe`
  - 包含依赖版本命名为：`StS2ModManager.exe`

### 发布强制命令（必须按此执行，禁止自行改参数）

1. 打包 framework-dependent（依赖 .NET）：
   - `dotnet publish ".\StS2ModManager.csproj" -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:EnableCompressionInSingleFile=false -o ".\ReleaseSingle\FrameworkDependent"`

2. 打包 self-contained（自包含）：
   - `dotnet publish ".\StS2ModManager.csproj" -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o ".\ReleaseSingle\SelfContained"`

3. 复制并命名最终文件（禁止反向复制）：
   - `Copy-Item .\ReleaseSingle\FrameworkDependent\StS2ModManager.exe .\ReleaseSingle\StS2ModManager.依赖.Net环境版本.exe -Force`
   - `Copy-Item .\ReleaseSingle\FrameworkDependent\StS2ModManager.exe .\ReleaseSingle\StS2ModManager.Net.exe -Force`
   - `Copy-Item .\ReleaseSingle\SelfContained\StS2ModManager.exe .\ReleaseSingle\StS2ModManager.exe -Force`

4. 发布前强制校验大小（不满足即停止发布）：
   - 依赖版（`StS2ModManager.依赖.Net环境版本.exe`）必须 `< 10MB`
   - 自包含版（`StS2ModManager.exe`）必须 `> 30MB`

5. GitHub Release 上传命令（仅上传两个exe）：
   - `$env:GODEBUG='http2client=0'; gh release create <tag> ".\ReleaseSingle\StS2ModManager.Net.exe" ".\ReleaseSingle\StS2ModManager.exe" --title "<tag>" --notes "<notes>" --target main`
