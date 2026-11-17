using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace OC2ModSettings
{
    internal sealed class RegisteredAction
    {
        internal string Owner, Id, Name, Description, Category;
        internal int Order;
        internal int? CategoryOrder;
        internal bool ClosePage;
        internal ConfigEntryBase Entry;
        internal Action Execute;
        internal Func<bool> CanExecute;
        internal string Key { get { return Owner + "\n" + Id; } }
        internal static RegisteredAction Read(string owner, string id, IDictionary<string, object> descriptor)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(id) || owner.IndexOf('\n') >= 0 || id.IndexOf('\n') >= 0)
                throw new ArgumentException("An owner GUID and action ID without newlines are required.");
            if (descriptor == null) throw new ArgumentNullException("descriptor");
            object version, execute, name;
            if (!descriptor.TryGetValue("Version", out version) || !Equals(version, 1)
                || !descriptor.TryGetValue("Execute", out execute) || !(execute is Action)
                || !descriptor.TryGetValue("DisplayName", out name) || !(name is string) || string.IsNullOrEmpty((string)name))
                throw new ArgumentException("Action v1 requires Version=1, DisplayName and Action Execute.");
            return new RegisteredAction { Owner = owner, Id = id, Name = (string)name, Execute = (Action)execute,
                Description = Optional(descriptor, "Description", ""), Category = Optional(descriptor, "Category", "General"),
                Order = Optional(descriptor, "Order", 0), ClosePage = Optional(descriptor, "ClosePage", false),
                CategoryOrder = Optional<int?>(descriptor, "CategoryOrder", null),
                Entry = Optional<ConfigEntryBase>(descriptor, "Entry", null), CanExecute = Optional<Func<bool>>(descriptor, "CanExecute", null) };
        }
        private static T Optional<T>(IDictionary<string, object> values, string key, T fallback)
        {
            object value;
            if (!values.TryGetValue(key, out value) || value == null) return fallback;
            if (!(value is T)) throw new ArgumentException("Invalid action field: " + key);
            return (T)value;
        }
        internal EntryMetadata Metadata()
        { return new EntryMetadata { Name = Name, Description = Description, Category = Category, CategoryOrder = CategoryOrder, Order = Order, HideDefault = true }; }
    }
}
