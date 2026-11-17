param([string]$GameRoot='E:\SteamLibrary\steamapps\common\Overcooked! 2',[switch]$WithoutFastInit,
    [string[]]$EmbeddedAssemblies=@(),[string]$SelectedAssembly='', [string]$ExpectedVersion='1.0.0',
    [int]$FixtureCount=30,[switch]$NoStandalone,[switch]$WithGameplay,[switch]$ShortcutOnly,[switch]$OpeningPerformance,[switch]$TextEditingOnly)
$ErrorActionPreference='Stop'
if(Get-Process -Name Overcooked2 -ErrorAction SilentlyContinue){throw 'A player game is running; no test deployment performed.'}
& (Join-Path $PSScriptRoot 'build.ps1') -GameRoot $GameRoot
$testProperties=@();if($SelectedAssembly){$testProperties+="/p:ModSettingsAssembly=$SelectedAssembly"}
& 'D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' (Join-Path $PSScriptRoot '..\tests\UnityTests.csproj') /t:Rebuild "/p:GameRoot=$GameRoot" /p:PreBuildEvent= /p:PostBuildEvent= /v:minimal /nologo @testProperties
if($LASTEXITCODE -ne 0){throw 'Unity test build failed'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$variant=if($WithoutFastInit){'without-fastinit'}else{'with-fastinit'}
$run=Join-Path $root ('.validation\'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$variant)
$backup=Join-Path $run 'backup'; New-Item -ItemType Directory -Path $backup -Force | Out-Null
function Hash([string]$Path){if(Test-Path -LiteralPath $Path -PathType Leaf){return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash}; return '-'}
$plugins=Join-Path $GameRoot 'BepInEx\plugins'
$targets=@((Join-Path $plugins 'OC2ModSettings.dll'),(Join-Path $plugins 'OC2ModSettings.UnityTests.dll'),(Join-Path $GameRoot 'BepInEx\LogOutput.log'),(Join-Path $GameRoot 'BepInEx\config\com.ch3ngyz.plugin.overcooked2.modsettings.cfg'),(Join-Path $GameRoot 'BepInEx\config\com.ch3ngyz.plugin.overcooked2.modsettings.tests.cfg'))
if($WithoutFastInit){$targets+=Join-Path $plugins 'DIYLevelFastInit.dll'}
$embeddedTargets=@();foreach($assembly in $EmbeddedAssemblies){$target=Join-Path $plugins ([IO.Path]::GetFileName($assembly));$embeddedTargets+=$target;$targets+=$target}
foreach($name in @('a','b')){$targets+=Join-Path $GameRoot "BepInEx\config\modsettings.fixture.host.$name.cfg"}
$manifest=@(); foreach($target in $targets){$saved=Join-Path $backup ([IO.Path]::GetFileName($target));$exists=Test-Path -LiteralPath $target;if($exists){Copy-Item -LiteralPath $target -Destination $saved};$manifest+=[pscustomobject]@{Target=$target;Backup=$saved;Existed=$exists;Hash=(Hash $target)}}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'manifest.json') -Encoding utf8
$owned=@{}; $failure=$null
try{
    if(Get-Process -Name Overcooked2 -ErrorAction SilentlyContinue){throw 'A player game started during preparation'}
    if($NoStandalone){if(Test-Path -LiteralPath $targets[0]){Remove-Item -LiteralPath $targets[0]}}
    else{Copy-Item -LiteralPath (Join-Path $root 'bin\Release\OC2ModSettings.dll') -Destination $targets[0];$owned[$targets[0]]=Hash $targets[0]}
    Copy-Item -LiteralPath (Join-Path $root 'bin\UnityTests\OC2ModSettings.UnityTests.dll') -Destination $targets[1]
    $owned[$targets[1]]=Hash $targets[1]
    for($i=0;$i -lt $EmbeddedAssemblies.Count;$i++){Copy-Item -LiteralPath $EmbeddedAssemblies[$i] -Destination $embeddedTargets[$i];$owned[$embeddedTargets[$i]]=Hash $embeddedTargets[$i]}
    if($WithoutFastInit -and (Test-Path -LiteralPath $targets[5])){Remove-Item -LiteralPath $targets[5]}
    $steam=(Get-Process -Name steam -ErrorAction SilentlyContinue | Select-Object -First 1).Path
    if(-not $steam){$steam=(Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam').SteamExe}
    $arguments=@('-applaunch','728880','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"'+(Join-Path $run 'player.log')+'"'),'-modSettingsTest',('"'+$run+'"'),'-modSettingsExpectedVersion',$ExpectedVersion,'-modSettingsFixtureCount',"$FixtureCount",'-modSettingsExpectedHosts',"$($EmbeddedAssemblies.Count)")
    if($WithGameplay){$arguments+='-modSettingsGameplay'}
    if($ShortcutOnly){$arguments+='-modSettingsShortcutOnly'}
    if($OpeningPerformance){$arguments+='-modSettingsOpeningPerformance'}
    if($TextEditingOnly){$arguments+='-modSettingsTextEditingOnly'}
    Start-Process -FilePath $steam -ArgumentList $arguments -WindowStyle Hidden | Out-Null
    Write-Output "Unity run: $run"
    $deadline=(Get-Date).AddSeconds(220); $seen=$false
    while((Get-Date) -lt $deadline){
        $processes=@(Get-CimInstance Win32_Process -Filter "Name='Overcooked2.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Contains($run)})
        if($processes.Count -gt 0){$seen=$true}
        if($seen -and $processes.Count -eq 0){break}
        Start-Sleep -Milliseconds 500
    }
    if(-not $seen){throw 'No owned game process appeared'}
    if($processes.Count -gt 0){throw 'Unity scenario timed out'}
    $result=Get-Content -LiteralPath (Join-Path $run 'result.txt') -Raw
    Write-Output $result
    if(-not $result.StartsWith('PASS:')){throw 'Unity scenario failed'}
}catch{$failure=$_; $_ | Out-String | Set-Content -LiteralPath (Join-Path $run 'script-error.txt'); Write-Warning "Test failure: $_"}
finally{
    $restoreErrors=[Collections.Generic.List[string]]::new(); $preserved=[Collections.Generic.List[string]]::new()
    foreach($process in @(Get-CimInstance Win32_Process -Filter "Name='Overcooked2.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Contains($run)})){
        $ownedProcess=Get-Process -Id $process.ProcessId -ErrorAction SilentlyContinue
        if($ownedProcess){$ownedProcess.Kill();$ownedProcess.WaitForExit(15000) | Out-Null}
    }
    Start-Sleep -Milliseconds 700
    if(Test-Path -LiteralPath $targets[2]){Copy-Item -LiteralPath $targets[2] -Destination (Join-Path $run 'bepinex.log')}
    $saveManifest=Join-Path $run 'saves.tsv'
    if(Test-Path -LiteralPath $saveManifest){foreach($line in Get-Content -LiteralPath $saveManifest){$parts=$line.Split("`t");if((Hash $parts[1]) -ne $parts[0]){$restoreErrors.Add("Original save changed: $($parts[1])")}}}
    foreach($item in $manifest){
        if($owned.ContainsKey($item.Target) -and (Hash $item.Target) -ne $owned[$item.Target]){$preserved.Add($item.Target);Write-Warning "Replacement preserved: $($item.Target)";continue}
        if($WithoutFastInit -and $item.Target -eq $targets[5] -and (Test-Path -LiteralPath $item.Target)){$preserved.Add($item.Target);Write-Warning "New FastInit preserved; original backup: $($item.Backup)";continue}
        try{
            if($item.Existed){Copy-Item -LiteralPath $item.Backup -Destination $item.Target -Force;if((Hash $item.Target) -ne $item.Hash){throw 'Restore hash mismatch'}}
            elseif(Test-Path -LiteralPath $item.Target){Remove-Item -LiteralPath $item.Target}
        }catch{$restoreErrors.Add("$($item.Target): $_")}
    }
    if($restoreErrors.Count){$failure=$restoreErrors -join "`n";Write-Warning $failure}
    elseif($preserved.Count){Write-Warning "Original saves checked; $($preserved.Count) replacement file(s) preserved, other temporary files restored. Evidence: $run"}
    else{Write-Output "Original saves checked; temporary installation restored. Evidence: $run"}
}
if($failure){throw $failure}
