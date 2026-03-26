# 本次开发任务分解（GitHub同步：settings.json 列表版）

## T1 分支与基线

- [x] 新建分支：`codex/feature/github-sync-json-settings-list`
- [x] 保持现有功能不回退（在原工程上增量实现）

## T2 配置模型（settings.json 扩展列表）

- [x] 新增 `GithubSyncModItem` 模型
- [x] `AppSettings` 新增 `GithubSyncMods` 列表字段
- [x] `SettingsService` 对 `GithubSyncMods` 做读写兼容（缺省自动初始化）
- [x] `settings.json` 读写统一显式 UTF-8

## T3 自动检测与编码修正

- [x] 自动路径探测改为仅识别 `SlayTheSpire2.exe`
- [x] 手动选择和已保存路径校验逻辑保持原有行为
- [x] `libraryfolders.vdf` 读取改为显式 UTF-8

## T4 GitHub 同步服务（核心）

- [x] 新增 `GithubModSyncService`
- [x] 启动/刷新时从 Mod 元数据自动补录到 `settings.GithubSyncMods`（不新增 csv）
- [x] 同步范围限定：`Enabled=true && Available=true`
- [x] 同仓库多 Mod 仅更新一个，并统计重复提示数
- [x] release 拉取方式：`gh api repos/<owner>/<repo>/releases/latest`
- [x] 下载资产支持：`dll/pck/json/zip`
- [x] `zip` 解压后二次扫描
- [x] 同步失败自动 `Enabled=false` 且 `Available=false`

## T5 扁平化安装与回写策略

- [x] 下载产物强制扁平化到同一目录（禁止嵌套目录）
- [x] 同名冲突自动重命名（`name (2).ext`）
- [x] 若无 `dll/pck/json` 判定为无效链接
- [x] 更新前备份旧版本到 `Backup/Mods/Updates/<时间戳>`
- [x] 更新后目录命名为 `原目录名_版本号`
- [x] 同步后回写 Mod 同名 json（版本、链接、描述等）
- [x] 下载 json 缺字段时用本地字段补齐；无 json 时自动生成可用 json

## T6 ViewModel 与界面入口

- [x] 新增“同步GitHub Mod”命令
- [x] 新增同步进度文本（`x/xx`）
- [x] 新增 Mod 详情区 GitHub 仓库显示
- [x] 新增 Mod 详情区“启用GitHub同步更新”开关（仅控制同步）
- [x] 新增“链接可用”只读状态显示

## T7 本地化

- [x] 新增中英文本地化键（同步按钮、状态、统计弹窗、详情字段）

## T8 验收

- [x] `dotnet build -c Debug` 通过
- [ ] 实机验证：同步成功后目标 Mod 目录 `Get-ChildItem -Recurse` 无子目录
- [ ] 实机验证：同步统计“更新/无效/最新/重复提示”数字正确
- [ ] 实机验证：同仓库多启用时提示行为符合预期
