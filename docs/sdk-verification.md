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
