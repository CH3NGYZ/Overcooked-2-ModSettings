# 即时输入与校验反馈验证

日期：2026-11-17。用户要求输入后立即反应，搜索即时筛选；配置输入校验通过即刻保存，失败在下面显示“数值有误”。生产及测试提交：`ffc4725`。

## 修改与调用链

- `NativeTextInput`统一使用`Changed`传递编辑，回车只结束有效编辑，不再承担搜索提交。批量文本仍一次通知，鼠标选区和剪贴板继续共用该实现。
- `NativePage`在搜索输入变化时重建当前MOD结果，保留搜索草稿、选区和光标。延迟布局若发现用户已离开搜索则停止，不恢复旧滚动或抢回焦点。
- 普通输入原本已有即时保存，但解析异常文字会被Description刷新覆盖。现在统一显示“数值有误”，编辑期间优先保留；合法后、取消或失焦后清除，非法草稿不写入配置。
- 普通数值、向量分量、颜色Hex复用现有解析/范围校验与`ConfigCommit.Apply`；保留原ConfigFile、SaveOnConfigSet及真实值回读。保存失败返回实际错误，不能把失败回调当成成功校验。
- 色板Hex与A输入在弹层内各有就近提示，避免错误显示在被遮罩盖住的底层页脚。滑条/色板拖动预览、松手保存的规则保持不变。

环境为本机安装的BepInEx 5.4.23.5、Unity 2017.4.8.5945332；生产目标.NET3.5/C#3，TreatWarningsAsErrors开启。未修改HostUtilities、游戏或其他MOD源码。

## 验证命令与结果

```powershell
.\scripts\test.ps1
.\scripts\test-examples.ps1
.\scripts\test-unity.ps1 -WithoutFastInit -FixtureCount 30 -TextEditingOnly
.\scripts\test-unity.ps1 -WithoutFastInit -FixtureCount 30
.\scripts\test-unity.ps1 -WithoutFastInit -FixtureCount 30 -OpeningPerformance
```

| 场次 | 结果 |
| --- | --- |
| 生产构建、桌面、消费者 | 生产构建无警告；真实ConfigFile桌面36项通过；反射消费者/原生Renderer无需服务DLL引用即可独立编译。桌面裁剪DTO保留2个既有CS0649警告。 |
| `20261117-125342-710-without-fastinit` | 79项输入/分区专项通过；包括无回车搜索、替换/清空、焦点不被延迟刷新抢回、磁盘写入、非法草稿保留，以及SaveOnConfigSet开/关的保存失败。之后只调整色板错误间距，最终完整场再次覆盖这些断言。 |
| `20261117-125606-767-without-fastinit` | 第560项失败：色板错误区域与输入框贴边的几何检查不通过。实际截图文字可见；原始失败保留。将错误区域缩高，留出明确间距后重跑。 |
| `20261117-125854-153-without-fastinit` | 最终完整859项通过，覆盖即时输入、选区/剪贴板、分区排序、色板、动作隔离、字体遮罩、原生弹窗及常驻页生命周期。未进入厨房。 |
| `20261117-130125-444-without-fastinit` | 最终打开性能45项通过：首次submit10.674ms、内容106.440ms完成、最大观测帧17.197ms；六次重开10.264/9.873/9.403/9.688/9.369/9.947ms，平均9.757ms；未重建控件。 |

最终完整场与性能场合计904项真实Unity脚本断言；专项与失败场不重复累计。初始玩家会话存在期间只做本地构建，等待自然退出后才开始临时安装。未结束玩家进程。没有在旧DLL下运行本次新测试的红测，不把静态回调证据描述为已完成红转绿。

## 已查看的真实截图和几何

- [无回车筛选boolean](../.validation/20261117-125854-153-without-fastinit/live-search.png)：搜索仍在编辑，右侧仅剩匹配配置。
- [非法数值与底部提示](../.validation/20261117-125854-153-without-fastinit/invalid-input.png)：输入`-`保留草稿，配置/磁盘保留最后有效值12，页脚显示“数值有误”。反馈刷新不会覆盖错误。
- [色板内非法Hex](../.validation/20261117-125854-153-without-fastinit/invalid-modal-input.png)：`#12GG`不保存，错误显示在弹层输入框下方。

以上最终截图实际PNG均为3840×2160，不能以启动参数1280×720当作实测尺寸。主面板数值输入725.86×48 UI，页脚1433.60×56.72 UI；输入下边界y=-313.84，页脚上边界y=-341.02，间隔27.18 UI。色板Hex输入387.07×70.96 UI，错误区域387.07×35.48 UI，间隔3.94 UI。原始坐标见同场`live-input.txt`、`modal-validation.txt`。仅做本分辨率实际渲染验收。

保存失败场使用本轮证据目录内的文件阻断路径，不接触玩家配置。两种SaveOnConfigSet均验证：内存实际变为3，磁盘未保存，界面持续显示真实保存错误，保存策略保持不变；不会假报恢复旧内存值。正常自动保存/手动保存的边界仍由桌面和完整UI场共同覆盖。

动作完整回归故意抛出`fixture action failure`和`fixture predicate failure`，用于验证隔离；页面继续运行，不屏蔽生产日志。另有HarmonyX缺少`System.Exception.PrepForRemoting`警告，不来自本次输入校验路径，未在本轮处理。

## 安装与存档恢复

四场runner结束时均报告`Original saves checked; temporary installation restored`；各场`saves.tsv`非空且均包含1个原存档。13:05左右再次按全部manifest复核36项，32项一致，4个差异记录均指向同一个FastInit DLL，其他设置DLL/测试DLL/配置/日志及原存档核对一致。没有残留游戏进程。

- 各场FastInit备份SHA256：`E11F2416BBAB0EA009DF74216DB3E7E569D309D968CF65308B1D191CE8FC98AE`。
- 外部新FastInit文件SHA256：`E1D12E5D43F3BD47CF0BB07450B625A00918178BCE9C1F9358E9DD81EC5EA669`，时间戳2026-11-17 13:02:43.0116071。它与最后一场备份不同，保留现状和旧备份，未用旧文件覆盖外部更新；不能宣称收尾所有文件仍与基线相同。
- 当前安装的ModSettings已恢复到前轮`D77EC9AB40B1EB7D705B9F0EFE9B1F6C0DBB0D150187F48708DF768095DDA6A6`；本次生产产物为`bin/Release/OC2ModSettings.dll`，SHA256 `D5D903AFFD63E91EFA39C8FC9540ACDA2C41FE88A078DD407BA54107FC592F4D`。

## 未覆盖与运行前提

已验证实际Unity内输入方法、原生事件/射线、真实ConfigFile及渲染截图，不等同于物理鼠标/键盘/手柄或真实IME验收。本轮未重跑真实厨房、Steam联机、共享服务全矩阵、FastInit启用组合、其他分辨率/平台、全部第三方自定义转换器；打开性能数字仅代表本机本场景，未测大量配置逐字搜索的性能上限。

没有正式部署、push或发布。新功能需安装本次DLL并重启；所有源码内置公共服务副本需同步完整实现，避免同版本旧副本被选为服务。HostUtilities业务按钮和Section rank仍由其Agent负责。
