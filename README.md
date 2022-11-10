# PCarsTools

Sets of tools to fully unpack (with file names) and decrypt Project Cars related files.

Allows:
- PAK unpacking (with file names)
- TOC unpacking (with file names)
- Script decryption (bundled inside executables as bmp...)
- Model decryption (.meb)
- build.dat decryption

NOTE: Requires oo2core_7_win64 dll file for later games, not included in this repository.

## Usage

* TOC Unpack
  * `PCarsTools toc -i <.../TOCFiles/DirPaks.toc> -g <path to game directory>`
  
* PAK Unpack
  * `PCarsTools pak -i <path to pak> -g <path_to_game_directory>`
  
* Script decryption
  * `PCarsTools decryptscript -i <path to script>` (script is extracted from executable, normally red bmp file)
  
* Model decryption/encryption
  * `PCarsTools decryptmodel -i <path to meb file>`
  
* Build.dat decryption
  * `PCarsTools build-dat -i <path to build.dat>`
  
# Download
https://github.com/Nenkai/PCarsTools/releases
