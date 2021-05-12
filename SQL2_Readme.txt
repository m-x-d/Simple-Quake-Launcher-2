==== Simple Quake Launcher 2 ====
A simple ZDL-inspired map/mod/demo launcher for Quake, Quake 2, Hexen 2 and Half-Life.

==== FEATURES ====
- Small and easy to use.
- Detects maps and demos in folders, .pak and .pk3 files.
- Displays map titles ("message" worldspawn key).
- Can launch demos (displays map titles for them as well).
- Can create shortcuts to play the game using currently selected options.
- Can run a random map at random skill.

==== INSTALLATION ====
Extract SQLauncher2.exe into your Quake / Quake 2 / Hexen 2 / Half-Life directory.

==== SYSTEM REQUIREMENTS ====
.net Framework 4.5 (https://www.microsoft.com/download/details.aspx?id=30653). 

==== LEGACY ====
The older, WinXP-compartible iteration of this project can be found here: https://sourceforge.net/projects/simplequakelauncher/.

==== CHANGELOG ====

2.5:
Added handling for mods without map files. More specifically:
- Quake, Hexen 2: count folder as a mod if it (or PAK/PK3 files within it) contains progs.dat.
- Quake2: count folder as a mod if it contains a variant of gamex86.dll.
- Half-Life: count folder as a mod if it contains "cl_dlls\client.dll".

2.4:
- Quake: improved .DEM reader compatibility (FTE/FTE2 demos support).
- Quake, UI: renamed "Medium" skill to "Normal".

2.3:
- Desktop resolution can now be selected in the "Resolution" combo box.
- Fixed, Quake 2: Official Mission Pack names were swapped.

2.2:
- Quake: improved demo map path validation logic.
- Quake: renamed official mission pack menu items ("EP" -> "MP").
- UI: added a tooltip to Command Line textbox.
- UI: custom command line parameters can now be cleared by MMB-clicking them.
- Fixed several issues related to (re)storing and displaying of custom command line parameters.
- Fixed a bug in folder maps detection logic.

2.1:
- Quake: improved .DEM reader compatibility.
- Optimized PK3 entries processing speed.

2.0:
- First public release.