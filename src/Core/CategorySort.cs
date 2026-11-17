using System;
using System.Collections.Generic;

namespace OC2ModSettings
{
    // One rank per displayed category keeps ordinary settings and standalone actions together.
    internal static class CategorySort
    {
        private sealed class Category
        {
            internal int? Order;
            internal string Identity;
        }
        internal static void Sort(List<SettingRow> rows)
        {
            Dictionary<string, Category> categories = new Dictionary<string, Category>(StringComparer.Ordinal);
            foreach (SettingRow row in rows)
            {
                string name = row.Metadata.Category ?? "";
                Category category;
                if (!categories.TryGetValue(name, out category)) categories.Add(name, category = new Category());
                int? order = row.Metadata.CategoryOrder;
                if (order.HasValue && (!category.Order.HasValue || order.Value > category.Order.Value)) category.Order = order;
                // Config section keys and action IDs survive display-language changes.
                string identity = row.Entry == null ? "1\n" + row.Action.Id : "0\n" + row.Entry.Definition.Section;
                if (category.Identity == null || StringComparer.Ordinal.Compare(identity, category.Identity) < 0) category.Identity = identity;
            }
            rows.Sort(delegate(SettingRow a, SettingRow b)
            {
                string aName = a.Metadata.Category ?? "", bName = b.Metadata.Category ?? "";
                if (!StringComparer.Ordinal.Equals(aName, bName))
                {
                    Category x = categories[aName], y = categories[bName];
                    int c = y.Order.GetValueOrDefault().CompareTo(x.Order.GetValueOrDefault());
                    if (c == 0) c = StringComparer.Ordinal.Compare(x.Identity, y.Identity);
                    return c == 0 ? StringComparer.Ordinal.Compare(aName, bName) : c;
                }
                int within = b.Metadata.Order.CompareTo(a.Metadata.Order);
                if (within == 0) within = StringComparer.CurrentCultureIgnoreCase.Compare(a.Metadata.Name, b.Metadata.Name);
                return within == 0 ? StringComparer.Ordinal.Compare(a.Identity, b.Identity) : within;
            });
        }
    }
}
