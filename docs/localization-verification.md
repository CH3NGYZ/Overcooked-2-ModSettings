# 12语界面与下拉语言刷新验证（2026-11-17）

## 实现与接入

- `Texts` 继续作为公共文案唯一入口，按通用操作、编辑、色板拆分，21个key各自集中12种译文。实际安装的`Assembly-CSharp.dll`枚举顺序为English、French、Italian、German、Spanish、Russian、Brazilian、Polish、Chinese、Japanese、Korean、ChineseTraditional；未知枚举回退英文。开关仍调用游戏的`OptionValue.On/Off`翻译。
- 游戏语言变化时原位更新已缓存的入口、标题、搜索placeholder、工具栏、行按钮、校验提示及已打开的色板、捕获、恢复默认弹层；不重建编辑器，不保存配置。有效值、非法草稿、光标和输入抑制规则保留。
- 下拉候选值与显示名通过`ChoiceItems`同时取样，复用原`ValueCodec.Format`，因此`ModSettingsAttributes.ObjToStr`及兼容的旧attributes均可按MOD独立语言生成文案。显示名不参与保存；枚举、Flags和KeyCode共用这条路径，KeyCode保留去重、允许值和当前键焦点规则。
- 打开中的内置下拉约每0.2秒重读候选集合、标题及formatter；无变化不换按钮，有变化按原始值恢复焦点及滚动。调用SDK `Refresh()`触发目录重建时，只要同一ConfigEntry仍有可用下拉，就重开该列表；延迟布局不抢弹层焦点，关闭回到新编辑器按钮。
- 消费方标题、分区、说明或业务动作改变时，仍须批量更新其元数据/注册并统一`client.Refresh()`。这类显式刷新沿用取消未确认编辑的既有规则；不是游戏公共文案的原位刷新。只有启动时翻译字符串的旧列表不能被公共服务自动翻译，须消费方提供formatter或更新自己的候选来源。接入见[SDK说明](../sdk/README.md)和[示例](../examples/ConsumerPlugin.cs)。

## 命令与结果

```powershell
./scripts/test.ps1
./scripts/test-examples.ps1
./scripts/test-shared.ps1 -BuildOnly
./scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 3 -LocalizationOnly
./scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 30
./scripts/test-unity.ps1 -WithoutFastInit -FixtureCount 30 -OpeningPerformance
```

- 生产.NET3.5/C#3、warnings-as-errors构建通过；358项桌面断言通过，使用真实ConfigFile、生产程序集、独立SDK程序集及实际安装的语言枚举。覆盖12译文完整性、未知语言回退、formatter闭包变化、稳定值、候选重排、历史section/key、Flags勾选和KeyCode别名去重。
- SDK及完整消费者/原生renderer示例在没有服务DLL引用的情况下独立编译通过；四份源码内置程序集编译通过，未重跑共享选优启动矩阵。
- 最终三场真实Unity脚本验收合计1044项：语言专项`173245`场137项、完整页面`172945`场862项、性能`173435`场45项。早先`172709`场135项属于后续补测前的结果，不重复计数。
- 语言专项通过实际控件选择MOD语言，从英文`Force host`更新为`强制主机`；已打开的列表在显式SDK刷新后恢复标题、焦点与候选，并向磁盘保存原始`host`值。另覆盖无显式Refresh时相同formatter原位更新、无变化保留按钮、候选重排后点击不串值、色板非法草稿、捕获租约、默认确认、搜索空结果和隐藏缓存页面重开。
- 性能场首次submit 11.092ms，内容90.297ms完成；六次重开submit均值10.257ms。该场首次观测帧间隔最大17.107ms；只是本机本场数据，不代表任意MOD回调或所有机器无卡顿。

## UI与恢复证据

最终专项原始证据：`.validation/20261117-173245-141-without-fastinit/`；完整回归及性能分别为`.validation/20261117-172945-561-without-fastinit/`和`.validation/20261117-173435-089-without-fastinit/`。截图和测试数据不进入Git。

- 已查看12语实际截图联系表`languages-contact.png`及独立MOD中文下拉`consumer-dropdown-zh.png`；截图实际3840×2160。简繁中、日、韩、西里尔和重音字符正常显示，长按钮文案在固定区域缩字或两行显示；MOD业务文字继续使用消费方自己的语言。
- `localization-geometry.txt`记录搜索499.20×46.00 UI、关闭163.20×46.00 UI；`localized-labels.tsv`记录各语言文字矩形、字体和best-fit信息。入口标题、蓝色按钮、橙色焦点、白色滑条抓手及64×64开关沿用原样式。
- 七场（含失败调查与被替代专项）退出后复核56项安装文件、7项存档哈希，63/63一致；详情`173435`场`restoration-audit.json`。没有残留游戏进程。测试开始前重新确认玩家进程已退出，没有终止玩家会话；测试只临时安装，收尾恢复测试前的DLL。

## 调查失败与边界

`171408`、`171903`场在非法草稿保留断言失败；`172403`场进一步捕获到控件销毁。测试栈证明已安装`OC2HudCustomization.SettingsLanguageSync.Poll()`在游戏语言变化后主动调用全目录`Refresh()`，与公共文案原位刷新叠加，按既有规则取消草稿。专项测试随后仅在测试进程中临时patch该轮询，以独立验收公共界面；显式消费方Refresh另通过fixture真实SDK路径完整测试。没有修改HUD或HostUtilities源码/DLL，也没有改变生产显式刷新取消草稿的契约；失败原始证据保留。

12语来源通过测试Harmony覆盖真实`Localization.GetLanguage()`返回值；没有写玩家游戏语言。实际Unity绘制与脚本输入已验证，物理设备手动切换语言、真实IME、Steam联机、其他平台/分辨率和母语校对未验证；本轮没有重跑厨房跨场景或共享选优全矩阵。

本轮沿用未发布源码版本1.1.0/API v1；生产DLL为`bin/Release/OC2ModSettings.dll`，SHA256 `671E320DF03F2B01BBDE0D13020E8691870A095F7451FF4E8412E32FFDEA81D7`。实际当选公共服务及源码内置副本需同步完整实现后重启。没有正式部署、push或Release；本轮没有修改技能源码或执行Library/Agents同步。
