# Metadata

This directory is the source of truth for Unity Mod Manager manifests. Build and release packaging copy one of these files into the shipped mod as `Info.json`.

- [`Info.json`](Info.json) - current ADOFAI build, packaged with `KorenResourcePack.dll`.
- [`Info_legacy.json`](Info_legacy.json) - legacy pre-3.1.0 / r141 build, packaged as `Info.json` with `KorenResourcePack_legacy.dll`.

Keep each manifest's `AssemblyName` aligned with the DLL produced by the matching build.
