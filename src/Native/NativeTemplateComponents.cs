using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    // Scene AssetBundle.Unload(true) invalidates serialized MonoScript handles even on DDOL clones.
    // Recreate the retained UI components while their source is alive, using runtime AddComponent.
    internal static class NativeTemplateComponents
    {
        internal static void Rebuild(GameObject template)
        {
            MonoBehaviour[] source = template.GetComponentsInChildren<MonoBehaviour>(true);
            Dictionary<Object, Object> replacements = new Dictionary<Object, Object>();
            foreach (MonoBehaviour old in source)
            {
                // Stage on a separate inactive object: Graphic disallows duplicate components.
                GameObject stage = new GameObject("TemplateComponentStage", typeof(RectTransform)); stage.SetActive(false);
                Behaviour fresh = (Behaviour)stage.AddComponent(old.GetType());
                replacements[old] = fresh;
                CopyProperties(old, fresh);
            }
            foreach (MonoBehaviour old in source)
            {
                CopyReferences(old as Selectable, replacements[old] as Selectable, replacements);
            }
            // All source data is now on runtime-created components. Copy their serialized fields into
            // runtime components on the retained hierarchy, then rebind references in the second pass.
            Dictionary<Object, Object> retained = new Dictionary<Object, Object>();
            foreach (MonoBehaviour old in source)
            {
                GameObject go = old.gameObject; Behaviour stage = (Behaviour)replacements[old];
                System.Type type = old.GetType(); Object.DestroyImmediate(old);
                Behaviour fresh = (Behaviour)go.AddComponent(type); retained[stage] = fresh;
                CopyProperties(stage, fresh);
            }
            foreach (Object staged in replacements.Values)
            {
                CopyReferences(staged as Selectable, retained[staged] as Selectable, retained);
            }
            foreach (Object staged in replacements.Values) Object.DestroyImmediate(((Component)staged).gameObject);
        }
        private static T Replace<T>(T value, Dictionary<Object, Object> replacements) where T : Object
        { Object replacement; return value != null && replacements.TryGetValue(value, out replacement) ? (T)replacement : value; }
        private static void CopyProperties(Behaviour source, Behaviour target)
        {
            target.enabled = source.enabled;
            Graphic graphic = source as Graphic; if (graphic != null) CopyGraphic(graphic, (Graphic)target);
            LayoutElement layout = source as LayoutElement;
            if (layout == null) return;
            LayoutElement copy = (LayoutElement)target;
            copy.ignoreLayout = layout.ignoreLayout; copy.minWidth = layout.minWidth; copy.minHeight = layout.minHeight;
            copy.preferredWidth = layout.preferredWidth; copy.preferredHeight = layout.preferredHeight;
            copy.flexibleWidth = layout.flexibleWidth; copy.flexibleHeight = layout.flexibleHeight; copy.layoutPriority = layout.layoutPriority;
        }
        private static void CopyReferences(Selectable source, Selectable target, Dictionary<Object, Object> replacements)
        {
            if (source == null) return;
            target.colors = source.colors; target.transition = source.transition; target.interactable = source.interactable;
            target.targetGraphic = Replace(source.targetGraphic, replacements);
            Scrollbar bar = source as Scrollbar;
            if (bar == null) return;
            Scrollbar copy = (Scrollbar)target; copy.handleRect = bar.handleRect; copy.direction = bar.direction;
            copy.numberOfSteps = bar.numberOfSteps; copy.size = bar.size; copy.value = bar.value;
        }
        private static void CopyGraphic(Graphic source, Graphic copy)
        {
            copy.color = source.color; copy.material = source.material; copy.raycastTarget = source.raycastTarget;
            Text text = source as Text;
            if (text != null)
            {
                Text target = (Text)copy; target.font = text.font; target.text = text.text;
                target.fontSize = text.fontSize; target.fontStyle = text.fontStyle; target.alignment = text.alignment;
                target.lineSpacing = text.lineSpacing; target.supportRichText = text.supportRichText;
                target.horizontalOverflow = text.horizontalOverflow; target.verticalOverflow = text.verticalOverflow;
                target.resizeTextForBestFit = text.resizeTextForBestFit;
                target.resizeTextMinSize = text.resizeTextMinSize; target.resizeTextMaxSize = text.resizeTextMaxSize;
            }
            Image image = source as Image;
            if (image != null)
            {
                Image target = (Image)copy; target.sprite = image.sprite; target.overrideSprite = image.overrideSprite;
                target.type = image.type; target.preserveAspect = image.preserveAspect; target.fillCenter = image.fillCenter;
                target.fillMethod = image.fillMethod; target.fillOrigin = image.fillOrigin;
                target.fillAmount = image.fillAmount; target.fillClockwise = image.fillClockwise;
            }
        }
    }
}
