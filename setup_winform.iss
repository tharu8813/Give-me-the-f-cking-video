; ============================================================================
; Inno Setup 스크립트 템플릿
; ============================================================================
; 아래 항목들만 수정하여 다른 프로그램에도 사용 가능합니다.
; ============================================================================

#define MyAppName "GMTFV"
#define MyAppVersion "3.5.0.0"
#define MyAppPublisher "Ji Beak min(tharu8813)"
#define MyAppCopyright "© 2024-2026 Ji Beak min(tharu8813). All rights reserved."
#define MyAppURL "https://github.com/tharu8813/Give-me-the-f-cking-video"
#define MyAppExeName "GMTFV.exe"
#define MyAppGUID "{{d27d3702-a216-4b19-aea8-d5367acee59f}"

; 선택적 설정 (필요시 수정)
#define SourcePath "src\GMTFV.WinForms\bin\Release"
#define SetupIconPath "icon.ico"
#define LicenseFilePath "LICENSE.txt"  ; 라이센스 파일 경로 (예: "license.txt")
#define ReadmeFilePath ""   ; Readme 파일 경로 (예: "readme.txt")

; ============================================================================
; 이하 코드는 수정하지 마세요
; ============================================================================

[Setup]
AppId={#MyAppGUID}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright={#MyAppCopyright}
DefaultDirName={commonpf}\{#MyAppName}
PrivilegesRequired=admin
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output-setup
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-setup
#if SetupIconPath != ""
SetupIconFile={#SetupIconPath}
#endif
#if LicenseFilePath != ""
LicenseFile={#LicenseFilePath}
#endif
#if ReadmeFilePath != ""
InfoBeforeFile={#ReadmeFilePath}
#endif
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
Uninstallable=yes
AlwaysRestart=no
CloseApplications=yes
RestartApplications=no
DisableDirPage=no
CloseApplicationsFilter=*.exe,*.dll
ArchitecturesInstallIn64BitMode=x64compatible
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoCopyright={#MyAppCopyright}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "chromeextension"; Description: "Chrome 탭 가져오기 확장 프로그램 폴더 열기"; GroupDescription: "추가 기능"; Flags: checkedonce

[Files]
Source: "{#SourcePath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "{#MyAppExeName},*.pdb,*.log,*.tmp,*.xml"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
Filename: "explorer.exe"; Parameters: "{app}\chrome-extension"; Description: "Chrome 탭 가져오기 확장 프로그램 설치 안내 열기"; Flags: postinstall skipifsilent; Tasks: chromeextension
