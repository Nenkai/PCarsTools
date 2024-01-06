# PCarsTools

Sets of tools to fully unpack (with file names) and decrypt Project Cars related files. 

Supports:
* Project Cars 1
* Project Cars 2
* Project Cars 3
* Project Cars GO
* Test Drive Ferrari Racing Legends

Allows:
- PAK unpacking (with file names)
- TOC unpacking (with file names)
- Script decryption (bundled inside executables as bmp...)
- Model decryption (.meb)
- Texture conversion (.tex)
- build.dat decryption

NOTE: Requires oo2core_7_win64 dll file for later games, not included in this repository.

## Usage

* TOC Unpack
  * `PCarsTools toc -i <.../TOCFiles/DirPaks.toc> -g <path to game directory> [--game-type <PC1 or TDFRL>]`
  
* PAK Unpack
  * `PCarsTools pak -i <path to pak> -g <path_to_game_directory> [--game-type <PC1 or TDFRL>]`
  
* Script decryption
  * `PCarsTools decryptscript -i <path to script>` (script is extracted from executable, normally red bmp file)
  
* Model decryption/encryption
  * `PCarsTools decryptmodel -i <path to meb file>`

* Texture conversion (removes the header from .tex files)
  * `PCarsTools convert-texture -i <path to tex file>`
  
* Build.dat decryption
  * `PCarsTools build-dat -i <path to build.dat>`
  
# Download
https://github.com/Nenkai/PCarsTools/releases
