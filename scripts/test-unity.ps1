param([string]$GameRoot='E:\SteamLibrary\steamapps\common\Overcooked! 2',[switch]$WithoutFastInit,
    [string[]]$EmbeddedAssemblies=@(),[string]$SelectedAssembly='', [string]$ExpectedVersion='',
    [int]$FixtureCount=30,[switch]$NoStandalone,[switch]$WithGameplay,[switch]$ShortcutOnly,[switch]$OpeningPerformance,[switch]$TextEditingOnly,[switch]$LocalizationOnly,[switch]$TranslationOnly,[switch]$ThemeFixture)
$ErrorActionPreference='Stop'
if($TranslationOnly -and ($WithGameplay -or $ShortcutOnly -or $OpeningPerformance -or $TextEditingOnly -or $LocalizationOnly -or $ThemeFixture)){throw '-TranslationOnly cannot be combined with another Unity scenario.'}
$GameRoot=[IO.Path]::GetFullPath($GameRoot)
if(Get-Process -Name Overcooked2 -ErrorAction SilentlyContinue){throw 'A player game is running; no test deployment performed.'}
& (Join-Path $PSScriptRoot 'build.ps1') -GameRoot $GameRoot
if(-not $ExpectedVersion){$ExpectedVersion=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $PSScriptRoot '..\bin\Release\OC2ModSettings.dll')).Version.ToString(3)}
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
$translationRoot=Join-Path $GameRoot 'BepInEx\translate';$translationFixtures=@{}
function Assert-TranslationPath([string]$Target){
    foreach($path in @($GameRoot,(Join-Path $GameRoot 'BepInEx'),$translationRoot,(Split-Path -LiteralPath $Target),$Target)){
        if(-not (Test-Path -LiteralPath $path)){continue}
        $item=Get-Item -LiteralPath $path -Force
        if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or ($item.PSIsContainer -eq ($path -eq $Target))){throw "Unsafe translation fixture path: $path"}
    }
}
if($TranslationOnly){
    foreach($locale in @('en','zh-CN')){
        $relative="$locale\fixture.modsettings.translation.txt"
        $target=Join-Path $translationRoot $relative
        $translationFixtures[$target]=Join-Path $root "tests\fixtures\translate\$relative"
        $targets+=$target
    }
}
$manifest=@();$seenTargets=@{}
foreach($target in $targets){
    if($seenTargets.ContainsKey($target)){throw "Duplicate Unity test target: $target"}
    $seenTargets[$target]=$true
    $saved=Join-Path $backup ('{0:D2}-{1}' -f $manifest.Count,[IO.Path]::GetFileName($target))
    if($translationFixtures.ContainsKey($target)){Assert-TranslationPath $target}
    $exists=Test-Path -LiteralPath $target -PathType Leaf;$originalHash=Hash $target;$fixtureHash=$null
    if($translationFixtures.ContainsKey($target)){
        $fixtureHash=Hash $translationFixtures[$target]
        if($fixtureHash -eq '-'){throw "Missing translation fixture source: $($translationFixtures[$target])"}
    }
    if($exists){Copy-Item -LiteralPath $target -Destination $saved;if((Hash $saved) -ne $originalHash){throw "File changed while backing up: $target"}}
    $manifest+=[pscustomobject]@{Target=$target;Backup=$saved;Existed=$exists;Hash=$originalHash;FixtureHash=$fixtureHash}
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'manifest.json') -Encoding utf8
$owned=@{}; $failure=$null; $launchRequested=$false
$translationDirectories=[Collections.Generic.List[string]]::new()
try{
    if(Get-Process -Name Overcooked2 -ErrorAction SilentlyContinue){throw 'A player game started during preparation'}
    foreach($item in $manifest){
        if(-not $item.FixtureHash){continue}
        Assert-TranslationPath $item.Target
        if((Hash $item.Target) -ne $item.Hash){throw "Translation fixture target changed during preparation: $($item.Target)"}
        foreach($directory in @($translationRoot,(Split-Path -LiteralPath $item.Target))){
            if(-not (Test-Path -LiteralPath $directory)){
                New-Item -ItemType Directory -Path $directory | Out-Null
                $translationDirectories.Add($directory)
            }
        }
        $owned[$item.Target]=$item.FixtureHash
        Copy-Item -LiteralPath $translationFixtures[$item.Target] -Destination $item.Target
        if((Hash $item.Target) -ne $item.FixtureHash){throw "Translation fixture deployment hash mismatch: $($item.Target)"}
    }
    if($NoStandalone){if(Test-Path -LiteralPath $targets[0]){Remove-Item -LiteralPath $targets[0];$owned[$targets[0]]='-'}}
    else{Copy-Item -LiteralPath (Join-Path $root 'bin\Release\OC2ModSettings.dll') -Destination $targets[0];$owned[$targets[0]]=Hash $targets[0]}
    Copy-Item -LiteralPath (Join-Path $root 'bin\UnityTests\OC2ModSettings.UnityTests.dll') -Destination $targets[1]
    $owned[$targets[1]]=Hash $targets[1]
    for($i=0;$i -lt $EmbeddedAssemblies.Count;$i++){Copy-Item -LiteralPath $EmbeddedAssemblies[$i] -Destination $embeddedTargets[$i];$owned[$embeddedTargets[$i]]=Hash $embeddedTargets[$i]}
    if($WithoutFastInit -and (Test-Path -LiteralPath $targets[5])){Remove-Item -LiteralPath $targets[5];$owned[$targets[5]]='-'}
    $steam=(Get-Process -Name steam -ErrorAction SilentlyContinue | Select-Object -First 1).Path
    if(-not $steam){$steam=(Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam').SteamExe}
    $arguments=@('-applaunch','728880','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"'+(Join-Path $run 'player.log')+'"'),'-modSettingsTest',('"'+$run+'"'),'-modSettingsExpectedVersion',$ExpectedVersion,'-modSettingsFixtureCount',"$FixtureCount",'-modSettingsExpectedHosts',"$($EmbeddedAssemblies.Count)")
    if($WithGameplay){$arguments+='-modSettingsGameplay'}
    if($ShortcutOnly){$arguments+='-modSettingsShortcutOnly'}
    if($OpeningPerformance){$arguments+='-modSettingsOpeningPerformance'}
    if($TextEditingOnly){$arguments+='-modSettingsTextEditingOnly'}
    if($LocalizationOnly){$arguments+='-modSettingsLocalizationOnly'}
    if($TranslationOnly){$arguments+='-modSettingsTranslationOnly'}
    if($ThemeFixture){$arguments+='-modSettingsThemeFixture'}
    if(Get-Process -Name Overcooked2 -ErrorAction SilentlyContinue){throw 'A player game started before launching the scenario'}
    $launchRequested=$true
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
    if(Get-Process -Name Overcooked2 -ErrorAction SilentlyContinue){throw "A game process is still running; installation and backups left untouched. Evidence: $run"}
    if(Test-Path -LiteralPath $targets[2]){Copy-Item -LiteralPath $targets[2] -Destination (Join-Path $run 'bepinex.log')}
    $saveManifest=Join-Path $run 'saves.tsv'
    if(Test-Path -LiteralPath $saveManifest){foreach($line in Get-Content -LiteralPath $saveManifest){$parts=$line.Split("`t");if((Hash $parts[1]) -ne $parts[0]){$restoreErrors.Add("Original save changed: $($parts[1])")}}}
    foreach($item in $manifest){
        if($item.FixtureHash){
            if(-not $owned.ContainsKey($item.Target)){continue}
            try{Assert-TranslationPath $item.Target}catch{$restoreErrors.Add("$($item.Target): $_");continue}
            $currentHash=Hash $item.Target
            if($currentHash -eq $item.Hash){continue}
            if($currentHash -ne $item.FixtureHash){$restoreErrors.Add("Translation target changed externally ($currentHash): $($item.Target); original ($($item.Hash)) backup: $($item.Backup)");continue}
        }elseif(-not $launchRequested -and -not $owned.ContainsKey($item.Target)){continue}
        if($WithoutFastInit -and $item.Target -eq $targets[5] -and (Test-Path -LiteralPath $item.Target)){$preserved.Add($item.Target);Write-Warning "New FastInit preserved; original backup: $($item.Backup)";continue}
        if(-not $item.FixtureHash -and $owned.ContainsKey($item.Target) -and (Hash $item.Target) -ne $owned[$item.Target]){$preserved.Add($item.Target);Write-Warning "Replacement preserved: $($item.Target)";continue}
        try{
            if($item.Existed){Copy-Item -LiteralPath $item.Backup -Destination $item.Target -Force;if((Hash $item.Target) -ne $item.Hash){throw 'Restore hash mismatch'}}
            elseif(Test-Path -LiteralPath $item.Target){Remove-Item -LiteralPath $item.Target}
        }catch{$restoreErrors.Add("$($item.Target): $_")}
    }
    for($directoryIndex=$translationDirectories.Count-1;$directoryIndex -ge 0;$directoryIndex--){
        $directory=$translationDirectories[$directoryIndex]
        try{
            $resolved=[IO.Path]::GetFullPath($directory)
            if($resolved -ne $translationRoot -and -not $resolved.StartsWith($translationRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Directory outside translation fixture root: $resolved"}
            if(-not (Test-Path -LiteralPath $resolved)){continue}
            Assert-TranslationPath (Join-Path $resolved 'fixture.modsettings.translation.txt')
            if(-not (Get-ChildItem -LiteralPath $resolved -Force | Select-Object -First 1)){[IO.Directory]::Delete($resolved)}
        }catch{$restoreErrors.Add("${directory}: $_")}
    }
    if($restoreErrors.Count){$failure=$restoreErrors -join "`n";Write-Warning $failure}
    elseif($preserved.Count){Write-Warning "Original saves checked; $($preserved.Count) replacement file(s) preserved, other temporary files restored. Evidence: $run"}
    else{Write-Output "Original saves checked; temporary installation restored. Evidence: $run"}
}
if($failure){throw $failure}
