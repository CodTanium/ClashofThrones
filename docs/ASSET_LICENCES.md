# Asset licences
**What:** every asset in the game, where it comes from, and its licence. Add a row before an asset is used.

| Asset | Source | Author | Licence | Commercial use | Attribution | Modified |
|---|---|---|---|---|---|---|
| All sprites, tiles, layers, props (`src/scripts/art/*`) | generated in code | this project | same as the project | yes | no | - |
| Sound effects (`Sfx` in `src/scripts/world/Effects.cs`) | synthesised in code | this project | same as the project | yes | no | - |
| Font | Godot default font | Godot Engine contributors | MIT | yes | in credits | no |
| Godot Engine | godotengine.org | Godot Engine contributors | MIT | yes | in credits | no |
## AI models used as tools (not shipped with the game)
Installed outside the repo by `scripts/setup-art-tools.ps1`. Images they produce are cleaned and edited for this project.
| Model | Source | Licence | Commercial use of outputs |
|---|---|---|---|
| Stable Diffusion XL base 1.0 | huggingface.co/stabilityai/stable-diffusion-xl-base-1.0 | CreativeML Open RAIL++-M | yes (use-based restrictions apply) |
| Pixel Art XL (LoRA) | huggingface.co/nerijs/pixel-art-xl | CreativeML Open RAIL-M | yes (use-based restrictions apply) |
| SDXL VAE fp16 fix | huggingface.co/madebyollin/sdxl-vae-fp16-fix | MIT | yes |
| Inno Setup 6 (installer builder) | jrsoftware.org | Inno Setup licence (free, commercial use allowed) | n/a (tool) |
| ComfyUI (software) | github.com/comfyanonymous/ComfyUI | GPL-3.0 (tool only, not distributed with the game) | n/a |

## Generated with the AI tools
| Asset | How | Licence |
|---|---|---|
| `src/art/hero.png` (hero sheet) | rig frames restyled with SDXL + Pixel Art XL (img2img, denoise 0.58-0.68), cleaned in-engine | same as the project |
| `reports/art/hero-concept-*.png` | SDXL + Pixel Art XL, text prompt (concepts only) | same as the project |
| `src/art/warden.png`, `ghoul.png`, `archer.png`, `abbot.png` | code rigs restyled with SDXL + Pixel Art XL (img2img, denoise 0.6-0.62), cleaned in-engine | same as the project |
| Level art, props, music, sound effects (v0.3) | generated / synthesised in code | same as the project |
