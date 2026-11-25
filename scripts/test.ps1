param([string]$GameRoot='E:\SteamLibrary\steamapps\common\Overcooked! 2')
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -GameRoot $GameRoot
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=Join-Path $root '.validation\core'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$compiler='D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe'
$sdkSources=Get-ChildItem -LiteralPath (Join-Path $root 'sdk') -Filter *.cs | ForEach-Object FullName
& $compiler /nologo /target:library /langversion:3 /warnaserror "/out:$output\ModSettings.SdkConsumer.dll" "/reference:$GameRoot\BepInEx\core\BepInEx.dll" "/reference:$GameRoot\Overcooked2_Data\Managed\UnityEngine.dll" "/reference:$GameRoot\Overcooked2_Data\Managed\UnityEngine.CoreModule.dll" @sdkSources
if($LASTEXITCODE -ne 0){throw 'SDK source-only consumer compilation failed'}
$sources=Get-ChildItem -LiteralPath (Join-Path $root 'tests') -Filter Core*.cs | ForEach-Object FullName
# Exercise the built provider, and an independently compiled copy of the SDK tag type.
& $compiler /nologo /target:exe /langversion:3 "/out:$output\OC2ModSettings.Tests.exe" "/reference:$root\bin\Release\OC2ModSettings.dll" "/reference:ConsumerSdk=$output\ModSettings.SdkConsumer.dll" "/reference:$GameRoot\BepInEx\core\BepInEx.dll" "/reference:$GameRoot\Overcooked2_Data\Managed\UnityEngine.dll" "/reference:$GameRoot\Overcooked2_Data\Managed\UnityEngine.CoreModule.dll" "/reference:$GameRoot\Overcooked2_Data\Managed\Assembly-CSharp.dll" @sources
if($LASTEXITCODE -ne 0){throw 'Core test compilation failed'}
Copy-Item -LiteralPath "$GameRoot\BepInEx\core\BepInEx.dll" -Destination $output
Copy-Item -LiteralPath "$root\bin\Release\OC2ModSettings.dll" -Destination $output
foreach($assembly in @('Assembly-CSharp.dll','UnityEngine.dll','UnityEngine.CoreModule.dll')){
    Copy-Item -LiteralPath "$GameRoot\Overcooked2_Data\Managed\$assembly" -Destination $output
}
# These tests exercise BepInEx's actual ConfigFile; no Unity internal calls are invoked.
& "$output\OC2ModSettings.Tests.exe" (Join-Path $output ([guid]::NewGuid().ToString('N')))
if($LASTEXITCODE -ne 0){throw 'Core tests failed'}
