# 存档门禁后的全局设置层调查

调查日期：2026-11-16 至 2026-11-17。范围：本地 Windows 安装的 DLL、原生 C# 反编译树和 Unity 序列化资产，只读调查；仅新增本文。没有启动/结束游戏、部署 DLL、操作存档、执行本轮 Unity 测试或提交。父任务并行修改实现，本文不评价其最终 diff，也不把已有历史测试计作新架构验收。

## 可执行结论

首次 `PCSaveManager.LoadProfile` 返回 `Exists` / `NotExist`，且主菜单实际可交互后，一次性捕获原生模板并建立插件自有 DDOL Canvas、输入域与页面。此后快捷键及页面生命周期不再要求场景 `RootMenu` 存在，也不因 `LoadingScreenFlow.IsLoading` / 原生对话框存在而排队。菜单入口和快捷键应共同打开同一页面，复用已有目录、编辑事务、弹层和分帧构建逻辑。

推荐 `GlobalEventSystem : T17EventSystem` 只覆写 `Awake`，跳过 `T17EventSystem.Awake` 的自动注册，缓存反射写入本实例的 `m_T17Module`。在当前安装的 UnityEngine.UI 中，`EventSystem` 没有覆写 `Awake`，其继承的 `UIBehaviour.Awake` 是空体；跳过该链没有发现遗漏的 EventSystem 初始化。继承的 `EventSystem.OnEnable/OnDisable`、T17 的 Update/延迟选择和 suppression 行为必须保留。这样不必操作游戏管理器的私有 free pool，也不会被游戏用户重新分配。

打开时同时持有原生 T17 输入域的 `Disable(owner)` 令牌、各 `PlayerControls.Suppress(owner)` 令牌及必要的 engagement 令牌；持续补齐场景切换中新出现的对象。关闭/禁用/销毁只释放自己持有的令牌，不全局 Reset、EnableAll 或修改原生弹窗队列。独立域排除在抑制目标外。

全局 Canvas 用 `ScreenSpaceOverlay`、Default sorting layer、`sortingOrder=32767` 才能超过原生转场的 32766；原生 spinner 已用 32767，所以仍需真实 Unity 验证同级注册/渲染顺序和射线首命中。32000 会被转场盖住，32768 不可作为可靠方案：安装资产的 `m_SortingOrder` 类型明确为 `SInt16`。不能根据“数值最高”宣称任意第三方 IMGUI 窗口也一定在其下。

## 证据来源与身份

游戏根目录：`E:\SteamLibrary\steamapps\common\Overcooked! 2`。

