# 1.0.0 本地验证记录

验证日期：2026-11-16至2026-11-17。生产编译、桌面边界、真实 Unity 内脚本输入与物理设备/联机分别记录。

最新即时搜索、无回车写盘、持续校验提示和色板内就近提示见[即时输入验证](immediate-input-verification.md)：最终完整859项、性能45项，合计904项；专项79项不重复计数。最终收尾除外部替换的FastInit DLL外，安装与原存档32项哈希/存在性核对一致，外部新DLL保留且未覆盖。

最新鼠标点击定位、局部拖选和CategoryOrder见[输入与分区排序验证](text-selection-category-order.md)：最终完整842项、性能45项，另有专项58项及桌面36项；中间场不重复计数。

最新门禁后全场景可用、字体/遮罩修复、顶部关闭和面板外点击见[常驻设置层验证](global-overlay-verification.md)。统一列宽、互斥高亮、Description和固定首行的历史阶段见[界面修订](settings-feedback.md)。色板、紧凑颜色行和公共动作API的上一阶段证据见[色板和业务动作验证](color-picker-actions.md)。HostUtilities业务接入交给其Agent，完整任务见[双界面接入提示词](hostutilities-integration-prompt.md)。

打开卡顿优化的起因与前后测量见[打开性能记录](opening-performance.md)；分档布局、改键文案、输入背景及该阶段381项Unity脚本回归见[配置行验证](responsive-layout.md)。
以下实现、产物、共享服务矩阵和截图均保留为色板实施前的历史快照，不将其断言数计入本轮回归，也不把旧RGBA布局或旧DLL哈希当作当前产物。

## 历史实现与产物

- 公共 GUID：`com.ch3ngyz.plugin.overcooked2.modsettings`；生产版本 `1.0.0` / `1.0.0.0`。
- 产物：`bin/Release/OC2ModSettings.dll`，SHA256 `74B891B416AD10D702AFDE1E7343654A36F47FB631F16277EE4F2C1EF328F2E4`。
- 36个生产 C# 文件，最长351行；按服务、目录/租约、原生页面、模板、输入与编辑器分工。
- 取消重复 MOD 标题，当前 MOD 搜索固定在蓝色大标题右侧；分类居中、分隔与子项缩进。
- 名称、编辑器、默认值操作同行：普通最少68 UI、向量最少88 UI、颜色最少244 UI；无编辑确认按钮或隐藏占位。名称按实际字形选择24%/32%/40%/49%，超长换行增高；行间与操作区间距及当前测量见[配置行验证](responsive-layout.md)。
- 有效文本变化、开关立即保存，非法中间输入标红不写配置；滑条拖动预览、松手保存。有效修改清除上次校验错误，取消不回滚已保存值。
- RGBA四条联动渐变轨道＋精确输入，透明度棋盘格，底部Hex/预览；外部刷新保留未保存颜色预览，取消后显示最新实际值。
- KeyCode为单键下拉＋同行“改键”＋可选默认按钮；“改键”切为“请按键”，捕获完成自动保存并更新左侧。下拉按数值去重、过滤允许值，有独立滚动条；KeyboardShortcut独立显示值与改键按钮，保留组合键弹层。
- 厨师入口原生Highlight显式管理，悬停/点击均获得焦点、离开清除高亮。
- 开关使用游戏 `OptionValue.On` / `OptionValue.Off` 当前语言文案；简体中文为“开/关”。xicons Material SVG 原样保留，预转换透明 PNG 嵌入程序集，运行时不依赖网络或 SVG 解析器。
- 二级菜单拥有全画布遮罩；框外点击关闭、框内空白保留，射线不穿透；关闭捕获释放输入。
- 体验优化参考仓未修改。其四个图钉 SVG/PNG 保持逐字节相同；新增开关资源单独记录来源和 Apache-2.0 许可证。
- 测试只临时安装，结束后恢复原有 DLL/配置。当前构建没有正式部署，安装升级需要重启。

## 历史编译、桌面与模板

```powershell
& .\scripts\test.ps1
& .\scripts\test-examples.ps1
```

