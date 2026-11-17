# HostUtilities 双设置界面接入提示词

下面整段可直接交给HostUtilities仓库的Agent。本公共服务已经实现按钮接口，HostUtilities业务接入由该Agent完成。

```text
请在 E:\code\OC2\Overcooked-2-HostUtilities-Code 实施：让HostUtilities同时兼容 Configuration Manager 和独立 Mod Settings 插件，保留各自入口，复用相同业务逻辑。直接完成代码、适当验证和中文本地提交，不push、不发布；部署/网络/Steam测试遵守该仓库现有授权与隔离流程。

开始先读当地AGENTS.md、GOAL/PROGRESS、git status、近期log及暂存/未暂存diff，保护其他Agent的工作。公共Mod Settings仓库由另一Agent负责，不要修改它。

先只读以下已实现契约及示例：
- E:\code\OC2\OtherMOD\Overcooked-2-ModSettings\docs\api-v1.md
- E:\code\OC2\OtherMOD\Overcooked-2-ModSettings\sdk\ModSettingsClient.cs
- E:\code\OC2\OtherMOD\Overcooked-2-ModSettings\docs\hostutilities-actions.md

目标：
1. Mod Settings页面中为HostUtilities增加独立“检查更新”按钮，调用现有 UpdateCoordinator.CheckUpdatesFromPanel()，继续复用StartCheck(false)以及checking/downloading/installing/preparingExit/plan/instance等重入保护。按既有流程Input.ResetInputAxes()。保留原IMGUI面板的检查更新按钮，若Configuration Manager还有既有更新入口也保留；不要通过虚构bool配置表示一次性动作，不复制下载/更新网络逻辑。
2. 在“修改名字”的原ConfigEntry<string>输入框右侧增加“改名网页”按钮，Mod Settings负责内置输入框、有效值保存、布局、导航及取消。不要为这一个按钮注册整行Renderer，不复制或绕开playerName.SettingChanged驱动的改名逻辑。
3. Configuration Manager继续使用现有ConfigurationManagerAttributes.CustomDrawer=DrawPlayerNameTextArea，保持多行输入框和右侧网页按钮。两个UI按钮共同调用从现有drawer提取的OpenPlayerNameWebsite之类的内部业务函数：隐藏Configuration Manager窗口；复用 playerNameWebService.GetWebsiteUrl()，服务为空时才使用 DisplayNameWebService.GetWebsiteUrlWithLanguage()；经 SteamFriends.ActivateGameOverlayToWebPage 打开；保留Input.ResetInputAxes()。不要硬编码地址，不丢当前语言、实际oc2Port/oc2Token与网页回传能力，不在日志中输出含token的完整URL。

真实调用位置（按当前checkout核实，行号可能漂移）：
- HostUtilities/HostUtilities/Panels/PanelMiscellaneous.cs:118，检查更新按钮。
- HostUtilities/HostUtilities/VersionCheck/UpdateCoordinator.cs:71，CheckUpdatesFromPanel() -> StartCheck(false)。
- HostUtilities/HostUtilities/DisplayName/DisplayNameConfig.cs，private static playerName；键 01-Replace_Name_First_Time_Hover_Mouse_For_Instructions；其SettingChanged保留。
- HostUtilities/HostUtilities/DisplayName/DisplayNameConfigDrawer.cs，DrawPlayerNameTextArea和HideConfigurationManagerWindow。
- HostUtilities/HostUtilities/DisplayName/DisplayNameWebService.cs，GetWebsiteUrl()保留动态回传参数。

公共服务协议：
GUID = com.ch3ngyz.plugin.overcooked2.modsettings
通过BepInDependency SoftDependency（使用当地常量/约定）和 Chainloader.PluginInfos[GUID].Instance 访问当选服务，不引用OC2ModSettings.dll，不反射私有成员。即使宿主内置了公共源码，也不得直接调用同程序集static Instance，BepInEx可能选中独立或另一宿主副本。
公开API版本仍为1，当前源码版本1.1.0。源码SDK和独立ModSettingsAttributes见sdk目录，可直接源码引用；保留Configuration Manager兼容时可继续用旧tag，或同时带两种tag（新tag优先）。老版本1.0.0可能没有动作方法，必须探测实际公开方法并缓存MethodInfo（含缺失结果），不能只比版本字符串。
GetApiVersionV1(): int
GetStatusV1(): string，ready/unavailable；ready不代表原生菜单当前可交互。
RegisterConfigV1(string ownerGuid,string displayName,string description,ConfigFile config): IDisposable
RegisterActionV1(string ownerGuid,string actionId,IDictionary<string,object> descriptor): IDisposable
描述仅共享BCL/BepInEx类型：Version(int)=1，DisplayName(string)，Execute(Action)必填；Description(string)、Category(string)、CategoryOrder(int)、Order(int)、CanExecute(Func<bool>)、ClosePage(bool)、Entry(ConfigEntryBase)可选。
Entry缺省为独立按钮；Entry=现有playerName就是输入框旁按钮。Category决定独立行分类，CategoryOrder决定分区降序，Order决定分区内部行或同一配置项附加动作的降序；附加按钮始终跟随原配置行。ClosePage=true先经公共NativePage.Hide释放菜单/弹层/编辑/玩家输入，再同步在Unity主线程调用Execute。更新会显示原生业务弹窗，网页会打开Steam overlay，两个动作都使用ClosePage=true。

分区排序补充（公共Mod Settings已实现读取）：从HostUtilities现有Section声明顺序统一生成稳定CategoryOrder，高值在前，恢复“Language → MOD设置 → 改名及消息传递 → 常用功能设置……”；不要按翻译后的名称重算。经ConfigureEntryV1原有覆盖字典传入，或在已有ConfigurationManagerAttributes加public int? CategoryOrder供公共服务读取；不另建重复排序表。同一分区的普通配置与独立动作传同一值，检查更新的Order紧接开启刷分模式，CategoryOrder不用于调整区内行。语言切换只换DispName/DisplayName、Category、Description并重新注册租约，CategoryOrder/Order及稳定actionId保持；保留历史cfg section/key，不给标题加回数字。行旁改名按钮沿用playerName所在分区，不自行分区。Configuration Manager现有分区/CustomDrawer逻辑继续保持，这个扩展字段不代表它也自动支持CategoryOrder；需要其排序时复用HostUtilities原有兼容路径。公共实现仍是API v1，旧副本会忽略字段，需安装/内置一致的新源码。

下拉多语言补充（公共侧已补支持）：ObjToStr同时用于当前值和全部候选显示名；已打开内置下拉会在Refresh后按同一ConfigEntry重建并恢复原始焦点值，候选重排不按旧索引提交。请核查SessionForceHost.Awake的strList、SessionLobbyJoiner.Awake的distanceStrList以及其他AcceptableValueList，它们有在启动时一次性调用Language.GetConfigText保存翻译文字的路径，仅RefreshLocalizedMetadata改标题不会更新这些候选。优先在既有显示元数据中给相同实际值提供ObjToStr，由它按HostUtilities当前ConfigLanguage返回显示文字；不要强制跟随游戏语言，不复制公共UI/缓存。保留历史cfg Section/Key、已有值及业务比较语义，不能把旧文字列表直接换成新ID导致旧值失效；若需迁移必须沿用宿主权威转换和兼容路径。新配置可以用稳定ID/枚举。所有显示tag和动作更新完成后通过现有桥接统一RefreshV1/SDK Refresh一次；不要磁盘Reload或重复Bind。Configuration Manager仍走其原有formatter/drawer能力，两套UI复用同一语义翻译函数。验证“只改MOD语言、游戏语言不变”时当前值、候选、标题、说明及业务动作均更新，且选择结果仍为原值。
CanExecute约0.2秒及点击前在主线程调用，必须轻量；仅在适合暴露既有权威状态时使用，业务入口的重入保护始终是权威，不要在接入层复制更新内部布尔状态。
相同ownerGuid/actionId后注册替换旧动作，旧IDisposable释放不能删新动作。插件销毁释放自己的全部租约；语言变化按现有语言刷新入口更新DisplayName/Description/Category，稳定actionId不变，无每帧注册、重复按钮或重复事件订阅。
Mod Settings缺失、状态unavailable、旧版缺RegisterActionV1或可选注册失败时，只跳过该可选接入，不能影响Configuration Manager、更新、改名或HostUtilities启动。错误记录一次或限频，日志不带敏感URL。

示例语义（调整为项目既有语言/桥接实现）：
RegisterActionV1(MODEntry插件GUID,"check-updates",new Dictionary<string,object> {
  {"Version",1},{"DisplayName",检查更新的本地化文案},{"Category",适合的分类},{"ClosePage",true},
  {"Execute",new Action(delegate { UpdateCoordinator.CheckUpdatesFromPanel(); Input.ResetInputAxes(); })}
});
RegisterActionV1(MODEntry插件GUID,"display-name-website",new Dictionary<string,object> {
  {"Version",1},{"DisplayName",Language.GetConfigText("Display_Name_Website_Button")},{"Entry",playerName},
  {"ClosePage",true},{"Execute",new Action(OpenPlayerNameWebsite)}
});
private playerName可以在DisplayNameManager内部登记或提供窄内部入口，不为UI公开整套私有状态。使用已有配置目录/注册桥接，不创建第二个ConfigFile；公共插件可自动发现HostUtilities.Config，只有需要覆盖显示信息时才RegisterConfigV1。

验证：
- 实际安装BepInEx5/HarmonyX/Unity Mono和当地项目目标框架编译；可选消费者必须不引用Mod Settings DLL也能编译。
- Configuration Manager单装、Mod Settings单装、两者同时、Mod Settings缺失/旧版无动作方法都能启动并保留对应可用入口；反复打开和语言变化不重复注册，替换/释放租约正确。
- 原ConfigEntry输入仍走现有SettingChanged；网页按钮使用原动态URL，回传到同一个playerName；配置保存与恢复默认不执行动作。
- 检查更新走原协调器，忙碌时不重入；公共页关闭后更新原生弹窗有真实焦点、射线/确认和取消；网页Steam overlay关闭后游戏菜单/玩家输入恢复，无公共页抑制残留。
- 真实网络、Steam overlay/网页回传、物理设备、联机分别报告证据，不能用编译或fixture当成实测通过。未获得或未完成的实机边界明确写待验。
- 按当地规范做任务范围内中文本地提交，汇报文件/行为、验证命令及数字、未验证项目、运行时前提。最终给出兼容矩阵与安装所需Mod Settings动作方法探测条件。
```
