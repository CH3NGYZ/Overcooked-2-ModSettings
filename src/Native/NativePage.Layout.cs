using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private bool contentBuilt, builtAdvanced;
        private T17EventSystem contentDomain;
        private Coroutine initialBuild;
        internal bool ContentReady { get { return contentBuilt && !rebuilding; } }
        private void RefreshContent()
        {
            RefreshLanguage();
            RefreshShortcutKey();
            Catalog.ConsumeDirty();
            List<ModConfig> snapshot = Catalog.Snapshot(); snapshot.Sort(CompareMods);
            if (!contentBuilt || contentDomain != Domain || revision != Catalog.Revision
                || builtAdvanced != PageMemory.Advanced
                || (active == null ? PageMemory.Selected != null : active.Guid != PageMemory.Selected)
                || !SameShape(mods, snapshot)) { Reload(snapshot); return; }
            // Values can change while hidden without a structural API refresh. Keep the existing
            // controls and callbacks, but always read the current ConfigEntry values on reopening.
            RefreshShortcutConflicts(snapshot);
            RefreshDisplayText();
            foreach (EditorRow editor in editors) editor.Refresh();
            WireNavigation(); RefreshFeedback(); RefreshPins();
            StopAllCoroutines(); StartCoroutine(RestoreLayout(false, false, false));
        }
        internal void Reload()
        {
            Catalog.ConsumeDirty(); Reload(Catalog.Snapshot());
        }
        private void Reload(List<ModConfig> snapshot)
        {
            StopContentBuild();
            NativeOverlay.ChoiceState choice = Overlay.RememberChoice();
            bool firstBuild = !contentBuilt;
            contentBuilt = false;
            Remember(); CancelEdits(false);
            mods = snapshot;
            mods.Sort(CompareMods); pinRevision = pinned.Revision; hoveredPin = null;
            RefreshShortcutConflicts(mods);
            revision = Catalog.Revision;
            RefreshLanguage();
            contentDomain = Domain;
            string selected = PageMemory.Selected;
            active = mods.Find(delegate(ModConfig m) { return m.Guid == selected; });
            if (active == null && mods.Count > 0) active = mods[0];
            PageMemory.Selected = active == null ? null : active.Guid;
            if (firstBuild)
            {
                rebuilding = true; search.interactable = clearSearch.interactable = false; SetHelp(Texts.Loading, Color.white);
                initialBuild = StartCoroutine(LoadSettings());
            }
            else
            {
                IEnumerator tabsBuild = BuildTabs(false); while (tabsBuild.MoveNext()) { }
                BuildSettings(false, false); contentBuilt = true;
                if (choice != null)
                {
                    foreach (EditorRow editor in editors)
                        if (ReferenceEquals(editor.Row.Entry, choice.Entry)) { editor.OpenChoices(); break; }
                    Overlay.RestoreChoice(choice);
                }
            }
        }
        private IEnumerator BuildTabs(bool distribute)
        {
            System.Diagnostics.Stopwatch budget = System.Diagnostics.Stopwatch.StartNew();
            // Keep incomplete clones out of the live Canvas until labels, pins and geometry are ready.
            Left.Content.gameObject.SetActive(false);
            try
            {
                NativeUi.ClearChildren(Left.Content); tabs.Clear();
                float width = Left.Viewport.rect.width;
                for (int i = 0; i < mods.Count; i++)
                {
                    ModConfig mod = mods[i];
                    T17Button button = NativeUi.CloneButton(ButtonTemplate, Left.Content, "Mod_" + mod.Guid, Catalog.GetDisplayName(mod),
                        delegate { Activate(mod.Guid); }, this);
                    NativeUi.Box(button.GetComponent<RectTransform>(), ModTabInset, i * 72, Mathf.Max(100, width - 2 * ModTabInset), 68);
                    foreach (T17Text text in button.GetComponentsInChildren<T17Text>(true))
                    {
                        text.fontSize = 23; text.resizeTextForBestFit = false; text.lineSpacing = 1; text.verticalOverflow = VerticalWrapMode.Truncate;
                        text.rectTransform.offsetMax = new Vector2(-58, -3);
                    }
                    button.SetEventSystem(Domain);
                    RowFocus focus = button.gameObject.AddComponent<RowFocus>(); focus.Column = Left; focus.Row = button.GetComponent<RectTransform>();
                    focus.Selected = delegate { onRight = false; };
                    tabs.Add(button);
                    ModPin.Create(this, button, mod.Guid);
                    if (distribute && budget.ElapsedMilliseconds >= 4)
                    { yield return null; budget.Reset(); budget.Start(); }
                }
                Left.Height(mods.Count * 72);
            }
            finally { Left.Content.gameObject.SetActive(true); }
        }
        private void Activate(string guid)
        {
            ModConfig selected = mods.Find(delegate(ModConfig m) { return m.Guid == guid; });
            if (selected == null) return;
            Remember(); CancelEdits(false); active = selected; PageMemory.Selected = guid; query = string.Empty;
            BuildSettings(false, false);
        }
        private void BuildSettings(bool focusRight, bool preserveSearch)
        {
            StopContentBuild(); StopAllCoroutines();
            IEnumerator build = PopulateSettings(false, preserveSearch);
            while (build.MoveNext()) { }
            contentBuilt = true;
            StartCoroutine(RestoreLayout(focusRight, true, preserveSearch));
        }
        private IEnumerator LoadSettings()
        {
            // Show the native frame immediately, then spread first-use editor creation across
            // frames. All ConfigFile callbacks and Unity component work stay on the main thread.
            yield return null;
            IEnumerator build = PopulateInitialContent();
            bool complete = false;
            while (true)
            {
                bool next; object current;
                try { next = build.MoveNext(); current = next ? build.Current : null; }
                catch (Exception error)
                {
                    ModSettingsPlugin.Instance.Report("NativePage.LoadSettings", error);
                    ShowError(error.Message); break;
                }
                if (!next) { complete = true; break; }
                yield return current;
            }
            initialBuild = null; rebuilding = false; contentBuilt = complete;
            Left.Content.gameObject.SetActive(true); Right.Content.gameObject.SetActive(true); search.interactable = clearSearch.interactable = true;
            if (complete) StartCoroutine(RestoreLayout(false, true, false));
        }
        private void StopContentBuild()
        {
            if (initialBuild == null) return;
            StopCoroutine(initialBuild); initialBuild = null; rebuilding = false; contentBuilt = false;
            DisposeEditors(); NativeUi.ClearChildren(Right.Content); rightControls.Clear();
            Left.Content.gameObject.SetActive(true); Right.Content.gameObject.SetActive(true); search.interactable = clearSearch.interactable = true;
        }
        private IEnumerator PopulateInitialContent()
        {
            IEnumerator tabsBuild = BuildTabs(true); while (tabsBuild.MoveNext()) yield return tabsBuild.Current;
            yield return null;
            IEnumerator rows = PopulateSettings(true, false); while (rows.MoveNext()) yield return rows.Current;
        }
        private IEnumerator PopulateSettings(bool distribute, bool preserveSearch)
        {
            builtAdvanced = PageMemory.Advanced;
            rebuilding = true;
            try
            {
                CancelEdits(preserveSearch); DisposeEditors(); rightControls.Clear();
                bool inactive = CanBuildInactive();
                if (inactive) Right.Content.gameObject.SetActive(false);
                try
                {
                    IEnumerator rows = BuildRows(distribute);
                    while (rows.MoveNext()) yield return rows.Current;
                }
                finally { if (inactive) Right.Content.gameObject.SetActive(true); }
                CollectRightControls(); WireNavigation(); UpdateSelectionMarker(); RefreshPins();
            }
            finally { rebuilding = false; }
        }
        private bool CanBuildInactive()
        {
            // Consumer factories may query active children during Create. Preserve that public
            // lifecycle; only batch rows whose construction is owned entirely by this plugin.
            if (active != null) foreach (SettingRow row in active.Rows)
            {
                IDictionary<string, object> renderer;
                if (row.Entry != null && row.Matches(query, PageMemory.Advanced) && !row.Metadata.ReadOnly
                    && Catalog.Renderers.TryGet(row.Entry, out renderer)) return false;
            }
            return true;
        }
        private IEnumerator BuildRows(bool distribute)
        {
            System.Diagnostics.Stopwatch budget = System.Diagnostics.Stopwatch.StartNew();
            NativeUi.ClearChildren(Right.Content);
            categoryLabels.Clear();
            ClearFeedbackError(); SetHelp(null, Color.white);
            float width = Mathf.Max(300, Right.Viewport.rect.width), y = 0;
            search.SetValue(query);
            T17Button advanced = ActionButton(Texts.Advanced + (PageMemory.Advanced ? " ✓" : ""), 0, y, width * .45f,
                delegate { PageMemory.Advanced = !PageMemory.Advanced; BuildSettings(true, false); });
            T17Button defaults = ActionButton(Texts.Defaults, width * .46f, y, width * .31f,
                delegate { RestoreDefaults(); });
            T17Button refresh = ActionButton(Texts.Refresh, width * .78f, y, width * .22f,
                delegate { Catalog.Invalidate(); Reload(); });
            advancedButton = advanced; defaultsButton = defaults; refreshButton = refresh;
            y += 50;
            string category = null;
            if (active != null)
                foreach (SettingRow row in active.Rows)
                {
                    if (!row.Matches(query, PageMemory.Advanced)) continue;
                    if (category != row.Metadata.Category)
                    {
                        category = row.Metadata.Category;
                        if (editors.Count > 0) y += 18;
                        RectTransform header = NativeUi.Rect(Right.Content, "Category", Vector2.zero, Vector2.one);
                        NativeUi.Box(header, 4, y, width - 8, 40);
                        NativeUi.Backer(header, Palette.Category).raycastTarget = false;
                        T17Text label = NativeUi.Text(TextTemplate, header, "Label", row.DisplayCategory, 24);
                        categoryLabels.Add(label, row);
                        label.alignment = TextAnchor.MiddleCenter; label.color = Color.white;
                        label.resizeTextForBestFit = true; label.resizeTextMinSize = 18; label.resizeTextMaxSize = 24;
                        RectTransform divider = NativeUi.Rect(header, "Divider", Vector2.zero, new Vector2(1, 0));
                        divider.sizeDelta = new Vector2(0, 2);
                        NativeUi.Backer(divider, new Color(1, 1, 1, .38f)).raycastTarget = false;
                        y += 48;
                    }
                    EditorRow editor = EditorRow.Create(this, row, width - 32);
                    NativeUi.Box(editor.Root, 16, y, width - 32, editor.Height); y += editor.Height + 4;
                    editors.Add(editor);
                    if (distribute && budget.ElapsedMilliseconds >= 4)
                    { yield return null; budget.Reset(); budget.Start(); }
                }
            if (editors.Count == 0)
            {
                RectTransform empty = NativeUi.Rect(Right.Content, "Empty", Vector2.zero, Vector2.one);
                NativeUi.Box(empty, 4, y, width - 8, 58); NativeUi.Text(TextTemplate, empty, "Label", Texts.Empty, 24); y += 64;
            }
            Right.Height(y);
        }
        internal T17Button ActionButton(string label, float x, float y, float width, Action click)
        {
            T17Button button = NativeUi.CloneButton(ButtonTemplate, Right.Content, label, label, click, this);
            NativeUi.Box(button.GetComponent<RectTransform>(), x + 3, y, Mathf.Max(20, width - 6), 42);
            button.SetEventSystem(Domain);
            foreach (T17Text text in button.GetComponentsInChildren<T17Text>(true))
            { text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = 21; }
            return button;
        }
        private void RestoreDefaults()
        {
            if (active == null) return;
            Overlay.ChooseDefaults(delegate(int ignored)
            {
                bool checkShortcuts = false;
                SettingRow failed = null;
                string error = null;
                foreach (SettingRow row in active.Rows)
                {
                    if (row.Entry == null || row.Metadata.ReadOnly || row.Metadata.Hidden || row.Metadata.HideDefault) continue;
                    checkShortcuts |= ShortcutConflicts.Tracks(row.Entry);
                    CommitResult result = ConfigCommit.Apply(row, row.Entry.DefaultValue);
                    if (!result.Success) { failed = row; error = row.DisplayName + ": " + result.Error; break; }
                }
                if (checkShortcuts) RefreshShortcutConflicts(mods);
                foreach (EditorRow editor in editors) editor.Refresh();
                if (failed == null) ClearError();
                else ShowCommitError(failed, error);
            });
        }
        private void UpdateSelectionMarker()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                NativeUi.Label(tabs[i].transform, (mods[i] == active ? "● " : "") + Catalog.GetDisplayName(mods[i]));
            }
        }
        private void CollectRightControls()
        {
            rightControls.Clear();
            rightControls.Add(search); rightControls.Add(clearSearch); rightControls.Add(close);
            rightControls.Add(advancedButton); rightControls.Add(defaultsButton); rightControls.Add(refreshButton);
            foreach (EditorRow editor in editors) rightControls.AddRange(editor.Controls);
        }
        private void WireNavigation()
        {
            T17Button current = ActiveTab() ?? shortcutButton;
            Selectable remembered = RememberedControl();
            for (int i = 0; i < tabs.Count; i++)
            {
                Navigation n = new Navigation { mode = Navigation.Mode.Explicit };
                n.selectOnUp = i > 0 ? tabs[i - 1] : shortcutButton; n.selectOnDown = i + 1 < tabs.Count ? tabs[i + 1] : shortcutButton;
                ModPin pin = tabs[i].GetComponent<ModPin>();
                n.selectOnRight = pin.Button == null ? remembered : pin.Button; tabs[i].navigation = n;
                if (pin.Button == null) continue;
                Navigation pinNav = n; pinNav.selectOnLeft = tabs[i]; pinNav.selectOnRight = remembered;
                pinNav.selectOnUp = i > 0 ? (Selectable)tabs[i - 1].GetComponent<ModPin>().Button ?? tabs[i - 1] : shortcutButton;
                pinNav.selectOnDown = i + 1 < tabs.Count ? (Selectable)tabs[i + 1].GetComponent<ModPin>().Button ?? tabs[i + 1] : shortcutButton;
                pin.Button.navigation = pinNav;
            }
            WireShortcut(remembered);
            for (int i = 0; i < rightControls.Count; i++)
            {
                Selectable control = rightControls[i];
                Navigation n = new Navigation { mode = Navigation.Mode.Explicit };
                n.selectOnUp = i > 0 ? rightControls[i - 1] : current;
                n.selectOnDown = i + 1 < rightControls.Count ? rightControls[i + 1] : null;
                n.selectOnLeft = current; control.navigation = n;
                RowFocus focus = control.GetComponent<RowFocus>();
                if (focus == null) focus = control.gameObject.AddComponent<RowFocus>();
                focus.Column = control.transform.IsChildOf(Right.Content) ? Right : null;
                if (focus.Row == null) focus.Row = control.GetComponent<RectTransform>();
                focus.Selected = delegate
                {
                    onRight = true;
                    RefreshPins();
                    RefreshFeedback();
                };
            }
            Navigation searchNav = search.navigation; searchNav.selectOnRight = clearSearch;
            searchNav.selectOnDown = advancedButton; search.navigation = searchNav;
            Navigation clearNav = clearSearch.navigation; clearNav.selectOnLeft = search; clearNav.selectOnRight = close;
            clearNav.selectOnUp = null; clearNav.selectOnDown = advancedButton; clearSearch.navigation = clearNav;
            Navigation closeNav = close.navigation; closeNav.selectOnLeft = clearSearch; closeNav.selectOnUp = null;
            closeNav.selectOnDown = advancedButton; close.navigation = closeNav;
            foreach (EditorRow row in editors)
                for (int i = 0; i < row.Controls.Count; i++)
                {
                    Selectable control = row.Controls[i]; Navigation n = control.navigation;
                    if (!(control is UnityEngine.UI.Slider))
                    { n.selectOnLeft = i > 0 ? row.Controls[i - 1] : current; n.selectOnRight = i + 1 < row.Controls.Count ? row.Controls[i + 1] : null; }
                    control.navigation = n;
                }
        }
        internal void ResizeRow(EditorRow row, float height)
        {
            float delta = height - row.Height; row.Height = height;
            if (rebuilding || !editors.Contains(row) || Mathf.Abs(delta) < .1f) return;
            row.Root.sizeDelta += new Vector2(0, delta);
            for (int i = row.Root.GetSiblingIndex() + 1; i < Right.Content.childCount; i++)
            { RectTransform rect = Right.Content.GetChild(i) as RectTransform; if (rect != null) rect.anchoredPosition -= new Vector2(0, delta); }
            Right.Height(Right.Content.rect.height + delta);
        }
        private IEnumerator RestoreLayout(bool focusRight, bool layoutChanged, bool preserveSearch)
        {
            yield return null;
            if (layoutChanged) { Canvas.ForceUpdateCanvases(); yield return null; }
            // A search rebuild does not replace its field. If the user already clicked a
            // result, do not restore the old scroll or steal selection on this later frame.
            if (preserveSearch && (Domain == null || !search.IsEditing || Domain.currentSelectedGameObject != search.gameObject)) yield break;
            ModMemory memory = active == null ? null : PageMemory.For(active.Guid);
            Right.Content.anchoredPosition = new Vector2(0, memory == null ? 0 : Mathf.Clamp(memory.Scroll, 0, Mathf.Max(0, Right.Content.rect.height - Right.Viewport.rect.height)));
            if (preserveSearch) yield break;
            if (Overlay.IsOpen || IsRecordingToggleKey || (!focusRight && Domain != null && Domain.currentSelectedGameObject == shortcutButton.gameObject)) yield break;
            Selectable target = null;
            if (focusRight)
            {
                if (memory != null) foreach (EditorRow editor in editors) if (editor.Row.Identity == memory.Focus) { target = ControlFor(editor, memory); break; }
                if (target == null && editors.Count > 0) target = editors[0].First;
                if (target == null && rightControls.Count > 0) target = rightControls[0];
            }
            if (target == null) target = ActiveTab() ?? shortcutButton;
            if (Domain != null && target != null) Domain.SetSelectedGameObject(target.gameObject);
        }
        private Selectable RememberedControl()
        {
            ModMemory memory = active == null ? null : PageMemory.For(active.Guid);
            if (memory != null) foreach (EditorRow editor in editors) if (editor.Row.Identity == memory.Focus && editor.First != null) return ControlFor(editor, memory);
            return editors.Count > 0 && editors[0].First != null ? editors[0].First : (rightControls.Count > 0 ? rightControls[0] : null);
        }
        private static Selectable ControlFor(EditorRow editor, ModMemory memory)
        {
            foreach (Selectable control in editor.Controls)
                if (control != null && control.isActiveAndEnabled && control.IsInteractable() && control.name == memory.Control) return control;
            return editor.First;
        }
        private T17Button ActiveTab() { int index = active == null ? -1 : mods.IndexOf(active); return index >= 0 && index < tabs.Count ? tabs[index] : null; }
        private void FocusTab() { onRight = false; if (Domain != null) Domain.SetSelectedGameObject((ActiveTab() ?? shortcutButton).gameObject); }
    }
}
