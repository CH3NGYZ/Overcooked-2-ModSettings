param([string]$GameRoot='E:\SteamLibrary\steamapps\common\Overcooked! 2')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$out=Join-Path $root '.validation\examples';New-Item -ItemType Directory -Path $out -Force | Out-Null
$managed=Join-Path $GameRoot 'Overcooked2_Data\Managed'
$references=@('UnityEngine','UnityEngine.CoreModule','UnityEngine.UI','UnityEngine.UIModule','UnityEngine.TextRenderingModule','Assembly-CSharp','Assembly-CSharp-firstpass') | ForEach-Object {"/reference:$managed\$_.dll"}
$references+="/reference:$GameRoot\BepInEx\core\BepInEx.dll"
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v2.0.50727'
$references+=@("/reference:$framework\mscorlib.dll","/reference:$framework\System.dll",'/reference:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\v3.5\System.Core.dll')
$sources=Get-ChildItem -LiteralPath (Join-Path $root 'examples') -Filter *.cs | ForEach-Object FullName
& 'D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe' /nologo /noconfig /nostdlib+ /target:library /langversion:3 /warnaserror "/out:$out\ModSettings.ConsumerExample.dll" @references @sources
if($LASTEXITCODE -ne 0){throw 'Consumer/editor example compilation failed'}
Write-Output 'PASS: reflection-only consumer and native renderer compile without a service DLL reference'
