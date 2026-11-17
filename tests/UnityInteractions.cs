using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private void Submit(Component control, NativePage page)
    { ExecuteEvents.Execute(control.gameObject, new BaseEventData(page.Domain), ExecuteEvents.submitHandler); }
    private IEnumerator VerifyInteractions(NativePage page)
    {
        T17Button first = page.Left.Content.Find("Mod_fixture.00").GetComponent<T17Button>();
        T17Button second = page.Left.Content.Find("Mod_fixture.01").GetComponent<T17Button>();
        page.Domain.SetSelectedGameObject(second.gameObject); yield return null; yield return null;
        ExecuteEvents.Execute(second.gameObject, new PointerEventData(page.Domain), ExecuteEvents.pointerEnterHandler);
        Check(page.Right.Content.Find("Setting_Types_boolean") != null, "focus and hover do not activate another MOD");
        Submit(second, page); yield return null; yield return null;
        Check(page.Right.Content.Find("Setting_General_Enabled") != null, "explicit confirmation changes active MOD");
        Submit(first, page); yield return null; yield return null; yield return null;
        NativeTextInput input = page.Right.Content.Find("Setting_Types_string").GetComponentInChildren<NativeTextInput>();
        input.Begin(); input.ChangeText("saved on change");
        Check((string)fixture["Types", "string"].BoxedValue == "saved on change" && input.IsEditing,
            "string input saves immediately while editing without confirmation");
        Submit(second, page); yield return null; yield return null;
        Check((string)fixture["Types", "string"].BoxedValue == "saved on change", "switch retains already saved text without another blur commit");
        Submit(first, page); yield return null; yield return null; yield return null;
        input = page.Right.Content.Find("Setting_Types_string").GetComponentInChildren<NativeTextInput>();
        input.Begin(); input.ChangeText("confirmed"); input.Confirm();
        Check((string)fixture["Types", "string"].BoxedValue == "confirmed", "confirmed text commits real ConfigEntry");
        NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>(); search.Begin();
        search.EditKey(KeyCode.A, true, false); search.TypeText("__no_results__");
        yield return null; yield return null; yield return null;
        Check(page.Right.Content.Find("Empty") != null, "current MOD search renders empty state");
        Submit(second, page); yield return null; yield return null; yield return null;
        Check(page.Right.Content.Find("Setting_General_Enabled") != null, "search does not filter another MOD");
        Submit(first, page); yield return null; yield return null; yield return null;
        Transform vector = page.Right.Content.Find("Setting_Types_vector3");
        NativeTextInput[] axes = vector.GetComponentsInChildren<NativeTextInput>();
        Check(axes[0].navigation.selectOnRight == axes[1] && axes[1].navigation.selectOnLeft == axes[0], "compound editor preserves horizontal child navigation");
        Check(axes[0].GetComponent<RowFocus>().Row == vector, "compound children reveal their owning row");
        string rememberedChild = axes[1].name;
        page.Right.Content.anchoredPosition = new Vector2(0, 700);
        page.Domain.SetSelectedGameObject(axes[1].gameObject); yield return null; yield return null;
        float remembered = page.Right.Content.anchoredPosition.y;
        Submit(second, page); yield return null; yield return null; yield return null;
        Submit(first, page); yield return null; yield return null; yield return null; yield return null;
        Check(Mathf.Abs(page.Right.Content.anchoredPosition.y - remembered) < 1, "per-MOD right scroll restored");
        Selectable returnRight = first.GetComponent<ModPin>().Button.navigation.selectOnRight;
        Check(returnRight.transform.IsChildOf(page.Right.Content.Find("Setting_Types_vector3")), "return-right targets remembered row");
        Check(returnRight.name == rememberedChild, "return-right preserves compound child focus");
        float leftBefore = page.Left.Content.anchoredPosition.y;
        page.Right.Scroll.OnScroll(new PointerEventData(page.Domain) { scrollDelta = new Vector2(0, -1) });
        Check(Mathf.Approximately(leftBefore, page.Left.Content.anchoredPosition.y), "right wheel leaves left scroll unchanged");
        int created = 0, disposed = 0, refreshed = 0;
        ConfigEntryBase customEntry = fixture["Types", "boolean"];
        Dictionary<string, object> protocol = new Dictionary<string, object>(); protocol["Version"] = 1;
        protocol["Create"] = new Func<RectTransform, ConfigEntryBase, IDictionary<string, object>, IDisposable>(delegate(RectTransform parent, ConfigEntryBase entry, IDictionary<string, object> host)
        {
            created++; ((Action<float>)host["SetHeight"])(140);
            ((Action<IDictionary<string, object>>)host["SetCallbacks"])(new Dictionary<string, object> {
                { "Refresh", new Action(delegate { refreshed++; }) }, { "Cancel", new Action(delegate {}) },
                { "HasPending", new Func<bool>(delegate { return false; }) }, { "Commit", new Func<bool>(delegate { return true; }) } });
            return new ReleaseAction(delegate { disposed++; });
        });
        IDisposable old = ModSettingsPlugin.Instance.RegisterRendererV1(customEntry, protocol);
        yield return new WaitForSecondsRealtime(.35f); yield return null; yield return null;
        Check(created == 1 && refreshed > 0, "renderer registration rebuilds and refreshes per-row instance");
        IDisposable replacement = ModSettingsPlugin.Instance.RegisterRendererV1(customEntry, protocol); old.Dispose();
        yield return new WaitForSecondsRealtime(.35f); yield return null; yield return null;
        Check(created == 2 && disposed == 1, "replacement disposes old editor and stale lease preserves successor");
        replacement.Dispose(); yield return new WaitForSecondsRealtime(.35f); yield return null; yield return null;
        Check(disposed == 2 && page.Right.Content.Find("Setting_Types_boolean/Toggle") != null, "release disposes renderer and restores default discovery editor");
        IDisposable hide = ModSettingsPlugin.Instance.ConfigureEntryV1(customEntry, new Dictionary<string, object> { { "Browsable", false } });
        yield return new WaitForSecondsRealtime(.35f);
        Check(page.Right.Content.Find("Setting_Types_boolean") == null, "metadata hide refresh removes existing row"); hide.Dispose();
        yield return new WaitForSecondsRealtime(.35f);
        Check(page.Right.Content.Find("Setting_Types_boolean") != null, "metadata lease release restores row");
        Check(page.Right.Content.Find("Setting_Types_shortcut") != null && page.Right.Content.Find("Setting_Types_flags") != null, "shortcut and flags editors coexist");
    }
}
