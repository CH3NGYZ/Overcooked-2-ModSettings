# HostUtilities 业务按钮接入调查

调查及公共接口实现日期2026-11-16。HostUtilities仓库只读；业务接入交给其Agent，完整可复制任务见[双界面接入提示词](hostutilities-integration-prompt.md)。

## 真实业务入口

| 按钮 | 代码与调用链 |
|---|---|
| 检查更新 | `HostUtilities/HostUtilities/Panels/PanelMiscellaneous.cs:118`：IMGUI按钮调用 `UpdateCoordinator.CheckUpdatesFromPanel()`，随后 `Input.ResetInputAxes()`。入口在 `VersionCheck/UpdateCoordinator.cs:71`，复用 `StartCheck(false)` 及其已有检查、下载、安装、退出准备、计划和实例就绪保护。 |
| 修改名字 | `DisplayName/DisplayNameConfig.cs`：既有 `playerName`，key为 `01-Replace_Name_First_Time_Hover_Mouse_For_Instructions`，`ConfigurationManagerAttributes.CustomDrawer` 绑定 `DrawPlayerNameTextArea`；SettingChanged驱动原改名逻辑。 |
| 改名网页 | `DisplayName/DisplayNameConfigDrawer.cs`：名字IMGUI输入框右侧按钮先隐藏Configuration Manager，再经Steam overlay打开服务动态URL，最后重置输入轴。 |

`DisplayNameWebService.GetWebsiteUrl()` 保留当前语言及服务运行时的实际端口、握手参数；无服务时才回退 `GetWebsiteUrlWithLanguage()`。不能硬编码网址丢失网页回传，日志也不能暴露token URL。

## 现已具备的公共能力

`RegisterActionV1(string ownerGuid, string actionId, IDictionary<string,object> descriptor)` 返回IDisposable，字段详见[API](api-v1.md#业务按钮)。不提供Entry时为独立按钮，Entry为同owner原ConfigEntry时追加行旁按钮。只有动作的MOD也进入同一目录；不制造bool配置，不参与保存/默认。后注册替换旧注册，释放旧租约不会删除新动作。主线程执行、可选轻量CanExecute、异常隔离及ClosePage先释放菜单/输入再执行业务均由公共服务实现。

推荐HostUtilities登记两个稳定ID：`check-updates` 为独立按钮，`display-name-website` 附加到原playerName。两者 `ClosePage=true`，分别调用原更新入口和从drawer提取的共同打开网页函数。Configuration Manager继续保留原CustomDrawer与IMGUI按钮，Mod Settings保留自己的内置文本框和提交链，不用整行renderer复制输入。

GUID为 `com.ch3ngyz.plugin.overcooked2.modsettings`；公共API仍为1，动作方法是可选扩展，必须探测实际方法，不能仅检查1.0.0版本字符串。通过Chainloader当选实例及缓存反射软依赖接入；缺插件或旧版无方法时原功能继续。语言变更重新登记，销毁释放租约。

## 验证边界

本仓库用独立测试回调验证公共动作/原生控件，没有实现HostUtilities接入、触发其手动检测或打开改名网站。安装环境中的HostUtilities仍会按其原启动流程运行；这不是两种UI业务接入证据。HostUtilities Agent仍需验证更新弹窗、Steam overlay、动态URL回传和其各安装组合。
