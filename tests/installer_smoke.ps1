param([string]$InnoCompiler = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe')
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$caseRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot ('artifacts/stability-installer-' + [guid]::NewGuid().ToString('N'))))
if (!$caseRoot.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Test path escaped workspace.' }
New-Item -ItemType Directory -Path $caseRoot | Out-Null
$install = Join-Path $caseRoot 'app'
$oldSource = Join-Path $caseRoot 'old-source'
$oldZip = Join-Path $repoRoot 'artifacts/GMTFV-4.0.0.0-win-x64.zip'
$oldDigest = ((Get-Content (Join-Path $repoRoot 'artifacts/GMTFV-4.0.0.0-SHA256SUMS.txt') | Where-Object { $_ -match 'win-x64.zip$' }) -split '\s+')[0]
if ((Get-FileHash -LiteralPath $oldZip -Algorithm SHA256).Hash -ne $oldDigest) { throw 'Prior package checksum mismatch.' }
Expand-Archive -LiteralPath $oldZip -DestinationPath $oldSource
$testGuid = '/DMyAppGUID={{' + [guid]::NewGuid().ToString() + '}'
$setup = Join-Path $repoRoot 'setup.iss'
foreach ($version in @('4.0.0.0', '4.0.1.0')) {
    $source = if ($version -eq '4.0.0.0') { $oldSource } else { Join-Path $repoRoot 'artifacts/GMTFV-win-x64' }
    & $InnoCompiler "/O$caseRoot" "/Fsetup-$version" $testGuid "/DMyAppVersion=$version" "/DAppSourcePath=$source" $setup *> (Join-Path $caseRoot "compile-$version.txt")
    if ($LASTEXITCODE -ne 0) { throw "Test installer compilation failed: $version" }
    if ($version -eq '4.0.0.0') {
        New-Item -ItemType Directory -Path (Join-Path $install 'Downloads') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'user-content.txt'), 'preserve user content')
        [IO.File]::WriteAllText((Join-Path $install 'Downloads/video.mp4'), 'preserve download')
        [IO.File]::WriteAllText((Join-Path $install 'GMTFV.exe'), 'retired WinForms fixture')
    }
    $arguments = @('/CURRENTUSER', '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/NOICONS', "/DIR=`"$install`"", "/LOG=`"$(Join-Path $caseRoot "install-$version.txt")`"")
    $process = Start-Process -FilePath (Join-Path $caseRoot "setup-$version.exe") -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Install failed: $version, exit $($process.ExitCode)" }
    if ([Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $install 'GMTFV.Wpf.exe')).FileVersion -ne $version) { throw 'Wrong installed version.' }
    if ((Get-FileHash (Join-Path $install 'GMTFV.Wpf.dll')).Hash -ne (Get-FileHash (Join-Path $source 'GMTFV.Wpf.dll')).Hash) { throw 'Installed binary differs from package.' }
    foreach ($forbidden in @('.git', 'src', 'tests', 'artifacts')) {
        if (Test-Path -LiteralPath (Join-Path $install $forbidden)) { throw 'Installer included a repository directory.' }
    }
    if ([IO.File]::ReadAllText((Join-Path $install 'user-content.txt')) -ne 'preserve user content' -or [IO.File]::ReadAllText((Join-Path $install 'Downloads/video.mp4')) -ne 'preserve download') { throw 'User fixture changed.' }
    Write-Output "PASS current-user isolated install/upgrade ${version}: executable version, binary hash, preserved user files"
}
foreach ($file in @('coreclr.dll', 'PresentationFramework.dll', 'licenses/Microsoft.NETCore.App/LICENSE.TXT', 'licenses/Microsoft.WindowsDesktop.App/LICENSE', 'licenses/Newtonsoft.Json-MIT.md')) {
    if (!(Test-Path -LiteralPath (Join-Path $install $file))) { throw "Missing runtime/license: $file" }
}
if (Test-Path -LiteralPath (Join-Path $install 'GMTFV.exe')) { throw 'Legacy executable was not removed.' }
$process = Start-Process -FilePath (Join-Path $install 'unins000.exe') -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$(Join-Path $caseRoot 'uninstall.txt')`"") -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0 -or (Test-Path -LiteralPath (Join-Path $install 'GMTFV.Wpf.exe'))) { throw 'Isolated uninstall failed.' }
if ([IO.File]::ReadAllText((Join-Path $install 'user-content.txt')) -ne 'preserve user content' -or [IO.File]::ReadAllText((Join-Path $install 'Downloads/video.mp4')) -ne 'preserve download') { throw 'Uninstall changed user fixture.' }
Write-Output "PASS isolated uninstall preserves user files; runtime/licenses verified. Evidence: $caseRoot"
