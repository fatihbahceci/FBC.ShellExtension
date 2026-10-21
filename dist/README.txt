FBCTools
========

FBCTools adds an "FBCTools" menu to the right-click menu of Windows Explorer. What the
menu offers is set in a configuration file you edit with the FBCTools Editor. Out of the
box it converts videos to MP3 or MP4 and images to JPG or PNG with ffmpeg, copies paths,
names, UNC paths or the text of files to the clipboard, calculates and verifies checksums,
splits files into parts and joins them again, and sends a markdown file with its images
to a Wiki.js page.

Windows 10 or 11 on x64 (not Arm64). No administrator rights are needed.


Setup or portable folder
------------------------

FBCTools comes as a setup (FBCTools-<version>-setup.exe) and as a portable folder.

The setup installs it for your account in %LOCALAPPDATA%\Programs\FBCTools, without
administrator rights, adds the menu to Explorer and puts FBCTools (the Editor) in the Start
menu. A newer setup updates it; Settings > Apps removes it, and your menu stays. The rest of
this file is about the portable folder.


Getting started
---------------

1. Keep this folder where it is going to stay, for example under Documents or
   %LOCALAPPDATA%\Programs. Explorer uses the files from this folder.
2. Run FBC.ShellExtension.Editor.exe once. It registers the menu for your account and
   creates your menu from the template (config.template.json).
3. ffmpeg: the Editor offers to install it, with winget or as a download into this
   folder. You can also do it later from Tools > ffmpeg.
4. Right-click a file. On Windows 11 the menu is under "Show more options"
   (Shift + right-click opens that menu directly).

Your menu is stored in %APPDATA%\FBCTools\config.json. Tools in the Editor backs it up,
restores it or resets it to the template.


Moving or updating
------------------

If you move the folder, or put a newer version into another folder, run the Editor from
the new place once; it points Explorer at it. Then restart Explorer (sign out and in, or
run FBC.ShellExtension.Restarter.exe): the version still loaded in Explorer stays in use
until then, and an older one may not read the menu the new version has written ("The
configuration has errors").


Removing
--------

1. In this folder run:  FBC.ShellExtension.Manager.exe uninstall
   This removes the Explorer registration and, if you added it, the ffmpeg PATH entry.
2. Restart Explorer (or sign out and in), then delete this folder.
3. Your menu stays in %APPDATA%\FBCTools; delete that folder too if you do not need it.


Licence
-------

FBCTools is free software under the MIT licence, see LICENSE.txt. Its programs contain
parts of the .NET runtime, which is under the MIT licence too: dotnet-LICENSE.txt, and
the notices of the components .NET itself uses in dotnet-THIRD-PARTY-NOTICES.txt. The
Editor also contains Windows Forms; its notices are in winforms-THIRD-PARTY-NOTICES.txt.


ffmpeg
------

FFmpeg is free software licensed under the GPLv3. FBCTools does not include it. If you ask
for it, it is installed with winget (package Gyan.FFmpeg) or downloaded from gyan.dev into
tools\ffmpeg in this folder, together with its licence.