当前生产使用实际游戏引用、.NET Framework 3.5、C# 3和 TreatWarningsAsErrors，构建通过。
真实 BepInEx ConfigFile 的桌面边界测试 **19项通过**，覆盖精确转换、限制、保存与租约替换。
桌面测试裁剪编译部分生产文件，有4个 CS0649 DTO字段未赋值警告；生产完整构建无这些警告。
消费方反射桥、示例插件和原生布尔编辑器独立编译通过，不引用公共服务 DLL。

技能接入/控件模板替换占位符后，已用 `/noconfig /nostdlib+ /langversion:3 /warnaserror` 编译；
结果在 `.validation/skill-templates`。这不是所有消费方已接入的证据。
技能 frontmatter、Markdown引用、模板语法、原生弹窗模板双命名空间编译与61项模板检查另行验证；
修改仅限 `E:\code\Agents\magpie-skills\bepinex-mono-modding`，不改 Library/Agents 安装副本。

## 历史真实 Unity 矩阵

```powershell
& .\scripts\test-unity.ps1 -WithoutFastInit -WithGameplay -FixtureCount 30
& .\scripts\test-shared.ps1
```

以下均包含当前入口焦点、即时保存、颜色渐变与KeyCode两种编辑方式，**五场共1147项断言通过**：

| 场景 | 测试 MOD数 | 断言 | `.validation/` 下证据目录 |
|---|---:|---:|---|
| 独立1.0.0，无 FastInit，含真实厨房 | 30 | 293 | `20261116-173709-085-without-fastinit` |
| 两个内置1.0.0，同版本 | 10 | 221 | `20261116-173918-532-with-fastinit` |
| 内置1.0.0 / 0.9.0 | 1 | 191 | `20261116-174001-534-with-fastinit` |
| 独立1.0.0 ＋内置1.0.0 / 0.9.0 | 30 | 221 | `20261116-174041-111-with-fastinit` |
| 独立1.0.0 ＋内置1.1.0 / 0.9.0 | 10 | 221 | `20261116-174124-382-with-fastinit` |

测试版本只编译进忽略的 staging，生产源码仍为1.0.0。同版本使用相同实现。
“1/10/30”为注册的测试 MOD 数量，自动发现的已安装 MOD 同时存在，并非列表总数。
共享矩阵汇总在 `.validation/shared-20261116-173915/results.txt`；四种场景均PASS。
内置场景覆盖四个PNG从当选服务程序集加载、宿主业务继续运行和配置不串组。

覆盖：

- 自然存档读取成功并进入可交互主菜单后创建一个厨师入口、一个公共服务和页面。
- 最高版本/同版本选优、同名GUID排序、长名两行、空配置隐藏、动态刷新和搜索无结果。
- 图钉持久化、前移/取消、与已有用户置顶共存、置顶不激活及导航；独立滚轮和两侧原生拖动条。
- 当前搜索固定在右上、分类居中/分隔、七种行内控件几何、原生开关文案及外部改值刷新。
- 所有整数、float/double/decimal精确值、非法/溢出/范围、候选/枚举/Flags、百分比、RGBA/Hex及复合分量。
- 有效文本变化即时写内存及磁盘、非法中间值、取消保留已保存文本；数值与RGBA滑条预览/松手提交/取消。
- KeyCode列表单键去重、框外关闭、选中即保存、行内监听文案/完成/取消/关闭/外部禁用、隐藏默认按钮回收布局；KeyboardShortcut取消/清空。
- 颜色四通道渐变/精确值/Hex联动、真实事件系统拖动到松手保存、外部值变更期间保留预览、取消显示最新实际值。
- SaveOnConfigSet=false显式保存、真实保存失败显示错误并保留实际回读值。
- 自定义渲染器生命周期、动态高度、所有子控件导航、输入捕获及新旧租约替换释放。
- 二级菜单内部空白保留、全画布遮罩优先射线、框外关闭不改配置、捕获关闭释放输入。
- 前端拆除重绑、StartScreen自然重建后绑定不同root的当前输入域、唯一入口与可点击控件。
- 主菜单/大厅/厨房反引号路径、活跃输入所有者等待/取消、加载中排队、外部禁用及状态恢复。

