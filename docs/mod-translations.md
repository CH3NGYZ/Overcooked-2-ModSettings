# MOD 文案翻译

MOD 设置跟随游戏语言。新接入或更新多语言的 MOD 应用[源码 SDK](../sdk/README.md#让-mod-文案跟随游戏语言)一次注册全部 12 种语言，由提供端选语。翻译文件可补充缺词，也可用于没有接入翻译 API 的 MOD。两种来源都只改面板文字，不改变配置文件的分区、键名和值，不注册新的配置、动作或回调。

## 文件位置

把 UTF-8 文件（可带 BOM）放在 `BepInEx/translate/<语言>/<MOD GUID>.txt`，例如 `BepInEx/translate/zh-CN/example.modsettings.consumer.txt`。文件名中的 GUID 需精确匹配插件 GUID（区分大小写），语言代码不区分大小写，支持 `en`、`fr`、`it`、`de`、`es`、`ru`、`pt-BR`、`pl`、`zh-CN`、`ja`、`ko`、`zh-TW`。没有对应语言时仍尝试 `en`。

每行一对 `键=译文`，例如：

```text
# example.modsettings.consumer.txt
mod=示例 MOD
group/General=常用功能
config/General/Enabled=开启功能
config/General/Enabled/description=开启后启用示例功能。
action/check/title=检查服务状态
action/check/label=立即\n检查
action/details/title=功能详情
action/details/label=查看详情
```

- 用第一个未转义的 `=` 分隔键和值；保留值内部的 `=`。支持 `\=`、`\\`、`\n`、`\r`、`\uXXXX`；行首的 `#` 或 `//` 用于整行注释。
- 同一文件内同键重复时，以最后一条有效记录为准；跳过空行、`r:` 和 `sr:` 规则，不执行正则替换。格式错误的行记录警告，不影响后面的有效行。
- 仅在插件启动时读取并缓存。切换游戏语言会立即选用已缓存的译文；添加、删除或修改文件后需要重启游戏，`Refresh()` 不重读文件。

## 字段键

| 字段键 | SDK 生成方式 | 用途 |
|---|---|---|
| `mod` | `ModSettingsTranslations.ModName` | 左栏及快捷键重复提示里的 MOD 名称 |
| `group/<Category>` | `Group(category)` | 配置和独立动作的分区标题 |
| `config/<Section>/<Key>` | `ConfigName(section, key)` | 配置行显示名 |
| `config/<Section>/<Key>/description` | `ConfigDescription(section, key)` | 配置说明 |
| `action/<Id>/title` | `ActionTitle(actionId)` | 独立动作行标题；也用于动作搜索与错误提示 |
| `action/<Id>/label` | `ActionLabel(actionId)` | 独立或行旁动作的按钮文字 |
| `action/<Id>/description` | `ActionDescription(actionId)` | 独立动作行、聚焦的行旁动作按钮的说明；也用于动作搜索 |

键区分大小写。`Section` 和 `Key` 来自实际 `ConfigDefinition`，不是屏幕上显示的文字；动作 `Id` 是注册用的稳定 ID。`Category` 取翻译前最终生效的原分区名：配置默认用其 `Section`，独立动作未指定则用 `General`，行旁动作跟随原配置行的分区。每个键段内的 `~` 写为 `~0`，`/` 写为 `~1`，SDK 生成器会自动转义。翻译表不处理配置当前值、下拉候选和自定义编辑器的业务文字。

## 回退顺序

每个字段独立取第一条可用译文：**当前语言 API → 当前语言文件 → 英文 API → 英文文件 → 原文**。例如简中文件比英文 API 更优先，同为简中则 API 优先。名称、标题、分区、按钮标签跳过空白译文；说明可用空值显式清空，例如 `config/General/Enabled/description=`。原文来自已有 MOD 名称或单语 `DispName`、`DisplayName`、`ButtonLabel` 等；找不到按钮译文时保留原 `ButtonLabel`，未提供原按钮文字时沿用当前动作标题。建议原文用英文；已提供英文原文的字段无需再重复写英文译文。

[BepInEx.ConfigurationManager v19.1](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/tag/v19.1) 通过 XUnity.AutoTranslator 支持翻译。本功能只借鉴其纯文本映射写法，使用自己的目录和稳定字段键，不依赖 XUnity.AutoTranslator，也不直接读取其翻译文件。
