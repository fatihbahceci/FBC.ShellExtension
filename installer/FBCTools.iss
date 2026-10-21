; FBCTools setup: a per-user install that needs no administrator rights (ADR-0022).
;
; scripts\publish.ps1 builds it from the publish folder:
;   ISCC.exe /DAppVersion=<VersionPrefix> /DSourceDir=<publish folder> [/DCopyright=<text>]
;            [/DAppUrl=<public repository>] /O<output folder> installer\FBCTools.iss
;
; Needs Inno Setup 6.3 or later; written against 7.1. Kept to ASCII, like the scripts.

#if Ver < EncodeVer(6, 3, 0)
  #error Inno Setup 6.3 or later is needed.
#endif

#ifndef AppVersion
  #error Pass the version: /DAppVersion=<VersionPrefix>
#endif

#ifndef SourceDir
  #error Pass the publish folder: /DSourceDir=<folder>
#endif

; Never changes: the uninstall entry is "{CB5EEDDC-025A-4FBA-88B3-F4D5376C4A85}_is1" under
; HKCU, which winget's manifest names as the ProductCode (docs/winget.md). "{{" is a literal
; brace in [Setup].
#define AppId "{{CB5EEDDC-025A-4FBA-88B3-F4D5376C4A85}"
#define ShellExt "FBC.ShellExtension.ShellExt.dll"
#define Manager "FBC.ShellExtension.Manager.exe"
#define Editor "FBC.ShellExtension.Editor.exe"
#define Restarter "FBC.ShellExtension.Restarter.exe"

