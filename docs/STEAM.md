# Steam: how to get the demo on Steam
**What:** the steps, costs and files to publish a Clash of Thrones demo on Steam. **Updated:** 2026-11-21 · issue [#13](https://github.com/CodTanium/ClashofThrones/issues/13)
The build side is ready (`scripts\steam-upload.ps1`). The account side can only be done by the owner (identity, tax, payment).

## Costs and timing
| Item | Cost / time |
|---|---|
| Steam Direct fee | **100 USD per game** (paid back after the game earns 1,000 USD). A demo is part of its game, so it needs no fee of its own |
| Waiting period | 30 days between paying the fee and releasing the game; the demo can go live earlier, once its page and build are approved |
| Store page review | about 3-5 working days |
| Build review | about 3-5 working days |
| "Coming soon" page | must be public at least 2 weeks before release |

## Checklist
1. [ ] **Steamworks account**: partner.steamgames.com → sign up, then fill in identity, tax and bank details (owner only).
2. [ ] **Pay the Steam Direct fee** and create the app "Clash of Thrones" → you get an **App ID**.
3. [ ] **Store page** for the main game (description, tags, content survey, system requirements) and its assets:
   | Asset | Size |
   |---|---|
   | Header capsule | 920 x 430 |
   | Small capsule | 462 x 174 |
   | Main capsule | 1232 x 706 |
   | Vertical capsule | 748 x 896 |
   | Library capsule / hero / logo | 600 x 900 · 3840 x 1240 · 1280 x 720 (transparent) |
   | Screenshots | 5 or more, 1920 x 1080 (the game is 640 x 360, scale 3x with nearest filtering) |
   | Trailer | optional but strongly recommended |
4. [ ] **Create the demo**: in Steamworks open the main app → "Create Demo". The demo gets its own **App ID** and **Windows depot ID**.
5. [ ] **Configure the demo**: Installation → General: launch option `ClashOfThrones.exe`, OS Windows. Depots: one Windows depot.
6. [ ] **Install steamcmd** (Valve's SteamCMD zip, e.g. to `C:\steamcmd`).
7. [ ] **Upload**: `.\scripts\steam-upload.ps1 -AppId <demo app id> -DepotId <depot id> -SteamUser <your login>` (add `-Preview` to do a dry run first). You type your password / Steam Guard code into steamcmd yourself.
8. [ ] In Steamworks → SteamPipe → Builds: set the uploaded build live on the **default** branch, publish, and request review.
9. [ ] Optional: a **Steam Playtest** (closed test inside Steam), and a **Steam Next Fest** demo slot.

## Before a public Steam release (later)
- Code-signing is not needed on Steam (Steam handles the install), but a licence decision (#4), credits and the asset list ([ASSET_LICENCES.md](ASSET_LICENCES.md)) should be final.
- Steam features (achievements, cloud saves, overlay) need the Steamworks SDK in the game (for Godot: the GodotSteam add-on). They aren't needed for a first demo.
- The DevBridge is already off in release builds.

## Files
| File | Job |
|---|---|
| `steam/app_build.template.vdf` | SteamPipe build template (filled in by the script) |
| `scripts/steam-upload.ps1` | export → fill in the template → steamcmd upload |
| `scripts/package.ps1` | the Windows release build that is uploaded (`build\windows\`) |
