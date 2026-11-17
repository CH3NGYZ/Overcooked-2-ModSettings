using System.Collections.Generic;

namespace OC2ModSettings
{
    internal sealed class ModMemory
    {
        internal float Scroll;
        internal string Focus;
        internal string Control;
    }
    internal static class PageMemory
    {
        internal static string Selected;
        internal static bool Advanced;
        private static readonly Dictionary<string, ModMemory> mods = new Dictionary<string, ModMemory>();
        internal static ModMemory For(string guid)
        {
            ModMemory memory;
            if (!mods.TryGetValue(guid, out memory)) mods.Add(guid, memory = new ModMemory());
            return memory;
        }
    }
}
