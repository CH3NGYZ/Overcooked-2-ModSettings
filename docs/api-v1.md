# API v1 与编辑器协议

消费者以软依赖声明公共 GUID，通过 `Chainloader.PluginInfos[GUID].Instance` 缓存反射调用。所有服务方法签名只用共享的 BepInEx、Unity、BCL 类型。调用方捕获接入错误，不阻断 MOD 主业务。[源码 SDK](../sdk/README.md)、[完整桥接](../sdk/ModSettingsClient.cs) 和 [控件示例](../examples/NativeBooleanRenderer.cs) 可由 `scripts/test-examples.ps1` 编译。SDK 中的 tag 按公共字段读取，无需与服务共享 CLR 类型身份。

## 源码与资源内置

把 `src/**/*.cs` 及 `sdk/*.cs` 编入宿主（SDK 也可通过 `sdk/ModSettings.SDK.projitems` 导入），保留 `OC2ModSettings` 命名空间及固定 GUID/版本；宿主自己的 BepInPlugin 类型仍使用自己的 GUID。保留同一实现，排除本项目的 `Properties/AssemblyInfo.cs`，并将以下资源嵌入宿主程序集，LogicalName 必须相同：

```xml
<EmbeddedResource Include="assets\icons\pin-outline.png">
  <LogicalName>OC2ModSettings.Assets.PinOutline.png</LogicalName>
</EmbeddedResource>
<EmbeddedResource Include="assets\icons\pin-filled.png">
  <LogicalName>OC2ModSettings.Assets.PinFilled.png</LogicalName>
</EmbeddedResource>
<EmbeddedResource Include="assets\icons\toggle-on.png">
  <LogicalName>OC2ModSettings.Assets.ToggleOn.png</LogicalName>
</EmbeddedResource>
<EmbeddedResource Include="assets\icons\toggle-off.png">
  <LogicalName>OC2ModSettings.Assets.ToggleOff.png</LogicalName>
</EmbeddedResource>
```

资源从当选服务类型的程序集加载，不从消费方或游戏目录猜测。图钉沿用只读参考项目 `Overcooked-2-DIYLevel-ExperienceOptimization/assets/icons` 的原有资源；开关来自 xicons 的 Material SVG，原始 SVG、生成的透明 PNG 及许可证见 [图标说明](../assets/icons/README.md)。Unity 2017 使用嵌入 PNG，运行时无需 SVG 解析器或网络。完整游戏引用见项目文件。仅通过反射消费 API 的 MOD 无需复制源码或资源。

BepInEx 选择最高公共版本，同版本候选必须拥有相同实现；升级重启生效。失败候选不会自动回退到已跳过旧版，需移除失败副本后重启。公共服务仅安装自己的明确补丁，宿主不应对包含公共源码的程序集调用 PatchAll。

## 公共方法

| 方法 | 返回/含义 |
|---|---|
| `GetApiVersionV1()` | `int`，当前为 1 |
| `GetVersionV1()` | `string`，当选实现版本 |
| `GetStatusV1()` | `string`，`ready` 或 `unavailable`，指服务补丁可用，不代表当前前端已就绪 |
| `RegisterConfigV1(string ownerGuid, string displayName, string description, ConfigFile config)` | `IDisposable`，覆盖该 GUID 的显示信息和配置来源 |
| `RegisterModDisplayNameV1(string ownerGuid, string displayName)` | `IDisposable`，仅覆盖左栏和快捷键提示里的 MOD 名称；可选能力，消费者应探测此方法 |
| `RegisterTranslationsV1(string ownerGuid, IDictionary<string,IDictionary<string,string>> translations)` | `IDisposable`，登记指定 MOD 按语言组织的静态译文；可选能力，消费者应探测此方法 |
| `ConfigureEntryV1(ConfigEntryBase entry, IDictionary<string,object> metadata)` | `IDisposable`，覆盖配置项显示信息 |
| `RegisterRendererV1(ConfigEntryBase entry, IDictionary<string,object> protocol)` | `IDisposable`，注册原生控件工厂 |
| `RegisterActionV1(string ownerGuid, string actionId, IDictionary<string,object> descriptor)` | `IDisposable`，注册独立动作或配置项旁按钮；可选能力，消费者应探测此方法 |
| `RefreshV1()` | 使目录和编辑器重建，读取当前内存配置，不做磁盘 Reload |