这些是**真实 Unity 内脚本输入**：按钮用 Unity 事件系统，固定键调用生产 Update 使用的
`NativeShortcut.Tick(true)`，文本经实际编辑器ChangeText/确认收尾；滑条经pointer/beginDrag/drag/endDrag，
KeyCode成功捕获用生产Update调用的AcceptKey路径，候选选择用原生submit事件。
不能据此声称物理反引号键、空格键、鼠标、手柄或 IME 输入已经通过。

## 真实大厅与厨房

从原生本地街机入口进入 `ThemeSelectRootMenu` 大厅，对当前主题按钮发送原生 submit，
经游戏自身倒计时和加载进入 `s_mine_4_6`，核对 `GameState.InLevel` 和厨房流控制器。
加载中快捷键请求不显示页面，抵达厨房后自动打开；再次按相同路径关闭。
厨房中两位 PlayerControls 在页面打开时受抑制，关闭/外部禁用后恢复各自原状态。
没有修改 timeScale，不暂停世界或联机流程。

模板先经过真实前端来源捕获。在来源仍存活时用 runtime AddComponent 重建保留UI组件，
避免 AssetBundle.Unload(true) 使DDOL克隆的序列化MonoScript卸载，后续Instantiate丢T17Scrollbar。
大厅用独立scene-root overlay画布；厨房打开前只清理已释放的suppressor，保留其他活跃所有者。
测试沙箱路径统一正斜杠，避免PCSaveManager.SaveFile按“/”截目录时出现负length；未修改原SaveFile。

## 历史截图与几何

已经实际打开查看：

- [30个测试MOD与行内布局](../.validation/20261116-173709-085-without-fastinit/settings-30-mods.png)
- [RGBA渐变滑条和精确输入](../.validation/20261116-173709-085-without-fastinit/color-gradients.png)
- [KeyCode单键列表](../.validation/20261116-173709-085-without-fastinit/keycode-list.png)
- [KeyCode请按键状态](../.validation/20261116-173709-085-without-fastinit/keycode-capture.png)
- [厨师入口原生焦点](../.validation/20261116-173709-085-without-fastinit/chef-entry.png)
- [二级菜单与全画布遮罩](../.validation/20261116-173709-085-without-fastinit/secondary-dialog.png)
- [真实厨房内打开](../.validation/20261116-173709-085-without-fastinit/shortcut-kitchen.png)
- [本地街机大厅内打开](../.validation/20261116-173709-085-without-fastinit/shortcut-arcade.png)

截图3840×2160。启动参数的1280×720不是实际渲染大小，以PNG和geometry为准。
下表均为Canvas UI单位，不能当作像素或混用两种画布坐标：

| 项目 | 前端 | 大厅/厨房overlay |
|---|---:|---:|
| 背景框 | 1920×1012 | 2021×1012 |
| 蓝色大标题中心偏差 | 0 | 0 |
| 右上搜索框 | 691.2×46 | 727.56×46 |
| 蓝条到搜索横向间隔 | 42.6001 | 45.63013 |
| 搜索到设置视口间隔 | 42.77997 | 42.77997 |
| 左栏视口 | 472.66×652.74 | 499.10×652.74 |
| 右栏视口 | 1226.64×652.74 | 1292.75×652.74 |
| 提示到关闭按钮横向间隔 | 38.4 | 40.42 |

两栏滑轨20 UI宽、644.74 UI高，各预留30 UI gutter。当前30测试MOD前端左/右thumb比例
为0.2385746 / 0.3002484，与实际内容比例相符；单MOD非溢出轨道验证通过。
配置显示区域比最早布局上移45.54 UI、高度从607.20增加至652.74 UI。
厨师入口自有贡献高度47.13333 UI、总高226.5333 UI，重绑后相同，不重复扩高。
提示在原背景缺口右侧；普通配置名与控件的中心纵差≤3 UI、横间≥8 UI。
RGBA每轨道491.49×22 UI，精确框119×40 UI，横间16 UI，通道行距46 UI。
KeyCode值按钮548.27×48 UI，“改建/请按键”110×48 UI、默认按钮132.05×48 UI，横间分别16及17.95 UI。
原始数据为对应目录的 `layout-checks.txt`、`scrollbars.txt`、`entry-restore.txt`、`color-geometry.txt`、`keycode-geometry.txt`。
截图请求后等待帧末保存，再隐藏页面。

