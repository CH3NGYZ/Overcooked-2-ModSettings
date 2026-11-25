# Mod Settings 源码 SDK

SDK 随本仓库源码提供，命名空间为 `OC2ModSettings.SDK`，适用 BepInEx 5 / .NET 3.5 / C# 3。普通消费者不需要 Configuration Manager，也不需要引用 `OC2ModSettings.dll`。从 **1.1.0** 起提供端读取 `ModSettingsAttributes`；公开服务方法仍为 API v1。

## 引用方式

推荐将本目录作为源码复制或链接进自己的 MOD：

- [ModSettingsAttributes.cs](ModSettingsAttributes.cs)：配置项的显示声明；仅使用 BCL 类型，可以单独复制。
- [ModSettingsClient.cs](ModSettingsClient.cs)：连接实际当选服务、注册翻译、更新 MOD 名称、刷新配置文案、注册业务按钮和原生编辑器；内含强类型动作描述 `ModSettingsAction`。只依赖 BepInEx 及其 Unity 运行时，不引用服务 DLL。
- [ModSettingsTranslations.cs](ModSettingsTranslations.cs)：生成翻译条目键；同一标识可用于 API 和翻译文件。
- [ModSettings.SDK.projitems](ModSettings.SDK.projitems)：传统 MSBuild 项目可导入的源码清单，不是额外的运行时 DLL。

传统 `.csproj` 可以导入：

```xml
<Import Project="path\to\sdk\ModSettings.SDK.projitems" />
```

或者直接将本目录的 `.cs` 文件加入 Compile。使用默认递归 Compile 的项目将文件放进源码目录即可，不要再重复导入清单。SDK 同时编入本项目的生产 DLL；若选择直接引用生产 DLL，就不要再复制同名 SDK 源码，避免重复类型。源码接入推荐配合软依赖，插件缺失时仅跳过设置界面扩展，配置和业务继续运行：

```csharp
using OC2ModSettings.SDK;

[BepInDependency(ModSettingsClient.ServiceGuid,
    BepInDependency.DependencyFlags.SoftDependency)]
```

`BepInDependency` 放在自己的 `BaseUnityPlugin` 类上。[完整消费者](../examples/ConsumerPlugin.cs)演示一次注册翻译、独立动作、行旁按钮、原生 Renderer 和退出释放；其代码无需服务 DLL 引用即可编译。改命名空间不会影响 Attributes 识别，但必须保留类型名 `ModSettingsAttributes`、公共实例字段名称及类型；使用混淆器时应保留这些成员。

## 让 MOD 文案跟随游戏语言

**新接入或更新 ModSettings 多语言的 MOD，必须向 `RegisterTranslations` 一次传入游戏支持的全部 12 种语言。** 外层字典是语言代码，内层是该语言的字段键 → 译文；字段键由 `ModSettingsTranslations` 生成。代码为 `en`、`fr`、`it`、`de`、`es`、`ru`、`pt-BR`、`pl`、`zh-CN`、`ja`、`ko`、`zh-TW`。ModSettings 读取游戏语言并选择显示文本；消费 MOD 不先挑语言、不按语种分别注册，也不在换语后重新注册、重写元数据或调用 `Refresh()`。

初始化时先 `Connect()`，再检查 `SupportsTranslations`。例如，[完整消费者](../examples/ConsumerPlugin.cs) 用 [`ExampleSettingsTranslations.Create`](../examples/ExampleSettingsTranslations.cs) 为 MOD 名、分区、配置和两种动作生成整张 12 语表：

```csharp
if (client.Connect() && client.SupportsTranslations)
    translationLease = client.RegisterTranslations(ownerGuid, ExampleSettingsTranslations.Create());
```

