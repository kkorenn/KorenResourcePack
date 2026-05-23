# Unity Legacy

Unity 2022.3 project for building legacy ADOFAI-compatible AssetBundles.

Prefer running the legacy build from the repository root:

```bash
LEGACY=1 ./scripts/koren_build.sh

# or with dotnet directly
dotnet build -c Release -p:Legacy=true
```

For a manual Unity-only rebuild, use Unity Editor 2022.3.62f2 and run:

```
/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /path/to/KorenResourcePack/unity/legacy \
  -executeMethod CreateAssetBundle.BuildAllAssetBundles \
  -logFile /path/to/KorenResourcePack/.unity-legacy-build.log
```

Manual bundle outputs are written under `BuiltAssetBundles/`. Copy them to:

- `Bundles/Legacy/korenresourcepackbundle`
- `Bundles/Legacy/Linux/korenresourcepackbundle`
- `Bundles/Legacy/Mac/korenresourcepackbundle`

The legacy mod loader checks those paths before falling back to the normal `Bundles/` tree.
