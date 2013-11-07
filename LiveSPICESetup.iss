[Setup]
AppName=LiveSPICE
AppVersion=0.1
AppPublisher=Dillon Sharlet
AppPublisherURL="www.livespice.org"
AppSupportURL="https://github.com/dsharlet/LiveSPICE/issues"
DefaultDirName={pf}\LiveSPICE
UninstallDisplayIcon="{app}\LiveSPICE.exe"
UninstallDisplayName=LiveSPICE
DefaultGroupName=LiveSPICE
SetupIconFile="LiveSPICE\LiveSPICE.ico"
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=LiveSPICESetup
OutputDir=Setup

[Files]
Source: "LiveSPICE\bin\Release\LiveSPICE.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "LiveSPICE\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Components\*.xml"; DestDir: "{app}\Components"; Flags: onlyifdoesntexist

Source: "Circuits\Active1stOrderLowpassRC.xml"; DestDir: "{userdocs}\LiveSPICE\Examples"; Flags: onlyifdoesntexist
Source: "Circuits\Passive1stOrderLowpassRC.xml"; DestDir: "{userdocs}\LiveSPICE\Examples"; Flags: onlyifdoesntexist
Source: "Circuits\BridgeRectifier.xml"; DestDir: "{userdocs}\LiveSPICE\Examples"; Flags: onlyifdoesntexist
Source: "Circuits\CommonCathodeTriodeAmplifier.xml"; DestDir: "{userdocs}\LiveSPICE\Examples"; Flags: onlyifdoesntexist
Source: "Circuits\CommonEmitterTransistorAmplifier.xml"; DestDir: "{userdocs}\LiveSPICE\Examples"; Flags: onlyifdoesntexist
Source: "Circuits\OpAmpModel.xml"; DestDir: "{userdocs}\LiveSPICE\Examples"; Flags: onlyifdoesntexist
Source: "Circuits\AmpParallelDiodesLoad.xml"; DestDir: "{userdocs}\LiveSPICE\Examples"; Flags: onlyifdoesntexist

[Run]
Filename: "{app}\LiveSPICE.exe"; Description: "Run LiveSPICE."; Flags: postinstall nowait

[Icons]
Name: "{group}\LiveSPICE"; Filename: "{app}\LiveSPICE.exe"