注册表并发安全；注册/释放只标记变化，UI 在 Unity 主线程更新。消费者的 Unity 控件/委托必须在主线程操作。元数据和协议字典在注册时复制；需更新时重新注册。相同键后注册替换旧注册；旧租约释放不能删除新注册。当前租约释放后恢复没有该类型覆盖时的行为，不恢复更早已被替换的注册。插件结束时释放自己的租约。

## MOD 名称

`RegisterModDisplayNameV1` 的 `ownerGuid` 和 `displayName` 均不能为 `null`、空字符串或全空白。GUID 按原样与目录中的身份以 `StringComparer.Ordinal` 精确匹配，不修剪或忽略大小写；名称会去掉首尾空白。原名称依次取当前名称租约、`RegisterConfigV1` 的名称、BepInEx 插件名，没有前述来源的纯动作 MOD 用 GUID；[翻译](mod-translations.md)在这些原名称之前生效。名称租约不会注册配置或动作，也不会让原本不在目录里的 MOD 多出一个左栏项。

同一 GUID 的新租约替换旧租约；应先取得新租约，再释放旧租约。释放当前租约恢复原名称回退规则，不恢复已被替换的名称。注册和释放仅通知名称变化，不需 `RefreshV1()`：打开时在现有约 0.2 秒刷新周期原位更新，关闭时下次打开生效。左栏按固定首位的 HostUtilities、置顶、普通分组，后两组内用 `StringComparer.Ordinal` 按 GUID 排序；改名不改变顺序、置顶、焦点、滚动、编辑和配置值。快捷键重复提示使用更新后的 MOD 名称。

此方法仍属 API v1 的可选能力。SDK 调用前先 `Connect()`，再检查 `SupportsModDisplayName`；公开 1.2.1 没有此方法，保留原显示名称，不影响配置发现。此方法接受单个名字；需要跟随游戏语言时，新 MOD 优先用下述翻译接口。

## 翻译

从提供端 1.3.0 起，`RegisterTranslationsV1` 的外层字典是语言代码，内层是字段键与译文。注册时复制两层字典；同一 `ownerGuid` 的新租约替换整张表，不与旧租约合并。释放当前租约后，服务继续使用缓存的文件译文和原文。注册翻译不会创建 MOD、配置或动作。SDK 通过 `SupportsTranslations` 探测实际当选服务，调用 `RegisterTranslations` 和 `ModSettingsTranslations` 键生成器；不需额外调用 `RefreshV1()`。

