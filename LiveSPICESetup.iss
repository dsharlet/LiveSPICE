[Setup]
AppName=LiveSPICE
AppVersion=0.13
AppPublisher=Dillon Sharlet
AppPublisherURL="www.livespice.org"
AppSupportURL="https://github.com/dsharlet/LiveSPICE/issues"
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DefaultDirName={autopf}\LiveSPICE
UninstallDisplayIcon="{app}\LiveSPICE.exe"
UninstallDisplayName=LiveSPICE
DefaultGroupName=LiveSPICE
SetupIconFile="LiveSPICE\LiveSPICE.ico"
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=LiveSPICESetup
OutputDir=.

[Components]
Name: "main"; Description: "LiveSPICE"; Types: full compact custom; Flags: fixed
Name: "vst"; Description: "VST Plugin"; Types: full custom

[Files]
Source: "LiveSPICE\bin\Release\LiveSPICE.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "LiveSPICE\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Circuit\Components\*.xml"; DestDir: "{app}\Components"
Source: "Circuit\Components\Readme.txt"; DestDir: "{userdocs}\LiveSPICE\Components"
Source: "Tests\Examples\*.schx"; DestDir: "{userdocs}\LiveSPICE\Examples"

Source: "LiveSPICEVst\bin\Release\*.dll"; DestDir: "{autopf}\Steinberg\VstPlugIns\LiveSPICE"; Flags: ignoreversion; Components: vst

[Run]
Filename: "{app}\LiveSPICE.exe"; Description: "Run LiveSPICE."; Flags: postinstall nowait

[Icons]
Name: "{group}\LiveSPICE"; Filename: "{app}\LiveSPICE.exe"
