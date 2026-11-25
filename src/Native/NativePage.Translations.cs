using System.Collections.Generic;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private readonly Dictionary<T17Text, SettingRow> categoryLabels = new Dictionary<T17Text, SettingRow>();
        private int displayRevision;

        private bool RefreshDisplayText()
        {
            int current = Catalog.TextRevision;
            if (displayRevision == current) return false;
            displayRevision = current;
            if (SearchResultsChanged())
            {
                Remember();
                bool preserveSearch = search.IsEditing && Domain.currentSelectedGameObject == search.gameObject;
                BuildSettings(!preserveSearch && onRight, preserveSearch);
            }
            else
            {
                UpdateSelectionMarker();
                foreach (KeyValuePair<T17Text, SettingRow> group in categoryLabels)
                {
                    string caption = group.Value.DisplayCategory;
                    if (group.Key.text != caption) NativeUi.Label(group.Key.transform, caption);
                }
                bool layoutChanged = false;
                foreach (EditorRow editor in editors) layoutChanged |= editor.RefreshLocalizedText();
                if (layoutChanged) { CollectRightControls(); WireNavigation(); }
                Overlay.RefreshLocalizedText();
            }
            return true;
        }

        private bool SearchResultsChanged()
        {
            if (active == null || string.IsNullOrEmpty(query)) return false;
            int index = 0;
            foreach (SettingRow row in active.Rows)
            {
                if (!row.Matches(query, builtAdvanced)) continue;
                if (index == editors.Count || editors[index].Row != row) return true;
                index++;
            }
            return index != editors.Count;
        }
    }
}
