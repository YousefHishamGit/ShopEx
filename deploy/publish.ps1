# Builds everything into E:\ShopEx\deploy\out : API + Angular (inside wwwroot) + shopex.sql
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out  = Join-Path $PSScriptRoot 'out'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

Write-Host '== Angular build ==' -ForegroundColor Cyan
Push-Location (Join-Path $root 'Frontend\ShopEx')
npm ci
npx ng build --configuration production
if ($LASTEXITCODE) { throw 'ng build failed' }
Pop-Location

Write-Host '== API publish ==' -ForegroundColor Cyan
dotnet publish (Join-Path $root 'ShopEx\ShopEx.csproj') -c Release -o "$out\app"
if ($LASTEXITCODE) { throw 'dotnet publish failed' }

$dist = Join-Path $root 'Frontend\ShopEx\dist\shop-ex'
if (Test-Path "$dist\browser") { $dist = "$dist\browser" }
New-Item "$out\app\wwwroot" -ItemType Directory -Force | Out-Null
Copy-Item "$dist\*" "$out\app\wwwroot" -Recurse -Force

Write-Host '== SQL migration script ==' -ForegroundColor Cyan
dotnet ef migrations script --idempotent --project (Join-Path $root 'ShopEx.Infrastructure') --startup-project (Join-Path $root 'ShopEx') --context ShopContext -o "$out\shopex.sql"
if ($LASTEXITCODE) { throw 'ef script failed' }

Remove-Item "$out\app\appsettings.Development.json" -ErrorAction SilentlyContinue
Copy-Item (Join-Path $PSScriptRoot 'DEPLOY.md') $out
Write-Host "Done -> $out" -ForegroundColor Green
