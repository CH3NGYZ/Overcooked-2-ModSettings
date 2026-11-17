using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OC2ModSettings;

[assembly: AssemblyTitle(ModSettingsPlugin.DisplayName)]
[assembly: AssemblyProduct(ModSettingsPlugin.DisplayName)]
[assembly: AssemblyCompany(ModSettingsPlugin.Author)]
[assembly: AssemblyCopyright("Copyright © " + ModSettingsPlugin.Author)]
[assembly: AssemblyDescription(ModSettingsPlugin.Description)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion(ModSettingsPlugin.Version)]
[assembly: AssemblyFileVersion(ModSettingsPlugin.Version)]
[assembly: AssemblyInformationalVersion(ModSettingsPlugin.Version)]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// COM identity is independent from the permanent BepInEx plugin GUID.
[assembly: ComVisible(false)]
[assembly: Guid("37222f2d-41c2-43d7-85db-7210875a1e8d")]
[assembly: CLSCompliant(false)]
[assembly: InternalsVisibleTo("OC2ModSettings.Tests")]
[assembly: InternalsVisibleTo("OC2ModSettings.UnityTests")]
