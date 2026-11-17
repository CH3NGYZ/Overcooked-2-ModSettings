param([string]$GameRoot='E:\SteamLibrary\steamapps\common\Overcooked! 2')
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -GameRoot $GameRoot
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=Join-Path $root '.validation\core'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$compiler='D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe'
$sources=@('src\Core\LeaseTable.cs','src\Core\EntryMetadata.cs','src\Core\RegisteredAction.cs','src\Core\CategorySort.cs','src\Core\ValueCodec.cs','src\Core\PinnedMods.cs','tests\CoreTests.cs','tests\CoreCategoryOrder.cs') | ForEach-Object {Join-Path $root $_}
& $compiler /nologo /target:exe /langversion:3 "/out:$output\CoreTests.exe" "/reference:$GameRoot\BepInEx\core\BepInEx.dll" "/reference:$GameRoot\Overcooked2_Data\Managed\UnityEngine.dll" @sources
if($LASTEXITCODE -ne 0){throw 'Core test compilation failed'}
Copy-Item -LiteralPath "$GameRoot\BepInEx\core\BepInEx.dll" -Destination $output
# These tests exercise BepInEx's actual ConfigFile; no Unity internal calls are invoked.
& "$output\CoreTests.exe" (Join-Path $output ([guid]::NewGuid().ToString('N')))
if($LASTEXITCODE -ne 0){throw 'Core tests failed'}
