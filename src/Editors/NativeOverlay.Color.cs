using System;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed partial class NativeOverlay
    {
        internal void PickColor(Func<string> title, Func<Color> current, Action<Color> preview, Func<Color, string> commit, Action cancelled)
        {
            RectTransform body = Begin(title());
            localizedTitle = title;
            colorPicker = body.gameObject.AddComponent<ColorPicker>();
            colorPicker.Initialize(page, current, preview, commit, cancelled);
            colorClose = NativeUi.CloneButton(page.ButtonTemplate, body, "ClosePicker", Texts.Close, Close, page);
            RectTransform rect = colorClose.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.68f, .19f); rect.anchorMax = new Vector2(.96f, .28f);
            rect.offsetMin = rect.offsetMax = Vector2.zero; colorClose.SetEventSystem(page.Domain);
            Selectable[] controls = { colorPicker.Plane, colorPicker.Hue, colorPicker.Alpha,
                colorPicker.Hex, body.Find("ExactAlpha").GetComponent<NativeTextInput>(), colorClose };
            for (int i = 0; i < controls.Length; i++)
            {
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = controls[(i + controls.Length - 1) % controls.Length];
                nav.selectOnDown = controls[(i + 1) % controls.Length];
                if (!(controls[i] is Slider))
                { nav.selectOnLeft = nav.selectOnUp; nav.selectOnRight = nav.selectOnDown; }
                controls[i].navigation = nav;
            }
            if (page.Domain != null) page.Domain.SetSelectedGameObject(colorPicker.Plane.gameObject);
        }
    }
}
