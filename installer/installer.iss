#define MyAppVersion GetEnv("APP_VERSION")
#define MyAppVersionName GetEnv("APP_VERSION_NAME")

[Setup]
AppName=MonkeySpeak
AppVersion={#MyAppVersionName}
AppVerName=MonkeySpeak_{#MyAppVersionName}
DefaultDirName={pf}\MonkeySpeak
DefaultGroupName=MonkeySpeak
OutputDir=Output
OutputBaseFilename=MonkeySpeakSetup
Compression=lzma
SolidCompression=yes

[Files]
Source: "..\build\win\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\MonkeySpeak"; Filename: "{app}\MonkeySpeak.exe"
Name: "{commondesktop}\MonkeySpeak"; Filename: "{app}\MonkeySpeak.exe"