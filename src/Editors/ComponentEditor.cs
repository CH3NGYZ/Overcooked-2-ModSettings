using System;
using System.Globalization;
using UnityEngine;

namespace OC2ModSettings
{
    internal static class ComponentEditor
    {
        internal static bool Supports(Type t)
        { return t == typeof(Color) || t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) || t == typeof(Quaternion) || t == typeof(Rect); }
        internal static string[] Names(Type t)
        {
            if (t == typeof(Color)) return new string[] { "R", "G", "B", "A" };
            if (t == typeof(Rect)) return new string[] { "X", "Y", "W", "H" };
            if (t == typeof(Vector2)) return new string[] { "X", "Y" };
            if (t == typeof(Vector3)) return new string[] { "X", "Y", "Z" };
            return new string[] { "X", "Y", "Z", "W" };
        }
        internal static float[] Read(object value)
        {
            if (value is Color) { Color v = (Color)value; return new float[] { v.r, v.g, v.b, v.a }; }
            if (value is Vector2) { Vector2 v = (Vector2)value; return new float[] { v.x, v.y }; }
            if (value is Vector3) { Vector3 v = (Vector3)value; return new float[] { v.x, v.y, v.z }; }
            if (value is Vector4) { Vector4 v = (Vector4)value; return new float[] { v.x, v.y, v.z, v.w }; }
            if (value is Quaternion) { Quaternion v = (Quaternion)value; return new float[] { v.x, v.y, v.z, v.w }; }
            Rect r = (Rect)value; return new float[] { r.x, r.y, r.width, r.height };
        }
        internal static object Create(Type t, float[] v)
        {
            if (t == typeof(Color)) return new Color(v[0], v[1], v[2], v[3]);
            if (t == typeof(Vector2)) return new Vector2(v[0], v[1]);
            if (t == typeof(Vector3)) return new Vector3(v[0], v[1], v[2]);
            if (t == typeof(Vector4)) return new Vector4(v[0], v[1], v[2], v[3]);
            if (t == typeof(Quaternion)) return new Quaternion(v[0], v[1], v[2], v[3]);
            return new Rect(v[0], v[1], v[2], v[3]);
        }
    }

    internal sealed partial class EditorRow
    {
        private void BuildComponents(float width)
        {
            Type type = Row.Entry.SettingType;
            if (type == typeof(Color)) { BuildColor(); return; }
            string[] names = ComponentEditor.Names(type);
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                NativeTextInput input = NativeTextInput.Create(page, Root, names[i], "", names[i]);
                input.Changed = delegate(string raw)
                {
                    float value;
                    if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || float.IsInfinity(value) || float.IsNaN(value))
                    { return false; }
                    float[] current = ComponentEditor.Read(Row.Entry.BoxedValue); current[index] = value;
                    input.ValidationError = Commit(ComponentEditor.Create(type, current)); return input.ValidationError == null;
                };
                RectTransform rect = input.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(i * .8f / names.Length, .12f); rect.anchorMax = new Vector2((i + 1) * .8f / names.Length, .60f);
                rect.offsetMin = new Vector2(3, 2); rect.offsetMax = new Vector2(-3, -2);
                components.Add(input); Controls.Add(input);
            }
        }
        private void RefreshComponents(object value)
        {
            if (hex != null) { RefreshColor((Color)value); return; }
            if (components.Count == 0) return;
            float[] values = ComponentEditor.Read(value);
            for (int i = 0; i < values.Length; i++) components[i].SetValue(values[i].ToString("R", CultureInfo.InvariantCulture));
        }
    }
}
