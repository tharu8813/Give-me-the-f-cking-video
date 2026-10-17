$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'src/GMTFV.Wpf/GMTFV.Wpf.csproj'
$publishPath = Join-Path $PSScriptRoot 'artifacts/GMTFV-Wpf'
$archivePath = Join-Path $PSScriptRoot 'artifacts/GMTFV-Wpf.zip'
dotnet publish $projectPath --configuration Release --self-contained false --artifacts-path (Join-Path $PSScriptRoot 'artifacts/wpf-build') --output $publishPath -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'WPF publish failed.' }
Compress-Archive -Path (Join-Path $publishPath '*') -DestinationPath $archivePath -Force
Write-Host "WPF: $archivePath"
Write-Host 'Requires Windows x64 and .NET 10 Desktop Runtime. Run GMTFV.Wpf.exe.'
