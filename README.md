# 原生 MOD 设置 1.2.0

《胡闹厨房 2》Windows Steam / Unity Mono 的公共设置页面。入口位于主菜单 **厨师 → MOD 设置**，在“选择厨师”下方。GUID 为 `com.ch3ngyz.plugin.overcooked2.modsettings`。

## 安装与使用

从 [GitHub Release](https://github.com/CH3NGYZ/Overcooked-2-ModSettings/releases/latest) 下载运行包，解压后将 `OC2ModSettings.dll` 放进游戏 `BepInEx/plugins`，重启游戏。需要 BepInEx 5 / HarmonyX；不需要 Configuration Manager、DIYLevel 或 FastInit。也可以用 `scripts/build.ps1` 自行构建，产物位于 `bin/Release/`，不会自动部署。

默认按 **反引号／波浪号键（`KeyCode.BackQuote`）** 开关页面。左下角只显示当前键；点一下，按字母、数字、标点、小键盘数字／运算键或 F 键等非保留单键，立即保存到 `Navigation.ToggleKey`。Ctrl、Alt、Shift、Windows／Command、锁定键、Tab、空格、导航／编辑／系统键和鼠标、手柄不参与录入；Esc 取消。录入完成后，再按一次新键才会开关。按住 Ctrl、Alt 或 Windows／Command 时不触发全局开关。在本页输入框里，绑定的普通字符键仍可正常输入；默认反引号和 F 键保留直接关闭页面的行为。

首次成功读取存档且主菜单可交互后，一次缓存原生界面素材并创建独立常驻画布与输入域；此后主菜单、本地街机大厅、厨房、加载中、原生弹窗打开或场景菜单暂不存在时均可用当前快捷键开关。首次门禁前按键只记住请求，再按一次取消。关闭释放自己持有的菜单/玩家输入令牌，保留底层菜单和弹窗状态；从厨师入口打开则回到入口。厨房中不暂停世界或联机流程。Unity 主线程同步阻塞期间仍无法处理新按键或渲染新帧。

左栏列出已加载且有可见配置或已注册业务动作的 MOD；`com.ch3ngyz.plugin.HostUtilities`存在时固定第一行，不显示图钉，也不参与置顶；其他置顶项统一在它下面，然后是普通项，每组按名称排序，同名按 GUID 稳定排列。没有HostUtilities时不生成空占位。悬停和焦点移动只高亮，点击/确认才激活。其他MOD右侧使用体验优化 MOD 同款图钉，悬停或聚焦显示，置顶后金色常显；置顶按 GUID 保存在公共服务自己的 `Navigation.PinnedMods`，重启保留。点击图钉不切换当前 MOD。右栏支持分类、当前 MOD 搜索、高级项、默认值和刷新。左右各有原生可拖动竖向滚动条，独立滚动；会话内记住最后选择、各 MOD 的右栏滚动和焦点。

首次打开先显示原生页面，再在主线程按约4毫秒工作预算分帧创建 MOD 列表和设置控件；单个控件或消费方回调不可中途抢占。加载时可以直接关闭，再次打开会正常重建。后续打开在目录、元数据、语言和输入域不变时保留已有控件，只回读最新值并恢复导航；新增/移除配置、API 刷新或自定义编辑器租约变化仍会重建。

文本点击进入编辑，支持光标、Home/End、Ctrl+A/C/X/V 和 IME 组合预览；搜索随输入立即筛选，连续输入、替换和清空均不需要回车，刷新结果保留搜索焦点，选中结果后不抢回焦点。配置输入校验通过立即生效并保存，非法中间值标红、保留编辑，不覆盖配置；编辑期间在下方显示“数值有误”，修正或结束编辑后恢复Description。色板Hex与透明度的校验提示直接显示在弹层对应输入框下方。开关立即保存。滑条拖动只预览，松手提交；手柄方向调整后确认提交。取消结束输入或丢弃未保存的滑条预览，不回滚已经保存的有效更改。组合键捕获支持 Esc/取消键返回、Delete/Backspace 或清空按钮清空。设置继续写到各 MOD 自己的配置；保持 SaveOnConfigSet，自动保存关闭时每次提交显式保存；保存失败保留真实错误并回读实际值，不冒充保存成功。刷新不会执行磁盘 Reload。

KeyCode 左侧按钮展开单键候选二级菜单，去除枚举别名重复项、遵守配置允许值；菜单有独立滚动条，框外点击关闭。其右侧、恢复默认之前是“改键”按钮，点击变为“请按键”，捕获到一个键后立即保存并更新左侧值，文案恢复；Esc/取消结束监听。KeyCode 始终保存单键，KeyboardShortcut 将当前组合和“改键”按钮分开显示，继续使用组合键捕获弹层；当前全局开关键优先切换公共页面。

共享文本输入支持鼠标点击移动光标、正反拖动选择局部文本、Shift方向键扩选及局部复制/剪切/粘贴；选区用独立裁剪图形绘制，拖选不滚动外层列表。搜索、数值、分量和颜色输入共用该实现；有效值仍立即保存，非法中间输入不覆盖配置。

分区支持独立`CategoryOrder`（高值在前），配置项与独立动作的`Order`仍只控制分区内先后。消费者可从自己的固定Section声明顺序生成分区值，翻译只改标题，不改变次序或历史cfg section/key；同区取统一分区值。字段、默认/冲突规则及旧版本行为见[API元数据](docs/api-v1.md)。

返回顺序：弹层/捕获 → 当前编辑/未保存预览 → 左栏 → 关闭页面并回到入口。点击设置面板外的上下空白可关闭整页，面板内空白保留；二级菜单存在时，框外点击只关闭该弹层。遮罩覆盖整个画布，避免点击穿透，捕获窗口关闭时释放输入。原生游戏 OptionsData 的重置/保存逻辑不参与本页面。

方向导航：普通MOD行向右进入图钉，再向右进入当前 MOD 的设置；固定首行直接向右进入设置。图钉向左回到所属行，设置在未消费左方向时回到当前 MOD 行。厨师入口悬停/选中显示原生高亮。搜索框固定在蓝色大标题右侧，关闭按钮在搜索框右侧且等高，左右方向可互相导航。分区标题居中并带分隔，子项缩进；选项标题和分量标题使用白色，名称宽度按实际字形选择24%/32%/40%/49%档位，与操作区间隔整行2%。超长名称在49%内换行并增加行高，保持22号字。普通及颜色行最少68 UI、向量最少88 UI。为避免不同名称档位造成锯齿，开关、改键、恢复默认、选色和滑条精确输入统一使用整行11.76%的宽度，按钮列从右向左对齐；列间隔统一采用最宽操作区的2%，即整行1.48%，不再按单行名称长度变化。文本/下拉和滑条填满余量，隐藏默认按钮后收回空间，行旁业务按钮挤压输入至160 UI以下时改为续行。窄按钮允许文案缩至12号，开关图标仍为64×64 UI。

搜索、输入、下拉、开关和业务按钮跟随游戏主题的普通色与高亮色，选项行和分区背景由普通色派生。首次过门禁时从游戏已加载的原生菜单按钮取色并缓存；修改了对应按钮贴图的主题也适用，无法取色时回退蓝底、橙色高亮。更换 bundle 后需重启游戏。分区标题使用白色；滑条轨道高亮时抓手始终白色，色板的实际颜色与渐变保持真实色值。悬停子选项的标题或空白区域时只高亮整行，移到行内控件时取消整行高亮，只亮该控件；鼠标与方向焦点共用一个页面反馈状态。底部显示当前选项的原 Description，缺失时为空，验证/保存错误仍显示真实错误；已移除固定保存说明。

颜色行保留Hex、棋盘颜色预览和“选色”入口。色板弹层提供二维饱和度/明度、彩虹色相、A透明度条及0–1精确透明度框，Hex与预览实时联动。Hex支持6/8位、有无`#`，6位视为不透明；合法输入立即保存，拖动松手保存。方向键在色板上先确认进入编辑，再确认保存。返回、框外点击或关闭丢弃未保存预览，不回滚已保存修改；外部值变化后取消会读取最新实际值。截图与实测见[色板和业务动作验证](docs/color-picker-actions.md)。

## 接入与源码内置

无需接入即可自动发现 `Chainloader.PluginInfos` 中已加载插件的 Config。显示信息或自定义编辑器参见 [API v1](docs/api-v1.md) 和 [接入示例](examples/ConsumerPlugin.cs)。示例使用软依赖、缓存反射及共享类型，无需引用公共插件 DLL。

[源码 SDK](sdk/README.md) 随仓库提供：`sdk/ModSettingsAttributes.cs` 声明名称、说明、分区及排序等显示元数据，`sdk/ModSettingsClient.cs` 提供刷新、业务按钮和原生编辑器接入；可直接复制、源码链接或导入 `sdk/ModSettings.SDK.projitems`，不依赖 Configuration Manager。MOD 自己切换语言后，更新 tag 再统一 Refresh；新旧声明兼容及覆盖规则见 SDK 文档。新 tag 需要 1.1.0 或更新的实际当选服务。

公共页面的按钮、提示、搜索、改键及色板跟随游戏的12种官方语言，开关使用游戏原生译文。消费方可以继续独立选择自己的语言，通过`ObjToStr`让当前值和下拉候选一起翻译；已打开的内置下拉也支持刷新，保存值保持原始身份。仅有启动时生成的翻译字符串时，需消费方提供对应formatter，接入与历史值兼容边界见SDK说明。

业务按钮通过可选`RegisterActionV1`注册：没有`Entry`为独立行，提供原配置项时追加行旁按钮，继续复用内置输入和保存。支持轻量`CanExecute`、替换/释放租约、异常隔离及`ClosePage`先关闭公共页再执行业务；动作不参与恢复默认和配置保存。消费者需探测实际方法，旧版缺方法时跳过接入，不能只凭版本字符串判断。HostUtilities真实调用链见[按钮接入调查](docs/hostutilities-actions.md)，可将[双界面接入提示词](docs/hostutilities-integration-prompt.md)交给其Agent实施，同时保留Configuration Manager原有入口；本仓库未修改HostUtilities。

单 DLL 内置：将本项目 `src/**/*.cs` 及 `sdk/*.cs` 作为源码编译进宿主，并按 [内置清单](docs/api-v1.md#源码与资源内置) 嵌入四个 PNG（两种图钉、两种开关）。保持全部公共源码、资源、命名空间、GUID 和版本不变，使用相同游戏引用，不复制本项目 `Properties/AssemblyInfo.cs`。BepInEx 选出最高版本的公共插件；宿主自身继续使用自己的 GUID。不要使用 `PatchAll(宿主程序集)` 安装公共服务补丁。公共服务只安装自己的明确补丁。

同版本必须是同一实现；内置后不要让宿主直接访问其程序集内的公共服务 static Instance，应始终反射调用 `Chainloader.PluginInfos[GUID].Instance`。升级要重启；最高候选加载失败时，加载器不会自动回退到已跳过旧版，需移除失败候选后重启。不要把两个插件条目合并成一个 BepInPlugin。

## 本地验证

- `scripts/test.ps1`：真实 BepInEx ConfigFile、已构建提供端及独立 SDK 程序集的桌面边界测试。
- `scripts/test-examples.ps1`：SDK、消费方和自定义控件按.NET3.5独立编译，无服务DLL引用。
- `scripts/test-unity.ps1` / `-WithoutFastInit`：自然登录、隔离存档、真实原生 UI 与脚本输入，保存截图/几何并恢复临时安装。
- `scripts/test-unity.ps1 -WithoutFastInit -WithGameplay`：完整页面回归后，经本地街机大厅原生确认、倒计时加载进入真实厨房，验证全局快捷键路径；`-ShortcutOnly` 只跑快捷键、前端重建、大厅与厨房分支。
- `scripts/test-unity.ps1 -OpeningPerformance`：真实入口首次/六次重复打开耗时、观测帧间隔、控件身份、隐藏期间值/结构变化、自定义编辑器和首次加载取消回归；原始数据写入 `opening-performance.txt`。
- `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 30 -TextEditingOnly`：即时搜索、无回车写盘、非法草稿/底部提示、两种自动保存策略的写盘失败，以及鼠标选区和分区顺序专项。
- `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 3 -LocalizationOnly`：覆盖真实语言API的专项，检查12语现有控件、独立MOD语言下拉刷新、候选重排、编辑草稿和弹层输入租约；测试不写玩家语言设置，实际字形仍需检查截图。
- `scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 3 -ThemeFixture`：只在测试进程内重着色已加载的原生按钮贴图，验证粉底、蓝高亮及白色滑条抓手；不修改磁盘上的 bundle。取色证据见[主题与快捷键验证](docs/theme-shortcut-verification.md)，当前页脚按钮见[快捷键录入验证](docs/shortcut-capture-verification.md)。
- `scripts/test-shared.ps1`：临时编译同版/异版/混合内置副本，真实 BepInEx 启动选优。测试版本只存在于忽略目录。
- `scripts/test-shared.ps1 -BuildOnly`：仅验证四份源码内置程序集编译，不安装或启动游戏。

证据与覆盖限制见 [语言与下拉刷新验证](docs/localization-verification.md)、[SDK验证](docs/sdk-verification.md)、[验证记录](docs/verification.md)、[即时输入和校验反馈](docs/immediate-input-verification.md) 和 [常驻画布、字体及遮罩验证](docs/global-overlay-verification.md)。物理设备、联机和其他平台需单独验证。实机脚本拒绝接管已有玩家游戏，只结束带本轮唯一标记的测试进程。完整场会故意抛出`fixture action failure`及`fixture predicate failure`，验证业务动作异常隔离；结果以同轮`result.txt`及恢复核对为准。