新消费方按 [SDK 接入约定](../sdk/README.md#让-mod-文案跟随游戏语言)一次提交 12 语整表，不自行选语或随游戏换语重新注册。接口仍允许缺项并回退，这与新消费方应提交全量语言的接入约定相互独立。

GUID 和字段键精确匹配，语言代码忽略大小写。空白 GUID 或 `null` 外层字典不接受；不认识的语言和 `null` 子表跳过，同一支持语言不可以写两个大小写变体。`null` 文案视为未填写，只有说明可用空字符串显式清空。打开时在常规刷新周期更新；搜索结果集合未变时保留控件、未提交编辑和焦点，已打开的内置下拉在候选值及顺序未变时保留选中和滚动。搜索结果集合变化时重新筛选，不清空搜索词。独立的显式结构刷新或动作重新注册仍遵循原重建规则。

跟随游戏语言，逐字段使用 **当前语言 API → 当前语言文件 → 英文 API → 英文文件 → 原文**。原文包括原有单语 `RegisterModDisplayNameV1`、配置元数据及动作 `DisplayName` / `ButtonLabel` 等；名称、分区、标题和按钮标签不采用空白译文，说明允许显式清空。读取语言不会改变配置键、动作 ID、保存值或回调。文件在插件启动时从 `BepInEx/translate/<语言>/<MOD GUID>.txt` 加载，修改文件需重启。完整键表、文本格式及用法见 [MOD 文案翻译](mod-translations.md)。

## 元数据

从实现版本1.1.0起，原生支持源码 SDK 的 `ModSettingsAttributes`，并继续识别跨程序集复制的旧 `ConfigurationManagerAttributes`。公共字段为 `DispName`、`Category`、`CategoryOrder`、`Description`、`Browsable`、`IsAdvanced`、`ReadOnly`、`ShowRangeAsPercent`、`HideDefaultButton`、`Order`、`ObjToStr`、`StrToObj`。按 ConfigEntry 原始信息 → 旧tag → 新tag → 显式覆盖字典的顺序应用，与新旧tag在数组中的位置无关；同类tag后者覆盖前者。覆盖字典使用同名键，布尔值用 `bool`、顺序用 `int`；tag可用 `bool?`、`int?`，null表示不覆盖，空字符串可清空说明。转换器分别为 `Func<object,string>`、`Func<string,object>`。读取以类型名和公共实例字段为准，不要求引用相同程序集；混淆时保留类型/字段名。

`DispName` 只控制配置行显示的键名，未覆盖时取 `ConfigDefinition.Key`；用于该行标题和搜索，不修改实际 `Section` / `Key` 或配置值。在绑定时把 `ModSettingsAttributes` 加到 tag 即可声明，也可用 `ConfigureEntryV1` 的同名键覆盖已有配置项。行旁按钮有独立的动作描述，不会代替该配置项的标题。

MOD自行切换显示语言时，更新保留在 `ConfigDescription.Tags` 中的同一tag对象。`ConfigureEntryV1` 字典和动作描述在注册时取快照，更新时需重新注册；新注册本身会标记刷新。若只改了 tag，在批量更新后调用一次 `RefreshV1()` / SDK `Refresh()`。这些调用只标脏，由页面主线程读取并重建；当前未确认编辑会取消，已保存值不回滚。不要重新Bind、磁盘Reload、改cfg身份或每帧注册；排序值保持稳定。完整接入及源码导入方式见 [SDK说明](../sdk/README.md)。

`ObjToStr`用于当前值及内置下拉的每个候选项，可按消费方自己的语言动态返回显示名；选择仍提交原始值，不把译文当保存值。显式刷新时，已打开内置下拉会在同一ConfigEntry仍有下拉编辑器时恢复，保留原始焦点值和滚动，按当前元数据和候选列表重建。列表打开时另按约0.2秒检查候选值/格式化文案变化，无变化不重建。业务语言、候选来源及历史值转换仍由消费方负责；只有旧翻译字符串的列表无法由公共服务自动翻译。见SDK下拉接入示例。

公共页面及注册／文件译文跟随 `Localization.GetLanguage()` 的 12 种官方语言：英、法、意、德、西、俄、巴西葡萄牙、波兰、简中、日、韩、繁中；未知枚举值回退英文。开关继续使用游戏原生 On/Off 翻译。游戏语言变化不需消费方显式刷新，也不会替消费方修改语言设置或写 cfg。消费方显式 Refresh 仍遵循上述结构重建规则。

`CategoryOrder`决定分区先后，高值在前；`Order`只决定分区内配置/独立动作顺序，高值在前。同一 owner、同一原始 `Category` 为一个分区，普通配置和独立动作共同参与排序；调用方应给该区所有行传相同 `CategoryOrder`。部分行未传时沿用该区显式值；若显式值冲突，统一取最高值；整个分区都未传时为 0，允许负数。分区同序时按原始 cfg Section（纯动作分区按稳定 actionId）做 Ordinal 排序，最后才按原始 `Category` 区分；行内同 `Order` 仍按原显示名、原始身份排序。译文不参与分组或排序。行旁动作跟随原配置行，其 `Category` / `CategoryOrder` 不覆盖原行。元数据变化会重建目录；不修改 cfg 的 Section/Key，不要求标题添加序号。旧服务会忽略 `CategoryOrder`，需更新实际当选的公共实现。

自动支持 bool、string、有符号/无符号整数、float/double/decimal、范围和百分比、候选值、枚举及 Flags、KeyCode、KeyboardShortcut、Color色板/Hex/透明预览、Vector2/3/4、Quaternion、Rect。自定义类型有可靠 TOML 或成对格式/解析转换器时使用文本编辑，否则显示只读原因。任意 IMGUI CustomDrawer 不会被调用或自动转为原生 UI。

KeyCode 提供单键下拉列表及同行“改键”按钮：下拉按 KeyCode 数值去重、过滤 AcceptableValues，包含 None；“改键”变为“请按键”，捕获后直接提交、更新左侧值并释放输入。KeyCode 不存组合键，KeyboardShortcut 分开显示当前值和“改键”按钮，并保留组合捕获弹层。取消、切页、关闭、外部禁用和销毁均释放本行捕获；当前全局开关键仍优先于这些编辑器。改全局开关键时，点击左下角的键名按钮后按非保留键盘单键；录入帧不会触发面板开关。全局开关键的保留规则不限制消费方配置项，见 [安装与使用](../README.md#安装与使用)。

重复检测只读取公共目录中未隐藏的 `ConfigEntry<KeyCode>`、`ConfigEntry<KeyboardShortcut>`（含高级项和只读项）以及面板的 `Navigation.ToggleKey`。同一配置项只算一次；`None` 或默认未绑定组合不计。比较实际主键和完整修饰键组合，与修饰键顺序、显示格式和当前搜索无关；单键与无修饰的同键组合视为相同。不会从字符串、其他枚举、配置名称或说明猜测快捷键，也无法检测未纳入目录或写死在 MOD 代码中的键。重复提示只表示配置相同，不保证动作同时触发，不阻止保存；下拉候选和组合键录入保持原样。

内置行名称按实际字形选择24%/32%/40%/49%宽度，超长换行增高；名称与操作区间隔整行2%，操作区内部间隔为其宽度的2%。开关、改键、恢复默认和滑条精确输入共用操作区24%的单元；其余输入/下拉/滑条平分或填满余量。开关图标保持64×64。原生自定义编辑器仍自行安排其容器，现有host协议不变。

Color行保留Hex、棋盘颜色预览和“选色”按钮。弹层有二维饱和度/明度色板、彩虹色相条、A透明度条、0–1透明度精确框及Hex；支持有/无 `#` 的6/8位Hex（6位视为不透明）。合法文本立即保存，拖动预览、松手保存，方向键调节滑条后确认保存；色板先确认进入方向编辑，再确认保存。返回、框外点击、关闭和外部禁用会丢弃未保存拖动，保留已经保存的修改。外部值变化不覆盖拖动，取消后读取最新实际值。色板和渐变使用网格，不为每次拖动创建贴图。

## 业务按钮

`RegisterActionV1` 与配置、renderer使用相同目录及租约替换规则；不必创建假的bool配置或重写内置输入框。无 `Entry` 时标题是动作的 `DisplayName`，提供 `Entry` 时沿用配置项的 `DispName`；`ButtonLabel` 独立控制两种位置的按钮文字。SDK 的 `client.RegisterAction(ownerGuid, actionId, new ModSettingsAction(displayName, execute) { ... })` 将强类型描述转换成下面同一份 v1 字典，不要求服务识别 SDK 类型，原有 `client.Action(..., descriptor)` 仍可使用。独立及行旁按钮示例见 [SDK 说明](../sdk/README.md#业务按钮)。

| 键 | 类型与含义 |
|---|---|
| `Version` | 必填 `int` 1 |
| `DisplayName` | 必填非空 `string`，动作名称，用于独立行标题、搜索和错误提示 |
| `ButtonLabel` | 可选 `string`，只用于按钮，可包含换行（C# 字符串中的 `\n`）；没有独立按钮译文或原按钮文案时，使用当前动作标题 |
| `Execute` | 必填 `Action`，Unity主线程业务回调 |
| `Description` | 可选 `string`，独立行动作或行旁动作按钮聚焦时的说明；省略时行旁按钮仍可显示配置行说明；参与搜索 |
| `Category` | 可选 `string`，独立行分类，默认 `General` |
| `CategoryOrder` | 可选 `int`，独立动作所属分区的降序；同区与普通配置统一取值。行旁动作跟随原配置行，此字段不覆盖原分区 |
| `Order` | 可选 `int`，默认0；独立行在分类内降序，同序按名称及稳定ID；附加按钮在原行内降序，同序按稳定ID |
| `Entry` | 可选 `ConfigEntryBase`，同一owner配置目录中的原配置项；缺省时为独立行，提供时为行旁按钮 |
| `CanExecute` | 可选 `Func<bool>`，主线程约每0.2秒及点击前检查，返回false禁用；应轻量、无I/O |
| `ClosePage` | 可选 `bool`，默认false；true时先经原生Hide释放公共页菜单、弹层、编辑和玩家输入，再同步执行回调 |

ownerGuid/actionId须非空且不含换行。更新文案或其他描述字段时，用相同 ownerGuid/actionId 重新注册，先取得新租约，再释放旧租约；可变业务状态可由CanExecute读取，不需每帧重注册。只有动作的owner也出现在MOD目录；已加载插件/显式RegisterConfig提供显示信息，未知owner回退显示GUID。附加动作只显示在其原配置项可见时，跟随原项的分类/高级项规则；搜索附加动作的 `DisplayName` 或说明也能找到原配置行，`ButtonLabel` 不参与搜索。

字符串/下拉等普通行的第一个附加按钮位于原编辑器后、恢复默认前，宽度同开关/默认按钮；更多按钮或复合编辑器的按钮在操作区续行，每行最多3个，间隔仍为操作区2%。自定义renderer的附加按钮在其容器下方，动态SetHeight保留动作空间。附加动作不会替换消费者原配置项或CustomDrawer元数据。

动作不参加保存或恢复默认。点击前确认注册仍为当前版本，替换/释放后的旧按钮不能执行；Execute/CanExecute异常隔离并记录在公共服务日志，打开的页面显示错误。ClosePage回调异常记录日志，页已关闭时错误要在下次打开前查看日志；回调自身打开的业务弹窗由消费者负责释放。ClosePage是关闭当前公共页，不负责隐藏第三方IMGUI窗口。业务若已有重入保护，Execute继续调用原入口，不在UI复制一套判断。

这是API v1的可选方法。通过 SDK `SupportsActions` 或反射 `GetMethod("RegisterActionV1", …)` 探测，旧版缺方法时跳过动作注册，已有功能照常。方法存在不代表支持 `ButtonLabel`：已发布的 1.2.1 服务端忽略该字段，只使用 `DisplayName`，需实际当选的服务包含该功能，仅更新消费方 SDK 无效。完整反射示例见 [ModSettingsClient](../sdk/ModSettingsClient.cs)，双界面接入见 [HostUtilities提示词](hostutilities-integration-prompt.md)。

```csharp
IDisposable lease = client.Action(ownerGuid, "open-website", new Dictionary<string, object> {
    { "Version", 1 }, { "DisplayName", "改名网页" }, { "ButtonLabel", "打开网页" }, { "Entry", playerName },
    { "ClosePage", true }, { "Execute", new Action(OpenPlayerNameWebsite) }
});
```

## 原生编辑器

协议字典必须含 `Version=1` 与：

```csharp
Func<RectTransform, ConfigEntryBase, IDictionary<string, object>, IDisposable> Create
```

每次重建调用 Create，参数是该行容器、原配置项和 host 能力字典。必须返回独立行实例的 IDisposable；销毁时移除监听器、释放自有资源。工厂失败会显示错误并回退只读。不要在注册字典存跨行共享状态。

关闭页面会 Cancel 并释放输入捕获，但保留完整页面与行实例；目录、元数据、语言或输入域不变时，再次打开只调用 Refresh，不重复 Create/Dispose。切换 MOD、结构刷新、租约替换/释放和页面销毁仍按行生命周期释放。首次构建按主线程帧预算分批执行，Create 自身不可抢占；消费方应避免在 Create/Refresh 中同步做重型计算或 I/O，且不能假定其他配置行已经创建完成。

| host 键 | 委托/值 |
|---|---|
| `Version` | `int` 1 |
| `Domain` | 当前用户的 `T17EventSystem` |
| `Commit` | `Func<object,IDictionary<string,object>>`；返回 `Success:bool`、`ActualValue:object`、`Error:string` |
| `SetHeight` | `Action<float>`；行高 80–600，页面调整后续行 |
| `RegisterSelectable` | `Action<Selectable>`；登记属于本行的每一个子控件，设置输入域与所属行导航 |
| `SetCallbacks` | `Action<IDictionary<string,object>>`；登记本行实例的 Refresh/Cancel/Commit/HasPending |
| `Choose` | `Action<string,string[],Action<int>>`；公共页面拥有的候选弹层 |
| `CaptureInput` | `Func<IDisposable>`；临时抑制当前输入域，行取消/销毁兜底释放 |

实例回调：`Refresh:Action` 只回读配置；`Cancel:Action` 撤销未保存预览/非法草稿，不回滚已保存修改；`HasPending:Func<bool>` 报告正在编辑/未保存预览。页面不创建确认按钮，自定义编辑器在有效输入/开关变化或滑条松手时自行调用 host.Commit。`Commit:Func<bool>` 为已有 v1 接入保留接受，页面不通过按钮调用它。切页、关闭和返回调用 Cancel，配置事件仅触发主线程 Refresh。构建期间登记所有子控件，避免背景导航和滚动抢焦点。弹层必须经 host.Choose 管理。

`Commit` 保持 ConfigFile.SaveOnConfigSet；自动保存关闭时每次有效提交补一次 Save。setter/自动保存异常可能发生在内存值改变之后，结果 ActualValue 才是权威状态。失败不得声称旧值已恢复，也不得反向写入做假回滚。内置文本编辑器每次有效变化提交；非法中间值只标红，不覆盖已保存配置。滑条拖动只预览，松手提交，方向键调整后原生确认提交。
