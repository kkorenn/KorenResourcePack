using UnityModManagerNet;
using UnityEngine;

namespace JipperKeyViewer
{
    public class Main
    {
        public static UnityModManager.ModEntry Mod { get; private set; }

        static GameObject KeyViewerGO;

        public static bool Exists { get { return KeyViewerGO != null; } }

        public static void Bind(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
        }

        public static void Create()
        {
            if (Mod == null || KeyViewerGO != null) return;
            KeyViewerGO = new GameObject("KorenJipperKeyViewer");
            GameObject.DontDestroyOnLoad(KeyViewerGO);
            KeyViewerGO.AddComponent<KeyViewer.KeyViewer>();
        }

        public static void SetActive(bool value)
        {
            if (KeyViewerGO != null && KeyViewerGO.activeSelf != value)
                KeyViewerGO.SetActive(value);
        }

        public static void Shutdown()
        {
            if (KeyViewerGO != null)
            {
                GameObject.Destroy(KeyViewerGO);
                KeyViewerGO = null;
            }
        }
    }
}
