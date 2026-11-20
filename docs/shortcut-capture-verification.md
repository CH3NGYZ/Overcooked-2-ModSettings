# 左下角开关快捷键录入验证

2026-11-20，未发布的 1.2.0，公共 API v1。

## 放宽快捷键范围

录入和 `Navigation.ToggleKey` 共用同一规则。可选字母、数字、标点、小键盘数字／运算键和 F 键。保留 Ctrl、Alt（含 AltGr）、Shift、Windows／Command、锁定键、Tab、空格、Esc、方向／翻页及编辑／系统键；拒绝鼠标、手柄和未知键值。手动写入不支持的值时回退反引号。按住 Ctrl、Alt 或 Windows／Command 时不录入或触发开关；在 MOD 设置自己的文本框内，普通字符键优先用于输入。默认反引号和 F 键仍可关闭页面。

| 命令 | 结果与证据 |
| --- | --- |
| `scripts/test.ps1` | .NET 3.5 / C# 3 生产构建和 358 项桌面断言通过。 |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 30` | 915 项。真实录入流程覆盖 `A`、`-`、小键盘 `+`、`F15` 和主键盘 `7`，各类保留键与 Esc 不写盘；非法 cfg 值回退默认。数字绑定立即读盘，录入帧不误关；搜索框与数值框仍可输入 `7`，离开编辑后再按可开关面板。[实机截图](../.validation/20261120-190257-991-without-fastinit/shortcut-typing.png) · [几何](../.validation/20261120-190257-991-without-fastinit/shortcut-button-geometry.txt) |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 3 -LocalizationOnly` | 151 项。12 语仍只显示键名，录入中的提示继续跟随语言。[法语录入截图](../.validation/20261120-190506-342-without-fastinit/shortcut-recording-french.png) |

实机截图为 3840×2160，页脚按钮仍为 168×40 UI，距上方列表 10.36 UI。实机使用脚本替换 Unity 硬件读键与文本输入，运行生产捕获、保存、开关和文字编辑逻辑。未复测实体键盘、不同键盘布局、手柄、厨房或 Steam 联机。两场退出后，16 次临时安装和 2 次原存档核对全部一致，无残留游戏进程（[恢复记录](../.validation/20261120-190506-342-without-fastinit/restoration-audit.json)）。最新本地 `bin/Release/OC2ModSettings.dll` 的 SHA-256 为 `F62183D31E5266712302F8396D49342942C87E3B85FD47D4B539B8C874BD9275`；测试后恢复了上一次构建，正式使用须更新实际当选的 DLL 并重启。

## 点击录入取代下拉（上一轮）

左下角空闲时只显示当前键。点击或用原生方向导航确认后，按钮显示“请按键”，页脚才提示可用键和 Esc。按反引号／波浪号或 F1–F12 即保存到原 `Navigation.ToggleKey`；Esc 取消。录入结束的同一帧不会触发开关，再按新键才会开关。普通配置的按键录入仍为全局开关键保留优先级。原生主题取色沿用[上一轮实现](theme-shortcut-verification.md)。

| 命令 | 证据 |
| --- | --- |
| `scripts/test.ps1` | .NET 3.5 / C# 3 生产构建和 358 项桌面断言通过。 |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 30` | 884 项真实 Unity 脚本断言通过。检查原生方向导航、鼠标命中、无效键与 Esc 不写盘、合法键即时读盘、录入帧不误关、旧键失效、新键开关，以及关闭、外部禁用和目录重建释放录入租约。 |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 3 -LocalizationOnly` | 151 项真实 Unity 脚本断言通过。十二语空闲均只显示键名，录入时切换法语能原位更新提示，保留录入状态。 |

真实 Canvas 中按钮为 **168×40 UI**，距列表 **10.36 UI**，上一版下拉为 324×40 UI。实机截图：[空闲](../.validation/20261120-183121-354-without-fastinit/shortcut-button.png)、[录入](../.validation/20261120-183121-354-without-fastinit/shortcut-recording.png)、[改键后](../.validation/20261120-183121-354-without-fastinit/shortcut-rebound.png)（均为 2560×1440）、[法语录入](../.validation/20261120-183240-943-without-fastinit/shortcut-recording-french.png)（3840×2160）。`.validation` 为本机证据，不进入 Git。

键盘测试替换 Unity 的硬件读键，运行真实捕获组件和插件 Update。`182003` 与 `182757` 中间场停在两处底层焦点断言；最终按原生入口的实际顺序先选中、等待获得焦点、再提交，页面关闭后返回入口的断言通过。保留中间证据，未据此归因于第三方或改动生产的入口焦点策略。未复测实体键鼠、手柄、真实厨房、Steam 联机或源码内置选优矩阵。

四场（含两次中间失败）退出后，32 次安装文件核对和 4 次原存档核对全部一致，无残留游戏进程（[恢复记录](../.validation/20261120-183240-943-without-fastinit/restoration-audit.json)）。新产物 `bin/Release/OC2ModSettings.dll` 的 SHA-256 为 `44311E4284E1B19081CA905B4A0E1F5B7E8C01B387FD89186CBFF61ADB7F0C48`。当前安装已恢复至测试前的 1.1.1；更新实际当选的独立 DLL 或源码内置副本并重启，首次存档门禁通过后可用。
