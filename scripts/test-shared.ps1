param([string]$GameRoot='E:\SteamLibrary\steamapps\common\Overcooked! 2',[switch]$BuildOnly)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=Join-Path $root ('.validation\shared-'+(Get-Date -Format yyyyMMdd-HHmmss))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$msbuild='D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
$pluginSource=[IO.File]::ReadAllText((Join-Path $root 'src\ModSettingsPlugin.cs'))
$versionMatch=[regex]::Match($pluginSource,'public const string Version = "([0-9.]+)";')
if(-not $versionMatch.Success){throw 'Cannot resolve production plugin version'}
$currentVersion=[version]$versionMatch.Groups[1].Value
$baseVersion=$currentVersion.ToString(3)
$higherVersion=[version]::new($currentVersion.Major,$currentVersion.Minor,$currentVersion.Build+1).ToString(3)
$newerVersion=[version]::new($currentVersion.Major,$currentVersion.Minor+1,0).ToString(3)
# The higher fixture is above the intermediate fixture, which is above the standalone version.
# Mixed mode therefore tests a strictly higher embedded winner without a filename-dependent tie.
function Build-Embedded([string]$HostId,[string]$Version){
    $stage=Join-Path $output "$HostId-$Version";New-Item -ItemType Directory -Path $stage | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'src') -Destination $stage -Recurse
    Copy-Item -LiteralPath (Join-Path $root 'sdk') -Destination $stage -Recurse
    Copy-Item -LiteralPath (Join-Path $root 'Properties') -Destination $stage -Recurse
    Copy-Item -LiteralPath (Join-Path $root 'assets') -Destination $stage -Recurse
    $plugin=Join-Path $stage 'src\ModSettingsPlugin.cs'
    [IO.File]::WriteAllText($plugin,([IO.File]::ReadAllText($plugin).Replace($versionMatch.Value,"public const string Version = `"$Version`";")),[Text.UTF8Encoding]::new($false))
    $project=[IO.File]::ReadAllText((Join-Path $root 'OC2ModSettings.csproj')).Replace('<AssemblyName>OC2ModSettings</AssemblyName>',"<AssemblyName>ModSettingsHost$HostId</AssemblyName>")
    [IO.File]::WriteAllText((Join-Path $stage 'Host.csproj'),$project,[Text.UTF8Encoding]::new($false))
    $source=@"
using BepInEx;
[BepInPlugin("modsettings.fixture.host.$HostId", "Embedded host $HostId", "1.0.0")]
[BepInDependency("com.ch3ngyz.plugin.overcooked2.modsettings", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class EmbeddedHost : BaseUnityPlugin {
    private void Awake() { Config.Bind("Host $HostId", "Enabled", true, "Independent host config"); }
}
"@
    [IO.File]::WriteAllText((Join-Path $stage 'src\EmbeddedHost.cs'),$source,[Text.UTF8Encoding]::new($false))
    & $msbuild (Join-Path $stage 'Host.csproj') /t:Rebuild /p:Configuration=Release "/p:GameRoot=$GameRoot" /v:minimal /nologo | Out-Host
    if($LASTEXITCODE -ne 0){throw 'Embedded source compilation failed'}
    return Join-Path $stage "bin\Release\ModSettingsHost$HostId.dll"
}
$aSame=Build-Embedded 'a' $higherVersion;$bSame=Build-Embedded 'b' $higherVersion
$bOld=Build-Embedded 'b' $baseVersion;$aNew=Build-Embedded 'a' $newerVersion
# Version changes exist only in ignored test staging, never in shipping source.
if($BuildOnly){Write-Output "PASS: four embedded SDK/provider assemblies built without deployment: $output";return}
$scenarios=@(
    @{Name='embedded-same';EmbeddedAssemblies=@($aSame,$bSame);SelectedAssembly=$aSame;ExpectedVersion=$higherVersion;NoStandalone=$true;FixtureCount=10},
    @{Name='embedded-different';EmbeddedAssemblies=@($aSame,$bOld);SelectedAssembly=$aSame;ExpectedVersion=$higherVersion;NoStandalone=$true;FixtureCount=1},
    @{Name='mixed';EmbeddedAssemblies=@($aSame,$bOld);SelectedAssembly=$aSame;ExpectedVersion=$higherVersion;FixtureCount=30},
    @{Name='embedded-newer';EmbeddedAssemblies=@($aNew,$bOld);SelectedAssembly=$aNew;ExpectedVersion=$newerVersion;FixtureCount=10}
)
foreach($scenario in $scenarios){
    $name=$scenario.Name;$scenario.Remove('Name');Write-Output "Shared loading scenario: $name"
    & (Join-Path $PSScriptRoot 'test-unity.ps1') -GameRoot $GameRoot @scenario
    "$name PASS" | Add-Content -LiteralPath (Join-Path $output 'results.txt')
}
Write-Output "Shared loading matrix passed: $output"
