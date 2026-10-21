# FBCTools

FBCTools adds an **FBCTools** menu to the right-click menu of Windows Explorer. What the menu
offers is up to you: it is described by a JSON file that you edit with the FBCTools Editor. An
entry can run any program on the selected files, folders or the folder background, or one of
the tools built into FBCTools.

Out of the box the menu converts videos to MP3 or MP4 and images to JPG or PNG with ffmpeg,
copies paths, names and file contents to the clipboard, calculates and verifies checksums,
splits and joins files, tidies up folders, and sends a markdown file with its images to a
Wiki.js page.

Windows 10 and 11 on x64 (not Arm64). No administrator rights are needed, and no .NET
installation.

> FBCTools is young (version 0.x). Please report problems on the
> [Issues](https://github.com/fatihbahceci/FBC.ShellExtension/issues) page.

## Contents

- [Features](#features)
- [Built-in tools](#built-in-tools)
- [Ready-made commands in the default menu](#ready-made-commands-in-the-default-menu)
- [The default menu at a glance](#the-default-menu-at-a-glance)
- [Installing](#installing)
- [Writing your own entries](#writing-your-own-entries)
- [Building from source](#building-from-source)
- [Licence](#licence)

## Features

**A menu you define.** Commands, submenus and separators, in any order and depth. Each entry
says where it appears:
- on files, folders, the background of a folder or drives;
- by extension or name pattern, with exclusions;
- for a minimum or maximum number of selected items.

Every entry can be switched off without deleting it. The shell extension reads the file again
whenever it changes, so Explorer never needs a restart for a menu change.

**Any program, run the way you need.**
- **Variables** for the selection: `{path}`, `{dir}`, `{name}`, `{nameNoExt}`, `{ext}`,
  `{pathNoExt}`, `{allPaths}`, plus `%ENVIRONMENT%` variables.
- **Several selected items:** one at a time, several in parallel (with a limit), or all
  together in one run.
- **Window:** hidden, minimized or maximized.
- **As administrator:** the program runs elevated, and its output still comes back to the
  progress window.

**A progress window that tells you what happened.**
- Commands that FBCTools tracks show their progress, optionally the program's live output,
  and a summary at the end: how many succeeded and how many failed.
- A failure shows the program's error output, or its exit code.
- Long runs can be cancelled.
- The window closes by itself when everything succeeded, if you want it to.

**The Editor.**
- A tree of the menu, with a check box on every entry to switch it on or off; drag and drop,
  keyboard shortcuts, undo and redo.
- A details panel for the selected entry. Errors are listed below it, and an entry with an
  error is shown in red.
- **Preview:**
  - pick files and folders, or drag them in from Explorer, to see the menu as it would look
    for them;
  - see the processes a command would start, with the variables filled in;
  - **Run** a command through FBCTools, as the menu would.
- **Edit FBCTools menu…** at the bottom of the Explorer menu opens it.
- Its **Tools** menu checks the installation, backs up, restores or resets your menu, and
  checks and installs ffmpeg.

**Updates that respect your changes.** New entries of a newer default menu are added to your
menu, even if you changed it. Entries you deleted do not come back, and entries you switched
off stay off.

## Built-in tools

These run inside FBCTools, without starting another program. An entry uses one by naming it
as its `tool` (see [Writing your own entries](#writing-your-own-entries)); the Editor offers
them as the entry's **Kind**.

| Tool | `tool` | What it does |
| --- | --- | --- |
| Copy full path | `copyPath` | Copies the full path of every selected item, one per line |
| Copy name | `copyName` | Copies the names |
| Copy UNC path | `copyUncPath` | Like the full path, but a path on a mapped network drive as `\\server\share\…` |
| Copy internet path | `copyInternetPath` | Copies `file:///` addresses |
| Copy title | `copyTitle` | Copies each file's Title property (of a song, a document), as Explorer's details pane shows it |
| Copy content | `copyContent` | Copies the text of the files, detecting the encoding as Notepad does (up to 16 MB) |
| Append clipboard | `appendClipboard` | Adds the clipboard's text to the end of each file, on a line of its own, in the file's own encoding |
| Duplicate | `duplicate` | Copies a file next to itself as "name - Copy", keeping dates, attributes and the "downloaded from the internet" mark |
| Calculate checksums | `checksum` | MD5, SHA-1, SHA-256 and SHA-512 of each file |
| Verify checksum | `verifyChecksum` | Checks a checksum file (`.sha256`, `.md5`, `SHA256SUMS` …; GNU, BSD and certutil formats), or a file against the checksum file next to it or a checksum on the clipboard |
| Split into parts | `split` | Splits a file into `name.001`, `name.002` … of a size you choose, plus `name.sha256`; 7-Zip can join them too |
| Join parts | `join` | Joins `name.001` … back into `name` and verifies it with `name.sha256` |
| Count lines | `countLines` | Counts the lines of each file |
| Folder size | `folderSize` | Size of a folder with everything in it, hidden files included, and its file and folder counts; does not follow links |
| Pack into folder | `packToFolder` | Moves the selected items into a new folder whose name it asks for |
| New folder | `newFolder` | Creates a folder, asking for its name |
| Unpack folder | `unpackFolder` | Moves a folder's contents up one level and removes the folder; moves nothing if a name is taken |
| Delete empty folders | `deleteEmptyFolders` | Lists the empty folders under a folder and removes them after you confirm; leaves hidden and system folders (such as `.git`) alone |
| Delete permanently | `deletePermanently` | Deletes without the Recycle Bin after you confirm, as Shift+Delete does |
| Run with parameters | `runWithParameters` | Asks for arguments and starts the selected program with them |
| Send to Wiki.js | `wikiImport` | Sends a markdown file (written in Typora, for example) and the images it links to into a Wiki.js 2 page: see [Send to Wiki.js](docs/wiki-import/en.md) |

The clipboard tools work without opening a window. The file and folder tools show their
progress in a window that can be cancelled and stays open with the results. The built-in tools
never replace an existing file: split, join, duplicate and the folder tools stop instead.

## Ready-made commands in the default menu

Besides the built-in tools, the default menu comes with entries that run other programs.
They are ordinary entries you can change, copy as a starting point for your own, or switch off.

| Entry | Shown for | What it runs |
| --- | --- | --- |
| Open in Notepad | Up to 5 files | Opens each file in Notepad |
| Command prompt here | A folder, the folder background or a drive | A command prompt in that folder |
| Command prompt here (administrator) | A folder or the folder background | The same, as administrator |
| Register DLL | `.dll`, `.ocx`, `.ax` | `regsvr32` as administrator |
| Unregister DLL | `.dll`, `.ocx`, `.ax` | `regsvr32 /u` as administrator |
| Convert › To MP3 | Video and audio files (`mp4`, `mkv`, `avi`, `mov`, `webm`, `wav`, `flac`, `m4a`, `ogg` …) | ffmpeg: extracts the sound as an MP3 next to the file (LAME, VBR quality 2), two files at a time |
| Convert › To MP4 | Videos (`mkv`, `avi`, `mov`, `webm`, `wmv`, `flv`, `mpg`, `ts` …) | ffmpeg: H.264 and AAC into an `.mp4` next to the file, one at a time |
| Convert › To JPG | Images (`png`, `bmp`, `gif`, `webp`, `tif`) | ffmpeg: a `.jpg` next to the file |
| Convert › To PNG | Images (`jpg`, `bmp`, `gif`, `webp`, `tif`) | ffmpeg: a `.png` next to the file |

The conversions need ffmpeg. FBCTools does not include it, since ffmpeg has a licence of its own
(GPL). When it is missing, the Editor offers to install it, with winget or as a download from
gyan.dev into the FBCTools folder; you can also do it later from **Tools › ffmpeg**. A
conversion replaces a file of the same name next to the original: `photo.png` › To JPG
replaces `photo.jpg`.

## The default menu at a glance

Each submenu shows only the entries that fit the selection; "Delete permanently…" comes
switched off.

```
FBCTools
├─ Open in Notepad
├─ Command prompt here
├─ Command prompt here (administrator)
├─ Copy
│  ├─ Full path
│  ├─ Name
│  ├─ UNC path
│  ├─ Internet path
│  ├─ Title
│  └─ Content
├─ File tools
│  ├─ Duplicate
│  ├─ Calculate checksums
│  ├─ Verify checksum
│  ├─ Split into parts…
│  ├─ Join parts
│  ├─ Run with parameters…
│  ├─ Register DLL
│  └─ Unregister DLL
├─ Folder tools
│  └─ Folder size
├─ Organize
│  ├─ Pack into folder…
│  ├─ New folder…
│  ├─ Unpack folder
│  ├─ Delete empty folders…
│  └─ Delete permanently…   (off)
├─ Text files
│  ├─ Count lines
│  ├─ Append clipboard
│  └─ Send to Wiki.js…
└─ Convert
   ├─ To MP3
   ├─ To MP4
   ├─ To JPG
   └─ To PNG
```

The default menu itself is [dist/config.template.json](dist/config.template.json).

## Installing

Download from the [Releases](https://github.com/fatihbahceci/FBC.ShellExtension/releases) page:

- **Setup**, `FBCTools-<version>-setup.exe`:
  - installs for your account into `%LOCALAPPDATA%\Programs\FBCTools`, without administrator
    rights;
  - adds the menu to Explorer and puts **FBCTools** (the Editor) in the Start menu.

  A newer setup updates it; **Settings › Apps** removes it.
- **Portable**, `FBCTools-<version>-win-x64.zip`:
  - unzip it where it is going to stay;
  - run `FBC.ShellExtension.Editor.exe` once to add the menu;
  - to remove it, run `FBC.ShellExtension.Manager.exe uninstall`, restart Explorer, then delete
    the folder.

Each file has a `.sha256` next to it.

On Windows 11 the FBCTools menu is in the classic menu: **Show more options**, or
Shift+right-click to open it directly.

Your menu is `%APPDATA%\FBCTools\config.json`. Uninstalling leaves it there; the Editor's
Tools menu backs it up and restores it.

## Writing your own entries

The Editor is the easy way. The file itself looks like this:

```jsonc
{
  "version": 7,
  "items": [
    { "type": "command", "label": "Copy path", "tool": "copyPath" },
    {
      "type": "command",
      "label": "Open in VS Code",
      "match": { "targets": ["file", "folder", "background"] },
      "command": {
        "fileName": "%LOCALAPPDATA%\\Programs\\Microsoft VS Code\\Code.exe",
        "arguments": "\"{path}\"",
        "runMode": "launch"
      }
    },
    { "type": "separator" },
    {
      "type": "submenu",
      "label": "Images",
      "match": { "targets": ["file"], "extensions": ["jpg", "png"], "mode": "any" },
      "items": [
        {
          "type": "command",
          "label": "Convert to WebP",
          "command": {
            "fileName": "cwebp",
            "arguments": "\"{path}\" -o \"{pathNoExt}.webp\"",
            "windowStyle": "hidden",
            "createNoWindow": true,
            "multiSelect": "perFileParallel",
            "maxParallelism": 4
          }
        }
      ]
    }
  ]
}
```

Comments and trailing commas are allowed. An unknown field is an error, so a misspelled key is
never silently ignored.

| Field | Meaning |
| --- | --- |
| `match.targets` | `file`, `folder`, `background`, `drive` |
| `match.extensions`, `match.patterns`, `match.exclude` | Extensions, and `*`/`?` patterns on the name |
| `match.mode` | `all`: every item must match. `any`: the entry takes only the items that match |
| `match.minCount`, `match.maxCount` | How many items the entry takes |
| `command.fileName`, `arguments`, `workingDirectory` | What to run; variables and `%ENV%` are expanded |
| `command.windowStyle`, `createNoWindow`, `useShellExecute`, `verb` | As in .NET's `ProcessStartInfo`; `verb: "runas"` runs as administrator |
| `command.runMode` | `track` (default): wait and show the results. `launch`: start it and let go |
| `command.multiSelect`, `maxParallelism` | `perFileSequential` (default), `perFileParallel`, `allAtOnce` |
| `command.showOutput`, `closeOnSuccess`, `outputEncoding` | The program's live output in the window; closing it on success; `oem`, `utf8` or `ansi` |
| `tool` | A [built-in tool](#built-in-tools) instead of `command` |
| `enabled` | `false` keeps the entry but hides it |

Variables are inserted without quotes, so write the quotes yourself: `"\"{path}\""`.

| Variable | For a file | For a folder | Folder background | Drive |
| --- | --- | --- | --- | --- |
| `{path}` | full path | full path | the folder | `C:\` |
| `{dir}` | its folder | its parent | the folder | `C:\` |
| `{name}` | `report.pdf` | its name | its name | `C` |
| `{nameNoExt}` | `report` | its name | its name | `C` |
| `{ext}` | `.pdf` | empty | empty | empty |
| `{pathNoExt}` | `C:\docs\report` | full path | the folder | `C:\` |
| `{allPaths}` | every selected path, each quoted, separated by spaces | | | |

## Building from source

You need the .NET 10 SDK. The shell extension, the runner and the small helper programs are
compiled with NativeAOT, which needs Windows and Visual Studio's **Desktop development with
C++** workload. On other systems `dotnet build` and `dotnet test` work, but nothing can be
published.

```bat
dotnet build
dotnet test
dev-run.cmd -Install -RestartExplorer
publish.cmd
```

- `dev-run.cmd` builds, tests and publishes into `artifacts\FBCTools`.
  - `-Install` copies the result to `%LOCALAPPDATA%\Programs\FBCTools` and registers it.
  - `-RestartExplorer` does the copy while Explorer is stopped, then reopens your folder
    windows.
- `publish.cmd` makes the portable zip and the setup in `artifacts\dist`. The setup needs
  [Inno Setup](https://jrsoftware.org/isinfo.php) 6.3 or later; `-NoSetup` skips it.

The projects:

| Folder | What it is |
| --- | --- |
| `src/FBC.ShellExtension.Core` | The menu file, matching, variables, running commands, all texts |
| `src/FBC.ShellExtension.ShellExt` | The shell extension: a NativeAOT COM DLL that Explorer loads |
| `src/FBC.ShellExtension.Runner` | Runs the commands and the built-in tools, shows progress (NativeAOT, plain Win32) |
| `src/FBC.ShellExtension.Editor` | The Editor (WinForms) |
| `src/FBC.ShellExtension.Manager` | Registration, the menu's updates, backups, ffmpeg (NativeAOT, console) |
| `src/FBC.ShellExtension.Restarter` | Restarts Explorer and reopens its windows (NativeAOT, console) |
| `src/FBC.ShellExtension.WikiImport` | Send to Wiki.js |
| `installer/` | The setup (Inno Setup) |
| `tests/` | Unit tests (MSTest), and a host that loads the DLL without Explorer |

## Licence

FBCTools is free software under the [MIT licence](LICENSE). Its programs contain parts of the
.NET runtime, which is under the MIT licence too; the published folder carries .NET's notices.
ffmpeg is not part of FBCTools; it is installed only when you ask for it.
