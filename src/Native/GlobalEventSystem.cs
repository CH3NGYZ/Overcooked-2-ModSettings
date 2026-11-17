using System;
using System.Reflection;
using HarmonyLib;

namespace OC2ModSettings
{
    // Keep T17 selection/capture semantics, without joining the game's recyclable domain pool.
    internal sealed class GlobalEventSystem : T17EventSystem
    {
        private static readonly FieldInfo moduleField = AccessTools.Field(typeof(T17EventSystem), "m_T17Module");
        protected override void Awake()
        {
            // Verified Unity 2017 EventSystem inherits the empty UIBehaviour.Awake. Calling
            // T17.Awake would register us as a free scene domain, which ResetAll can reclaim.
            if (moduleField == null) throw new MissingFieldException("T17EventSystem.m_T17Module");
            moduleField.SetValue(this, GetComponent<T17StandaloneInputModule>());
        }
    }
}
