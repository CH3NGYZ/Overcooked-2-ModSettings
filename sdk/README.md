# Mod Settings 源码 SDK

SDK 随本仓库源码提供，命名空间为 `OC2ModSettings.SDK`，适用 BepInEx 5 / .NET 3.5 / C# 3。普通消费者不需要 Configuration Manager，也不需要引用 `OC2ModSettings.dll`。从 **1.1.0** 起提供端读取 `ModSettingsAttributes`；公开服务方法仍为 API v1。

## 引用方式

推荐将本目录作为源码复制或链接进自己的 MOD：

- [ModSettingsAttributes.cs](ModSettingsAttributes.cs)：配置项的显示声明；仅使用 BCL 类型，可以单独复制。
- [ModSettingsClient.cs](ModSettingsClient.cs)：连接实际当选服务、刷新、注册业务按钮和原生编辑器。只依赖 BepInEx 及其 Unity 运行时，不引用服务 DLL。
- [ModSettings.SDK.projitems](ModSettings.SDK.projitems)：传统 MSBuild 项目可导入的源码清单，不是额外的运行时 DLL。

传统 `.csproj` 可以导入：

```xml
<Import Project="path\to\sdk\ModSettings.SDK.projitems" />
```

或者直接将两个 `.cs` 文件加入 Compile。使用默认递归 Compile 的项目将文件放进源码目录即可，不要再重复导入清单。SDK 同时编入本项目的生产 DLL；若选择直接引用生产 DLL，就不要再复制同名 SDK 源码，避免重复类型。源码接入推荐配合软依赖，插件缺失时仅跳过设置界面扩展，配置和业务继续运行：

```csharp
using OC2ModSettings.SDK;

[BepInDependency(ModSettingsClient.ServiceGuid,
    BepInDependency.DependencyFlags.SoftDependency)]
```

`BepInDependency` 放在自己的 `BaseUnityPlugin` 类上。现成的[完整消费者](../examples/ConsumerPlugin.cs)包含语言切换、独立动作、行旁按钮、原生 Renderer 和退出释放；其代码无需服务 DLL 引用即可编译。改命名空间不会影响 Attributes 识别，但必须保留类型名 `ModSettingsAttributes`、公共实例字段名称及类型；使用混淆器时应保留这些成员。

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
| `DispName` / `Category` / `Description` | `string` | 显示名称、分区及悬停/搜索说明 |
| `CategoryOrder` | `int?` | 分区顺序，高值优先；同分区统一取值 |
| `Order` | `int?` | 分区内顺序，高值优先 |
| `Browsable` | `bool?` | false 隐藏 |
| `IsAdvanced` / `ReadOnly` | `bool?` | 高级项 / 只读 |
| `ShowRangeAsPercent` / `HideDefaultButton` | `bool?` | 范围百分比 / 隐藏默认按钮 |
| `ObjToStr` | `Func<object,string>` | 格式化自定义值 |
| `StrToObj` | `Func<string,object>` | 解析自定义值；非法输入抛异常，不直接写配置 |

字段为 null 表示不覆盖前一层；空字符串可清空说明，false、0、负数均为明确值。元数据优先级固定为：ConfigEntry 原始信息 → 旧 `ConfigurationManagerAttributes` → 新 `ModSettingsAttributes` → `ConfigureEntryV1` 显式覆盖，与新旧 tag 在数组中的先后无关。若同一种 tag 出现多次，则后面的同名非 null 字段覆盖前面。

旧 tag 继续兼容，不要求已有 MOD 迁移。只用新 tag 不会让 Configuration Manager 自动理解它；若开发者还要维护其旧界面，可保留旧 tag。IMGUI `CustomDrawer` 不在新契约内；业务按钮用 `client.Action`，自定义原生控件用 `client.Renderer`，具体协议见 [API 文档](../docs/api-v1.md)。

## 切换 MOD 自己的语言

保留同一个 tag 实例，先更新所有显示文案，再统一通知：

