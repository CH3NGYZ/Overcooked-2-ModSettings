# 鼠标文本选区与分区顺序

日期：2026-11-17。本轮修复共享输入的鼠标定位/拖选，增加独立分区排序。未修改HostUtilities、游戏、DIYLevel或FastInit原DLL。

## 输入修复

旧`NativeTextInput`继承原生T17Button，但仅处理键盘光标和全选标记，没有实现鼠标字形命中及拖动处理。真实Unity红测`20261117-021416-750-without-fastinit`在第21项失败：单击输入左端后光标仍留在末尾；外层ScrollRect还会接管输入中的拖动。

- `NativeTextInput.Pointer.cs`按实际TextGenerator的字形、行信息和pixelsPerUnit，把屏幕坐标转成光标位置；输入持有拖动处理，支持正向、反向、换行选区及Shift单击扩选。
- `NativeTextInput.cs`使用caret/anchor描述局部选区，支持局部复制、剪切、粘贴、替换、删除及Shift方向键；方向和删除不拆开UTF16代理对。一次inputString提交中的连续文本作为一次编辑通知，保留有效值即时保存和非法中间草稿规则。
- `TextSelectionGraphic.cs`用独立MaskableGraphic绘制淡蓝选区和白色光标，遵守原遮罩；不再往正文插入假光标字符。输入按字面显示富文本标记，保证字形位置和原字符串一致。
- 搜索、数值精确输入、颜色及向量分量共用该实现。没有添加第二套保存、外层滚动或配置目录逻辑。

## CategoryOrder

- 普通配置的跨程序集`ConfigurationManagerAttributes`公共字段及`ConfigureEntryV1`字典均支持`CategoryOrder`；独立动作的描述字典同样支持。字段为`int`，公共tag也允许`int?`；null表示未指定。
- `CategorySort`统一排序：分区按CategoryOrder降序，分区内部仍按Order降序。同owner、同显示Category统一取最高显式分区值，未传行继承该值；全部未传才默认0，负数有效。
- 分区同序时按原始cfg Section的Ordinal稳定身份排序，纯动作分区使用稳定actionId；保留原有同区同Order时按显示名及Identity的规则。行旁按钮始终跟随原配置项，其分区字段不覆盖原行。
- 显示翻译不修改历史cfg Section/Key，标题无需加回数字。HostUtilities应从已有Section声明顺序生成固定rank，同区普通配置和独立动作使用同一值；该宿主接入由另一Agent负责。
- API仍为v1，源码版本仍为1.0.0。旧公共副本会忽略新字段，需要更新实际当选实现；该扩展字段不代表Configuration Manager也自动识别它。完整契约见[API](api-v1.md)，宿主实施要求见[接入提示词](hostutilities-integration-prompt.md)。

## 验证结果

| 命令/场次 | 结果与范围 |
| --- | --- |
| `scripts/test.ps1` | **36项真实ConfigFile桌面断言通过**，含nullable tag/覆盖、负数/默认/冲突、语言稳定、独立动作及历史cfg身份。生产.NET3.5/C#3编译无警告；桌面裁剪DTO编译保留2个既有CS0649警告。 |
| `scripts/test-examples.ps1` | 反射消费者和原生Renderer在不引用公共服务DLL的情况下独立编译通过。 |
| `test-unity.ps1 -WithoutFastInit -FixtureCount 30 -TextEditingOnly`，`20261117-023802-092-without-fastinit` | **58项真实Unity专项断言通过**，含单击定位、正反拖选、换行字形命中/选区几何、父列表不滚动、局部剪贴板、批量输入一次通知及分区翻译稳定。 |
| `test-unity.ps1 -WithoutFastInit -FixtureCount 30`，`20261117-023930-095-without-fastinit` | 最终构建**842项真实Unity断言通过**。包含上述输入/排序以及既有颜色弹层、动作异常隔离、键位捕获、字体/遮罩、前端重建、常驻页面和关闭交互回归。此场未进入厨房。 |
| `test-unity.ps1 -WithoutFastInit -FixtureCount 30 -OpeningPerformance`，`20261117-024207-796-without-fastinit` | 最终构建**45项通过**；首次submit14.271ms，内容116.855ms就绪，首次最大观测帧22.446ms；六次重开9.674/9.790/9.433/10.004/9.371/9.911ms，平均9.697ms，控件未重建。 |

最终完整及性能两场合计**887项真实Unity脚本断言**；专项和中间场不重复计入。两场runner均退出0并完成原存档核对、临时安装恢复；收尾重新检查16项安装manifest和2项原存档，**18项存在性/哈希全一致，无残留游戏进程**。

### 实际查看的截图和几何

- [局部选中beta](../.validation/20261117-023930-095-without-fastinit/text-partial-selection.png)：白色光标、局部淡蓝选区，其他中英文保留。
- [换行拖选](../.validation/20261117-023802-092-without-fastinit/text-wrapped-selection.png)：长文本跨两行选区，没有把外层列表滚走。
- [英文标题分区顺序](../.validation/20261117-023930-095-without-fastinit/category-order-1.png)：故意使用ZZ/AA/YY/BB开头，仍依次Language → MOD Settings → Rename → Options；Check update紧随EnableScore。
- 实际PNG为3840×2160，页面参考1920×1080 UI。测试输入为725.86×48 UI，文字区域705.86×42 UI；精确记录见同场`text-selection.txt`。分区标题sibling为3/5/7/9，EnableScore为10，独立动作是11，中英文相同，见`category-order.txt`。

### 保留的中间证据

- `20261117-022511-309-without-fastinit`在第29项立即读剪贴板失败；随后相同生产复制路径通过。测试调整为最多1秒逐帧等待真实系统剪贴板一致，没有替换为假剪贴板，也未修改生产复制逻辑。
- `20261117-023053-778-without-fastinit`为补充换行/批量输入前的836项通过场；`023413`性能45项也通过，其中一次重开25.989ms、六次平均13.258ms。保留该尖峰，不将最终一次测量当作所有打开都无尖峰的保证。
- 其他早期红测及中间通过记录保留，不覆盖为PASS。完整回归中的`fixture action failure`和`fixture predicate failure`为故意注入的异常隔离验收，页面继续可用。

## 未验证与运行前提

已验证真实Unity射线、ExecuteEvents及实际渲染几何；未执行物理鼠标/键盘/手柄、真实输入法候选与组合输入验收。批量输入脚本通过不等于真实IME通过。未重跑厨房/共享服务完整矩阵、FastInit组合、其他分辨率及真实Steam联机；既有厨房原生内存分配失败未在本轮归因或宣称修复。

HostUtilities实际Section rank生成、双界面业务按钮和语言刷新接入仍交其Agent实施，不把本轮fixture当作宿主业务验收。更新后的提示词保留Configuration Manager自己的兼容路径，不要求它理解新字段。

最终产物`bin/Release/OC2ModSettings.dll` SHA256：`D77EC9AB40B1EB7D705B9F0EFE9B1F6C0DBB0D150187F48708DF768095DDA6A6`。测试后恢复旧安装；使用新功能需安装一致的公共实现并重启游戏，源码内置副本也需完整同步。没有正式部署、push或发布。

源码及测试已本地提交`d943df2`；验证及HostUtilities交接文档另作本地提交。`.validation`截图和原始记录、bin产物留在本地，不进入Git。
