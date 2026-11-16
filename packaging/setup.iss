[Setup]
AppId={{A99F48CF-385D-4C76-91F5-56B931DA8E9E}
AppName=Al Asmai Lab
AppVersion=0.1.1
DefaultDirName={localappdata}\Programs\AlAsmaiLab
DefaultGroupName=Al Asmai Lab
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\installer-output
OutputBaseFilename=AlAsmaiLab-Setup
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\MedicalLab.exe
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Al Asmai Lab"; Filename: "{app}\MedicalLab.exe"
Name: "{autodesktop}\Al Asmai Lab"; Filename: "{app}\MedicalLab.exe"
