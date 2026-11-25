using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BepInEx.Configuration;
using HarmonyLib;
using OC2ModSettings;
using OC2ModSettings.SDK;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private static Dictionary<string, object> ActionDescriptor(string name, Action execute)
    { return new Dictionary<string, object> { { "Version", 1 }, { "DisplayName", name }, { "Execute", execute }, { "Category", "Actions" } }; }
    private IEnumerator VerifyActions(NativePage page)
    {
        int calls = 0, attachedCalls = 0, thread = Thread.CurrentThread.ManagedThreadId;
        bool enabled = false, onMain = false;
        ModSettingsClient client = new ModSettingsClient();
        Check(client.Connect() && client.SupportsActions, "SDK finds the elected service and its existing v1 action method");
        Dictionary<string, object> descriptor = ActionDescriptor("进入视角编辑模式", delegate { calls++; onMain = Thread.CurrentThread.ManagedThreadId == thread; });
        descriptor["ButtonLabel"] = "进入视角\n编辑模式";
        descriptor["CanExecute"] = new Func<bool>(delegate { return enabled; });
        IDisposable standalone = client.Action("fixture.00", "update", descriptor);
        ConfigEntryBase nameEntry = fixture["Types", "string"];
        IDisposable title = client.Configure(nameEntry, new Dictionary<string, object> { { "DispName", "玩家名称" } });
        ModSettingsAction attachedDescriptor = new ModSettingsAction("改名网页", delegate { attachedCalls++; }) {
            Entry = nameEntry, ButtonLabel = "打开\n网页"
        };
        IDisposable attached = client.RegisterAction("fixture.00", "rename", attachedDescriptor);
        descriptor["DisplayName"] = "mutated caller dictionary";
        attachedDescriptor.ButtonLabel = "访问网页";
        yield return new WaitForSecondsRealtime(.35f);
        Transform row = page.Right.Content.Find("Setting_@Action_update");
        T17Button action = row.Find("Action_update").GetComponent<T17Button>();
        T17Text standaloneTitle = row.Find("Name/Text").GetComponent<T17Text>();
        T17Text standaloneCaption = action.GetComponentInChildren<T17Text>();
        Check(standaloneTitle.text == "进入视角编辑模式" && standaloneCaption.text == "进入视角\n编辑模式"
            && !action.interactable && row.Find("Default") == null,
            "raw v1 camera-style action keeps its title separate from its caption and starts disabled without a default button");
        enabled = true; yield return new WaitForSecondsRealtime(.3f);
        Check(action.interactable, "CanExecute changes refresh on main thread without a config notification");
        page.Right.Reveal(row as RectTransform); yield return null; yield return null;
        CheckRaycast(action, page.Domain, "standalone business button receives real canvas raycasts");
        MeasureActionLabel(standaloneTitle, 1, "standalone title");
        MeasureActionLabel(standaloneCaption, 2, "standalone caption");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "standalone-business-action.png")); yield return new WaitForEndOfFrame(); yield return null;
        Submit(action.transform, page);
        Check(calls == 1 && onMain && page.gameObject.activeInHierarchy, "standalone action invokes existing delegate on main thread");
        RectTransform textRow = page.Right.Content.Find("Setting_Types_string") as RectTransform;
        NativeTextInput input = textRow.Find("ExactValue").GetComponent<NativeTextInput>();
        Enter(input, "name with action"); Submit(textRow.Find("Action_rename"), page);
        Check(attachedCalls == 1 && (string)fixture["Types", "string"].BoxedValue == "name with action",
            "attached action retains shared native input and owning ConfigFile commit behavior");
        T17Text configTitle = textRow.Find("Name/Text").GetComponent<T17Text>();
        T17Text attachedCaption = textRow.Find("Action_rename").GetComponentInChildren<T17Text>();
        Check(configTitle.text == "玩家名称" && attachedCaption.text == "打开\n网页",
            "typed attached action snapshots its caption separately from the localized config title");
        Rect field = BoundsIn(input.GetComponent<RectTransform>(), textRow), button = BoundsIn(textRow.Find("Action_rename") as RectTransform, textRow);
        Rect reset = BoundsIn(textRow.Find("Default") as RectTransform, textRow);
        float gap = textRow.rect.width * .74f * .02f;
        Check(field.xMax < button.xMin && Mathf.Abs(button.width - reset.width) < 1
            && Mathf.Abs(button.xMin - field.xMax - gap) < 1 && Mathf.Abs(reset.xMin - button.xMax - gap) < 1,
            "attached button sits after input with shared action width and two-percent gaps");
        Check(input.navigation.selectOnRight == textRow.Find("Action_rename").GetComponent<T17Button>()
            && input.navigation.selectOnRight.navigation.selectOnRight == textRow.Find("Default").GetComponent<T17Button>(),
            "attached action navigation follows input, action and default in their visible order");
        page.Right.Reveal(textRow); yield return null; yield return null;
        CheckRaycast(textRow.Find("Action_rename").GetComponent<T17Button>(), page.Domain, "attached business button receives real canvas raycasts");
        MeasureActionLabel(configTitle, 1, "config title");
        MeasureActionLabel(attachedCaption, 2, "attached caption");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "attached-business-action.png")); yield return new WaitForEndOfFrame(); yield return null;
        File.AppendAllText(Path.Combine(output, "action-geometry.txt"), "row=" + textRow.rect + " input=" + field + " action=" + button + " default=" + reset + "\n");
        NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
        Enter(search, "改名网页"); yield return null;
        Check(page.Right.Content.Find("Setting_Types_string") != null && page.Right.Content.Find("Setting_@Action_update") == null,
            "attached action name is searchable without replacing the setting row");
        Enter(search, "玩家名称"); yield return null;
        Check(page.Right.Content.Find("Setting_Types_string") != null, "localized config title is independently searchable");
        Enter(search, ""); yield return null;
        byte[] before = File.ReadAllBytes(fixture.ConfigFilePath);
        IDisposable nextAttached = client.RegisterAction("fixture.00", "rename", attachedDescriptor);
        attached.Dispose(); attached = nextAttached;
        yield return new WaitForSecondsRealtime(.35f);
        textRow = page.Right.Content.Find("Setting_Types_string") as RectTransform;
        Check(textRow.Find("Name/Text").GetComponent<T17Text>().text == "玩家名称"
            && textRow.Find("Action_rename").GetComponentInChildren<T17Text>().text == "访问网页",
            "same-ID typed registration updates only the caption and survives disposal of the old lease");
        Check(nameEntry.Definition.Section == "Types" && nameEntry.Definition.Key == "string"
            && Convert.ToBase64String(before) == Convert.ToBase64String(File.ReadAllBytes(fixture.ConfigFilePath)),
            "caption changes do not rename the config key or write its file");
        Submit(page.Right.Content.Find("Setting_@Action_update/Action_update"), page);
        Check(Convert.ToBase64String(before) == Convert.ToBase64String(File.ReadAllBytes(fixture.ConfigFilePath)), "executing business action does not save configuration");
        int beforeDefaults = calls;
        AccessTools.Method(typeof(NativePage), "RestoreDefaults").Invoke(page, null);
        Submit(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_0"), page); yield return null;
        Check(calls == beforeDefaults && attachedCalls == 1, "restore defaults never executes standalone or attached callbacks");
        T17Button stale = page.Right.Content.Find("Setting_@Action_update/Action_update").GetComponent<T17Button>();
        IDisposable replacement = client.Action("fixture.00", "update", ActionDescriptor("Replacement", delegate { calls += 10; }));
        standalone.Dispose(); stale.onClick.Invoke();
        Check(calls == beforeDefaults, "stale action cannot execute after same-ID replacement");
        yield return new WaitForSecondsRealtime(.35f);
        row = page.Right.Content.Find("Setting_@Action_update");
        Check(row.Find("Name/Text").GetComponent<T17Text>().text == "Replacement"
            && row.Find("Action_update").GetComponentInChildren<T17Text>().text == "Replacement",
            "old title-only dictionary renders both the title and the button");
        Submit(page.Right.Content.Find("Setting_@Action_update/Action_update"), page);
        Check(calls == beforeDefaults + 10, "stale lease release preserves successor action");
        replacement.Dispose(); replacement.Dispose(); yield return new WaitForSecondsRealtime(.35f);
        Check(page.Right.Content.Find("Setting_@Action_update") == null, "current action lease release removes standalone row idempotently");
        List<IDisposable> extras = new List<IDisposable>();
        for (int i = 0; i < 5; i++)
        {
            Dictionary<string, object> extra = ActionDescriptor("附加操作 " + i, delegate { }); extra["Entry"] = fixture["Types", "string"];
            extras.Add(ModSettingsPlugin.Instance.RegisterActionV1("fixture.00", "extra" + i, extra));
        }
        textRow = page.Right.Content.Find("Setting_Types_string") as RectTransform;
        T17Text measure = textRow.Find("Name").GetComponentInChildren<T17Text>();
        string longTitle = "";
        while (measure.cachedTextGeneratorForLayout.GetPreferredWidth(longTitle, measure.GetGenerationSettings(Vector2.zero)) / measure.pixelsPerUnit < textRow.rect.width * .7f)
            longTitle += "较长配置名称";
        IDisposable longName = ModSettingsPlugin.Instance.ConfigureEntryV1(fixture["Types", "string"], new Dictionary<string, object> { { "DispName", longTitle } });
        yield return new WaitForSecondsRealtime(.35f);
        textRow = page.Right.Content.Find("Setting_Types_string") as RectTransform;
        Check(textRow.rect.height > 68 && textRow.Find("ExactValue") != null, "multiple attached actions wrap into additional row space");
        Check(Mathf.Abs((textRow.Find("Name") as RectTransform).rect.width / textRow.rect.width - .49f) < .001f,
            "attached actions preserve the maximum 49-percent long-name tier");
        foreach (T17Button candidate in textRow.GetComponentsInChildren<T17Button>())
        {
            if (!candidate.name.StartsWith("Action_")) continue;
            Rect bounds = BoundsIn(candidate.GetComponent<RectTransform>(), textRow);
            Check(bounds.width > 100 && bounds.xMin >= textRow.rect.xMin && bounds.xMax <= textRow.rect.xMax + 1
                && bounds.yMin >= textRow.rect.yMin - 1, "wrapped attached action retains usable in-row bounds: " + candidate.name);
        }
        longName.Dispose(); title.Dispose(); foreach (IDisposable extra in extras) extra.Dispose(); attached.Dispose();
        IDictionary<string, object> customHost = null;
        IDisposable renderer = ModSettingsPlugin.Instance.RegisterRendererV1(fixture["Types", "boolean"], new Dictionary<string, object> {
            { "Version", 1 }, { "Create", new Func<RectTransform, ConfigEntryBase, IDictionary<string, object>, IDisposable>(
                delegate(RectTransform parent, ConfigEntryBase entry, IDictionary<string, object> host) {
                    customHost = host; ((Action<float>)host["SetHeight"])(160); return new ReleaseAction(delegate {});
                }) }
        });
        Dictionary<string, object> customAction = ActionDescriptor("Custom action", delegate {}); customAction["Entry"] = fixture["Types", "boolean"];
        IDisposable customLease = ModSettingsPlugin.Instance.RegisterActionV1("fixture.00", "custom", customAction);
        yield return new WaitForSecondsRealtime(.35f);
        RectTransform customRow = page.Right.Content.Find("Setting_Types_boolean") as RectTransform;
        Rect editorBounds = BoundsIn(customRow.Find("CustomEditor") as RectTransform, customRow);
        Rect customButton = BoundsIn(customRow.Find("Action_custom") as RectTransform, customRow);
        Check(Mathf.Abs(customRow.rect.height - 214) < 1 && customButton.yMax < editorBounds.yMin,
            "custom editor initial height preserves a separate continuation for attached actions");
        ((Action<float>)customHost["SetHeight"])(205); yield return null;
        editorBounds = BoundsIn(customRow.Find("CustomEditor") as RectTransform, customRow);
        customButton = BoundsIn(customRow.Find("Action_custom") as RectTransform, customRow);
        Check(Mathf.Abs(customRow.rect.height - 259) < 1 && customButton.yMax < editorBounds.yMin,
            "custom editor dynamic height keeps attached action outside its owned content");
        customLease.Dispose(); renderer.Dispose();
        bool releasedBeforeCallback = false;
        T17EventSystem originalDomain = page.Domain;
        T17DialogBox businessDialog = null;
        Dictionary<string, object> closeDescriptor = ActionDescriptor("Open existing dialog", delegate {
            releasedBeforeCallback = !page.gameObject.activeSelf && !originalDomain.IsDisabled()
                && !(bool)AccessTools.Field(typeof(NativePage), "ownedState").GetValue(page);
            businessDialog = T17DialogBoxManager.GetDialog(false);
            businessDialog.Initialize("Text.Menu.QuitTitle", "Text.Menu.QuitBody", "Text.Button.Quit", "Text.Button.Cancel", "",
                T17DialogBox.Symbols.Warning, true, true, false);
            businessDialog.Show();
        }); closeDescriptor["ClosePage"] = true;
        IDisposable close = ModSettingsPlugin.Instance.RegisterActionV1("fixture.only-action", "open", closeDescriptor);
        yield return new WaitForSecondsRealtime(.35f);
        Check(page.Left.Content.Find("Mod_fixture.only-action") != null, "action-only owner joins the same MOD catalog without a fake ConfigEntry");
        Submit(page.Left.Content.Find("Mod_fixture.only-action"), page); yield return null;
        Check(page.Right.Content.Find("Setting_@Action_open") != null, "action-only owner renders its independent action row");
        Submit(page.Right.Content.Find("Setting_@Action_open/Action_open"), page);
        Check(releasedBeforeCallback, "ClosePage releases public page and native input before business callback");
        yield return new WaitForSecondsRealtime(.7f);
        Check(businessDialog != null && businessDialog.IsActive && businessDialog.m_ConfirmButton.isActiveAndEnabled,
            "business callback can open the actual native dialog after the public page releases ownership");
        CheckRaycast(businessDialog.m_ConfirmButton, originalDomain, "business native dialog confirm receives real canvas raycast");
        businessDialog.Hide(); yield return null;
        close.Dispose();
        FrontendRootMenu root = T17FrontendFlow.Instance.m_Rootmenu;
        page.Open();
        yield return new WaitForSecondsRealtime(.35f);
        Check(page.Left.Content.Find("Mod_fixture.only-action") == null, "releasing last action removes action-only owner");
        Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null;
        Logger.LogInfo("Expected fault injection: fixture action failure and fixture predicate failure; checking action isolation.");
        Dictionary<string, object> broken = ActionDescriptor("Fault", delegate { throw new InvalidOperationException("fixture action failure"); });
        IDisposable failing = ModSettingsPlugin.Instance.RegisterActionV1("fixture.00", "fault", broken);
        yield return new WaitForSecondsRealtime(.35f);
        Submit(page.Right.Content.Find("Setting_@Action_fault/Action_fault"), page);
        Check(page.gameObject.activeSelf && page.transform.Find("SettingsBody/Explanation/Help").GetComponent<T17Text>().text.Contains("fixture action failure"),
            "callback failure is isolated and shown through existing error UI");
        failing.Dispose(); yield return new WaitForSecondsRealtime(.35f);
        Check(page.Right.Content.Find("Setting_@Action_fault") == null, "failed action can be disposed without leftover controls");
        Dictionary<string, object> brokenPredicate = ActionDescriptor("Predicate fault", delegate { });
        brokenPredicate["CanExecute"] = new Func<bool>(delegate { throw new InvalidOperationException("fixture predicate failure"); });
        IDisposable predicateLease = ModSettingsPlugin.Instance.RegisterActionV1("fixture.00", "predicate", brokenPredicate);
        yield return new WaitForSecondsRealtime(.35f);
        Check(!page.Right.Content.Find("Setting_@Action_predicate/Action_predicate").GetComponent<T17Button>().interactable,
            "CanExecute exception disables only its action instead of breaking the page refresh");
        predicateLease.Dispose(); yield return new WaitForSecondsRealtime(.35f);
        page.Right.Content.anchoredPosition = Vector2.zero;
    }

    private void MeasureActionLabel(T17Text label, int lines, string role)
    {
        Rect rect = label.rectTransform.rect;
        TextGenerator glyphs = label.cachedTextGenerator;
        bool rendered = glyphs.Populate(label.text, label.GetGenerationSettings(rect.size));
        File.AppendAllText(Path.Combine(output, "action-geometry.txt"), role + "\t" + label.text.Replace('\n', '/')
            + "\t" + rect.width + "x" + rect.height + " UI\tlines=" + glyphs.lineCount + "\n");
        Check(label.isActiveAndEnabled && rendered && glyphs.lineCount == lines
            && glyphs.characterCountVisible >= label.text.Replace("\n", "").Length, role + " renders fully on " + lines + " line(s)");
    }
}