[Setup]
AppId={#AppId}
AppName=FBCTools
AppVersion={#AppVersion}
AppPublisher=Fatih Bahceci
#ifdef Copyright
AppCopyright={#Copyright}
#endif
#ifdef AppUrl
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
#endif
; The name and version in Apps & Features are the ones winget matches against its manifest.
UninstallDisplayName=FBCTools
UninstallDisplayIcon={app}\{#Editor}
VersionInfoVersion={#AppVersion}
VersionInfoProductName=FBCTools

; Per user, in %LOCALAPPDATA%\Programs\FBCTools: the place dev-run.cmd -Install uses, and the
; registration is per user too (HKCU, ADR-0005).
PrivilegesRequired=lowest
DefaultDirName={autopf}\FBCTools
DisableDirPage=yes
DisableProgramGroupPage=yes
; x64 Windows only: on Arm64 the native Explorer cannot load an x64 shell extension, so a
; setup that ran there would succeed and show no menu.
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0

; Restart Manager would offer to close Explorer, which holds the shell extension. The files in
; use are moved aside instead (see [Code]).
CloseApplications=no
RestartApplications=no

WizardStyle=modern
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=FBCTools-{#AppVersion}-setup
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
; dev-run.cmd -Install may have made the folder first; it goes at uninstall all the same.
Name: "{app}"; Flags: uninsalwaysuninstall

[InstallDelete]
; What dev-run.cmd -Install leaves that the setup does not ship (its Editor is framework-dependent).
Type: files; Name: "{app}\FBC.ShellExtension.*.deps.json"
Type: files; Name: "{app}\FBC.ShellExtension.*.runtimeconfig.json"
Type: files; Name: "{app}\FBC.ShellExtension.*.pdb"
Type: files; Name: "{app}\FBC.ShellExtension.*.lib"
Type: files; Name: "{app}\FBC.ShellExtension.*.exp"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\FBCTools"; Filename: "{app}\{#Editor}"; Comment: "Edit the FBCTools menu"

[Run]
; Registers this copy with Explorer and creates or updates the menu from the template (ADR-0010).
Filename: "{app}\{#Manager}"; Parameters: "check"; StatusMsg: "Adding the menu to Explorer..."; Flags: runhidden waituntilterminated
; Explorer keeps an earlier version's DLL until it restarts; the restarter reopens its windows.
Filename: "{app}\{#Restarter}"; Description: "Restart Explorer now, so that the menu is the new version"; Flags: postinstall skipifsilent runhidden waituntilterminated; Check: WasInstalledBefore
Filename: "{app}\{#Editor}"; Description: "Open the FBCTools editor"; Flags: postinstall skipifsilent nowait unchecked

[UninstallDelete]
; ffmpeg as "Install it: download" put it next to the programs (ADR-0011); nothing else there.
Type: filesandordirs; Name: "{app}\tools\ffmpeg"
Type: dirifempty; Name: "{app}\tools"

[Code]
var
  Installed: Boolean;
  Finished: Boolean;
  MovedFrom: TArrayOfString;
  MovedTo: TArrayOfString;

function WasInstalledBefore(): Boolean;
begin
  Result := Installed;
end;

function Stamp(): String;
begin
  Result := GetDateTimeString('yyyymmdd-hhnnss', #0, #0);
end;

procedure Add(var Names: TArrayOfString; const Name: String);
var
  Count: Integer;
begin
  Count := GetArrayLength(Names);
  SetArrayLength(Names, Count + 1);
  Names[Count] := Name;
end;

procedure Collect(const Pattern: String; var Names: TArrayOfString);
var
  Find: TFindRec;
begin
  if FindFirst(Pattern, Find) then
  begin
    try
      repeat
        if (Find.Attributes and FILE_ATTRIBUTE_DIRECTORY) = 0 then
          Add(Names, Find.Name);
      until not FindNext(Find);
    finally
      FindClose(Find);
    end;
  end;
end;

{ Copies moved aside by an earlier run; one that something still holds stays for next time. }
procedure DeleteAsideCopies(const Folder: String);
var
  Names: TArrayOfString;
  I: Integer;
begin
  Collect(Folder + '\FBC.ShellExtension.*.old-*', Names);
  for I := 0 to GetArrayLength(Names) - 1 do
    DeleteFile(Folder + '\' + Names[I]);
end;

{ Explorer keeps the shell extension loaded, and a Runner or Editor window may be open. Such a
  file cannot be overwritten, but it can be renamed, and whatever holds it keeps the old copy
  until it exits (scripts\install-files.ps1 does the same, G-0101). So every program file of
  FBCTools is moved aside before the new ones are copied; only FBCTools' own, since /DIR= (and
  winget's --location) can point the setup at a folder other programs live in. }
procedure MoveProgramsAside(const Folder: String);
var
  Names: TArrayOfString;
  I: Integer;
  Path, Aside: String;
begin
  Collect(Folder + '\FBC.ShellExtension.*.dll', Names);
  Collect(Folder + '\FBC.ShellExtension.*.exe', Names);
  for I := 0 to GetArrayLength(Names) - 1 do
  begin
    Path := Folder + '\' + Names[I];
    Aside := Path + '.old-' + Stamp();
    if RenameFile(Path, Aside) then
    begin
      Add(MovedFrom, Path);
      Add(MovedTo, Aside);
    end
    else
      Log('Could not move aside: ' + Path);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Folder: String;
begin
  Folder := ExpandConstant('{app}');
  if CurStep = ssInstall then
  begin
    Installed := FileExists(Folder + '\{#ShellExt}');
    DeleteAsideCopies(Folder);
    MoveProgramsAside(Folder);
  end
  else if CurStep = ssPostInstall then
  begin
    Finished := True;
    DeleteAsideCopies(Folder);
  end;
end;

{ An install that did not finish puts the old programs back where no new one arrived, so the
  registration does not point at a missing file. }
procedure DeinitializeSetup();
var
  I: Integer;
begin
  if not Finished then
    for I := 0 to GetArrayLength(MovedFrom) - 1 do
      if not FileExists(MovedFrom[I]) then
        RenameFile(MovedTo[I], MovedFrom[I]);
end;

{ Before the uninstaller removes the files:
  1. The registration and the ffmpeg PATH entry go. A registration that points at another copy
     (a portable folder) is that copy's and stays; so does the menu in %APPDATA%.
  2. Explorer keeps the DLL loaded until it restarts, so it cannot be deleted. It is moved to the
     temporary folder instead, which lets the program folder go now. This has to come after the
     first step: the registration is removed through the DLL itself. }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Folder: String;
  Names: TArrayOfString;
  I, ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    Folder := ExpandConstant('{app}');
    if not Exec(Folder + '\{#Manager}', 'uninstall --own', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      Log('Could not run the manager: ' + SysErrorMessage(ResultCode));

    Collect(Folder + '\FBC.ShellExtension.*.dll', Names);
    Collect(Folder + '\FBC.ShellExtension.*.old-*', Names);
    for I := 0 to GetArrayLength(Names) - 1 do
      if not DeleteFile(Folder + '\' + Names[I]) then
        RenameFile(Folder + '\' + Names[I], ExpandConstant('{%TEMP}') + '\FBCTools-' + Stamp() + '-' + Names[I]);
  end;
end;
