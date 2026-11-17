using System;
using System.Collections;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private ConfigFile fixture;
    private void RegisterFixtures(int count)
    {
        for (int i = 0; i < count; i++)
        {
            ConfigFile config = new ConfigFile(Path.Combine(output, "fixture-" + i + ".cfg"), false);
            config.SaveOnConfigSet = false;
            if (i == 0)
            {
                fixture = config;
                config.Bind("Types", "boolean", false, "Boolean"); config.Bind("Types", "string", "original", "Text");
                config.Bind("Types", "sbyte", sbyte.MinValue, "Signed byte"); config.Bind("Types", "byte", byte.MaxValue, "Byte");
                config.Bind("Types", "short", short.MinValue, "Short"); config.Bind("Types", "ushort", ushort.MaxValue, "Unsigned short");
                config.Bind("Types", "int", int.MinValue, "Integer"); config.Bind("Types", "uint", uint.MaxValue, "Unsigned integer");
                config.Bind("Types", "long", long.MaxValue, "Long"); config.Bind("Types", "ulong", ulong.MaxValue, "Unsigned long");
                config.Bind("Types", "float", .25f, "Float"); config.Bind("Types", "double", .125, "Double");
                config.Bind("Types", "decimal", decimal.MaxValue, "Exact decimal");
                config.Bind("Types", "range", .3f, new ConfigDescription("Preview before release", new AcceptableValueRange<float>(0, 1), new object[0]));
                config.Bind("Types", "choice", "A", new ConfigDescription("Candidates", new AcceptableValueList<string>("A", "B", "C"), new object[0]));
                config.Bind("Types", "enum", KeyCode.Space, "Key capture");
                config.Bind("Types", "ordinaryEnum", SampleChoice.First, "Ordinary enum");
                config.Bind("Types", "flags", SampleFlags.A, "Flags");
                config.Bind("Types", "shortcut", new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl), "Shortcut");
                config.Bind("Types", "color", new Color(.1f, .2f, .3f, 1), "Color");
                config.Bind("Types", "vector2", new Vector2(1, 2), "Vector2"); config.Bind("Types", "vector3", new Vector3(1, 2, 3), "Vector3");
                config.Bind("Types", "vector4", new Vector4(1, 2, 3, 4), "Vector4");
                config.Bind("Types", "quaternion", Quaternion.identity, "Quaternion"); config.Bind("Types", "rect", new Rect(1, 2, 3, 4), "Rect");
            }
            else config.Bind("General", "Enabled", true, "Fixture setting");
            leases.Add(ModSettingsPlugin.Instance.RegisterConfigV1("fixture." + i.ToString("D2"),
                i < 2 ? "00 Native configuration / 原生配置设置" : "Fixture " + i.ToString("D2") + " Long MOD name / 长名称用于两行显示验证", "Fixture " + i, config));
        }
    }
    [Flags] private enum SampleFlags { None = 0, A = 1, B = 2, Both = 3 }
    private enum SampleChoice { First, Second }
    private IEnumerator VerifyEditing(NativePage page)
    {
        Transform row = page.Right.Content.Find("Setting_Types_boolean");
        T17Button toggle = row.Find("Toggle").GetComponent<T17Button>();
        ExecuteEvents.Execute(toggle.gameObject, new BaseEventData(page.Domain), ExecuteEvents.submitHandler);
        yield return null;
        Check((bool)fixture["Types", "boolean"].BoxedValue, "native toggle commits owning ConfigFile");
        Check(File.Exists(fixture.ConfigFilePath) && !fixture.SaveOnConfigSet, "explicit save preserves autosave false");
        fixture["Types", "boolean"].BoxedValue = false;
        yield return new WaitForSecondsRealtime(.3f);
        Check(toggle.GetComponentInChildren<T17Text>().text == Texts.Boolean(false), "external change refreshes localized OFF without reverse write");
        Transform exactRow = page.Right.Content.Find("Setting_Types_decimal");
        NativeTextInput exact = exactRow.GetComponentInChildren<NativeTextInput>();
        Check(exact.Value == decimal.MaxValue.ToString(), "decimal retains full exact digits in UI");
        Transform sliderRow = page.Right.Content.Find("Setting_Types_range"); PreviewSlider slider = sliderRow.GetComponentInChildren<PreviewSlider>();
        Check(slider != null, "range has native preview slider"); slider.value = .8f;
        Check(Mathf.Approximately((float)fixture["Types", "range"].BoxedValue, .3f), "slider move does not commit");
        slider.Cancel(); Check(Mathf.Approximately((float)fixture["Types", "range"].BoxedValue, .3f), "slider cancellation leaves config unchanged");
        slider.value = .7f; slider.Commit(); Check(Mathf.Abs((float)fixture["Types", "range"].BoxedValue - .7f) < .001f, "slider explicit confirmation commits");
        Transform dropdown = page.Right.Content.Find("Setting_Types_choice").Find("Dropdown");
        ExecuteEvents.Execute(dropdown.gameObject, new BaseEventData(page.Domain), ExecuteEvents.submitHandler);
        yield return null;
        Check(page.Overlay.IsOpen, "candidate list opens page-owned overlay");
        page.Cancel(); yield return null; Check(!page.Overlay.IsOpen && page.gameObject.activeSelf, "first cancel closes overlay only");
        Transform key = page.Right.Content.Find("Setting_Types_shortcut").Find("Capture");
        ExecuteEvents.Execute(key.gameObject, new BaseEventData(page.Domain), ExecuteEvents.submitHandler);
        yield return null; Check(page.Domain.IsDisabled(), "key capture suppresses underlying input");
        page.Overlay.Close(); Check(!page.Domain.IsDisabled(), "key capture close releases input suppression");
        Check(page.Right.Content.Find("Setting_Types_color").Find("ColorPreview") != null, "compact hex color preview exists");
        Check(page.Right.Content.Find("Setting_Types_vector3").GetComponentsInChildren<NativeTextInput>().Length == 3, "vector3 has three exact components");
    }
}
