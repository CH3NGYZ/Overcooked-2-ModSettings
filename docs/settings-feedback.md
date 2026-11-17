# 设置行对齐与交互反馈

日期2026-11-16。用户要求修复不同名称长度导致按钮左右错列，指定RGB配色、整行和控件互斥高亮、Description跟随，以及HostUtilities固定首行。

## 当前实现

- `EditorRow.Layout.cs`统一右侧按钮列宽为整行11.76%（49%操作区的24%），恢复默认始终贴右；开关/改键/选色/滑条精确框在其前一列。名称仍按24/32/40/49%分档、与操作区间隔整行2%，超过49%换行。为保证所有行按钮列一致，内部间隔统一为整行1.48%（74%最宽操作区的2%），替代原先随每行剩余宽度变化的间隔。弹性输入/下拉/滑条使用余量；行旁动作挤压输入至160 UI以下时续行。多动作共享此规则。开关图标仍64×64 UI。
- `NativeUi.ThemeControl`和`SettingsFeedback.cs`为搜索、输入、按钮、下拉候选、开关/业务动作提供同一蓝色RGB(90,150,190)背景和橙色RGB(255,163,57)反馈；不再让原生深色sprite乘色改变目标色值。附着反馈时显式终止原生Graphic颜色渐变，避免活跃弹层内输入框创建后继续被旧tween染深。滑条轨道与手柄同样反馈，色板/渐变不改真实颜色。原生厨师入口保留其原生菜单高亮。
- `NativePage.Feedback.cs`页面集中选出唯一反馈目标。行标题/空白区命中时只高亮行，命中子控件时只高亮该控件，清掉之前行或控件的高亮；原生方向导航焦点同样工作。设置行背景继续将拖动/滚轮传给原ScrollRect。
- 选项名称和向量分量名为白色，固定保存提示已删除。底部只显示当前选项Description，搜索/非选项区域没有可用Description时为空，真实验证/保存错误仍通过原错误区域显示。
- `PinnedMods`统一排序权威：精确GUID `com.ch3ngyz.plugin.HostUtilities`存在时固定第一行，无图钉且Toggle无写入；旧cfg中的该GUID不再参与置顶，下一次正常置顶修改清除此旧值。其他置顶项在其后、普通项最后，组内沿用名称/GUID排序。缺失不造占位，其他GUID和置顶记录保留。固定行向右直接进入设置，相邻图钉向上回到固定行。

## 最终验证

- .NET3.5/C#3生产编译、真实BepInEx ConfigFile桌面27项及无公共DLL引用的消费者独立编译通过。桌面裁剪编译有2个DTO未赋值警告；生产TreatWarningsAsErrors构建无警告。
- `test-unity.ps1 -WithoutFastInit -FixtureCount 30`，`.validation/20261116-223644-192-without-fastinit`：**345项真实Unity脚本断言全部通过**。覆盖25个选项白色标题、真实Canvas射线下整行/控件互斥、输入/搜索/滑条/下拉候选配色、Description切换、原生选择焦点、24/32/40/49%所有按钮列、长名称换行/色值与滑条可用宽度，以及真实HostUtilities固定首行/无图钉/无写入/直接向右导航、其他置顶顺序与独立滚动。原有即时保存/错误、色板拖动与取消、动作/业务弹窗、搜索/重开和前端场景重建一并通过。
- 最终性能场`test-unity.ps1 -WithoutFastInit -FixtureCount 30 -OpeningPerformance`，`.validation/20261116-223835-306-without-fastinit`：**44项通过**。cold submit 11.792ms，内容110.136ms完成，首次构建最大观测帧26.114ms；六次warm submit为7.623/10.893/9.356/7.114/7.491/7.699ms，平均8.363ms。重开继续复用控件，没有重新CloneButton/Create行；仍需首次分帧构建，不是首次全内容即时完成，也不是物理设备端到端耗时。两场合计**389项**，不重复加中间场次或历史矩阵。
- 实测行宽1194.64 UI；全部Default横坐标1054.15、宽140.49 UI；Toggle/Rebind/ExactValue横坐标895.98、同宽140.49；名称49%为585.37。普通行68 UI，超长名称测试行125 UI；图标64×64。PNG实际3840×2160。
- 真实PNG为3840×2160。截图：[整行高亮](../.validation/20261116-223644-192-without-fastinit/feedback-row.png)、[仅控件高亮](../.validation/20261116-223644-192-without-fastinit/feedback-control.png)、[长名列对齐](../.validation/20261116-223644-192-without-fastinit/responsive-long-rows.png)、[下拉候选](../.validation/20261116-223644-192-without-fastinit/feedback-dropdown.png)、[色板蓝色输入](../.validation/20261116-223644-192-without-fastinit/color-picker.png)、[色板输入焦点](../.validation/20261116-223644-192-without-fastinit/color-picker-focus.png)。本次已实际查看整行/控件/长名/下拉和修正前后色板图；原始色值/几何在同目录`feedback-geometry.txt`、`responsive-rows.txt`、`picker-theme.txt`。
- 两场runner均报告原存档核对与临时安装恢复；收尾再次核对两场8项安装manifest和1项原存档，**18项全部一致**，无残留游戏进程。正式安装仍为旧DLL，SHA256 `009016E317BF115C2EEC31E0F89E7A7AE84F960158DBDBF2D6B354ADF46B93C5`。最终构建`bin/Release/OC2ModSettings.dll` SHA256 **`071D3468D96F5A52856BAB579AD00C1CE5DF8217E0CBDE1F790AD292C70FE280`**。

