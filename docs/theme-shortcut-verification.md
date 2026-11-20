# 主题取色与开关快捷键验证

日期：2026-11-20。实现版本 1.2.0，公共 API v1。

本文保留主题取色与原页脚下拉的测量。当前页脚改为点击后录入，见[快捷键录入验证](shortcut-capture-verification.md)。

## 取色和快捷键实现

- 参考 OtherMOD 的 `Overcooked-2-HudCustomization`（本地 `fee0f7a`）：`src/Editing/HudNativeAssets.cs` 从已实例化的 `GameOptions/SettingsBody/ContentPC/Viewport/Content/ScreenAdjuster` 取得 `targetGraphic` 的按钮 Sprite。它用 `Graphics.Blit` 和 `ReadPixels` 复制可能不允许 CPU 读取的按钮贴图，因此会继承玩家替换的贴图。当前所查版本的 `HudNativeUi.cs` 通过提亮该贴图表现悬停，没有单独解析原生高亮色。
- `src/Native/NativePalette.cs` 使用同一原生按钮，在首次存档门禁就绪时采样隐藏副本的 `Normal` / `Highlighted` 动画。原生控件通过动画换 Sprite，读序列化 `ColorBlock` 不能得到这两张贴图的颜色。副本保留 Animator，读取 Sprite 中央非透明区域，并应用 Image tint；兼容 SpriteSwap 和 ColorTint。高亮缺失或与普通色相同则尝试厨师按钮的 `Highlight`，再回退到默认蓝、橙。只在模板捕获时取样，打开缓存页不再读取 GPU 纹理。不修改原生控件、游戏 bundle 或 HUD Customizer。
- 普通色应用于搜索、按钮、开关、输入、下拉候选和滚动条轨道。行和分区背景从普通色派生，整行与控件高亮仍互斥。滑条只给轨道着色，抓手保持白色；开关图标 64×64 UI，颜色控件保留实际色值。
- `src/Native/NativePage.Shortcut.cs` 在左栏下面、面板缺口上方复用原生下拉。提供反引号／波浪号（默认）和 F1–F12；点击或确认后通过 `ConfigCommit` 即时写入 `Navigation.ToggleKey`，无需重开菜单。快捷键文案跟随游戏 12 种语言，按键捕获和文本编辑使用当前全局开关键。保留 `NativeShortcut.Tick(bool pressed)` 供 HUD Customizer 的输入互斥前缀继续匹配。

## 实机与桌面证据

以下 `.validation` 为本机忽略的证据目录，未随源码发布。Unity 场为 Windows Steam、实际游戏和已安装 MOD 的脚本输入，均使用 `-WithoutFastInit`。

| 命令 | 结果与证据 |
| --- | --- |
| `scripts/test.ps1` | .NET 3.5 / C# 3 生产构建通过，372 项桌面断言通过。 |
| `scripts/test-examples.ps1` | 独立 SDK、消费方和编辑器示例编译通过，无提供端 DLL 引用。 |
| `scripts/test-shared.ps1 -BuildOnly` | 四份源码内置程序集编译通过，未运行选优矩阵。 |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 30` | **868 项**。常规页面、原版取色、下拉焦点、选键后读盘、新旧键开关与捕获、无原生 RootMenu、原生弹窗、前端重建。热键验证只替换 Unity 的硬件读键，执行真正的插件 Update；日志确认 HUD 前缀成功注册。[页脚](../.validation/20261120-174219-906-without-fastinit/shortcut-picker.png)、[展开](../.validation/20261120-174219-906-without-fastinit/shortcut-picker-choices.png)。 |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 3 -ThemeFixture` | **65 项**。只在测试进程中改变 `bundle24` 中已加载的 `t_ui_MenuBar_01`、`t_ui_MenuBar_Highlight_01` 纹理像素，保留原 Sprite 和 Animator。[粉底蓝高亮与白抓手](../.validation/20261120-173639-432-without-fastinit/feedback-slider.png)、[取色记录](../.validation/20261120-173639-432-without-fastinit/palette.txt)。 |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 3 -LocalizationOnly` | **149 项**。12 语页脚、草稿保留、消费方候选和已打开下拉刷新。[中文界面](../.validation/20261120-174825-304-without-fastinit/language-Chinese.png)。 |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 30 -OpeningPerformance` | **45 项**。首次 submit 8.711 ms、内容 104.934 ms 就绪、首次打开最大观测帧 19.040 ms；六次重开 submit 均值 9.021 ms。[原始数据](../.validation/20261120-174925-915-without-fastinit/opening-performance.txt)。 |

四个最终 Unity 场共 **1127 项**。实机 PNG 为 3840×2160；页脚按钮 324×40 UI，距离列表 10.36 UI，实际 Canvas 命中通过。原版取到普通 `#5A96BE`、高亮 `#FF9D3E`；粉蓝场取到普通 `#B05285`、高亮 `#347ECC`。像素只是测试进程内模拟换色，未用玩家改造的磁盘 bundle 验证。性能数字是此次本机观察，不包含用户物理输入到画面的全过程。

## 中间场与运行前提

- `172038` 原版取色失败：隐藏副本去掉 Animator 后，Unity 2017 不采样非 Legacy 动画。同场还查到 HUD Customizer 按原签名绑定快捷键前缀失败。保留 Animator 与 `Tick(bool pressed)` 后完整回归通过。
- `173731` 完整场的无 RootMenu 射线点未命中；证据未记录当时坐标，无法确定是否裁剪所致。测试后来记录首行坐标及滚动位置，仅当超出 viewport 时调用现有 `Reveal`，保留同一命中断言。重跑时首行原本在视口内，命中通过；未据此修改生产滚动状态。
- `174349`、`174633` 语言场中，街机 MOD 的 `Language.Update → ModSettingsBridge.Refresh → RegisterActionV1` 主动使全目录失效，导致被检测的输入控件已销毁。最终专项只在测试进程中隔离 HUD 和街机 MOD 的语言轮询，通过 fixture 另测真实 SDK 刷新；生产仍尊重消费方的显式重建请求。
- 八场（含失败）退出后复查 **64 项安装文件与 8 项原存档，共 72/72 一致**，无残留游戏进程。详见 [恢复核对](../.validation/20261120-174925-915-without-fastinit/restoration-audit.json)。本轮未重测实体键鼠/手柄、其他分辨率、真实厨房与 Steam 联机，也未验证改变取色来源以外素材的自定义主题。
- 产物 `bin/Release/OC2ModSettings.dll`，程序集版本 `1.2.0.0`，SHA-256 `4EEC191E8BE088BA8D24555863C56B5E2D6183731379335245AB771E300064EC`。未正式替换安装、推送或发布；临时测试结束恢复了原 1.1.1 DLL。实际当选的公共服务或源码内置副本需要更新，重启游戏并通过存档门禁后取色。更换主题 bundle 同样需要重启；仅改别处素材时，设置继续取原生 `ScreenAdjuster` 状态的颜色。
