using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;
using UnityEngine;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyCategoryOrder(NativePage page)
    {
        ConfigFile config = new ConfigFile(Path.Combine(output, "category-order.cfg"), false); config.SaveOnConfigSet = false;
        ConfigEntryBase[] entries = { config.Bind("Language", "Language", "zh", "language"), config.Bind("02-MOD", "Setting", true, "mod"),
            config.Bind("03-Name", "PlayerName", "original", "name"), config.Bind("04-Options", "EnableScore", false, "score") };
        IDisposable registration = ModSettingsPlugin.Instance.RegisterConfigV1("fixture.categoryorder", "Category order", "ordering regression", config);
        List<IDisposable> metadata = new List<IDisposable>();
        string[][] captions = { new string[] { "语言", "MOD设置", "改名及消息传递", "常用功能设置" },
            new string[] { "ZZ Language", "AA MOD Settings", "YY Rename", "BB Options" } };
        for (int pass = 0; pass < captions.Length; pass++)
        {
            foreach (IDisposable lease in metadata) lease.Dispose(); metadata.Clear();
            for (int i = 0; i < entries.Length; i++) metadata.Add(ModSettingsPlugin.Instance.ConfigureEntryV1(entries[i],
                new Dictionary<string, object> { { "Category", captions[pass][i] }, { "CategoryOrder", 400 - i * 100 }, { "Order", 20 } }));
            metadata.Add(ModSettingsPlugin.Instance.RegisterActionV1("fixture.categoryorder", "check-update", new Dictionary<string, object> {
                { "Version", 1 }, { "DisplayName", pass == 0 ? "检查更新" : "Check update" }, { "Execute", new Action(delegate {}) },
                { "Category", captions[pass][3] }, { "CategoryOrder", 100 }, { "Order", 19 } }));
            yield return new WaitForSecondsRealtime(.4f); yield return null;
            Submit(page.Left.Content.Find("Mod_fixture.categoryorder").GetComponent<T17Button>(), page);
            yield return new WaitForSecondsRealtime(.4f); yield return null;
            List<string> actual = new List<string>(); int lastSetting = -1, update = -1;
            for (int i = 0; i < page.Right.Content.childCount; i++)
            {
                Transform child = page.Right.Content.GetChild(i);
                if (child.name == "Category") actual.Add(child.GetComponentInChildren<T17Text>().text);
                if (child.name == "Setting_04-Options_EnableScore") lastSetting = i;
                if (child.name == "Setting_@Action_check-update") update = i;
                File.AppendAllText(Path.Combine(output, "category-order.txt"), pass + " " + i + " " + child.name + "\n");
            }
            Check(actual.Count == 4 && actual[0] == captions[pass][0] && actual[1] == captions[pass][1]
                && actual[2] == captions[pass][2] && actual[3] == captions[pass][3], "rendered category rank survives translated captions pass " + pass);
            Check(lastSetting >= 0 && update == lastSetting + 1, "standalone update action immediately follows score option pass " + pass);
            Check(config.Keys.Count == 4 && entries[2].Definition.Section == "03-Name" && entries[2].Definition.Key == "PlayerName",
                "display-only metadata preserves config section and key identity pass " + pass);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "category-order-" + pass + ".png")); yield return new WaitForEndOfFrame(); yield return null;
        }
        foreach (IDisposable lease in metadata) lease.Dispose(); registration.Dispose();
        yield return new WaitForSecondsRealtime(.4f);
        Submit(page.Left.Content.Find("Mod_fixture.00").GetComponent<T17Button>(), page);
        yield return new WaitForSecondsRealtime(.4f);
    }
}
