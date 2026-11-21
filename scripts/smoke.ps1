# smoke.ps1 - build, import, run the main scene headless for a few seconds, fail on any engine/script error.
#   .\scripts\smoke.ps1
. "$PSScriptRoot\_common.ps1"
Invoke-Build; Invoke-Import
$r = Invoke-Game -Headless -Script 'wait 1; press move_right; wait 1.2; release move_right; tap attack; wait 0.5; tap attack; wait 0.5; tap dodge; wait 0.6; tap parry; wait 0.6; tap jump; wait 1; print smoke-done; quit' -TimeoutSec 90
if ($r.Errors -gt 0) { Write-Host "SMOKE FAIL: $($r.Errors) error line(s)" -ForegroundColor Red; exit 1 }
Write-Host 'SMOKE PASS: main scene ran, inputs simulated, no errors' -ForegroundColor Green