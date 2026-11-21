# steam-upload.ps1 - upload the Windows build to Steam with SteamPipe (steamcmd).
# Needs: a Steamworks partner account, an App ID (for a demo: the demo's own App ID) and its Windows depot ID,
# and steamcmd (https://developer.valvesoftware.com/wiki/SteamCMD - unzip it, e.g. to C:\steamcmd).
# YOU log in: steamcmd asks for your password / Steam Guard code in this terminal. Nothing is stored by this script.
#   .\scripts\steam-upload.ps1 -AppId 1234560 -DepotId 1234561 -SteamUser yourname            upload to the 'beta' branch
#   .\scripts\steam-upload.ps1 -AppId 1234560 -DepotId 1234561 -SteamUser yourname -Preview   dry run (no upload)
# Making a build live on the default (public) branch is done by hand in Steamworks > SteamPipe > Builds.
param(
    [Parameter(Mandatory)][string]$AppId, [Parameter(Mandatory)][string]$DepotId, [Parameter(Mandatory)][string]$SteamUser,
    [string]$Branch = 'beta', [string]$SteamCmd = 'C:\steamcmd\steamcmd.exe', [switch]$Preview, [switch]$NoExport
)
. "$PSScriptRoot\_common.ps1"
if (-not (Test-Path $SteamCmd)) { throw "steamcmd not found at $SteamCmd (download it from Valve, see the comment at the top)" }
if (-not $NoExport) { & "$PSScriptRoot\package.ps1" }
$ver = (Get-Content (Join-Path $Root 'VERSION') -Raw).Trim()
$work = Join-Path $Root 'build\steam'
New-Item -ItemType Directory -Force (Join-Path $work 'output') | Out-Null
$vdf = (Get-Content (Join-Path $Root 'steam\app_build.template.vdf') -Raw).
    Replace('__APPID__', $AppId).Replace('__DEPOTID__', $DepotId).Replace('__DESC__', "Clash of Thrones v$ver").
    Replace('__CONTENT__', (Join-Path $Root 'build\windows\')).Replace('__OUTPUT__', (Join-Path $work 'output\')).
    Replace('__BRANCH__', $(if ($Branch -eq 'default') { '' } else { $Branch })).Replace('__PREVIEW__', $(if ($Preview) { '1' } else { '0' }))
$vdfPath = Join-Path $work "app_build_$AppId.vdf"
Set-Content $vdfPath $vdf -Encoding ASCII
Write-Host "steam    app $AppId, depot $DepotId, branch '$Branch', preview=$([bool]$Preview)"
& $SteamCmd +login $SteamUser +run_app_build $vdfPath +quit