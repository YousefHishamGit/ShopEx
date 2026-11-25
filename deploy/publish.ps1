# Builds everything into E:\ShopEx\deploy\out : API + Angular (inside wwwroot) + shopex.sql
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out  = Join-Path $PSScriptRoot 'out'
# clean the output but keep out\cert (the server's HTTPS certificate files)
if (Test-Path $out) { Get-ChildItem $out -Force | Where-Object { $_.Name -ne 'cert' } | Remove-Item -Recurse -Force }

Write-Host '== Angular build ==' -ForegroundColor Cyan
Push-Location (Join-Path $root 'Frontend\ShopEx')
if (-not (Test-Path 'node_modules')) { npm ci }
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

# UPDATE package: same files but WITHOUT web.config / appsettings*.json, so a re-deploy
# never overwrites the server's environment variables and connection strings.
Write-Host '== Update package (no config files) ==' -ForegroundColor Cyan
robocopy "$out\app" "$out\update" /E /XF web.config appsettings.json appsettings.*.json /NFL /NDL /NJH /NJS | Out-Null
if ($LASTEXITCODE -ge 8) { throw 'robocopy failed' }
Copy-Item (Join-Path $PSScriptRoot 'DEPLOY.md') $out
# HTTPS certificate (pfx for IIS, cer for the users' PCs); deploy\cert is git-ignored
$cert = Join-Path $PSScriptRoot 'cert'
if (Test-Path $cert) { Copy-Item $cert $out -Recurse -Force } else { Write-Warning 'deploy\cert not found: add the HTTPS certificate manually' }
Write-Host "Done -> $out" -ForegroundColor Green
