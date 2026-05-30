// JipperKeyViewer integration entry / JipperKeyViewer 集成入口
// NOTE: This is NOT a UnityModManager entry. JipperKeyViewer is embedded as a sub-system of
// KorenResourcePack, so KRP drives its lifecycle (see KorenResourcePack.JkvBridge) instead of
// UnityModManager calling Load/OnToggle directly. / 注意：这不是 UMM 入口，由 KRP 驱动生命周期。
using UnityModManagerNet;
using UnityEngine;

namespace JipperKeyViewer
{
    /// <summary>
    /// Host class for the embedded JipperKeyViewer. Owns the persistent GameObject and the
    /// ModEntry reference used for logging and path/asset resolution. / 嵌入式 JipperKeyViewer 的宿主类。
    /// </summary>
    public class Main
    {
        /// <summary>Reference to the mod entry for logging and path resolution / Mod 条目引用，用于日志和路径</summary>
        public static UnityModManager.ModEntry Mod { get; private set; }

        /// <summary>The persistent GameObject hosting the KeyViewer component / 持有 KeyViewer 组件的持久化 GameObject</summary>
        static GameObject KeyViewerGO;

        /// <summary>Whether the KeyViewer GameObject currently exists / KeyViewer GameObject 当前是否存在</summary>
        public static bool Exists { get { return KeyViewerGO != null; } }

        /// <summary>
        /// Bind the owning ModEntry. Does NOT create the GameObject — asset loading needs the
        /// graphics device, which is not ready during UMM mod load. / 仅绑定 ModEntry，不创建 GameObject。
        /// </summary>
        public static void Bind(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
        }

        /// <summary>
        /// Create the persistent KeyViewer GameObject (idempotent). Must be called after the graphics
        /// device is initialized (e.g. first OnFixedGUI tick), never during mod load. / 创建持久 GameObject（须在图形设备就绪后）。
        /// </summary>
        public static void Create()
        {
            if (Mod == null || KeyViewerGO != null) return;
            KeyViewerGO = new GameObject("KorenJipperKeyViewer");
            GameObject.DontDestroyOnLoad(KeyViewerGO);
            KeyViewerGO.AddComponent<KeyViewer.KeyViewer>();
        }

        /// <summary>
        /// Show or hide the overlay without destroying it. / 显示或隐藏覆盖层而不销毁。
        /// </summary>
        public static void SetActive(bool value)
        {
            if (KeyViewerGO != null && KeyViewerGO.activeSelf != value)
                KeyViewerGO.SetActive(value);
        }

        /// <summary>
        /// Destroy the KeyViewer GameObject and release it. / 销毁 KeyViewer GameObject 并释放。
        /// </summary>
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
