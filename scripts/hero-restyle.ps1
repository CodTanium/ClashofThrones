# hero-restyle.ps1 - shortcut for: .\scripts\restyle.ps1 -Rig hero  (same options: -Only, -Denoise, -Seed, -Revert)
param([string[]]$Only, [double]$Denoise = -1, [long]$Seed = 4242, [switch]$Revert, [switch]$KeepComfy)
& "$PSScriptRoot\restyle.ps1" -Rig hero -Only $Only -Denoise $Denoise -Seed $Seed -Revert:$Revert -KeepComfy:$KeepComfy