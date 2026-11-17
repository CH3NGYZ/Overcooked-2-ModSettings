using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class ModSettingsUnityTests
{
    private void Enter(NativeTextInput input, string value)
    {
        input.Begin(); input.ChangeText(value);
        input.Confirm();
    }
    private IEnumerator VerifyTypeEditors(NativePage page)
    {
        string[] keys = { "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal" };
        string[] values = { "127", "0", "32767", "0", "2147483647", "0", "-9223372036854775808", "18446744073709551615", "0.75", "0.123456789012345", "-79228162514264337593543950335" };
        for (int i = 0; i < keys.Length; i++)
        {
            NativeTextInput input = page.Right.Content.Find("Setting_Types_" + keys[i]).GetComponentInChildren<NativeTextInput>();
            Enter(input, values[i]);
            Check(Convert.ToString(fixture["Types", keys[i]].BoxedValue, CultureInfo.InvariantCulture) == values[i], "exact UI commit " + keys[i]);
        }
        NativeTextInput integer = page.Right.Content.Find("Setting_Types_int").GetComponentInChildren<NativeTextInput>();
        Enter(integer, "invalid"); Check((int)fixture["Types", "int"].BoxedValue == int.MaxValue, "invalid numeric text preserves actual value");
        Enter(integer, "2147483648"); Check((int)fixture["Types", "int"].BoxedValue == int.MaxValue, "numeric overflow rejected");
        Enter(page.Right.Content.Find("Setting_Types_range").GetComponentInChildren<NativeTextInput>(), "2");
        Check(Mathf.Abs((float)fixture["Types", "range"].BoxedValue - .7f) < .001f, "range text cannot bypass acceptable limits");
        foreach (string key in new string[] { "vector2", "vector3", "vector4", "quaternion", "rect" })
        {
            Transform row = page.Right.Content.Find("Setting_Types_" + key);
            Enter(row.Find("X").GetComponent<NativeTextInput>(), ".5");
            Check(Mathf.Abs(ComponentEditor.Read(fixture["Types", key].BoxedValue)[0] - .5f) < .0001f, "component editor commits " + key);
        }
        Enter(page.Right.Content.Find("Setting_Types_color/Hex").GetComponent<NativeTextInput>(), "#FF008080");
        Color color = (Color)fixture["Types", "color"].BoxedValue;
        Check(color.r == 1 && color.g == 0 && Mathf.Abs(color.a - 128f / 255) < .001f, "RGBA hex commits actual color");
        Submit(page.Right.Content.Find("Setting_Types_choice/Dropdown"), page); yield return null;
        Submit(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_1"), page); yield return null;
        Check((string)fixture["Types", "choice"].BoxedValue == "B", "candidate overlay choice commits selected value");
        Submit(page.Right.Content.Find("Setting_Types_ordinaryEnum/Dropdown"), page); yield return null;
        Submit(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_1"), page); yield return null;
        Check((SampleChoice)fixture["Types", "ordinaryEnum"].BoxedValue == SampleChoice.Second, "ordinary enum dropdown commits selected member");
        Submit(page.Right.Content.Find("Setting_Types_flags/Dropdown"), page); yield return null;
        Submit(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_2"), page); yield return null;
        Check((SampleFlags)fixture["Types", "flags"].BoxedValue == SampleFlags.Both && page.Overlay.IsOpen, "flags overlay toggles bit and stays open");
        page.Overlay.Close();
        ConfigFile empty = new ConfigFile(Path.Combine(output, "empty.cfg"), false);
        IDisposable emptyLease = ModSettingsPlugin.Instance.RegisterConfigV1("fixture.empty", "Empty", "", empty);
        yield return new WaitForSecondsRealtime(.3f);
        Check(page.Left.Content.Find("Mod_fixture.empty") == null, "empty config owner omitted"); emptyLease.Dispose();
        ConfigEntryBase boolean = fixture["Types", "boolean"];
        IDisposable advanced = ModSettingsPlugin.Instance.ConfigureEntryV1(boolean, new Dictionary<string, object> { { "IsAdvanced", true } });
        yield return new WaitForSecondsRealtime(.3f);
        Check(page.Right.Content.Find("Setting_Types_boolean") == null, "advanced setting hidden by default");
        PageMemory.Advanced = true; ModSettingsPlugin.Instance.RefreshV1(); yield return new WaitForSecondsRealtime(.3f);
        Check(page.Right.Content.Find("Setting_Types_boolean") != null, "advanced switch exposes setting");
        PageMemory.Advanced = false; advanced.Dispose(); yield return new WaitForSecondsRealtime(.3f);
        IDisposable percentage = ModSettingsPlugin.Instance.ConfigureEntryV1(fixture["Types", "range"], new Dictionary<string, object> { { "ShowRangeAsPercent", true } });
        yield return new WaitForSecondsRealtime(.3f);
        Transform percentRow = page.Right.Content.Find("Setting_Types_range");
        PreviewSlider percentSlider = percentRow.GetComponentInChildren<PreviewSlider>(); percentSlider.value = .45f;
        Check(percentRow.Find("Percent").GetComponentInChildren<T17Text>().text == "45.0%"
            && !percentRow.GetComponentInChildren<NativeTextInput>().Value.Contains("%"), "percent preview preserves parseable exact input");
        percentSlider.Cancel(); percentage.Dispose(); yield return new WaitForSecondsRealtime(.3f);
        foreach (string key in new string[] { "shortcut" })
        {
            object before = fixture["Types", key].BoxedValue;
            Submit(page.Right.Content.Find("Setting_Types_" + key + "/Capture"), page); yield return null;
            page.Cancel(); yield return null;
            Check(Equals(before, fixture["Types", key].BoxedValue) && !page.Domain.IsDisabled(), "capture cancel preserves value and releases " + key);
        }
        page.Right.Content.anchoredPosition = Vector2.zero;
        yield return null;
    }
}
