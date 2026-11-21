# setup-art-tools.ps1 - install the local AI art pipeline: ComfyUI + SDXL base + Pixel Art XL LoRA + SDXL fp16 VAE.
# Everything goes OUTSIDE the repo (default: <repo>\..\..\ArtTools); models are ~8-10 GB. Safe to rerun (skips what exists).
#   .\scripts\setup-art-tools.ps1                     .\scripts\setup-art-tools.ps1 -Root D:\ArtTools
# Needs: git, uv (winget install astral-sh.uv), an NVIDIA GPU with 8 GB+ VRAM.
# Model licences: SDXL base = CreativeML Open RAIL++-M, Pixel Art XL = CreativeML Open RAIL-M, VAE fix = MIT (see docs/ASSET_LICENCES.md).
param([string]$Root = (Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent) 'ArtTools'))
$ErrorActionPreference = 'Continue'
$t0 = Get-Date
New-Item -ItemType Directory -Force $Root | Out-Null
$comfy = Join-Path $Root 'ComfyUI'
Write-Host "art tools -> $Root"

if (-not (Test-Path (Join-Path $comfy 'main.py'))) {
    git clone --depth 1 https://github.com/comfyanonymous/ComfyUI.git $comfy 2>&1 | Select-Object -Last 1
}
Push-Location $comfy
if (-not (Test-Path '.venv')) { uv venv --python 3.12 .venv 2>&1 | Select-Object -Last 1 }
$py = Join-Path $comfy '.venv\Scripts\python.exe'
& $py -c "import torch,sys; sys.exit(0 if torch.cuda.is_available() else 1)" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'installing PyTorch (CUDA 12.8)...'
    uv pip install --python $py torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu128 2>&1 | Select-Object -Last 1
}
uv pip install --python $py -r requirements.txt 2>&1 | Select-Object -Last 1
Pop-Location

$models = @(
    @{ url = 'https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/resolve/main/sd_xl_base_1.0.safetensors'; to = 'models\checkpoints\sd_xl_base_1.0.safetensors' },
    @{ url = 'https://huggingface.co/nerijs/pixel-art-xl/resolve/main/pixel-art-xl.safetensors'; to = 'models\loras\pixel-art-xl.safetensors' },
    @{ url = 'https://huggingface.co/madebyollin/sdxl-vae-fp16-fix/resolve/main/sdxl.vae.safetensors'; to = 'models\vae\sdxl-vae-fp16-fix.safetensors' }
)
foreach ($m in $models) {
    $dest = Join-Path $comfy $m.to
    if ((Test-Path $dest) -and (Get-Item $dest).Length -gt 1MB) { Write-Host "model ok  $($m.to)"; continue }
    Write-Host "download  $($m.to)"
    curl.exe -L --fail --retry 3 -C - -o $dest $m.url 2>&1 | Select-Object -Last 1
}
& $py -c "import torch; print('cuda', torch.cuda.is_available(), torch.cuda.get_device_name(0) if torch.cuda.is_available() else '')"
Write-Host ("done in {0:n0} s" -f ((Get-Date) - $t0).TotalSeconds)
