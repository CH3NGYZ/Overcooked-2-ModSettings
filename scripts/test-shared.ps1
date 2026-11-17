param([string]$GameRoot='E:\SteamLibrary\steamapps\common\Overcooked! 2')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=Join-Path $root ('.validation\shared-'+(Get-Date -Format yyyyMMdd-HHmmss))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$msbuild='D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
function Build-Embedded([string]$HostId,[string]$Version){
    $stage=Join-Path $output "$HostId-$Version";New-Item -ItemType Directory -Path $stage | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'src') -Destination $stage -Recurse
    Copy-Item -LiteralPath (Join-Path $root 'Properties') -Destination $stage -Recurse
    Copy-Item -LiteralPath (Join-Path $root 'assets') -Destination $stage -Recurse
    $plugin=Join-Path $stage 'src\ModSettingsPlugin.cs'
    [IO.File]::WriteAllText($plugin,([IO.File]::ReadAllText($plugin).Replace('Version = "1.0.0"',"Version = `"$Version`"")),[Text.UTF8Encoding]::new($false))
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
$a10=Build-Embedded 'a' '1.0.0';$b10=Build-Embedded 'b' '1.0.0';$b09=Build-Embedded 'b' '0.9.0';$a11=Build-Embedded 'a' '1.1.0'
# Version changes exist only in ignored test staging, never in shipping source.
$scenarios=@(
    @{Name='embedded-same';EmbeddedAssemblies=@($a10,$b10);SelectedAssembly=$a10;ExpectedVersion='1.0.0';NoStandalone=$true;FixtureCount=10},
    @{Name='embedded-different';EmbeddedAssemblies=@($a10,$b09);SelectedAssembly=$a10;ExpectedVersion='1.0.0';NoStandalone=$true;FixtureCount=1},
    @{Name='mixed';EmbeddedAssemblies=@($a10,$b09);SelectedAssembly=$a10;ExpectedVersion='1.0.0';FixtureCount=30},
    @{Name='embedded-newer';EmbeddedAssemblies=@($a11,$b09);SelectedAssembly=$a11;ExpectedVersion='1.1.0';FixtureCount=10}
)
foreach($scenario in $scenarios){
    $name=$scenario.Name;$scenario.Remove('Name');Write-Output "Shared loading scenario: $name"
    & (Join-Path $PSScriptRoot 'test-unity.ps1') -GameRoot $GameRoot @scenario
    "$name PASS" | Add-Content -LiteralPath (Join-Path $output 'results.txt')
}
Write-Output "Shared loading matrix passed: $output"
