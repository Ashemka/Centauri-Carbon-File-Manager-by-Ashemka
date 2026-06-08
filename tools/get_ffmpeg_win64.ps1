$ErrorActionPreference = "Stop"

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $toolsDir "ffmpeg.exe"
if (Test-Path $target) {
    Write-Host "ffmpeg.exe est deja present : $target"
    exit 0
}

$url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
$zip = Join-Path $toolsDir "ffmpeg-release-essentials.zip"
$extractDir = Join-Path $toolsDir "_ffmpeg_extract"

Write-Host "Telechargement de FFmpeg..."
Write-Host $url
Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing

if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }
New-Item -ItemType Directory -Path $extractDir | Out-Null

Write-Host "Extraction..."
Expand-Archive -Path $zip -DestinationPath $extractDir -Force

$ffmpeg = Get-ChildItem -Path $extractDir -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1
if (-not $ffmpeg) {
    throw "ffmpeg.exe introuvable dans l'archive telechargee."
}

Copy-Item $ffmpeg.FullName $target -Force
Write-Host "OK : ffmpeg.exe place dans $target"

Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zip -Force -ErrorAction SilentlyContinue
