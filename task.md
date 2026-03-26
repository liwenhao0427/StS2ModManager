# T0 本次版本目标（JSON统一同步版）
- [x] 自动检测路径仅识别 `SlayTheSpire2.exe`（手动选择/已存路径逻辑不变）
- [x] 路径区与启动区UI合并，路径固定长度中间省略
- [x] Mod列表与右侧详情面板高度对齐
- [x] Mod详情主字段改为扩展详情(description)优先
- [x] GitHub自动同步改为 `settings.json` 扩展列表维护（按你后续确认替代同名json清单）
- [x] 同步完成后汇总弹窗：更新数/无效数/已最新数/重复仓库提示数

---

## T1 分支与基线
- [x] 创建分支 `codex/feature/github-sync-json-settings-list`
- [x] 记录基线并持续增量提交
- [x] 本地编译通过（Debug）

验收：
- [x] 能成功切到新分支
- [x] `dotnet build` 通过

---

## T2 路径检测收敛（仅自动检测）
- [x] `GamePathService` 增加自动检测专用校验（必须存在 `SlayTheSpire2.exe`）
- [x] `DetectGamePaths` 使用严格校验
- [x] 手动 Browse 与 settings 路径有效性仍使用宽松校验
- [x] 文本读写补全 UTF-8（settings/libraryfolders）

验收：
- [x] 自动检测不再因仅有 mods 目录误识别
- [x] 手动选择旧路径行为保持不变

---

## T3 UI重构（路径区+启动区+对齐）
- [x] 删除独立“启动游戏”卡片
- [x] 四个启动按钮并入“游戏路径”行右侧
- [x] 路径显示固定宽度+中间省略（含 tooltip 完整路径）
- [x] 左右主面板统一高度（消除明显空白断层）

验收：
- [x] `dotnet build` 验证布局改动可运行
- [ ] 1080p 与 1366x768 手工视觉验收
- [x] 启动按钮仍绑定原命令

---

## T4 Mod详情字段语义调整
- [x] 标签下主输入改为 `description`（多行）
- [x] 读取优先 `description`，兼容 `detail`
- [x] 保存保持 `detail/description` 兼容回写，不破坏旧数据

验收：
- [x] 编辑并保存后，`description` 为主
- [x] 旧有仅 `detail` 的 Mod 仍可显示/编辑

---

## T5 settings.json 同步列表模型（按最新确认）
- [x] 新增 `AppSettings.GithubSyncMods`
- [x] 新增 `GithubSyncModItem` 字段：`ModKey/FolderName/SourcePath/RepoUrl/Enabled/Available/CurrentVersion/Description/DetailUrl/DownloadUrl/AuthorUrl/LastSyncAt/LastError`
- [x] 启动/刷新时自动补录 GitHub 仓库映射（不存在才新增，保留开关）
- [x] 同步状态统一落在 `settings.json`（不生成 CSV）

验收：
- [x] `Config/settings.json` 持续更新 `GithubSyncMods`
- [x] 不生成 CSV 文件

---

## T6 下载+解压+扁平化安装
- [x] 资产下载支持 `dll/pck/json/zip`
- [x] `zip` 解压后二次扫描
- [x] 扁平化收集到同一目录（禁止嵌套）
- [x] 同名文件冲突自动重命名（`name (2).ext`）
- [x] 无 `dll/pck/json` 判定不可用并自动关闭同步
- [x] 更新前备份旧版本到 `Backup/Mods/Updates/<时间戳>`
- [x] 修复跨盘替换失败（`Move` 失败回退 `Copy+Delete`）

验收：
- [ ] 手工验证：同步成功目录 `Get-ChildItem -Recurse` 无子目录
- [x] 失败原因可写入 `LastError`

---

## T7 JSON合并补全策略（关键）
- [x] 下载包有json：以下载json为基底，补齐本地缺失字段
- [x] 下载包无json：自动生成json并填充字段
- [x] 强制更新字段：`version/download_url/author_url/detail_url/description`
- [x] 保留未知扩展字段（沿用现有 `JsonExtensionData` 链路）

验收：
- [x] `tag` 等字段在下载json缺失时可补齐
- [x] 自动生成json后可被现有程序识别

---

## T8 重复仓库冲突策略
- [x] 同仓库多Mod默认仅更新一个
- [x] 多个 `enabled=true` 时弹提示确认
- [x] 优先更新启用项并统计重复提示数

验收：
- [x] 同仓库不会重复下载多次
- [x] 汇总统计包含“重复仓库提示数”

---

## T9 ViewModel/本地化/进度与结果弹窗
- [x] 新增“同步GitHub Mod”命令与按钮
- [x] 增加进度显示 `x/xx`
- [x] 详情区增加 `github_sync` 开关（仅控制同步，不影响Mod启停）
- [x] 新增中英文本地化键
- [x] 同步结束弹窗显示统计
- [x] 增加同步详细日志文件（`Config/Logs/github_sync_*.log`）
- [x] 同步改为后台线程执行 + 独立进度弹窗（避免UI卡死）

验收：
- [x] UI可见同步进度实时变化
- [ ] 统计数字与真实结果手工核对

---

## T10 回归测试与提交
- [ ] 全量构建（Release）
- [ ] 手工回归：路径检测、启动、Mod编辑、同步成功/失败/已最新/重复仓库
- [ ] 更新 README/requirements（如有行为变化）
- [x] 多次 git 提交（中文信息，含核心变更）

已提交记录（当前分支）：
- [x] `69a8c4f` feat: settings.json维护GitHub同步列表并实现扁平化更新
- [x] `553b71a` feat: 同步前增加重复仓库映射确认提示
- [x] `4e915bf` fix: 修复跨盘更新失败并增加GitHub同步详细日志
- [x] `d23906a` feat: GitHub同步改为后台执行并新增进度弹窗