| 文件 | 本次身份 |
| --- | --- |
| `Overcooked2_Data/Managed/Assembly-CSharp.dll` | SHA256 `9BB6A3791331201D32CA89C3509F019A9780309DA7110002F04020E8491E1908`；MVID `114d606a-29d5-472c-8ebe-c2c20fbe3be3` |
| `Overcooked2_Data/Managed/UnityEngine.UI.dll` | SHA256 `D6925513E19634DD3872ABE83367690B87D64BEE00CF2EF3E197DCED056F7312`；MVID `38bc050c-bc6d-456c-b604-08a8f9b8848a` |
| `Overcooked2_Data/Managed/UnityEngine.TextRenderingModule.dll` | SHA256 `FBD69D33D916BB44E04F64E5CAF5BC30A6CEA7B7149A28411EB9DB822868ED5E`；MVID `e9299ea7-2cbd-404e-a3ee-8bb1248b606a`；Mono.Cecil 只读检查 Font API |
| `BepInEx/plugins/ConfigurationManager/ConfigurationManager.dll` | `BepInPlugin` 版本 `19.1`，GUID `com.bepis.bepinex.configurationmanager`；SHA256 `143BB25E8F03ED8728224DE0B72752DE29C0962FF69D20EC685831EC7491B323`；MVID `3fa6c5ce-24c4-467a-8e41-436af020c540` |
| `BepInEx/core/BepInEx.dll` | SHA256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70`；MVID `d1b92069-86c2-41dc-ad96-bb21eee55a97` |
| `Overcooked2_Data/StreamingAssets/Windows/bundle47` | SHA256 `7C3D4B03CDC088E1AB232392E2F881163F7F330D31CCC523D013A09AF200B576`；含 GameMetaEnvironment prefab |
| `Overcooked2_Data/StreamingAssets/Windows/loading` | SHA256 `098127E35D892148AB08BCC55002EFF54772CE6347019E9333C05D0F90D76220` |
| `Overcooked2_Data/StreamingAssets/Windows/bundle22` | SHA256 `A62E4CF87CD9CF2EFD694D43FB74A42C4BE611B23E441D6D6B8ECD82C9A0CAC5`；含 GameOptions 实际字体 |
| `Overcooked2_Data/StreamingAssets/Windows/startscreen` | SHA256 `A3C2C3C5B0963499DA0786BB18BB654EF5B149DCA1769A3A1B824017BE5279A8`；GameOptions 模板树 |
| `Overcooked2_Data/StreamingAssets/Windows/bundle24` | SHA256 `4A260C2CDD4D26AA0E3DDA58E922469784E3F695289BF9668FBC59432F993F4A`；8 个引用 Sprite/Texture |
| `Overcooked2_Data/StreamingAssets/Windows/bundle25` | SHA256 `26AB1E177E3BF478F5E1E9703806BA9291228E4DF616C7B65CC44F142ACE5B1E`；2 个引用 Sprite/Texture |
| `Overcooked2_Data/globalgamemanagers` | SHA256 `249075D1165775ECB5F673E1D22EA16CCE43AEC13C1C979756B22366B1B3B7EC`；TagManager 仅 `Default`，uniqueID 0 |

导航用源码根：`E:\code\OC2\Assembly-CSharp\Windows\Assembly-CSharp`。下文 `文件.cs:行` 均相对于此根。实际 DLL 通过 dnSpy 直接反编译核对，未卸载共享 dnSpy 已加载程序集。Canvas 数值来自本地 UnityPy 的 `read_typetree()`，没有导出或更改游戏资产；不是运行时截图/测量。

## 独立输入域的创建、更新与清理

### 原生 T17 的隐藏注册副作用

- `T17EventSystem.cs:30-40`：Awake 先调用基类，然后 `T17EventSystemsManager.Instance.RegisterEventSystem(this)`，销毁同物体旧 InControlInputModule，最后缓存 `GetComponent<T17StandaloneInputModule>()`。
- `T17EventSystemsManager.cs:64-89`：注册先 `SetAssignedGamepadUser(null)`，加入 `m_FreeEventSystems`；分配从 free list 尾部向前找未绑定对象，因此新加的独立域会成为优先候选。
- `T17EventSystemsManager.cs:39-61`：`ResetEventSystem` 会按用户 UID 删除 manager 字典项、ResetSystem、enabled=true、再放入 free pool；`ResetAll` 批量执行并清空原生选择/计数表。它们不是独立域的清理 API。
- 管理器未发现 Unregister 方法；T17EventSystem 自己也没有 OnDestroy 注销。直接 AddComponent 原生 T17 后 Destroy 容易把 Unity fake-null 留在原生池内。

不推荐为独立域调用 `AssignFreeEventSystemToGamepadUser`，也不能调用 ResetEventSystem 来“撤销注册”。覆写 Awake 跳过注册比反射删除 free pool 更窄；它不需要更改原生全局集合。若未来安装 DLL 的 EventSystem.Awake 新增初始化，必须重新核对这个约束。

### UnityEngine.UI 的实际生命周期

本次直接反编译 `UnityEngine.EventSystems.EventSystem`、`UIBehaviour`、`BaseInputModule`：

| 成员 | 确认行为 |
| --- | --- |
| `UIBehaviour.Awake/Start/OnDestroy` | 均为空；EventSystem 无自定义 Awake |
| `EventSystem.OnEnable` | 加入 Unity 自己的静态 `m_EventSystems` |
| `EventSystem.OnDisable` | Deactivate 当前 module、清空 current module、从 Unity 静态集合移除 |
| `EventSystem.UpdateModules` | 取得同物体 BaseInputModule，移除无效/非活动模块 |
| `BaseInputModule.OnEnable` | 缓存 `GetComponent<EventSystem>()` 并 UpdateModules |
| `BaseInputModule` 特性 | `[RequireComponent(typeof(EventSystem))]`，先加模块可能自动补出普通 EventSystem |

建议建立 inactive 对象，先加 GlobalEventSystem，再加 T17StandaloneInputModule，保证激活时两个组件均已存在；激活后绑定输入。GlobalEventSystem.Awake 只缓存字段，不能在模块尚未就绪时调用 `SetAssignedGamepadUser(nonNull)`。如显式设置 `EventSystem.current`，需要保存/恢复已有 current，不能把自己的域永久置成原生全局域。

`T17EventSystem.cs:50-65` 的 Update 在未抑制时临时设置 EventSystem.current 为自己、调用 EventSystem.Update 再恢复；独立域可直接沿用。`73-107` 的 LateUpdate 延迟实际选择，`149-165` 实现令牌抑制。`SetSelectedGameObject(null)` 仅写 pending=null，不能替代 `ForceDeselectSelectionObject()` 的立即取消选择。

`T17EventSystem.cs:128-145`：绑定非空 GamepadUser 会设置重复频率/反 Y 并 Initialize；AssignedGamepadUser 是元数据和原生选择上下文，不等于专属输入硬件通道。

### 原生模块读的是 Player.One

`T17StandaloneInputModule.cs:722-733` 的 Initialize 固定取 `PlayerInputLookup.Player.One` 的 UI X/Y、四方向、UISelectNotStart、UICancel；`PlayerInputLookup.cs:150-165` 再建立可重绑 logical input。因此“另建 T17 域”本身不会隔离物理键盘/手柄，必须同时抑制下层。

`Process`（241-264）负责 updateSelected、move/submit/cancel、touch/mouse。`ClearEventSystemLogicalButtons`（803-816）Claim 六种按钮的 press/release，既涉及恢复边界，也可能影响同帧其他消费者。域被抑制后，T17 Update 清理已释放令牌并在恢复时清逻辑事件（50-59）。恢复过程中必须保留其正常调用链。

## 原生菜单、对话框与加载输入仲裁

### 原生 EventSystems 对象不能成为父节点

`T17InGameFlow.cs:28-49,81-92` 的 Awake/Update 在没有暂停菜单、对话框、结算时停用名为 `EventSystems` 的 GameObject；OnDestroy 和打开暂停菜单再启用。`StartScreenFlow.cs:98-112` 也会启用此树、卸载存档、脱离主用户、ResetAll。独立域应挂在插件自有 DDOL 根，不能挂这棵原生树，也不应依赖其 active 状态。

### PlayerControls.Suppress 的范围

`PlayerControls.cs:204-240`：玩家控制权限检查 Application focus、direct control suppression、原生对话框和 InGameRootMenu 当前菜单。`Suppress` 是 direct-control controller 新增令牌，`ReleaseSuppressor` 仅释放给定令牌。`Update`（427-441）在同步/非暂停条件下清理 suppression，并在恢复时清 controls 事件。

它不会阻止 RootMenu.Update、对话框或其他 MOD 的输入。全局页打开时即使当前尚无 PlayerControls，也要跟踪随后创建的玩家；关闭已销毁玩家可直接释放 token，不调用失效 Unity 对象。`SuppressionController.IsSuppressed()` 只看集合数量；`Suppressor.Release()` 只是 owner=null，必要时仍需走 UpdateSuppressors，不能把“token已释放”等同于“controller立即无抑制”。

`FrontendRootMenu.cs:61-67`、`InGameRootMenu.cs:287-293` 直接读 UICancel.JustReleased，并检查 CachedEventSystem.IsDisabled。使用 T17.Disable 的令牌才能让这些调用方看到禁用状态；单纯 `domain.enabled=false` 不是完整仲裁。

### 原生对话框会持续维护焦点

- `T17DialogBoxManager.cs:52-99`：GetDialog 在当前版本强制 `forSingleUser=false`，禁用除主用户外的 manager 域，绑定原生主用户域，放入 global dialogs。不能借此接口创建设置页输入宿主。
- `T17DialogBoxManager.cs:161-205`：Update 处理排队 show，并每帧 EnsureFocus，把最后一个原生对话框重新 Focus。
- `208-242`：没有按钮的对话框额外持有用户域 suppression；有按钮时释放它自己的 hold。
- `286-306`：ReleaseMe 移除排队项、释放对话框 hold、更新 global dialogs，再 EnableAllEventSystems。不能为了关闭设置页调用它，也不能清它的队列。

独立页只抑制原生域，原生 manager 可继续维护自己的 selection/queue；它不能触碰独立域。这避免与 EnsureFocus 抢同一 currentSelected。关闭后原生对话框继续存在和处理输入。不要为了让设置可打开而强关/借用/重排原生对话框。

### 转场令牌与同步加载边界

`ScreenTransitionManager.cs:119-139` 的 TransitionUp 按主用户查 manager 域，`Disable(this)` 后启用转场图像；TransitionDown（188-192）只 Release 自己 token。独立域不进 manager 即不受此抑制。`LoadingScreenFlow.cs:67-73` 自己 DDOL；`88-106,165-174` 先转场并加载 Loading，再异步切目标场景。

不要为打开设置 Pause System：`LoadingScreenFlow.cs:179-184` 把 ActivateSceneWhenLoaded 设为 `!TimeManager.IsPaused(System)`，暂停可能反而阻断加载完成。保留后台加载，独立页只接管本地输入。

逐帧扫描/补租约仍有对象创建及执行顺序风险：新域/玩家可能在扫描之前已经 Update/FixedUpdate；需要实机覆盖新场景首帧。可在独立输入处理之前同步租约，必要时选择仅在全局页开启时作用的窄前置守卫；不能把轮询本身当作无一帧输入泄漏的证明。

主线程同步加载/资源上传/GC 阻塞期间没有新 Update 或渲染帧。DDOL 和独立域可去掉人为的 Loading 门禁，不能保证线程阻塞期间仍能即时响应。

## 实际 Canvas 排序与射线资产

由 UnityPy 读取本地 bundle 的 Canvas type tree，并沿同 serialized file 的 Transform m_Father 重建路径；mode 0=ScreenSpaceOverlay、2=WorldSpace。仅安装资产初始值，不代表其他 MOD 运行时没改。

| 资产 / PathID | 层级 | mode / layer / order |
| --- | --- | --- |
| `Windows/loading` / `463` | `FlowManager/Canvas` | 0 / 0 / 10 |
| `Windows/bundle47` / `-1928653386129436555` | `GameMetaEnvironment/DialogboxManager/GlobalDialogboxCanvas` | 0 / 0 / 9999 |
| 同上 / `7673472885331400193` | `GameMetaEnvironment/TransitionMananger/Canvas` | 0 / 0 / 32766 |
| 同上 / `1616261532417504218` | `GameMetaEnvironment/SpinnerIconManager/Canvas` | 0 / 0 / 32767 |
| 同上 | DebugManager / FixedAspectRatioManager / PlayerManager 的 Canvas | 0 / 0 / 10000 |
| `Windows/frontend` / `2532` | `TooltipManager/Tooltipcanvas` | 2 / 0 / 8888 |
| `Windows/ingamemenu` / `573,578` | InGameModeCanvas / PauseMenuCanvas | 0 / 0 / 3 |

bundle47 的 T17DialogBoxManager MonoBehaviour PathID `8253931785804839224` 的 `m_GlobalDialogBoxCanvas` 正式引用上述 `-1928653386129436555`；不是仅按名字猜测。ScreenTransitionManager PathID `-1078315047990516486` 的 `m_TransitionImage` 指向 `-6108110171533360335`，其路径为 TransitionMananger/Canvas/Transition，`m_RaycastTarget=0`。

SpinnerIconManager/Canvas 下 Loading、Saving、SpinnerIcon、ErrorIcon、WarningIcon 等 graphic 的 `m_RaycastTarget=1`。本次没有发现该 spinner canvas 上同层 GraphicRaycaster 的静态实例，不能仅凭 raycastTarget 宣称它一定截获点击；同级可见层级仍需验证。独立 Canvas 应有全屏透明/半透明 raycast shield，覆盖页面外空白；`EventSystem.RaycastAll` 遍历的是所有全局 raycaster，不按域隔离。

`UnityEngine.EventSystems.EventSystem.RaycastComparer` 会比较 camera、module priority、sorting layer/order/depth 等，独立域并不自动限制 raycast 到自己的树。UI 验收需同时测显示和 top hit，而非只看 sortingOrder。

## Configuration Manager 为什么能常驻

本次检查实际安装的 ConfigurationManager 19.1；不是引用网上某版本源码。

| 实际成员 | 行为与启示 |
| --- | --- |
| `ConfigurationManager.ConfigurationManager : BaseUnityPlugin` | 插件组件，无 RootMenu/T17 对象依赖 |
| `BepInEx.Bootstrap.Chainloader.Start` | 创建 `BepInEx_Manager`、DontDestroyOnLoad，再 AddComponent 各插件 |
| `ConfigurationManager.Update` | 每帧检查热键并切换 DisplayingWindow，没有场景/Loading/原生弹窗门禁 |
| `OnGUI` | 用 `GUILayout.Window(-68,...)` 画 IMGUI 设置；未移动窗口先画全屏 GUI.Button 背景 |
| `DisplayingWindow` setter | 重建配置列表、保存/恢复光标状态，并发送 DisplayingWindowChanged |
| `Update/LateUpdate/OnGUI` | 显示期间重复解锁并显示游标 |
| `OnGUI` | 非改键状态且全屏或鼠标在窗口内时 ResetInputAxes |

CM 能跨场景主要来自插件常驻 + IMGUI 自绘 + 自有热键，不是缓存 RootMenu。它没有在本 DLL 中调用 OC2 的 PlayerControls.Suppress / T17.Disable，不能拿“CM能画出来”证明 OC2 原生 UI 与玩家输入已正确仲裁。ResetInputAxes 是广域输入动作，原生逻辑按钮可能已在别的时序读取；当前方案应延续明确 owner token，而非照抄全局重置。

CM 的 IMGUI 与 Overlay Canvas 不是同一排序体系；并存时的热键、输入捕获、窗口优先级仍是兼容场景。不要修改 CM DLL 或公共插件 GUID。

## 资源缓存与克隆边界

原实现已经有 inactive DDOL `NativePageTemplate`，并非从零缓存。`NativeTemplateComponents.Rebuild` 在来源存活时用 runtime AddComponent 重建保留的 MonoBehaviour；已有 [验证记录](verification.md) 的 MonoScript 丢失是历史实际问题，本次没有重现/修复新问题。

`AssetBundles/AssetBundleManager.cs:316-334` 明确在引用计数归零时 `AssetBundle.Unload(true)`。普通 DDOL GameObject 和 C# 引用不能成为抵抗这条主动卸载的充分证据。建议：

1. 在首次成功门禁期间捕获并去掉场景控制器、OptionsData 保存/重置回调、EventTrigger、Animator；原生入口只转发到公共页面。
2. 复用既有 runtime 组件重建，独立复制只覆盖实际使用的 Font、Material、Sprite、Texture，按原始 Unity 对象引用去重，插件生命周期统一销毁自有副本；不全局改 Unload(true) 或长期 pin 整个 frontend 资源包。
3. Instantiate(Material) 仍需检查 mainTexture 等贴图引用；Instantiate(Sprite) 不能证明 texture 已复制。独立 Sprite 应使用自有 texture，保留源 rect、pivot（归一化）、pixelsPerUnit、border 及必要的 mesh/packing 行为。源 texture 不可读时不能盲目 GetPixels；GPU copy/readback 及压缩格式/色彩空间行为需实际 Unity 验证。
4. Instantiate(Font) 是否连同实际 glyph texture/material 完全独立，不能由编译或这份静态调查证明。动态字体重建会更新图集；给 font.material.mainTexture 固定一次纹理快照可能导致随后出现的中文/日文、长文本或语言切换缺字。需覆盖 `Font.textureRebuilt` 后的素材状态，或使用经过验证的动态 Font 自有生命周期。
5. 原生 UI 的 Texture/Sprite 可能是大图集。仅有一份副本也可能显著增内存，避免按每个控件重复克隆相同图集。记录复制数量/尺寸和开关前后内存，尤其本项目已有历史厨房原生 OOM 未归因。

上面的 Font/Sprite 注意项是实现风险和待测条件，不是已在 Unity 2017 中完成的克隆实验。

### GameOptions 字体的实际资产

`Windows/startscreen` 的 `BuildPlayer-StartScreen` 序列化文件中，GameOptions 的 16 个 Text 均指向 `m_FileID=19`，该外部文件解析为 `CAB-3ac2ffef9f262f38eecc7124c10c6b06`，位于 `Windows/bundle22`。沿同一资产引用核对：

| 模板对象 | Font / PathID | 内嵌 FontData | FontNames |
| --- | --- | --- | --- |
| `HeaderBacker/Header`、`Cancel/Text`、`Confirm/Text` | `Header_Font` / `-6922138709629050742` | 117,484 bytes；CharacterRects=0 | Naughty Squirrel、Naughty Squirrel edited.、Noto Sans CJK SC、Noto Sans Thai |
| `ScreenAdjuster/Title` 及 Windowed/SFXVolume/Resolution 等正文/值 | `Body_Font` / `4264312985008555238` | 690,516 bytes；CharacterRects=0 | Carlito、Noto Sans CJK SC、Noto Sans Thai |

这是嵌入字体程序并支持动态生成字形图集的静态证据，不能描述成纯粹依赖 OS 字体且没有 FontData。运行时 `Font.dynamic` 的实际返回和 Instantiate 后的本机资源归属仍需 Unity 验证，不能在桌面 CLR 调用 Unity ECall 来证明。

两份 Font 还有外部 `m_FallbackFonts`：FileID 3 指 `CAB-75d04d3ea0d8529a5606ecf6834a129c`，PathID `7455725164858908309`；FileID 4 指 `CAB-73668fd70b6b4fe2109582b76bd036b9`，PathID `5717724048211452215`。Header_Font 另有本 bundle 的 NaughtySquirrel2 fallback。只复制主 Font 可能继续共享这些 fallback；本次未验证其卸载后的有效性。实际安装 `UnityEngine.TextRenderingModule.dll` 的 Font 全部字段/属性/方法经 Mono.Cecil 只读检查，仅公开 fontNames、material、characterInfo、dynamic、请求字形和 textureRebuilt 等，没有 fallbackFontReferences 或 fallback 引用 setter；不能把 TextMeshPro/新 Unity 的同名能力当作此安装可用 API，也不能声称仅再 Instantiate fallback 对象就完成了主字体引用替换。

因此动态 Font 应保留其动态生成 atlas 路径；静态 Font 才复制固定 material/texture。仍需在资产卸载、动态图集扩张和语言切换后检查 Text 实际 font/material/texture 是否有效。后续若用 `Canvas.willRenderCanvases` 比较活动同级 rival 的 renderOrder、必要时重新注册自己的 Canvas，应只改自有 Canvas，并在回调中做重入保护与每帧限次；它是待验证的同级排序策略，静态资产数值不能证明最终排序。

### GameOptions 图像的数量、尺寸与打包状态

对 `BuildPlayer-StartScreen` 中 GameOptions（GameObject PathID `1442`）整棵树递归读取，包含 inactive PC/Console 分支：78 个 Image，70 个非空 m_Sprite 引用，按 PPtr 去重后为 10 个 Sprite、10 个 Texture2D；没有非空 SpriteState 或显式 m_Material 引用。运行时 overrideSprite、default material 和其他 MOD 改写不在此静态计数内。

下表所有 `m_Rect=(0,0,width,height)` 恰好覆盖其独立 Texture2D；实际 `m_RD.textureRect` 是紧裁渲染范围，未归一化像素坐标。前 8 项来自 `bundle24/CAB-4a037019a026e025b278814e7fcbb692`，后 2 项来自 `bundle25/CAB-c226f14b26c9ddde73168cbccdc14c43`。每份 Sprite 的 Texture 与其同名；不是一张包含无关界面的共享 atlas。

| Sprite 名 / PathID | 引用数 | Texture 尺寸 | textureRect：x,y,w,h |
| --- | ---: | --- | --- |
| t_ui_dialog_large_01_d / `-5190906492376146924` | 1 | 1920×849 | 0,65.073,1920,722.851 |
| t_ui_dialog_header_01_d / `-5621866459154724612` | 1 | 1207×179 | 10.076,18.076,1179.848,147.848 |
| t_ui_MenuBar_01 / `-1565848059162038949` | 10 | 1086×80 | 3.076,5.076,1079.848,70.848 |
| UI_AudioSegment_Dark_01 / `7571815116381174649` | 40 | 31×61 | 0,0,31,61 |
| UI_ArrowRight_Dark_01 / `-647435861570474255` | 6 | 29×51 | 0,0,29,51 |
| UI_ArrowLeft_Dark_01 / `2497133565851636015` | 6 | 29×51 | 0,0,29,51 |
| t_ui_button_02_standard_d / `7790064376257779284` | 1 | 495×156 | 6.076,9.076,480.848,140.848 |
| t_ui_button_01_standard_d / `2241799627486347525` | 1 | 495×156 | 4.076,11.076,481.848,138.911 |
| UI_TickBox_01 / `-7570584799299201920` | 2 | 65×66 | 1.076,1.076,62.848,61.848 |
| UI_tick / `-3147842081145239618` | 2 | 78×70 | 0.051,0,73.873,70 |

10 张 Texture 全是 format 4（RGBA32）、mipCount=1、m_IsReadable=false。各 Sprite 的 `m_SpriteAtlas` 与 `alphaTexture` 均 null、settingsRaw=64（UnityPy 解码 packed=false、rotation=0、meshType=Tight）；无需为这组源资产复制整个 bundle，或为并不存在的旋转大图集另加采样分支。独立 FullRect Sprite 使用原 m_Rect 与整张复制 texture，静态数据未显示 UV 区域偏移风险；它改变原 Tight mesh（4～68 顶点），视觉透明边缘/射线仍需实机检查，不能把静态 UV 推导写成实际渲染验收。

10 张单份 RGBA32 像素合计 **8,408,208 bytes（8.019 MiB）**，最大背景单张 6,520,320 bytes（6.218 MiB，约占77.55%）；这不是进程增量测量，不包含 Font 动态图集、material、Sprite/Unity 对象、GPU staging 和驱动开销。一次复制最大图至少还会临时申请相同分辨率的 ARGB32 RenderTexture；实际峰值与驱动分配需要测量。按对象身份去重将 40 个音量段指向同一副本正确，不能按每个控件复制；Capture 不应遍历 bundle24/25 的全部 Texture2D，后者含大量无关图片。

## 建议修改边界与验收

实现应集中在 NativeShortcut 的统一开关、GlobalSettingsHost 的 DDOL 画布/所有权、GlobalEventSystem 的窄 Awake 适配、NativePage 的独立 domain 绑定、现有模板资源所有权。保持一个目录、一个页面模型、同一保存/取消/自定义编辑器流程。保存门禁成功应成为常驻服务已就绪的锁存状态；后续存档切换的短暂无 RootMenu 不应重新让快捷键依赖主菜单。当前资料不足以代替对“存档卸载后是否仍允许编辑配置”的产品约定，但本任务明确要求首次成功后全场景可开。

必须保留的实机验收：

- 初次存档失败/未完成不初始化；Exists、全新 NotExist 成功后初始化一次。主菜单入口与快捷键指向同一页面。
- 主菜单、街机大厅、厨房、Loading、转场遮罩中打开；已开页跨场景；返回 StartScreen/重新选档；对象/模板只保留一份。
- 原生有按钮对话框、无按钮等待对话框、排队对话框均可上覆设置；设置关闭后原生焦点、令牌计数和业务按钮保持正确。
- 鼠标/触摸/键盘/手柄分别区分脚本与物理输入证据；编辑/改键捕获时只抑制独立域导航，取消和关闭逐层释放，不误触原生返回/暂停/换队/玩家动作。
- 新玩家/新域本帧创建、失焦再恢复、外部禁用、异常销毁都没有输入泄漏/遗留令牌；不重写原生 manager refcount。
- 每个场景截图记录 overlay/native canvas 的 renderOrder、sortingLayer/order、像素几何、EventSystem.RaycastAll 首命中；特别覆盖 32767 spinner 同级。
- 资产卸载后长中文/日文/语言切换、字体图集扩张、Sprite 边框/背景/滚动条完整；反复开关和跨场景内存不持续增长。
- 同装 ConfigurationManager 及其他原生弹窗 MOD；只作本地脚本验证不能替代物理设备或真实 Steam 联机。

## 本次交付证据分级

改了什么：仅新增 `docs/global-overlay-research.md`，提供实际 DLL/成员/资产排序与改动建议；没有改源码、测试或游戏文件。

验证了什么：`Get-FileHash -Algorithm SHA256` 核对本文 12 个输入文件；dnSpy 直接读取游戏、Unity UI、CM、BepInEx 的相关类型，Mono.Cecil 只读核对 Font API；Python + 既有 UnityPy 只读解析序列化资产，得到 loading=10、global dialog=9999、transition=32766、spinner=32767，以及 SInt16 排序字段、Default 唯一排序层、两份 GameOptions 主字体的内嵌数据和 fallback 引用、10 张源 Sprite/Texture 的尺寸及 packing 状态。`git diff --check -- docs/global-overlay-research.md` 通过。这里的通过只代表本地静态检查/反编译/资产解析。

没验证什么：没有新架构编译、桌面行为测试、Unity 脚本输入、真实界面截图/几何、物理输入、Steam 联机或第三方组合验证；没有声称常驻窗口已经交付。父任务实现及其验证结果应独立记录。

运行时前提：采用父任务最终构建并在允许部署的进程重启后验收；首次成功加载存档且主菜单可交互时抓模板。加载场景仍须有可执行/可渲染的 Unity 帧，主线程阻塞无法由常驻 UI 消除。本文不产生部署、终止玩家进程、push、发布或本地提交授权。
