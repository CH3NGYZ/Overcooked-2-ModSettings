using System.Collections.Generic;
using System.IO;
using System.Text;
using OC2ModSettings;

internal static partial class CoreTests
{
    private static void VerifyTranslationFiles(string root)
    {
        const string owner = "com.fixture.translation";
        string path = Path.Combine(root, "translate");
        List<string> warnings = new List<string>();
        Check(TranslationFiles.Load(path, warnings.Add).Get(owner, "mod", "en") == null && warnings.Count == 0,
            "missing translation directory is optional and does not emit warnings");
        string chinese = Directory.CreateDirectory(Path.Combine(path, "ZH-cn")).FullName;
        string english = Directory.CreateDirectory(Path.Combine(path, "en")).FullName;
        string unsupported = Directory.CreateDirectory(Path.Combine(path, "unrecognized")).FullName;
        string subdirectory = Directory.CreateDirectory(Path.Combine(chinese, "ignored-subdirectory")).FullName;
        string chineseFile = Path.Combine(chinese, owner + ".txt");
        File.WriteAllText(chineseFile, "// title\n# more\nmod = 测试模组\ngroup/Main=主选项\n"
            + "config/Main/Items\\=Max = 最多=四个\naction/apply/label=应\\u7528\\n保留\\\\文件\\r\\n新行\n"
            + "description=\nSearch:=查找：\nmod=新测试模组\nliteral=left\\qright # inline stays\n"
            + "r:\"^foo$\"=unwanted\nsr:\"^bar$\"=unwanted\n"
            + "invalid line\n = discarded\ninvalidUnicode=x\\u1OO0\nafter=still parsed\n", new UTF8Encoding(true));
        File.WriteAllText(Path.Combine(english, owner + ".TXT"), "mod=English name\nCase=Distinct key\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(unsupported, owner + ".txt"), "mod=Ignore this language\n");
        File.WriteAllText(Path.Combine(subdirectory, owner + ".txt"), "mod=Ignore nesting\n");
        File.WriteAllText(Path.Combine(chinese, owner + ".ini"), "mod=Ignore other extensions\n");
        File.WriteAllBytes(Path.Combine(chinese, "invalid.utf8.txt"), new byte[] { 0x6d, 0x6f, 0x64, 0x3d, 0xff, 0xfe });
        File.WriteAllText(Path.Combine(chinese, "invalid.utf16.txt"), "mod=Do not interpret UTF-16 as UTF-8", Encoding.Unicode);
        TranslationFiles translations = TranslationFiles.Load(path, warnings.Add);

        Check(translations.Get(owner, "mod", "zh-CN") == "新测试模组" && translations.Get(owner, "group/Main", "ZH-CN") == "主选项",
            "UTF-8 BOM file loads in a case-insensitive supported locale; last duplicate wins");
        Check(translations.Get(owner, "config/Main/Items=Max", "zh-CN") == "最多=四个",
            "first unescaped equals is the only key-value delimiter");
        Check(translations.Get(owner, "action/apply/label", "zh-CN") == "应用\n保留\\文件\r\n新行",
            "Unicode, newlines, carriage returns and literal backslashes decode in a caption");
        Check(translations.Get(owner, "description", "zh-CN") == "" && translations.Get(owner, "missing", "zh-CN") == null
            && translations.Get(owner, "invalidUnicode", "zh-CN") == null && translations.Get(owner, "after", "zh-CN") == "still parsed",
            "empty translations are distinct from missing and malformed rows do not stop later lines");
        Check(translations.Get(owner, "Search:", "zh-CN") == "查找：" && translations.Get(owner, "literal", "zh-CN") == "left\\qright # inline stays"
            && translations.Get(owner, "r:\"^foo$\"", "zh-CN") == null && translations.Get(owner, "sr:\"^bar$\"", "zh-CN") == null,
            "literal text-map keys work, unknown escapes stay literal and regex directives are not imported");
        Check(translations.Get(owner, "mod", "EN") == "English name" && translations.Get(owner, "Case", "en") == "Distinct key"
            && translations.Get(owner, "case", "en") == null && translations.Get(owner.ToUpperInvariant(), "mod", "en") == null,
            "valid languages load together while owner GUIDs and literal translation keys are ordinal");
        Check(translations.Get(owner, "mod", "fr") == null && translations.Get(owner, "mod", "unrecognized") == null
            && translations.Get(owner, "mod", null) == null && translations.Get(null, "mod", "en") == null,
            "missing and unsupported languages do not invent a fallback; null lookup arguments are safe");
        Check(translations.Get("invalid.utf8", "mod", "zh-CN") == null && translations.Get("invalid.utf16", "mod", "zh-CN") == null
            && warnings.Count == 5, "malformed lines and invalidly encoded files are isolated and logged");
        File.WriteAllText(chineseFile, "mod=文件已编辑\n");
        Check(translations.Get(owner, "mod", "zh-CN") == "新测试模组"
            && TranslationFiles.Load(path, null).Get(owner, "mod", "zh-CN") == "文件已编辑",
            "ordinary lookup uses the immutable in-process cache, while explicit load reads changes");
    }
}
