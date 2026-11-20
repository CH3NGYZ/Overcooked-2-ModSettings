# 面板标题、搜索清空与快捷键对齐验证

2026-11-20，本地 1.2.1，公共 API v1。简中面板标题为「MOD 设置面板」，其他语言跟随游戏。蓝色标题条右下显示插件版本。点击搜索右侧的「×」立即恢复当前 MOD 的未过滤列表，光标回到搜索框；原生方向导航按搜索、清空、关闭的顺序切换。

## 实测结果

| 项目 | 实际 Unity 布局 |
| --- | --- |
| 双栏 | 顶边同高，距搜索行 10 UI（原 42.78 UI）；上移 32.78 UI，底边不动。 |
| 标题与版本 | 标题居中，12 语均可单行渲染；`v1.2.1` 向左移 24 UI，版本文本框距标题条右边界 50 UI。 |
| 搜索、清空、关闭 | 同高 46 UI，宽度分别为 462、46、163.20 UI，相邻间隔 10 UI。 |
| 快捷键 | 168×40 UI，左移 8 UI 后与 MOD 列表按钮左边线齐平，距列表底边仍为 10.36 UI。 |

[简中界面与清空后的光标](../.validation/20261120-202120-958-without-fastinit/search-cleared.png) · [葡萄牙语长标题](../.validation/20261120-202322-237-without-fastinit/language-Brazilian.png)（原始截图均为 3840×2160）。几何记录位于各场的 `layout-checks.txt`、`banner-layout.txt` 和 `shortcut-button-geometry.txt`。

| 命令 | 结果 |
| --- | --- |
| `scripts/test.ps1` | .NET 3.5 / C# 3 生产构建通过，372 项桌面断言通过。 |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 30` | `20261120-202120-958-without-fastinit`：921 项。实际 Canvas 射线命中「×」，鼠标点击与原生确认均清空筛选、恢复结果及输入焦点；快捷键仍可录入。 |
| `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 3 -LocalizationOnly` | `20261120-202322-237-without-fastinit`：187 项。测试进程内覆盖语言读取，检查 12 语标题和版本号的字形与边界；不写入玩家语言设置。 |

收尾含位置微调前的一场，共核对 24 项安装状态及 3 项原存档，全部一致，无残留游戏进程。测试覆盖实际 Unity 界面与脚本输入；未复测物理设备、其他分辨率或 Steam 联机。本地产物为 `bin/Release/OC2ModSettings.dll`，测试后恢复了原安装的 1.2.0。使用新版本需替换实际加载的 DLL 并重启游戏；未正式部署、推送或发布。
