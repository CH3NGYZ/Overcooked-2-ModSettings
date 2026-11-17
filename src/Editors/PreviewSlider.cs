using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed class PreviewSlider : T17Slider, ISubmitHandler
    {
        internal bool Pending;
        internal Action Confirmed;
        internal Action Cancelled;
        public override void OnPointerUp(PointerEventData data) { base.OnPointerUp(data); Commit(); }
        public void OnSubmit(BaseEventData data) { Commit(); data.Use(); }
        public override void OnDeselect(BaseEventData data) { Cancel(); base.OnDeselect(data); }
        internal void Commit() { if (!Pending) return; Pending = false; if (Confirmed != null) Confirmed(); }
        internal void Cancel() { if (!Pending) return; Pending = false; if (Cancelled != null) Cancelled(); }
        internal static PreviewSlider Create(NativePage page, Transform parent, Action<float> preview)
        {
            RectTransform root = NativeUi.Rect(parent, "Range", Vector2.zero, Vector2.one);
            Image background = NativeUi.Backer(root, new Color(.35f, .42f, .47f, .7f));
            RectTransform handle = NativeUi.Rect(root, "Handle", Vector2.zero, Vector2.one);
            handle.sizeDelta = new Vector2(14, 0);
            Image knob = NativeUi.Backer(handle, new Color(.9f, .94f, .98f));
            PreviewSlider slider = root.gameObject.AddComponent<PreviewSlider>(); slider.targetGraphic = knob;
            slider.handleRect = handle; slider.minValue = 0; slider.maxValue = 1; slider.SetEventSystem(page.Domain);
            SettingsControlFeedback.Attach(slider, background, page, knob);
            slider.onValueChanged.AddListener(delegate(float v) { slider.Pending = true; preview(v); });
            background.raycastTarget = true; return slider;
        }
    }
}
