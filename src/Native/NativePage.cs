using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed partial class NativePage : FrontendMenuBehaviour
    {
        internal ConfigCatalog Catalog;
        internal T17Button ButtonTemplate;
        internal T17Text TextTemplate;
        internal T17Scrollbar ScrollbarTemplate;
        internal ScrollColumn Left, Right;
        internal NativeOverlay Overlay;
        internal NativeShortcut Shortcut { get; private set; }
        internal NativePalette Palette { get; private set; }
        internal T17EventSystem Domain { get { return CachedEventSystem; } }
        private List<ModConfig> mods = new List<ModConfig>();
        private readonly List<T17Button> tabs = new List<T17Button>();
        private readonly List<EditorRow> editors = new List<EditorRow>();
        private readonly List<Selectable> rightControls = new List<Selectable>();
        private ModConfig active;
        private T17Text help;
        private NativeTextInput search;
        private T17Button clearSearch, close, advancedButton, defaultsButton, refreshButton;
        private OverlayDismiss outsideDismiss;
        private SupportedLanguages language;
        private string query = string.Empty;
        private bool onRight, rebuilding, closing;
        private float nextRefresh;
        private int revision;
        private GlobalSettingsHost host;
        private GamepadUser cachedUser;
        private ILogicalButton cancelButton;
        private bool inputDirty;
        private bool ownedState;
        private int cancelHandledFrame = -2;

        internal static NativePage Create(GlobalSettingsHost host, ConfigCatalog catalog, NativeShortcut shortcut)
        {
            Transform source = NativePageTemplate.Source;
            if (source == null) throw new MissingMemberException("Native GameOptions frame unavailable.");
            T17Button sourceButton = source.Find(NativePageTemplate.ButtonPath).GetComponent<T17Button>();
            T17Text sourceText = source.Find("SettingsBody/HeaderBacker/Header").GetComponent<T17Text>();
            GameObject go = UnityEngine.Object.Instantiate(source.gameObject, host.Root.transform, false);
            try
            {
                go.name = "ModSettingsPage"; go.SetActive(false);
                RectTransform frame = (RectTransform)go.transform;
                frame.anchorMin = frame.anchorMax = new Vector2(.5f, .5f);
                frame.sizeDelta = new Vector2(1920, 1080); frame.anchoredPosition = Vector2.zero;
                frame.localScale = Vector3.one; frame.localRotation = Quaternion.identity;
                NativeUi.Strip(go);
                // Strip removed FrontendOptionsMenu before activation, including all native save/reset listeners.
                NativePage page = go.AddComponent<NativePage>();
                page.host = host; page.m_CachedEventSystem = host.Domain; page.cachedUser = host.Domain.AssignedGamepadUser;
                page.Catalog = catalog; page.ButtonTemplate = sourceButton; page.TextTemplate = sourceText; page.Shortcut = shortcut;
                page.Palette = NativePageTemplate.Palette;
                page.RefreshInputBindings();
                page.pinned = ModSettingsPlugin.Instance.Pins;
                page.ScrollbarTemplate = source.Find("SettingsBody/ContentPC/T17Scrollbar").GetComponent<T17Scrollbar>();
                Transform body = go.transform.Find("SettingsBody");
                foreach (string path in new string[] { "ContentPC", "ContentConsole", "Cancel", "Confirm", "SafeAreaAdjuster" })
                { Transform child = body.Find(path); if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject); }
                Canvas.ForceUpdateCanvases();
                ResizeFrame(body as RectTransform);
                page.BuildBanner(body);
                page.Build(body);
                page.outsideDismiss = host.Root.AddComponent<OverlayDismiss>();
                NavigateOnUICancel cancel = go.AddComponent<NavigateOnUICancel>();
                cancel.m_DoThisOnUICancel = new UnityEvent(); cancel.m_DoThisOnUICancel.AddListener(page.Cancel);
                page.m_bSelectTopElementOnShow = false;
                page.MenuName = Texts.Title;
                page.DoSingleTimeInitialize();
                PlayerInputLookup.OnRegenerateControls += page.MarkInputDirty;
                return page;
            }
            catch
            {
                UnityEngine.Object.Destroy(go);
                throw;
            }
        }
        private void Build(Transform body)
        {
            RectTransform area = NativeUi.Rect(body, "ModSettingsContent", new Vector2(ContentLeftAnchor, .18f), new Vector2(.97f, HeaderControlAnchor));
            // Share the top controls' anchor; keep only a small gap below their lower edge.
            area.offsetMax = new Vector2(0, -HeaderControlHeight * .5f - ContentTopGap);
            Left = new ScrollColumn(area, "Mods", Vector2.zero, new Vector2(.28f, 1));
            Right = new ScrollColumn(area, "Settings", new Vector2(.30f, 0), Vector2.one);
            Left.AddScrollbar(ScrollbarTemplate, Palette.Normal); Right.AddScrollbar(ScrollbarTemplate, Palette.Normal);
            search = NativeTextInput.Create(this, body, "Search", query, Texts.Search);
            search.Changed = delegate(string value)
            { Remember(); query = value; BuildSettings(false, true); return true; };
            const float closeStart = .885f, controlGap = 10;
            PlaceHeaderControl((RectTransform)search.transform, .61f, closeStart, 0, -HeaderControlHeight - 2 * controlGap);
            clearSearch = NativeUi.CloneButton(ButtonTemplate, body, "ClearSearch", "×", ClearSearch, this);
            PlaceHeaderControl((RectTransform)clearSearch.transform, closeStart, closeStart, -HeaderControlHeight - controlGap, -controlGap);
            clearSearch.SetEventSystem(Domain);
            // Leave the native frame's lower-left notch clear for the Description footer.
            help = NativeUi.Text(TextTemplate, NativeUi.Rect(body, "Explanation", new Vector2(.21f, .105f), new Vector2(.965f, .165f)), "Help", "", 18);
            close = NativeUi.CloneButton(ButtonTemplate, body, "CloseModSettings", Texts.Close, delegate { Hide(true, false); }, this);
            PlaceHeaderControl((RectTransform)close.transform, closeStart, .97f, 0, 0);
            close.SetEventSystem(Domain);
            Overlay = new NativeOverlay(this, body);
            BuildShortcut(body);
        }
        private void ClearSearch()
        {
            if (!ownedState || rebuilding) return;
            search.Select(); search.Begin(); search.ChangeText(string.Empty);
        }
        internal void Open()
        {
            if (ownedState) return;
            closing = false;
            ownedState = true;
            m_CurrentGamepadUser = cachedUser;
            try
            {
                RefreshInputBindings();
                outsideDismiss.Initialize(transform.Find("SettingsBody") as RectTransform, Domain,
                    delegate { Hide(true, false); }, delegate { return !Overlay.IsOpen; }, ContainsPanel);
                AcquireGameplayInput(); host.Show(); gameObject.SetActive(true);
                Left.Bar.SetEventSystem(Domain); Right.Bar.SetEventSystem(Domain);
                foreach (T17Button button in GetComponentsInChildren<T17Button>(true)) button.SetEventSystem(Domain);
                RefreshContent(); KeyboardFeedback();
            }
            catch { ReleaseOwnedState(); gameObject.SetActive(false); host.Hide(); m_CurrentGamepadUser = null; throw; }
        }
        internal void Cancel()
        {
            if (Time.frameCount <= cancelHandledFrame + 1) return;
            cancelHandledFrame = Time.frameCount;
            if (Overlay.IsOpen) { Overlay.Close(); return; }
            if (EndShortcutCapture()) return;
            if (search != null && search.HasPending) { search.Cancel(); return; }
            foreach (EditorRow editor in editors) if (editor.CancelPending()) return;
            if (onRight) { FocusTab(); return; }
            Hide(true, false);
        }
        internal void CaptureCancelled() { cancelHandledFrame = Time.frameCount; Overlay.Close(); }
        public override bool Hide(bool restoreInvokerState, bool isTabSwitch)
        {
            if (closing || !gameObject.activeSelf) return false;
            closing = true;
            Remember(); CancelEdits(false);
            ReleaseOwnedState();
            Domain.ForceDeselectSelectionObject();
            gameObject.SetActive(false); host.Hide(); m_CurrentGamepadUser = null;
            closing = false; return true;
        }
        protected override void OnDestroy()
        {
            PlayerInputLookup.OnRegenerateControls -= MarkInputDirty;
            ReleaseOwnedState(); DisposeEditors();
            if (host != null) host.Hide();
            base.OnDestroy();
        }
        private void OnDisable()
        {
            if (!ownedState || closing) return;
            closing = true; Remember(); ReleaseOwnedState();
            if (Domain != null) Domain.ForceDeselectSelectionObject();
            m_CurrentGamepadUser = null; host.Hide(); closing = false;
        }
        private void ReleaseOwnedState()
        {
            if (!ownedState) return; ownedState = false;
            StopContentBuild();
            hoveredPin = null;
            hoveredRow = null; hoveredControl = null; hoveredWarning = null;
            ClearFeedbackError();
            if (litRow != null) litRow.Paint(false);
            if (litControl != null) litControl.Paint(false);
            litRow = null; litControl = null;
            CancelEdits(false);
            ReleaseGameplayInput();
        }
        protected override void Update()
        {
            base.Update();
            if (inputDirty) RefreshInputBindings();
            bool keyboardCancel = Input.GetKeyUp(KeyCode.Escape);
            bool mappedCancel = cancelButton != null && cancelButton.JustReleased();
            if (ownedState && !Domain.IsDisabled() && (keyboardCancel || mappedCancel)) Cancel();
            if (!gameObject.activeInHierarchy || rebuilding || Time.realtimeSinceStartup < nextRefresh) return;
            nextRefresh = Time.realtimeSinceStartup + .2f;
            RefreshShortcutKey();
            foreach (EditorRow editor in editors) editor.RefreshActions();
            bool languageChanged = RefreshLanguage();
            if (pinRevision != pinned.Revision) ReorderPinnedTabs();
            bool shortcutsChanged = false;
            if (Catalog.ConsumeDirty())
            {
                // Value notifications refresh in place; explicit Refresh or structural changes rebuild below.
                List<ModConfig> snapshot = Catalog.Snapshot();
                snapshot.Sort(CompareMods);
                if (revision != Catalog.Revision || !SameShape(mods, snapshot)) Reload(snapshot);
                else
                {
                    shortcutsChanged = shortcutRevision != Catalog.ShortcutRevision;
                    if (shortcutsChanged) RefreshShortcutConflicts(snapshot);
                    foreach (EditorRow editor in editors) editor.Refresh();
                }
            }
            if (!rebuilding && (RefreshDisplayText() || shortcutsChanged || languageChanged) && feedbackError == null) RefreshFeedback();
            Overlay.RefreshChoices();
        }
        private bool RefreshLanguage()
        {
            SupportedLanguages current = Localization.GetLanguage();
            Catalog.Translations.SetLanguage(current);
            if (language == current) return false;
            language = current;
            MenuName = Texts.Title;
            NativeUi.Label(bannerTitle.transform, Texts.PanelTitle);
            search.SetPlaceholder(Texts.Search); NativeUi.Label(close.transform, Texts.Close);
            RefreshShortcutKey();
            if (advancedButton != null) NativeUi.Label(advancedButton.transform, Texts.Advanced + (PageMemory.Advanced ? " ✓" : ""));
            if (defaultsButton != null) NativeUi.Label(defaultsButton.transform, Texts.Defaults);
            if (refreshButton != null) NativeUi.Label(refreshButton.transform, Texts.Refresh);
            Transform empty = Right.Content.Find("Empty"); if (empty != null) NativeUi.Label(empty, Texts.Empty);
            foreach (EditorRow editor in editors) editor.RefreshLanguage();
            foreach (NativeTextInput input in GetComponentsInChildren<NativeTextInput>(true))
                if (input.Invalid) InputValidationChanged(input);
            Overlay.RefreshLanguage();
            return true;
        }
        private void MarkInputDirty() { inputDirty = true; }
        private void RefreshInputBindings()
        {
            Domain.ResetInputModule();
            cancelButton = PlayerInputLookup.GetUIButton(PlayerInputLookup.LogicalButtonID.UICancel);
            inputDirty = false;
        }
        private static bool SameShape(List<ModConfig> a, List<ModConfig> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Guid != b[i].Guid || a[i].Name != b[i].Name || a[i].Description != b[i].Description
                    || a[i].Rows.Count != b[i].Rows.Count) return false;
                for (int j = 0; j < a[i].Rows.Count; j++)
                {
                    SettingRow x = a[i].Rows[j], y = b[i].Rows[j];
                    if (!ReferenceEquals(x.Entry, y.Entry) || !ReferenceEquals(x.Action, y.Action)
                        || !x.Metadata.SameAs(y.Metadata) || x.Actions.Count != y.Actions.Count) return false;
                    for (int k = 0; k < x.Actions.Count; k++) if (!ReferenceEquals(x.Actions[k], y.Actions[k])) return false;
                }
            }
            return true;
        }
        private void Remember()
        {
            if (active == null) return;
            ModMemory memory = PageMemory.For(active.Guid); memory.Scroll = Right.Content.anchoredPosition.y;
            foreach (EditorRow editor in editors)
                if (editor.Owns(Domain == null ? null : Domain.currentSelectedGameObject))
                {
                    memory.Focus = editor.Row.Identity;
                    memory.Control = Domain.currentSelectedGameObject.name;
                }
        }
        private void CancelEdits(bool preserveSearch)
        {
            EndShortcutCapture();
            if (Overlay != null) Overlay.Close();
            if (search != null && !preserveSearch) search.Cancel();
            foreach (EditorRow editor in editors) editor.CancelPending();
        }
        private void DisposeEditors() { foreach (EditorRow editor in editors) editor.Dispose(); editors.Clear(); }
    }
}
