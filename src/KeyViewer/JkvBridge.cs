using System;
using UnityEngine;
using UnityModManagerNet;

namespace KorenResourcePack
{
    /// <summary>
    /// KRP-side bridge that drives the embedded JipperKeyViewer subsystem.
    /// JKV replaces the keyviewer "simple" mode; the "dmnote" (advanced) mode keeps KRP's own renderer.
    /// </summary>
    internal static class JkvBridge
    {
        /// <summary>True when the keyviewer mode selector is on "simple" (= JipperKeyViewer).</summary>
        internal static bool IsSimpleMode
        {
            get
            {
                return Main.settings != null
                    && string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Bind the ModEntry only. GameObject creation is deferred (graphics device not
        /// ready during mod load — see JipperKeyViewer.Main.Create).</summary>
        internal static void Initialize(UnityModManager.ModEntry modEntry)
        {
            try
            {
                JipperKeyViewer.Main.Bind(modEntry);
            }
            catch (Exception e)
            {
                modEntry?.Logger?.Log("[Warning] JipperKeyViewer bind failed: " + e.Message);
            }
        }

        /// <summary>Show/hide the JKV overlay for this frame. Lazily creates the GameObject on first
        /// show, once the graphics device is up (called from OnFixedGUI).</summary>
        internal static void Tick(bool show)
        {
            try
            {
                if (show) JipperKeyViewer.Main.Create();
                JipperKeyViewer.Main.SetActive(show);
            }
            catch { }
        }

        /// <summary>Force-hide the JKV overlay (toggle-off / scene teardown).</summary>
        internal static void Hide()
        {
            Tick(false);
        }

        /// <summary>Destroy the JKV GameObject (unload).</summary>
        internal static void Shutdown()
        {
            try { JipperKeyViewer.Main.Shutdown(); }
            catch { }
        }

        /// <summary>Render JKV's own settings UI inline inside KRP's keyviewer tab (simple mode).</summary>
        internal static void DrawSettingsGui()
        {
            var inst = JipperKeyViewer.KeyViewer.KeyViewer.instance;
            if (inst != null) inst.DrawSettingsWindow();
            else GUILayout.Label("JipperKeyViewer is initializing...");
        }

        /// <summary>Persist JKV settings (KRP save hooks).</summary>
        internal static void SaveSettings()
        {
            try { JipperKeyViewer.KeyViewer.KeyViewer.instance?.SaveSettings(); }
            catch { }
        }

        /// <summary>The user-facing "share keys with KeyLimiter" toggle for simple mode lives in JKV's
        /// own settings (KeyViewerGUI). This is the single source of truth for whether simple mode mirrors
        /// its keys into the KeyLimiter and locks the KeyLimiter editor. False when JKV isn't ready.</summary>
        internal static bool SyncToKeyLimiterOn
        {
            get
            {
                try
                {
                    var s = JipperKeyViewer.KeyViewer.KeyViewer.Settings;
                    return s != null && s.SyncToKeyLimiter;
                }
                catch { return false; }
            }
        }

        /// <summary>Active main+foot key codes of the JKV layout, for KeyLimiter sync in simple mode.
        /// Returns null/empty when JKV isn't ready (caller must not wipe existing keys).</summary>
        internal static int[] GetActiveKeyLimiterCodes()
        {
            try { return JipperKeyViewer.KeyViewer.KeyViewer.GetActiveLimiterKeyCodes(); }
            catch { return null; }
        }
    }
}
