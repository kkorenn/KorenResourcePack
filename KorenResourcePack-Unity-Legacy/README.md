# KorenResourcePack-Unity-Legacy

Unity 2022.3 project for building legacy ADOFAI-compatible AssetBundles.

Use Unity Editor 2022.3.62f2 and run:

```
/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/koren/Documents/KorenResourcePack/KorenResourcePack-Unity-Legacy \
  -executeMethod CreateAssetBundle.BuildAllAssetBundles \
  -logFile /Users/koren/Documents/KorenResourcePack/.unity-legacy-build.log
```

Copy the outputs to:

- `Bundles/Legacy/korenresourcepackbundle`
- `Bundles/Legacy/Linux/korenresourcepackbundle`
- `Bundles/Legacy/Mac/korenresourcepackbundle`

The legacy mod loader checks those paths before falling back to the normal `Bundles/` tree.