## 环境、恢复与历史证据

Windows Steam，Unity `2017.4.8.5945332`，BepInEx `5.4.23.5`，CLR `2.0.50727.1433`，
0Harmony程序集 `2.9.0.0`。游戏目录 `E:\SteamLibrary\steamapps\common\Overcooked! 2`。
除FastInit有/无变体外保留现有MOD，不能当作干净安装或全部可选MOD组合的验证。
每次测试在原生登录前隔离PC/DIYLevel存档，保护至退出；五场均报告原存档核对与临时安装恢复。
收尾重新核对五场manifest与原存档，共49项当前文件存在性/哈希检查通过，未发现残留测试进程。
只管理命令行带当次唯一目录的进程；先前玩家会话阻挡部署时保留会话，确认自然退出后才继续。
日志中仍有其他MOD及游戏自身告警/异常，不声称零告警环境。跳过重复/旧版服务的Skipping属于预期。

快捷键之前五场696项、49项恢复核对，以及中间版50项厨房/五场867项/五场1048项均保留历史证据，
不与当前1147项合并报数。867项在即时保存前；1048项在KeyCode修改前；284项在补充关闭兜底前。
旧“右侧MOD标题下移24 UI”要求已被最新“取消标题、右上搜索”替代。
早期失败目录也保留，不以最终PASS覆盖：

| 时间前缀 | 失败与处理 |
|---|---|
| `131437` / `133241` / `140126` | 输入释放缓存、入口射线、延迟选中/复合导航；分别修正后当前矩阵复验。 |
| `140505` / `140909` / `141143` | OS注入未证明送达或未获前台；没有作为物理输入通过证据。一次恢复锁冲突按manifest恢复核对。 |
| `141350` / `144440` | footer缺口与滑轨Graphic命中区；调整后当前几何/射线/拖动通过。 |
| `151915` / `153123` | 沙箱反斜杠触发SaveFile截串、DDOL模板丢滚动组件；统一路径与runtime组件重建，当前跨场景复验。 |
| `152414` / `152601` | 旧标题下移的间距断言；当前取消重复标题，验证固定搜索布局。 |
| `152801` | 非溢出轨道不保证value=1；断言内容在顶且thumb占满，当前1测试MOD场景通过。 |
| `153000` / `153500` / `153846` | 打开早于可交互及误认大厅根；按真实就绪和ThemeSelectRootMenu修正。 |
| `154505` / `154919` | 大厅嵌套Canvas与厨房inactive输入域残留released令牌；当前大厅/厨房复验通过。 |
| `162850` | 测试假设第一个MOD必为fixture，忽略用户已有置顶；改为记录并比较原完整顺序，当前五场通过。 |
| `165159` | 复现入口悬停无焦点：CustomChef依赖Animator的Highlight，克隆移除动画后未绑定targetGraphic。显式绑定原生Highlight和选中/离开事件，当前五场复验通过。 |

新增捕获关闭测试曾尝试直接调用private NativeEntry.Open，编译失败且未部署；改为原生入口submit后通过。

## 未验证与运行前提

- 物理键鼠/手柄、实际中文IME、成功捕获真实单键/组合键、剪贴板实际按键仍未验；捕获禁用输入域时框外鼠标分支只实现并检查生命周期，未证明物理事件送达。
- 未验真实Steam邀请/联机、其他平台/实际分辨率、厨房退出后重建及打开页面后热加入玩家。
- 未穷举全部可选MOD缺失、自定义转换器异常、极端有符号/高位Flags，未遍历切换全部13种语言。
- 最高候选加载失败不自动回退来自安装加载器检查，未故意破坏最高版本做实机失败注入。
- 安装升级后重启，依赖真实游戏程序集及BepInEx 5/HarmonyX；源码内置须四个固定LogicalName的PNG。
- 源码本地提交、远端push、Library刷新、Agents同步是独立状态，本次仅授权本地提交。
