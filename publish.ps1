param([string]$InnoCompiler = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe')
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src/GMTFV.Wpf/GMTFV.Wpf.csproj'
[xml]$definition = Get-Content -LiteralPath $project
$version = [string]$definition.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'A four-part release version is required.' }
$publish = Join-Path $PSScriptRoot 'artifacts/GMTFV-win-x64'
$archive = Join-Path $PSScriptRoot "artifacts/GMTFV-$version-win-x64.zip"
if (!(Test-Path -LiteralPath $InnoCompiler)) { throw "Inno Setup 6 compiler not found: $InnoCompiler" }
dotnet publish $project -c Release -r win-x64 --self-contained true --artifacts-path (Join-Path $PSScriptRoot 'artifacts/release-build') --output $publish -m:1 -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false
if ($LASTEXITCODE -ne 0) { throw 'WPF publish failed.' }
$packageRoot = (& dotnet msbuild $project -nologo -getProperty:NuGetPackageRoot).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Could not locate package licenses.' }
$runtime = Get-Content -LiteralPath (Join-Path $publish 'GMTFV.Wpf.runtimeconfig.json') | ConvertFrom-Json
foreach ($framework in $runtime.runtimeOptions.includedFrameworks) {
    $package = Join-Path $packageRoot ($framework.name.ToLowerInvariant() + '.runtime.win-x64/' + $framework.version)
    $notices = Join-Path $publish ('licenses/' + $framework.name)
    New-Item -ItemType Directory -Path $notices -Force | Out-Null
    $licenseFiles = @(Get-ChildItem -LiteralPath $package -File | Where-Object { $_.Name -match '^(LICENSE(\.TXT)?|THIRD-PARTY-NOTICES\.TXT)$' })
    if ($licenseFiles.Count -eq 0) { throw "Missing runtime license: $package" }
    $licenseFiles | Copy-Item -Destination $notices -Force
}
[xml]$engine = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/GMTFV.DownloadEngine/GMTFV.DownloadEngine.csproj')
$jsonVersion = [string]($engine.Project.ItemGroup.PackageReference | Where-Object Include -eq 'Newtonsoft.Json').Version
Copy-Item -LiteralPath (Join-Path $packageRoot "newtonsoft.json/$jsonVersion/LICENSE.md") -Destination (Join-Path $publish 'licenses/Newtonsoft.Json-MIT.md') -Force
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $archive -Force
& $InnoCompiler "/DMyAppVersion=$version" (Join-Path $PSScriptRoot 'setup.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$installer = Join-Path $PSScriptRoot "output-setup/GMTFV-$version-setup.exe"
$checksums = foreach ($file in @($archive, $installer)) {
    $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($file))"
}
$checksums | Set-Content -LiteralPath (Join-Path $PSScriptRoot "artifacts/GMTFV-$version-SHA256SUMS.txt") -Encoding ascii
Write-Host "Release $version ready: $archive and $installer"
