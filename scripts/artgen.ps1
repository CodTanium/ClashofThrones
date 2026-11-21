# artgen.ps1 - generate pixel art with the local ComfyUI (SDXL + Pixel Art XL LoRA), headless through its API.
# Starts ComfyUI in the background when it is not running (127.0.0.1:8188). Install it first: scripts\setup-art-tools.ps1
#   txt2img : .\scripts\artgen.ps1 -Prompt "knight ..." -Out reports\art\concept.png -Count 4
#   img2img : .\scripts\artgen.ps1 -Prompt "..." -Init in.png -Denoise 0.5 -Out out.png
#   batch   : .\scripts\artgen.ps1 -Prompt "..." -Init <dir of png> -Out <dir> -Denoise 0.45 -Seed 7   (same seed for every frame)
param(
    [Parameter(Mandatory)][string]$Prompt,
    [string]$Negative = 'blurry, smooth shading, gradient, 3d render, photo, text, watermark, signature, frame, border, multiple characters, cropped, extra limbs, lowres, jpeg artifacts',
    [string]$Out = 'reports\art\gen.png',
    [string]$Init, [double]$Denoise = 0.5,
    [int]$Width = 1024, [int]$Height = 1024, [int]$Count = 1, [long]$Seed = -1,
    [int]$Steps = 28, [double]$Cfg = 6.5, [double]$Lora = 1.0,
    [string]$ArtTools = (Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent) 'ArtTools'),
    [switch]$Stop
)
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent
$api = 'http://127.0.0.1:8188'
$comfy = Join-Path $ArtTools 'ComfyUI'

function Test-Comfy { try { Invoke-RestMethod "$api/system_stats" -TimeoutSec 2 | Out-Null; $true } catch { $false } }
if (-not (Test-Comfy)) {
    $py = Join-Path $comfy '.venv\Scripts\python.exe'
    if (-not (Test-Path $py)) { throw "ComfyUI not installed in $comfy - run scripts\setup-art-tools.ps1" }
    Write-Host 'comfyui  starting (first start loads the model, ~1 min)...'
    Start-Process $py -ArgumentList 'main.py', '--listen', '127.0.0.1', '--port', '8188', '--disable-auto-launch' -WorkingDirectory $comfy -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $ArtTools 'comfyui.log') -RedirectStandardError (Join-Path $ArtTools 'comfyui.err.log') | Out-Null
    $t = 0; while (-not (Test-Comfy)) { Start-Sleep 2; $t += 2; if ($t -gt 240) { throw "ComfyUI did not start - see $ArtTools\comfyui.err.log" } }
}
Write-Host 'comfyui  ready'

function Resolve-Out([string]$p) { if ([IO.Path]::IsPathRooted($p)) { $p } else { Join-Path $Root $p } }

function Invoke-Gen([string]$initFile, [string]$outFile, [long]$seed) {
    $wf = @{
        '1' = @{ class_type = 'CheckpointLoaderSimple'; inputs = @{ ckpt_name = 'sd_xl_base_1.0.safetensors' } }
        '2' = @{ class_type = 'LoraLoader'; inputs = @{ model = @('1', 0); clip = @('1', 1); lora_name = 'pixel-art-xl.safetensors'; strength_model = $Lora; strength_clip = $Lora } }
        '3' = @{ class_type = 'VAELoader'; inputs = @{ vae_name = 'sdxl-vae-fp16-fix.safetensors' } }
        '4' = @{ class_type = 'CLIPTextEncode'; inputs = @{ text = $Prompt; clip = @('2', 1) } }
        '5' = @{ class_type = 'CLIPTextEncode'; inputs = @{ text = $Negative; clip = @('2', 1) } }
        '7' = @{ class_type = 'KSampler'; inputs = @{ model = @('2', 0); positive = @('4', 0); negative = @('5', 0); seed = $seed; steps = $Steps; cfg = $Cfg; sampler_name = 'dpmpp_2m'; scheduler = 'karras'; denoise = 1.0 } }
        '8' = @{ class_type = 'VAEDecode'; inputs = @{ samples = @('7', 0); vae = @('3', 0) } }
        '9' = @{ class_type = 'SaveImage'; inputs = @{ images = @('8', 0); filename_prefix = 'cot' } }
    }
    if ($initFile) {
        $up = curl.exe -s -F "image=@$initFile" -F 'overwrite=true' "$api/upload/image" | ConvertFrom-Json
        $wf['10'] = @{ class_type = 'LoadImage'; inputs = @{ image = $up.name } }
        $wf['11'] = @{ class_type = 'VAEEncode'; inputs = @{ pixels = @('10', 0); vae = @('3', 0) } }
        $wf['7'].inputs.latent_image = @('11', 0); $wf['7'].inputs.denoise = $Denoise
    } else {
        $wf['6'] = @{ class_type = 'EmptyLatentImage'; inputs = @{ width = $Width; height = $Height; batch_size = 1 } }
        $wf['7'].inputs.latent_image = @('6', 0)
    }
    $body = @{ prompt = $wf; client_id = 'cot-artgen' } | ConvertTo-Json -Depth 10
    $id = (Invoke-RestMethod "$api/prompt" -Method Post -Body ([Text.Encoding]::UTF8.GetBytes($body)) -ContentType 'application/json').prompt_id
    $t0 = Get-Date
    while ($true) {
        Start-Sleep -Milliseconds 700
        $h = Invoke-RestMethod "$api/history/$id"
        $entry = $h.$id
        if ($entry -and $entry.outputs.'9') { break }
        if ($entry -and $entry.status.status_str -eq 'error') { throw "generation failed: $($entry.status.messages | ConvertTo-Json -Depth 5 -Compress)" }
        if (((Get-Date) - $t0).TotalSeconds -gt 600) { throw 'generation timed out' }
    }
    $img = $entry.outputs.'9'.images[0]
    New-Item -ItemType Directory -Force (Split-Path $outFile) | Out-Null
    Invoke-WebRequest "$api/view?filename=$([uri]::EscapeDataString($img.filename))&subfolder=$([uri]::EscapeDataString($img.subfolder))&type=output" -OutFile $outFile -UseBasicParsing
    Write-Host ("image    {0}  ({1:n0} s, seed {2})" -f $outFile, ((Get-Date) - $t0).TotalSeconds, $seed)
}

$rand = New-Object Random
if ($Seed -lt 0) { $Seed = $rand.Next() }
$outPath = Resolve-Out $Out
if ($Init -and (Test-Path $Init -PathType Container)) {
    New-Item -ItemType Directory -Force $outPath | Out-Null
    foreach ($f in Get-ChildItem $Init -Filter *.png | Sort-Object Name) { Invoke-Gen $f.FullName (Join-Path $outPath $f.Name) $Seed }
} else {
    $initFile = if ($Init) { (Resolve-Path $Init).Path } else { $null }
    for ($i = 0; $i -lt $Count; $i++) {
        $o = if ($Count -gt 1) { $outPath -replace '\.png$', "-$($i + 1).png" } else { $outPath }
        Invoke-Gen $initFile $o ($Seed + $i)
    }
}
if ($Stop) { Get-CimInstance Win32_Process -Filter "Name='python.exe'" | Where-Object CommandLine -match 'main.py.*8188' | ForEach-Object { Stop-Process -Id $_.ProcessId } ; Write-Host 'comfyui  stopped' }
