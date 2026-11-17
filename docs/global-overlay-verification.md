# 常驻设置层、文字裁剪与顶部关闭

日期：2026-11-17。按用户指定顺序实施：首次门禁后全场景可用 → 修复滚动文字越界 → 搜索框右侧关闭及面板外关闭。仅修改本设置插件；HostUtilities、游戏、DIYLevel、FastInit 原 DLL 未修改。

## 常驻页面与资源

- `NativeShortcut`在首次存档成功且主菜单可交互后捕获模板、创建页面，随后复用同一页面和独立输入域。后续打开不再以 Loading、RootMenu、ProfileReady 或原生弹窗不存在为条件。首次门禁前保留按键请求/再按取消。
- `GlobalSettingsHost`拥有 DDOL ScreenSpaceOverlay Canvas、1920×1080 参考缩放、全屏射线盾及光标恢复。使用最高现有 sorting layer、32767 sortingOrder；同级原生 spinner 后创建时，只按需重新注册自有 Canvas。它不证明优先于任意第三方 IMGUI。
- `GlobalEventSystem`不进入原生可回收域池，不替换玩家主域。页面不加入原生菜单栈；打开时持有自己的域、玩家和 engagement 抑制令牌，追踪场景中新对象，关闭或外部禁用时只释放自己的所有权。输入映射变化仅标记 dirty，在主线程重新绑定。
- 图片、贴图、Sprite 和材质按对象身份去重复制；不可读图片通过 GPU 暂存复制。动态字体不克隆，使用游戏字体 bundle22 及其 fallback 依赖的一次配对引用租约，不长期保留整个 frontend。卸载插件时释放租约和自有资源。
- UI根节点归一到1920×1080，避免克隆来源Canvas的像素尺寸在另一分辨率上再次缩放。

### 乱码的原因

`.validation/20261117-005706-948-without-fastinit/font-assets.txt`记录了动态字体副本与原字体共享材质/图集状态：Header_Font 对应 material97386，Body_Font 对应 material98688。随后独立材质实验虽然使设置文字正常，仍破坏底层原生菜单文字，因此没有采用该方案。

最终保留真实动态 Font 及 fallback 所属包的引用，不另建同字体动态图集。`NativeFontLease`验证实际包路径加载的 Font 对象与来源一致；契约变化明确报错，不静默换一个缺少中文字形的字体。`UnityFonts`记录前端重建前后的 Font/material/texture，并检查没有带缓存后缀的动态字体副本。实际截图同时查看设置页和底层原生菜单；非空对象引用不能替代字形验收。

### 文字越过遮罩的原因

旧路径显式把 `font.material` 赋给 Text，其实际 shader 为 `GUI/Text Shader`，不支持 RectMask2D 的局部裁剪。红测`.validation/20261117-011706-804-without-fastinit`在第44项失败，`text-clipping.txt`保留该 shader 证据。

最终动态 Text 使用默认 UI 材质（`text.material = null`），由 Text.mainTexture 提供当前动态图集；不修改原 Font.material。`UnityClipping`将左右列各滚动91 UI，检查 UI/Default、maskable 和完全越界文字的 CanvasRenderer.cull，并保留部分裁剪截图。

## 顶部布局与面板外点击

- 搜索框宽499.20 UI；右侧关闭按钮163.20 UI，两者高46 UI、水平间隔28.80 UI。它们都固定在顶部，不随右栏滚动；左右方向导航相连。底部只保留更宽的 Description 区域。
- 复用既有 `OverlayDismiss`，每次打开重新初始化。按原生背景Sprite的Tight网格计算实际可见范围，透明留边视作外部，标题横幅仍属于面板内部。面板内空白保持打开，面板外点击通过全屏盾关闭整页，重复打开仍有效。
- 二级弹层持有自己的全屏盾；主页面只在没有二级弹层时允许框外关闭，避免改键捕获关闭后同一次释放继续关闭页面。
- 实测屏幕高度95%和5%的上下空白均可关闭，面板内部空白/标题不关闭；二级菜单框外只关二级菜单。真实射线首个Graphic可以是SettingsBody或ModSettingsPage，沿层级找到的点击handler归属于全屏盾，而不要求首Graphic本身就是盾根。

## 本轮证据

