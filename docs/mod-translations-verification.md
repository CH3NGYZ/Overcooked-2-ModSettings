# MOD 多语言验收

## 构建与桌面

- `./scripts/test.ps1`：实际安装的 BepInEx 5 / Unity 2017 引用，.NET 3.5 / C# 3 构建无警告；用真实 `ConfigFile`、独立 SDK 和生产解析器通过 **491 项**桌面断言。12 种 `SupportedLanguages` 逐一映射；覆盖文件格式、非法编码隔离、逐字段回退、深拷贝、租约替换和释放、稳定配置键及候选值。
- `./scripts/test-examples.ps1`：独立消费者和 Renderer 不引用设置服务 DLL 即可编译。`./scripts/test-shared.ps1 -BuildOnly`：四份源码内置版本编译通过，未启动选优矩阵。
- 构建 `bin/Release/OC2ModSettings.dll`，源码 `2950f828d33633ef7125d49c54f6089e233700bd`，版本 `1.3.0` / API v1，SHA256 `C645F29AE8714F126D35508033DDBD11ED662EEFCAD777D98DC37FCF93F5267C`。

## Windows Steam 实机

| 命令（`./scripts/test-unity.ps1`） | 断言 | `.validation` 场次前缀 |
| :---: | :---: | :---: |
| `-WithoutFastInit -FixtureCount 3 -TranslationOnly` | 38 | `20261125-200322-589` |
| `-WithoutFastInit -FixtureCount 30` | 1012 | `20261125-200532-668` |
| `-WithoutFastInit -FixtureCount 3 -LocalizationOnly` | 187 | `20261125-200700-080` |

最终三场共 **1237 项实际 Unity 脚本断言**。新专项临时部署英／简中同名文件，覆盖独立／行旁动作，法语缺词回退，已打开下拉保留选中候选与焦点，配置草稿、查找文本、未变的搜索结果不因文案更新重建，搜索命中变动时保留搜索词和焦点。完整面板回归包含旧动作字典、快捷键两种编辑入口、重复提示、GUID 排序／置顶和独立改名；语言专项覆盖旧元数据刷新和原生界面 12 语。

最终专项 `.validation/20261125-200322-589-without-fastinit/` 的 `mod-translation-zh-CN.png` 为 **3840×2160**，画面同时显示 API 注册的 MOD 名、配置名和按钮文字及文件提供的分区、动作标题和底部说明。`mod-translation-geometry.txt` 记录换语前后左栏项位置与大小相同（464.66×68 UI）；独立动作按钮 140.49×48 UI，文字区域 120.49×42 UI，双行均落在按钮内。完整回归的 `shortcut-conflict-detail.png`、`shortcut-conflict-summary.png` 与 `shortcut-conflict-geometry.txt` 记录了 22×22 UI 的行内黄色警告及底部提示。证据文件只在本地，不随源码或 DLL 分发。

首次专项 `20261125-193750-426` 停在第 24 项：简中译文已显示，但没有保持原 UI 实例。实装 AdaptiveCamera 0.4.0 的 IL 确认旧轮询会在语言变化时改元数据并调用 `RefreshV1()` 触发结构重建；语言专项隔离已知旧轮询后通过。生产侧未屏蔽其他 MOD 的显式刷新，也未改消费者 DLL。`20261125-195645-763` 的 33 项通过是后续增加下拉和搜索用例前的结果。五场退出后共 **51/51** 原安装与存档哈希／存在性一致，见最终专项 `restoration-audit.json`；正式安装恢复为原 1.2.1，无残留游戏进程。

也收到 AdaptiveCamera 消费方独立验证回传：使用同一生产 SHA 的跨程序集桌面测试及纯隔离 Unity 测试（2241 项脚本断言）通过；未计入上述提供端断言。未用物理键鼠／手柄切换语言、未验证真实 IME、Steam 联机、其它平台或其它分辨率。以上测试使用隔离安装，不代表正式部署或发布。
