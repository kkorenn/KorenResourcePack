<p align="center">
  <a>🇺🇸 English</a> |
  <a href="README.kr.md">🇰🇷 한국어</a> | 
  <a href="CREDITS.md">⭐️ Credits</a>
</p>

# koren resource pack

a mod inspired heavily by [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack), wanted to make my own since so many people were saying it was laggy and i thought i would be good at having a go at it

also i actually listen to the community and take suggestions!

few reasons why my mod is better then jipper's:
- a selection of fonts instead of one
- more features (example: hold setting display! it tells people what hold setting ur using) 
- importing JSON from [DM Note](https://github.com/DmNote-App/DmNote) for easy transfer
- no JALib so performance sky rocket 🚀
- [XPerfect](https://github.com/8100print/XPerfect) support
- [KeyboardChatterBlocker](https://github.com/fangshenghan/KeyboardChatterBlocker) built-in!

join the [discord server!](https://discord.gg/mAzAghu5Xq)

here below is a screenshot from version 1.1.0.1
![gameplay](assets/gameplay.png)

## build it yourself

want to compile from source? easy.

### what you need
- **.NET SDK 6.0+** — https://dotnet.microsoft.com/download
- **ADOFAI** installed via Steam
- **UnityModManager** installed into ADOFAI — https://www.nexusmods.com/site/mods/21
- *(optional)* **Python 3** — only if `Fonts/` contains `.otf` files (auto-converts to `.ttf`)
- *(optional)* **Unity 6000.3.10f1** — only if you want to rebuild AssetBundles. otherwise use `SKIP_BUNDLE=1`

### setup (run once)
```
# macOS / Linux
./setup.sh

# Windows
setup.bat
```
checks tools, primes NuGet packages. tells you what's missing.

### build
use this command below to build the mod
```bash
dotnet build -c Release
```
outputs:
- `dist/KorenResourcePack.dll`
- `dist/KorenResourcePack.zip` (the distributable)

### install
use this command below to install the mod into your ADOFAI game
```bash
dotnet build -c Release -p:Install=true
```
outputs:
- copies mod into `<game>/Mods/KorenResourcePack/`

### options
- `SKIP_BUNDLE=1` — skip Unity AssetBundle rebuild (use existing `Bundles/`)
- `FORCE_BUNDLE=1` — force rebuild even if sources unchanged
- `LEGACY=1` — build for pre-3.1.0 / r141 ADOFAI

### custom paths
auto-detects ADOFAI in default Steam locations. override:
```
# Steam library on a different drive?
GAME="/my/custom/path/A Dance of Fire and Ice" ./koren_build.sh

# or pass to dotnet directly
dotnet build -c Release -p:Install=true -p:Game="/my/custom/path"
```