| 命令/场次 | 结果与范围 |
| --- | --- |
| `scripts/test.ps1` | 27项真实ConfigFile桌面断言通过；生产.NET3.5/C#3构建通过。桌面裁剪DTO编译有2个未赋值警告，生产TreatWarningsAsErrors无警告。 |
| `scripts/test-examples.ps1` | 无公共DLL引用的反射消费者及原生自定义编辑器独立编译通过。 |
| `test-unity.ps1 -WithoutFastInit -FixtureCount 30`，`20261117-015731-611-without-fastinit` | **803项真实Unity脚本断言通过**。全部编辑器、配色/抓手、色板/动作异常隔离、左右文字裁剪、前端重建/字体租约、全局弹窗、顶部等高布局、可见面板内外点击及重复关闭。此场未进厨房，厨房单独覆盖。 |
| `test-unity.ps1 -WithoutFastInit -FixtureCount 30 -OpeningPerformance`，`20261117-015900-969-without-fastinit` | **45项通过**。首次submit12.328ms、内容111.867ms就绪、首次最大观测帧22.352ms；六次重开9.354/9.054/9.171/9.255/11.647/9.146ms，平均9.605ms。控件身份保持、隐藏值/结构刷新、自定义编辑器及首次加载取消通过。 |
| `test-unity.ps1 -WithoutFastInit -FixtureCount 30 -ShortcutOnly -WithGameplay`，`20261117-020143-272-without-fastinit` | 最终构建**283项通过**。Root禁用、ProfileReady=false、timeScale=0、真实原生弹窗保留、全局Canvas/射线、真实大厅→Loading→厨房、跨场景同页同域及输入恢复。 |

上述最终803/45/283三场合计**1131项真实Unity脚本断言**，runner均报告进程退出、原存档核对及临时安装恢复。收尾对三场24项安装manifest和3项原存档再次核对，**27项全部一致**，没有残留测试/玩家游戏进程，记录在`.validation/final-global-overlay-closeout.json`。完整场会故意输出`fixture action failure`、`fixture predicate failure`两条错误，随后断言只隔离该动作、页面仍可用；它们是测试注入，不是生产业务按钮出错。测试已增加明确的预期日志说明，不屏蔽生产异常。

### 中间失败及保留证据

- `20261117-013038-531-without-fastinit` **FAIL after 792 assertions**：等高布局已通过，新增测试错误要求射线首Graphic等于全屏盾根；改为真实事件handler归属判断后`014234`场793项通过。该场仅覆盖屏幕边缘，尚不代表可见面板透明留边可关闭。
- `20261117-015420-487-without-fastinit` **FAIL after 795 assertions**：把点移到屏幕高度95%/5%的明显空白后，实际设置背景仍在射线链中，旧矩形Inside把透明留边算成内部。最终从捕获前的原生Tight网格取得归一可见边界，单独包含标题，修复后`015731`场803项通过。
- 早先`012548`场790项、`014234`场793项、`014409`场45项为中间版本，不重复加进最终总数。Font克隆、GUI/Text Shader红测和其他旧失败均保留在各自目录，不改写为PASS。
- 玩家进程数次在测试之间出现，runner均拒绝新安装；确认自然退出后才测试。01:45:36又出现无测试标记的玩家进程，01:45:50原存档更新，安装DLL基线也由071D…变为DD386…；所以更早场的历史manifest不可能继续与当前所有文件一致。没有回滚玩家存档、覆盖外部安装或终止玩家进程。最终803/45/283场均使用当时新基线并再次核对一致。

### 已实际查看的截图与几何

- [最终顶部关闭及双栏边界](../.validation/20261117-015731-611-without-fastinit/header-close.png)：来自803项通过场，搜索与关闭等高，左右文字在Viewport上下边界裁剪，不再悬浮到面板底部。
- [右栏部分裁剪](../.validation/20261117-013038-531-without-fastinit/clipping-Settings.png)：来自保留的792项失败场，只证明该截图状态；最终场同样覆盖该回归。
- [跨场景设置页](../.validation/20261117-020143-272-without-fastinit/settings-after-scene-rebuild.png)。
- [滑条高亮](../.validation/20261117-012548-700-without-fastinit/feedback-slider.png)：抓手始终白色，轨道橙色；开关图标仍64×64 UI。
- 最终基础配色：按钮/输入 RGB(90,150,190)，选项背景 RGB(118,173,209)，高亮 RGB(255,163,57)，选项与分区标题白色。
- 实际PNG3840×2160；页面参考1920×1080。SettingsBody为1920×1012 UI；左右Viewport分别472.66×652.74、1226.64×652.74 UI。行宽1194.64、固定操作列140.49、开关图标64×64 UI。顶部按钮精确几何见同场`layout-checks.txt`，不能把启动参数1280×720当截图尺寸。

## 边界与运行前提

这些是实际Unity对象上的脚本输入/射线/场景验证，不是物理鼠标、键盘、手柄或IME测试，也不是真实Steam联机。未覆盖任意第三方IMGUI顶层竞争、所有可选MOD组合、其他平台及所有分辨率。同步加载阻塞Unity主线程期间，仍不能处理新按键或渲染新帧。历史厨房原生33,177,616B分配失败未归因，没有宣称修复。

最终构建`bin/Release/OC2ModSettings.dll` SHA256：`16D8A3072FAA595B31B0141739BC0E9182AD0C0A5EC1E07B7ED4B1D2363E3192`。本轮不执行正式部署；测试后恢复各场测试前安装，新实现需安装一致源码构建并重启游戏。公共源码内置副本必须同步同一实现，不能只更新部分文件。没有push或发布；HostUtilities双界面接入继续由其Agent按[既有提示词](hostutilities-integration-prompt.md)实施。
