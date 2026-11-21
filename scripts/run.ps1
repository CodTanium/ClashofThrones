# run.ps1 - build and play.   .\scripts\run.ps1   (add -Editor to open the Godot editor instead)
param([switch]$Editor)
. "$PSScriptRoot\_common.ps1"
Invoke-Build
if ($Editor) { Start-Process (Find-Godot) -ArgumentList '--editor', '--path', "`"$Src`"" }
else { Start-Process (Find-Godot) -ArgumentList '--path', "`"$Src`"" }