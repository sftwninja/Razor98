Razor CE for Windows 98SE
=========================

Razor CE built against .NET 2.0 so it runs on 98SE. Tested on a
Pentium II with the 5.0.6.5 client on a private shard. Other 5.x clients
should be fine. 7.x clients need XP anyway.

Needs
-----

Install in this order, reboot when asked:

1. IE 5.01 or newer. The .NET installer won't run on the IE 5.0 that
   98SE ships with. IE 6 SP1 offline installer (unzip, run
   ie6setup.exe):
   https://archive.org/download/IE6SP1MultiOfflineInstaller/en_ie6_sp1.zip
2. Windows Installer 2.0. Already there if you have IE 6. Otherwise:
   https://web.archive.org/web/2005id_/http://download.microsoft.com/download/WindowsInstaller/Install/2.0/W9XMe/EN-US/InstMsiA.exe
3. .NET Framework 2.0, exactly 2.0.50727.42. 2.0 SP1 and everything
   after it dropped 98:
   https://web.archive.org/web/2010id_/http://download.microsoft.com/download/5/6/7/567758a3-759e-473e-bf8f-52154438565a/dotnetfx.exe
4. 16-bit color or better.

The links are archived copies. SHA-1 of what I installed from:

    8b5778e819c6cb966173046072364f093bd47ae8  en_ie6_sp1.zip
    e739c40d747e7c27aacdb07b50925b1635ee7366  InstMsiA.exe
    a3625c59d7a2995fb60877b5f5324892a1693b2a  dotnetfx.exe

Install
-------

1. Unzip anywhere, e.g. C:\Razor. The folder has to be writable.
2. Run NGEN-RAZOR.BAT once. Takes a few minutes. Without it Razor starts
   a lot slower and lags the first time you do things.
3. Run Razor.exe, pick client.exe and the UO folder, pick your server,
   Launch UO.

Updating
--------

Copy Razor-update over your Razor folder (it leaves out Razor.exe.config
and counters.xml, so your server list and counters survive). Run
NGEN-RAZOR.BAT again after every update.

Differences from normal Razor CE
--------------------------------

- No ClassicUO.
- Text goes through the ANSI APIs, so characters outside your code page
  show up as ?.
- No unrar.dll (Razor doesn't use it). zlib.dll is a 98-friendly build of
  zlib 1.3.1.

Problems
--------

- Razor.exe does nothing or complains about the application
  configuration: .NET 2.0 isn't installed (or the wrong version is).
- "Unable to load DLL 'Crypt.dll'": keep the DLLs next to Razor.exe.

Building: see win98/NOTES.md in the source.