## 中间失败与补充检查

- `.validation/20261116-220548-625-without-fastinit`前147项通过，第148项旧滚动断言失败。新增固定行导航测试先改变滚动，但断言基准采在导航之前；将基准移至被测置顶操作之前后，`.validation/20261116-221533-408-without-fastinit`342项通过。失败场仍保留，不记为PASS。
- 上述342项场虽通过，截图人工检查发现色板弹层输入仍为旧深色。`.validation/20261116-223008-469-without-fastinit`343项诊断场证实：Graphic.color已是蓝色，但CanvasRenderer仍被原生tween变为0.761倍。新增延时/最终渲染色、弹层互斥断言，并停止残留tween后，最终345项及截图都通过。不能只用Graphic.color断言替代实际画面。
- 玩家游戏运行时runner前置检查多次拒绝测试，无安装替换，未终止玩家游戏；后续确认自然退出才继续。

## 全场景可用的现状与建议

这次用户询问架构可行性；没有直接移除加载门禁或重构常驻输入层。

现有`NativePageTemplate.Capture`已在存档就绪且主菜单可交互后克隆原生GameOptions，挂在inactive的`ModSettingsNativeTemplate` GameObject并调用`DontDestroyOnLoad`。`NativeTemplateComponents.Rebuild`重建保留UI组件，避免场景AssetBundle卸载后的MonoScript句柄失效。因此目前缺的并非再加一个常驻素材对象。

`NativeShortcut.Tick`明确在Loading时关闭页面并等待，在原生RootMenu无效、CurrentGamepadUser不存在、T17EventSystem未就绪/被占用或原生对话框打开时等待。当前页面还有原生菜单栈、RootMenu、输入域和玩家抑制租约的所有权关系。直接删Loading判断会把这些对象留在已卸载场景，不能保证可用。

若目标包含加载画面、存档切换和没有原生菜单的场景，推荐在首次成功读取存档并捕获模板后建立常驻Overlay Canvas和页面宿主；输入、打开/关闭与焦点不再以某场景RootMenu存在为前提，原生菜单/玩家输入通过可重绑适配器接入。场景切换前释放旧domain/编辑/弹层租约，切换后重绑可用玩家，保留设置目录和页面数据；还要保证图片/字体/material的资源寿命，DDOL本身不能抵抗AssetBundle.Unload(true)。Configuration Manager类常驻IMGUI窗口与依赖原生菜单的这套UI不同。主线程被同步加载阻塞时，Unity无法渲染或响应新的UI帧，常驻也不能消除该停顿。

## 边界

本轮暂不把先前428项或更早完整矩阵合并为当前版本的回归数。上阶段长流程发生Unity原生33,177,616B分配失败的记录仍在[色板证据](color-picker-actions.md)，没有声称已修复。物理鼠标/手柄/IME、真实联机、其他分辨率及HostUtilities业务接入仍需分别验收。

构建不正式部署；测试退出恢复原安装。当前版本1.0.0的源码内置副本需同步相同实现并重启，不能只复制部分文件。没有push或发布。HostUtilities双界面业务仍由其Agent按[提示词](hostutilities-integration-prompt.md)实施。
