# 源码 SDK 与原生 Attributes 验证（2026-11-17）

## 实现

- `sdk/ModSettingsAttributes.cs`：公共 tag 类，12个公共字段覆盖显示文案、分区/行内排序、可见/高级/只读、范围百分比、默认按钮及转换器。布尔与顺序字段使用可空类型，避免空声明覆盖旧元数据。
- `sdk/ModSettingsClient.cs`：将原反射客户端从examples移入正式SDK，命名空间统一`OC2ModSettings.SDK`；同一份源码被生产程序集和独立消费者使用，没有平行桥接实现。
- `sdk/ModSettings.SDK.projitems`：可直接导入的源码清单；生产项目也导入该清单。消费者无需引用服务或Configuration Manager DLL。
- `EntryMetadata`仍为唯一解析入口，按原配置→旧tag→新tag→显式API字典的固定顺序读取，不依赖tag所在程序集的类型身份。旧tag兼容和已有动作/Renderer/刷新协议保留。
- 示例演示MOD独立中英文切换：原tag更新名称、说明、分类，动作按固定ID替换注册，统一Refresh。历史section/key和值不变；释放旧租约不会删除新租约，销毁时退订语言事件并释放注册。
- 提供端升至1.1.0（API仍1），避免与已发布1.0.0的不同实现同版本选优；AssemblyInfo仍从同一版本常量取值。内置构建包含SDK；测试版本从当前版本生成，Unity runner默认期望版本改从产物读取。

## 执行验证

```powershell
./scripts/test.ps1
./scripts/test-examples.ps1
./scripts/test-shared.ps1 -BuildOnly
# 只编译现有Unity验收程序集，不部署、不运行：
& 'D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' tests/UnityTests.csproj /t:Rebuild /p:Configuration=Release /p:PreBuildEvent= /p:PostBuildEvent= /v:minimal /nologo
```

- 生产.NET3.5/C#3构建通过，TreatWarningsAsErrors开启；桌面真实ConfigFile断言由36增至53，全部通过。新增17项覆盖独立SDK程序集、无可选服务DLL依赖、新旧tag两种排列、显式API优先级、null/false/0/负数/空字符串、语言快照、历史身份/值/磁盘不变、转换器，以及目录注册/释放/显式刷新标脏合并。
- 测试直接引用构建后的提供端（沿用既有InternalsVisibleTo），另编译消费者SDK副本，证明不是仅测试提供端自身类型。没有替换ConfigFile或元数据实现。
- SDK与完整消费者/原生Renderer在不引用`OC2ModSettings.dll`的情况下以.NET3.5/C#3、warnaserror独立编译通过。
- 四份源码内置产物（a/b的1.1.1、b的1.1.0、a的1.2.0，仅忽略目录中的测试副本）全部构建通过；验证SDK源码清单随内置流程可用，不代表四种启动选优实机复测。
- 现有Unity验收程序集编译通过；修改的PowerShell脚本语法和仓库Markdown本地链接检查通过。
- 最终原始输出在`.validation/sdk-20261117-153308/`，内置产物在`.validation/shared-20261117-153309/`。Cecil复核生产BepInPlugin为1.1.0、Assembly为1.1.0.0，Attributes/Client均public且12字段齐全；独立示例为CLR2/.NET3.5，无服务/Configuration Manager程序集引用。首次消费者编译发现UnityEngine.CoreModule引用缺失、示例字段遮蔽Behaviour.enabled，均修正后通过；未计为通过结果。

## 未覆盖与运行前提

本轮没有UI几何/绘制改动，未启动游戏或部署DLL；语言切换后的真实页面重建、物理设备/IME、Steam联机和共享服务启动矩阵均未重验。既有UI证据不作为本轮SDK实机验收。

使用新tag需更新实际当选的独立或源码内置公共服务至1.1.0并重启；旧1.0.0仍可读传统tag，但会忽略新tag。SDK源码本身可选，缺失服务时消费者应跳过扩展，继续原配置和业务。生产DLL在`bin/Release/OC2ModSettings.dll`，没有安装到游戏、推送GitHub或上传Release。源码引用和语言刷新细节见[SDK说明](../sdk/README.md)。

最终生产DLL SHA256：`C14BB5D2639EB14F1A2CD7B4F02D872A8F17B6B5E51871D7D4951DD146BCF076`。

## 配置行标题和独立按钮文案（2026-11-25）