```csharp
featureDisplay.DispName = Translate("Enabled");
featureDisplay.Description = Translate("EnabledDescription");
featureDisplay.Category = Translate("General");

// 更新动作文案时，以相同 ownerGuid/actionId 重新 client.Action(...)。
// 先取得新租约，再释放旧租约；新注册本身也会标记刷新。
if (client.Connect()) client.Refresh();
```

`Translate`、`feature` 和 `client` 为消费方自身的翻译方法、配置字段及客户端实例。集中在主线程完成本轮修改，调用方捕获可选接入错误；后台翻译完成后应先切回主线程。无需重新 Bind，不修改历史 section/key，也不调用磁盘 Reload；`CategoryOrder`、`Order` 使用稳定值，不随翻译重新计算。

更新可变 tag 后，`Refresh()` 让下一次页面更新重新读取它们，打开中的页面最多约 0.2 秒检查一次，关闭中则下次打开生效。当前刷新会重建页面并取消未确认的编辑/拖动，已保存值不回滚，不应逐项或逐帧调用。`ConfigureEntryV1` 的字典在注册时复制，走该接口的调用方必须重新注册，修改旧字典再 Refresh 无效；显式覆盖仍优先于 tag，需同步更新或释放该租约。

### 下拉选项也跟随 MOD 语言

`ObjToStr` 同时负责当前值和所有下拉候选项的显示。推荐让枚举或稳定字符串 ID 做保存值，用同一个 formatter 按 MOD 自己的语言生成文案：

```csharp
var roleDisplay = new ModSettingsAttributes {
    ObjToStr = delegate(object value) { return Translate((string)value); }
};
// default / host 是消费方定义的稳定业务 ID，Translate 返回当前 MOD 语言的文字。
var role = Config.Bind("General", "Role", "default",
    new ConfigDescription("Role", new AcceptableValueList<string>("default", "host"), roleDisplay));
```

纯候选选择只提交原始值，不需要通过 `StrToObj` 反解翻译；自由文本和自定义类型仍按原转换器契约解析。相同 formatter 委托可以读取变化后的 MOD 语言，不必每次创建新委托。标题、分区、说明和动作文案仍批量更新后统一 `client.Refresh()`。已打开的内置下拉弹层会按同一个 ConfigEntry 恢复，重读候选集合及其格式化文字，并按原始值恢复焦点和滚动；候选重排不依赖旧索引。项目示例包含完整语言事件及租约释放。

列表打开期间约每 0.2 秒检查变化，只有候选值、文案或标题改变才更新列表。改变游戏语言只更新公共页面自身文字，不强制覆盖 MOD 的独立语言。对自定义 renderer 的 `host.Choose(title,string[],...)`，数组属于一次显示快照；renderer 在收到刷新后应重新提供自己的选项，公共服务不能推断任意业务字符串的译文。

若旧 MOD 在启动时把翻译后的文字直接写进 `AcceptableValueList<string>`，仅更新标题或调用 Refresh 不会凭空产生新译文。消费方需为这些实际值提供 `ObjToStr`，或在自己的权威配置更新路径更新候选集合；保留历史 cfg 值和业务比较语义，不为换语言重 Bind、无条件重置或直接替换保存值。新配置推荐从一开始采用稳定 ID。

客户端只调用 `Chainloader.PluginInfos[GUID].Instance` 上的公开 API，适用于独立服务和源码内置选优。检查 `Connect()` 后再调用；业务按钮先检查 `SupportsActions`。配置、动作、Renderer 注册所得租约在退出时释放，语言事件取消订阅；旧租约释放不会删掉其替代注册。源码 SDK 不会自动订阅或改变消费方的语言设置。

## 本地验证

在仓库根目录运行 `scripts/test.ps1` 和 `scripts/test-examples.ps1`。前者使用真实 ConfigFile、已构建提供端及独立编译的 SDK 程序集验证字段、优先级和语言快照，后者按 .NET 3.5 / C# 3 独立编译 SDK 与消费者。构建不会安装游戏文件，桌面验证不代表实际 Unity 文案显示或物理输入验收。
