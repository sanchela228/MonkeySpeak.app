[Setup]
AppName=MonkeySpeak
AppVersion=1.0.0
DefaultDirName={pf}\MonkeySpeak
DefaultGroupName=MonkeySpeak
OutputDir=Output
OutputBaseFilename=MonkeySpeakSetup
Compression=lzma
SolidCompression=yes

[Files]
Source: "build\win\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\MonkeySpeak"; Filename: "{app}\MonkeySpeak.exe"
Name: "{commondesktop}\MonkeySpeak"; Filename: "{app}\MonkeySpeak.exe"