using System;
using UnityEngine;
using UnityModManagerNet;

namespace KorenResourcePack
{
    internal static class JkvBridge
    {
        internal static bool IsSimpleMode
        {
            get
            {
                return Main.settings != null
                    && string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);
            }
        }

        internal static bool IsDmNoteMode
        {
            get
            {
                return Main.settings != null
                    && string.Equals(Main.settings.KeyViewerMode, "dmnote", StringComparison.OrdinalIgnoreCase);
            }
        }

        internal static bool IsJkvRendererMode => IsSimpleMode || IsDmNoteMode;

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

        internal static void Tick(bool show)
        {
            try
            {
                if (show) JipperKeyViewer.Main.Create();
                JipperKeyViewer.Main.SetActive(show);
            }
            catch { }
        }

        internal static void Hide()
        {
            Tick(false);
        }

        internal static void Shutdown()
        {
            try { JipperKeyViewer.Main.Shutdown(); }
            catch { }
        }

        internal static void DrawSettingsGui()
        {
            var inst = JipperKeyViewer.KeyViewer.KeyViewer.instance;
            if (inst != null) inst.DrawSettingsWindow();
            else GUILayout.Label("JipperKeyViewer is initializing...");
        }

        internal static void SaveSettings()
        {
            try { JipperKeyViewer.KeyViewer.KeyViewer.instance?.SaveSettings(); }
            catch { }
        }

        internal static void ApplyImportedSettings(JipperKeyViewer.KeyViewer.KeyViewerSettings imported)
        {
            if (imported == null) return;
            try
            {
                JipperKeyViewer.KeyViewer.KeyViewer.Settings = imported;
                var inst = JipperKeyViewer.KeyViewer.KeyViewer.instance;
                if (inst != null) inst.ApplyImportedSettings(imported);
                else if (Main.settings != null)
                    Main.settings.KeyViewerJkvJson = JsonUtility.ToJson(imported, false);
            }
            catch { }
        }

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

        internal static int[] GetActiveKeyLimiterCodes()
        {
            try { return JipperKeyViewer.KeyViewer.KeyViewer.GetActiveLimiterKeyCodes(); }
            catch { return null; }
        }
    }
}