AdaptiveCamera `3b70345` 在原 `RegisterActionV1` 字典加了 `ButtonLabel`，提供端 `583d9fd` 已在独立和行旁按钮读取该字段。本轮将用法纳入[源码 SDK](../sdk/README.md#业务按钮)：`RegisterAction` 将 `ModSettingsAction` 转为同一 v1 字典，原字典调用和省略文案时沿用 `DisplayName` 的规则不变。配置行标题仍通过 `DispName` 声明或覆盖，不修改实际配置键。

| 验证等级 | 命令与结果 |
|---|---|
| 生产构建、桌面测试 | `scripts/test.ps1`：.NET 3.5 / C# 3 构建通过，377 项断言通过；旧字典及 null、空字符串使用标题，独立按钮文案不参与搜索。 |
| 消费者编译 | `scripts/test-examples.ps1`：不引用服务 DLL 的 SDK、示例和原生 Renderer 通过。`scripts/test-shared.ps1 -BuildOnly`：四份源码内置程序集编译通过。 |
| Windows Steam 真实 Unity | `scripts/test-unity.ps1 -WithoutFastInit`：950 项脚本断言通过；覆盖相机同格式字典、强类型行旁按钮、同 ID 换文案、旧租约和旧格式、真实射线命中与 cfg 不被文案更新重写。首次 Unity 测试编译因测试变量重名失败，游戏未启动；修正后重跑。 |

实机目录 `.validation/20261125-135454-914-without-fastinit/`。两张原始截图 `standalone-business-action.png`、`attached-business-action.png` 均为 3840×2160。标题文字区约 270.71×56 UI、单行；按钮文字区约 120.49×42 UI、双行；行旁按钮为 140.49×48 UI。见同目录 `action-geometry.txt`。退出后复核 8 项安装、配置和日志状态及 1 项原存档状态，全部一致，无游戏进程残留。

产物 `bin/Release/OC2ModSettings.dll`（本地 1.2.1 / API v1），SHA256 `7FF04F0D3E05EDB76EA847B819099852A1F2AC21AC66917EE5221E82D14DB03B`。未实测实体设备、Steam 联机、共享服务完整选优矩阵或实际视角编辑业务；未正式部署、推送或发布。公开 1.2.1 会忽略 `ButtonLabel`，使用独立文案需让包含该字段实现的提供端当选并重启游戏。

## 左栏 MOD 名称与 GUID 排序（2026-11-25）

新增独立的[名称租约](api-v1.md#mod-名称)。只改名称不必重注册 `ConfigFile` 或调用 `Refresh()`；同一 GUID 换文案不改变固定、置顶和普通分组内的顺序。冲突详情也读取最新名称。

| 验证等级 | 命令与结果 |
|---|---|
| 生产构建、桌面测试 | `scripts/test.ps1`：.NET 3.5 / C# 3 构建通过，457 项断言通过；新增空白输入、精确 GUID、仅标记名称变化、租约替换和释放、置顶不按显示名排序。 |
| 消费者及内置编译 | `scripts/test-examples.ps1`：SDK、示例和原生 Renderer 不引用服务 DLL 即可编译。`scripts/test-shared.ps1 -BuildOnly`：四份源码内置程序集编译通过，未启动选优矩阵。 |
| Windows Steam 真实 Unity | `scripts/test-unity.ps1 -WithoutFastInit`：1012 项脚本断言通过；运行时重命名、仅动作 MOD、同 GUID 的旧租约释放、关页时释放后重开、快捷键详情同步、排序及置顶和双栏滚动不变、编辑焦点及草稿不丢均有覆盖。 |

实机目录 `.validation/20261125-173521-159-without-fastinit/`：[截图](../.validation/20261125-173521-159-without-fastinit/mod-display-name.png)为 **3840×2160**；[几何](../.validation/20261125-173521-159-without-fastinit/mod-display-name-geometry.txt)记录左栏同一置顶项改名前后均为 **464.66×68 UI**，标签区域 **396.66×62 UI**，双栏滚动均未变动。`173202` 场第 467 项仍假定新置顶项总排组首，按新的 GUID 组内排序修正断言后重跑。两场退出后 **18/18** 项安装及原存档状态一致，无游戏进程残留。

产物 `bin/Release/OC2ModSettings.dll`（本地 1.2.1 / API v1），SHA256 `D2CC39B540337B839F88716915E2F78851ECC58956058A4272A5C664F5FB6140`。未验物理设备、真实 Steam 联机、其他平台及 AdaptiveCamera 十二语实际接入；未正式部署、推送或发布。公开 1.2.1 没有名称方法，使用时需让包含新实现的服务当选并重启游戏。