保留 `translationLease`，卸载时释放。只有整份词表确实变化时才替换注册：先拿到新租约，再释放旧租约。服务仍允许缺项并[逐字段回退](../docs/mod-translations.md#回退顺序)，文件可补充缺词或翻译没有接入 API 的旧 MOD；这不是新接入省略语种的办法。建议原始 `DispName`、`DisplayName`、`ButtonLabel` 等采用英文。旧服务没有此能力时跳过注册，不影响原配置和业务。接入现有 MOD 时也可直接改用 `bepinex-mono-modding` 技能的 `assets/modsettings/` 模板。

## 设置左栏 MOD 名称

用 `RegisterModDisplayName` 单独设置 MOD 在左栏和快捷键重复提示中的名称，不需要为此调用 `Register`、重新绑定配置或改变插件 GUID。它只接受单语文本；要跟随游戏语言，优先用 `ModSettingsTranslations.ModName`。例如在 `BaseUnityPlugin` 子类中实现一次单语改名：

```csharp
private readonly ModSettingsClient client = new ModSettingsClient();
private IDisposable modNameLease;

private void UpdateModName(string displayName)
{
    try
    {
        if (!client.Connect() || !client.SupportsModDisplayName) return;
        IDisposable next = client.RegisterModDisplayName(Info.Metadata.GUID, displayName);
        IDisposable previous = modNameLease;
        modNameLease = next;
        if (previous != null) previous.Dispose();
    }
    catch (Exception error)
    {
        Logger.LogWarning("Optional MOD Settings name: " + error.Message);
    }
}
```

服务不存在或不支持此方法时直接跳过；卸载 MOD 时释放 `modNameLease`。无需调用 `Refresh()`，已打开的面板会原位更新名称，不打断编辑；未打开则下次打开生效。更换名称不会新增目录项；名称优先级、输入限制与租约规则见 [API](../docs/api-v1.md#mod-名称)。

## 声明配置

```csharp
private readonly ModSettingsAttributes featureDisplay = new ModSettingsAttributes {
    DispName = "开启功能", Description = "开启后执行此功能。",
    Category = "常用功能", CategoryOrder = 100, Order = 20
};

// 在 MOD 初始化时执行一次：
feature = Config.Bind("General", "Enabled", true,
    new ConfigDescription("Fallback description", null, featureDisplay));
```

它是放进 `ConfigDescription.Tags` 的对象，不是写在类或属性上的 `[Attribute]`。不必登记每个 ConfigEntry；公共服务仍会自动发现 MOD 的 ConfigFile。支持字段如下：

| 字段 | 类型 | 含义 |
|---|---|---|
| `DispName` / `Category` / `Description` | `string` | 配置行标题、分区及悬停/搜索说明 |
| `CategoryOrder` | `int?` | 分区顺序，高值优先；同分区统一取值 |
| `Order` | `int?` | 分区内顺序，高值优先 |
| `Browsable` | `bool?` | false 隐藏 |
| `IsAdvanced` / `ReadOnly` | `bool?` | 高级项 / 只读 |
| `ShowRangeAsPercent` / `HideDefaultButton` | `bool?` | 范围百分比 / 隐藏默认按钮 |
| `ObjToStr` | `Func<object,string>` | 格式化自定义值 |
| `StrToObj` | `Func<string,object>` | 解析自定义值；非法输入抛异常，不直接写配置 |

字段为 null 表示不覆盖前一层；空字符串可清空说明，false、0、负数均为明确值。元数据优先级固定为：ConfigEntry 原始信息 → 旧 `ConfigurationManagerAttributes` → 新 `ModSettingsAttributes` → `ConfigureEntryV1` 显式覆盖，与新旧 tag 在数组中的先后无关。若同一种 tag 出现多次，则后面的同名非 null 字段覆盖前面。

`DispName` 只修改界面显示的配置键名，不修改 `ConfigDefinition.Section`、`ConfigDefinition.Key` 或已保存的值。持有已有配置项时，也可用 `client.Configure(feature, new Dictionary<string, object> { { "DispName", "开启功能" } })` 显式覆盖标题，并保留返回的租约。

旧 tag 继续兼容，不要求已有 MOD 迁移。只用新 tag 不会让 Configuration Manager 自动理解它；若开发者还要维护其旧界面，可保留旧 tag。IMGUI `CustomDrawer` 不在新契约内；业务按钮用 `client.RegisterAction`，自定义原生控件用 `client.Renderer`，具体协议见 [API 文档](../docs/api-v1.md)。

## 业务按钮

没有 `Entry` 时注册独立动作行，左侧标题取 `DisplayName`；指定已有 `ConfigEntry` 时把按钮加在该配置项旁，行标题仍由该配置的 `DispName` 决定。两种情况下，`ButtonLabel` 都只控制按钮文字，与行标题独立，可以用 `\n` 换行。不填、传 `null` 或空字符串时，按钮沿用动作的 `DisplayName`。动作不写入 cfg，也不需要为独立按钮 Bind 一个占位配置。

```csharp
if (client.Connect() && client.SupportsActions)
{
    checkLease = client.RegisterAction(ownerGuid, "check-status",
        new ModSettingsAction("检查服务状态", CheckStatus) {
            ButtonLabel = "立即\n检查", Category = "常用功能"
        });

    detailsLease = client.RegisterAction(ownerGuid, "show-feature-details",
        new ModSettingsAction("功能详情", ShowFeatureDetails) {
            Entry = feature, ButtonLabel = "查看详情"
        });
}
```

示例中的 `feature` 是已绑定的「开启功能」配置，`ownerGuid` 是本 MOD 的 GUID，两个回调是现有业务入口。`checkLease` 和 `detailsLease` 是消费方持有的 `IDisposable` 租约，退出时释放。其他选项见 [动作字段表](../docs/api-v1.md#业务按钮)。

SDK 的 `RegisterAction` 仍调用 `RegisterActionV1`，原有 `client.Action(ownerGuid, actionId, descriptor)` 字典调用保持不变。已发布的 **1.2.1** 会忽略 `ButtonLabel`，用 `DisplayName` 显示按钮。独立按钮文案取决于实际当选的服务是否支持该字段；仅更新消费方 SDK 或 `SupportsActions` 返回 `true` 都不能保证这一点。

## 已有单语配置元数据变化

这条路径适用于已有 MOD 业务需要动态更改原始文案，不用于跟随游戏换语。注册翻译和翻译文件优先于这些原文。

保留同一个 tag 实例，先更新所有显示文案，再统一通知：

```csharp
featureDisplay.DispName = newDisplayName;
featureDisplay.Description = newDescription;

if (client.Connect()) client.Refresh();
```

集中在主线程完成本轮修改，调用方捕获可选接入错误。无需重新 Bind，不修改历史 section/key，也不调用磁盘 Reload；`CategoryOrder`、`Order` 使用稳定值，不随文案重新计算。仅更新左栏名称不需要刷新；如果同时修改了配置行 tag，则仍按配置行刷新规则通知。

修改动作标题或按钮文字时，用同一 `ownerGuid` / `actionId` 重新注册；先取得新租约，再释放旧租约。动作参数在每次注册时取快照，仅修改原对象或字典再调用 `Refresh()` 不会更新动作。注册本身会触发刷新，因此先批量修改 tag，再注册有变化的动作；若只改了 tag，批量更新后调用一次 `Refresh()` 即可。

刷新后打开中的页面最多约 0.2 秒检查一次，关闭中则下次打开生效。刷新会重建页面并取消未确认的编辑/拖动，已保存值不回滚，不应逐项或逐帧调用。`ConfigureEntryV1` 的字典也在注册时复制；显式覆盖优先于 tag，需重新注册或释放该租约。

### 单独格式化下拉候选文字

`ObjToStr` 同时负责当前值和所有下拉候选项的显示。候选文字不在面板多语言 API 的字段表里；如需格式化，用枚举或稳定字符串 ID 作保存值：

```csharp
var roleDisplay = new ModSettingsAttributes {
    ObjToStr = delegate(object value) { return (string)value == "host" ? "Host only" : "Game default"; }
};
// default / host 是稳定的业务 ID，不把显示标签当成配置值。
var role = Config.Bind("General", "Role", "default",
    new ConfigDescription("Role", new AcceptableValueList<string>("default", "host"), roleDisplay));
```

纯候选选择只提交原始值，不需要通过 `StrToObj` 反解翻译；自由文本和自定义类型仍按原转换器契约解析。相同 formatter 委托可以读取变化后的 MOD 语言，不必每次创建新委托。已打开的内置下拉弹层会按同一个 ConfigEntry 恢复，重读候选集合及其格式化文字，并按原始值恢复焦点和滚动；候选重排不依赖旧索引。

列表打开期间约每 0.2 秒检查变化，只有候选值、文案或标题改变才更新列表。翻译表不翻译下拉候选内容或自定义 renderer 的业务文字，不会改变 MOD 自己的语言设置。对自定义 renderer 的 `host.Choose(title,string[],...)`，数组属于一次显示快照；renderer 在收到刷新后应重新提供自己的选项。

若旧 MOD 在启动时把翻译后的文字直接写进 `AcceptableValueList<string>`，仅更新标题或调用 Refresh 不会凭空产生新译文。消费方需为这些实际值提供 `ObjToStr`，或在自己的权威配置更新路径更新候选集合；保留历史 cfg 值和业务比较语义，不为换语言重 Bind、无条件重置或直接替换保存值。新配置推荐从一开始采用稳定 ID。

客户端只调用 `Chainloader.PluginInfos[GUID].Instance` 上的公开 API，适用于独立服务和源码内置选优。检查 `Connect()` 后再调用；业务按钮、MOD 名称、翻译分别先检查 `SupportsActions`、`SupportsModDisplayName`、`SupportsTranslations`。翻译、名称、配置、动作、Renderer 注册所得租约在退出时释放；旧租约释放不会删掉其替代注册。源码 SDK 不会自动订阅或改变消费方的语言设置。

## 本地验证

在仓库根目录运行 `scripts/test.ps1` 和 `scripts/test-examples.ps1`。前者使用真实 ConfigFile、已构建提供端及独立编译的 SDK 程序集验证字段、优先级和语言快照，后者按 .NET 3.5 / C# 3 独立编译 SDK 与消费者。构建不会安装游戏文件，桌面验证不代表实际 Unity 文案显示或物理输入验收。
