using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace OC2ModSettings
{
    internal static class RendererContract
    {
        internal static void Validate(IDictionary<string, object> protocol)
        {
            if (protocol == null || !protocol.ContainsKey("Version") || !Equals(protocol["Version"], 1)
                || !protocol.ContainsKey("Create")
                || !(protocol["Create"] is Func<RectTransform, ConfigEntryBase, IDictionary<string, object>, IDisposable>))
                throw new ArgumentException("Renderer v1 requires Version=1 and a shared Create delegate.");
        }
    }
}
