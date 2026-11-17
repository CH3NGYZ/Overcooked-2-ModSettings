# 色板与公共业务动作验证

日期2026-11-16。GUID仍为`com.ch3ngyz.plugin.overcooked2.modsettings`，插件1.0.0、API v1。
本文保留色板/动作阶段的产物和验证快照；后续统一列宽、配色/互斥高亮、Description及固定首行见[当前界面修订](settings-feedback.md)。下文“最终”“当前”均指本阶段当时状态。
本轮在已有打开复用/分帧构建、24/32/40/49%分档布局及“改键”文案基础上实施用户选定的色板弹层，增加业务按钮公共契约。HostUtilities源码由其Agent修改，本仓库提供[完整接入提示词](hostutilities-integration-prompt.md)。

实现与测试本地提交：`97073cb 实现颜色色板弹层与独立及行旁业务按钮`。

最终构建`bin/Release/OC2ModSettings.dll`，SHA256为`D5A5C82C29BE779CCF3BC1BFBE1587C55EE31081FA44652D663A7BFA12B19A07`。

## 最终行为与文件

- `EditorRow.Color.cs`：颜色行只留Hex、棋盘预览和选色入口，普通短名称行从244降至68 UI，高度减少约72%。Hex接受6/8位、有无`#`；6位视为不透明，有效输入立即保存。
- `ColorPicker.cs` / `ColorPlane.cs` / `NativeOverlay.Color.cs`：复用既有弹层所有权，二维饱和度/明度＋彩虹色相＋A透明度，提供0–1精确透明度和Hex，预览实时联动。拖动松手保存，色板确认后可方向调整、再确认保存。关闭、返回、外点和外部禁用释放输入，取消未保存预览；外部值变化不覆盖正在拖动的颜色，取消回读最新实际值。渐变用mesh绘制，不为每次拖动分配贴图。
- `RegisteredAction.cs` / `ConfigCatalog.cs` / `EditorRow.Actions.cs`：`RegisterActionV1(ownerGuid,actionId,descriptor)`，共用目录/搜索/租约模型。支持独立行、原配置项旁按钮、动作专用MOD目录、主线程执行、轻量CanExecute、异常隔离及ClosePage先关闭公共页再调用业务。动作不写cfg、不参与恢复默认，替换后的旧按钮和旧租约不能执行/删除新动作。
- 普通内置行首个动作在输入之后、默认之前，导航顺序相同；多动作在操作区续行，自定义编辑器SetHeight保留按钮空间。按钮宽度和操作区2%间隔沿用原布局，开关图标仍64×64。
- `examples/ModSettingsClient.cs` / `ConsumerPlugin.cs`：缓存实际方法探测，包括缺失结果；无需引用公共DLL。版本仍1.0.0不能证明动作方法存在，旧版降级应以能力探测为准。协议见[API v1](api-v1.md#业务按钮)。

## 编译、桌面与消费者

```powershell
& .\scripts\test.ps1
& .\scripts\test-examples.ps1
```

实际游戏引用、.NET3.5/C#3、TreatWarningsAsErrors生产编译通过，无警告。
真实BepInEx ConfigFile桌面边界**23项通过**，包括动作字典复制、非配置行拒绝提交及契约校验。
桌面裁剪源仍有4个原有DTO未赋值警告，不是生产构建警告。反射消费者和原生renderer以`/warnaserror`独立编译通过，没有公共服务DLL引用。
生产C#共41文件，最长351行。

## 当前真实Unity验证

```powershell
& .\scripts\test-unity.ps1 -WithoutFastInit -FixtureCount 30
& .\scripts\test-unity.ps1 -WithoutFastInit -FixtureCount 30 -OpeningPerformance
& .\scripts\test-unity.ps1 -WithoutFastInit -FixtureCount 30 -ShortcutOnly -WithGameplay
```

| 场景 | `.validation/`下目录 | 结果 |
|---|---|---|
| 最终前端完整回归 | `20261116-211955-300-without-fastinit` | PASS 292 |
| 最终打开性能 | `20261116-211731-146-without-fastinit` | PASS 44 |
| 最终前端/大厅/厨房快捷键 | `20261116-212204-261-without-fastinit` | PASS 92 |

三场最终验证合计**428项**，各场独立启动、结束并恢复。跨场景经原生本地街机确认进入实际厨房`s_festivemashup_1_3`的InLevel，覆盖加载排队、取消/关闭/外部禁用、原菜单栈及两位玩家输入抑制/恢复。不是完整长流程内存崩溃已修复的证据。

前端覆盖真实色板pointer/drag/release、合法/非法Hex、精确/越界A、灰黑再着色、方向编辑、外部值与未保存预览、关闭/外点/禁用/重开。动作覆盖当选服务反射契约、独立/附加/action-only、搜索、输入与保存、默认不执行、替换/释放、49%长名和多按钮、自定义renderer动态高度、CanExecute变化/异常、主线程及Execute异常隔离。ClosePage回调打开游戏实际T17DialogBox，确认按钮接收真实Canvas优先射线；没有触发HostUtilities更新/网页业务。

测试是**真实Unity内脚本事件**，使用实际控件、事件系统与配置对象；不作为物理键鼠/手柄、真实IME或Steam联机通过证据。

最终性能原始数据为该场`opening-performance.txt`：首次入口submit **13.990 ms**，内容 **107.275 ms**就绪，最大观测帧 **20.380 ms**。六次重开 **8.995/7.532/7.344/7.471/9.203/7.764 ms**，平均 **8.052 ms**，最大9.203 ms；控件身份复用、隐藏值/结构变化、自定义renderer、首次加载取消后恢复均通过。测量包含安装环境其他MOD及Harmony测量开销，不是物理输入端到端或所有机器保证。

## 实际截图与几何

PNG实际3840×2160，下面为Canvas UI单位：

| 项目 | 实测 |
|---|---:|
| 配置行宽/颜色行高 | 1194.64 / 68 |
| 行内Hex | 300.57×48 |
| 行内棋盘预览 | 106.08×48 |
| 选色、附加动作、默认按钮 | 212.17×48 |
| 短名称24% / 名称与操作区2% | 286.71 / 23.89 |
| 操作区内部2%间隔 | 17.68 |
| 附加动作行原输入 | 424.34×48 |
| 色板二维区域 | 815.62×457.27 |
| 色相/透明度条 | 815.62×31.54 |
| 弹层Hex、精确A、关闭 | 387.07×70.96 |
| 弹层棋盘预览 | 331.78×55.19 |
| 开关图标 | 64×64 |

已实际查看[色板弹层](../.validation/20261116-211955-300-without-fastinit/color-picker.png)、[紧凑颜色行](../.validation/20261116-211955-300-without-fastinit/compact-color-row.png)、[输入框旁业务按钮](../.validation/20261116-211955-300-without-fastinit/attached-business-action.png)。同目录`color-picker-geometry.txt`、`action-geometry.txt`、`responsive-rows.txt`及`raycasts.txt`保存原始几何和射线证据。[当前真实厨房截图](../.validation/20261116-212204-261-without-fastinit/shortcut-kitchen.png)来自独立跨场景验证。动作图为测试配置，不能当作HostUtilities已完成接入。

## 保留的中间场与失败

| 目录时间前缀 | 结果与处理 |
|---|---|
| `203955-993` | 173项后测试读取已关闭页面的Domain为null；保存原Domain后重跑，色板行为断言此前均通过。 |
| `204237-733` | 中间前端PASS284。 |
| `204525-038` | 中间性能PASS44，首次11.294ms、六次重开平均6.621ms；后续小改后以最终性能场为当前数字。 |
| `205427-143` | 188项后新增长名测试使用重建前已销毁的行引用；重新取当前行。 |
| `205718-899` | 201项后业务原生弹窗动画尚未完成，确认按钮射线为空；等待动画后验证实际射线。 |
| `210243-678` | 中间完整前端/大厅/厨房PASS365；随后仅补动作导航顺序与renderer动态高度回归，以当前分场结果为准。 |
| `211205-333` | 最终代码完整场已记录361项通过但没有PASS结果。厨房`s_summer_1_5`到达InLevel并完成开页几何后，Unity原生分配33,177,616B失败，日志明确`System out of memory`，外部runner超时并只结束该测试进程。未证明是哪一模块导致内存压力，也未声称修复此原生崩溃。 |

保留失败目录的player/bepinex日志、断言、截图和runner错误，不叠加失败场通过项报总数。此前布局阶段的未归因厨房退出仍见[原记录](responsive-layout.md)，不能凭本次内存失败反推其原因。

## 安装与原存档收尾复核

十场runner都报告退出后恢复，每场`saves.tsv`非空且记录的原存档当前SHA256一致。收尾对十场安装/原存档做90项核对，85项一致；前五场记录的设置DLL为`3A534BF5…`，当前为`009016E3…`，其余文件和原存档没有差异，早期备份均仍与各自manifest相符。当前DLL与最后五场开始前基线相同；不覆盖该现有文件，也不将其误称为最终构建。差异保留于`.validation/color-actions-restore-audit.txt`。

另对最后五场（含原生崩溃场）重新做**45项当前文件存在性/哈希核对全部通过**，无残留游戏进程，结果为`.validation/color-actions-final-restore-audit.txt`。最终产物`D5A5C82C…`仅在仓库`bin/Release`，未保留到正式安装目录。

## 未覆盖与运行前提

- HostUtilities双界面业务尚未接入。Configuration Manager/Mod Settings单装及同装、旧版无动作方法、真实更新流程、Steam overlay/动态URL回传由其Agent独立验证；本仓库只读核对原调用链并交付提示词。
- 本轮未重跑全部共享选优矩阵、FastInit变体、所有可选MOD缺失、物理设备/IME、Steam联机、其他平台/分辨率；历史矩阵不能当作当前新增功能覆盖。
- 依赖实际游戏BepInEx5/HarmonyX/Unity Mono；替换DLL后重启，源码内置须使用完全相同实现与四个固定LogicalName资源。可选动作须探测方法存在。
- 实机均通过现有隔离runner临时安装，退出后报告原存档核对和安装恢复；构建没有正式部署，没有push或发布。证据目录属于忽略的本地文件。
