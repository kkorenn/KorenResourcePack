using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace KorenResourcePack
{
    public static class Main
    {
        private const string HarmonyId = "koren.koren_resource_pack";

        // Promoted to internal so feature classes pulled out of `partial Main` can still
        // read shared state without round-tripping through accessors.
        internal static Settings settings;
        internal static Settings SettingsRef { get { return settings; } }
        internal static UnityModManager.ModEntry mod;
        private static Harmony harmony;
        // GUIStyle pool moved to the Styles class. Font cache stays here — populated from
        // the bundle by Font.cs and consumed by Styles.EnsurePercentStyle.
        internal static Font preferredHudFont;
        internal static bool modEnabled = true;
        // Run state shared across feature classes — Judgement reads these to gate hit
        // counting; Combo reads runVisible/perfectCombo to drive its display.
        internal static bool runVisible;
        internal static int perfectCombo;
        // Combo-pulse animation parameters live with Combo; kept as defaults here so
        // settings reset paths can fall back to known good values.
        // Level-name UI tracking moved to the LevelName class.
        // Judgement counters / slot weights / colors moved to the Judgement class.

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            modEnabled = true;
            runVisible = false;
            perfectCombo = 0;
            Judgement.ResetJudgementDisplay();
            Combo.comboPulseStartTime = -1f;

            try
            {
                settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Warning] Settings load failed, using defaults: " + ex.Message);
                settings = new Settings();
            }

            try { PlayCount.LoadPlayCount(); }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Warning] PlayCount load failed: " + ex.Message);
            }

            modEntry.OnToggle = OnToggle;
            modEntry.OnFixedGUI = OnFixedGUI;
            modEntry.OnGUI = SettingsGui.OnGUI;
            modEntry.OnSaveGUI = SettingsGui.OnSaveGUI;
            modEntry.OnUnload = OnUnload;

            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(Main).Assembly);
            XPerfectRecursionGuard.TryApply(harmony, modEntry);
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            modEntry.Logger.Log("koren resource pack loaded.");

            try
            {
                Thread updateThread = new Thread(() => Updater.CheckForUpdates(modEntry));
                updateThread.IsBackground = true;
                updateThread.Start();
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Warning] Update check failed to start: " + ex.Message);
            }

            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            modEnabled = value;

            if (!value)
            {
                ResourceChanger.RestoreOttoIcon();
                DisableRuntimeState();
                modEntry.Logger.Log("koren resource pack disabled at runtime.");
                return true;
            }

            perfectCombo = 0;
            Judgement.ResetJudgementDisplay();
            Combo.comboPulseStartTime = -1f;
            runVisible = DetectActiveRun();
            if (runVisible)
            {
                LevelName.AdjustLevelNameUi();
            }

            modEntry.Logger.Log("koren resource pack enabled at runtime.");
            return true;
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            ResourceChanger.RestoreOttoIcon();
            harmony?.UnpatchAll(HarmonyId);
            LevelName.RestoreLevelNameUi();
            PlayCount.DisposePlayCount();
            Overlay.DestroyOverlay();
            KeyViewer.DestroyKeyViewer();
            BundleLoader.UnloadBundle();
            modEntry.Logger.Log("koren resource pack unloaded.");
            return true;
        }

        private static void OnSceneUnloaded(Scene _)
        {
            PlayCount.OnRunHide();
            SetRunVisible(false, "sceneUnloaded");
            Overlay.HideOverlay();
            KeyViewer.HideKeyViewer();
        }

        private static void DisableRuntimeState()
        {
            runVisible = false;
            perfectCombo = 0;
            Judgement.ResetJudgementDisplay();
            Combo.comboPulseStartTime = -1f;
            LevelName.RestoreLevelNameUi();
            Overlay.HideOverlay();      // <-- FIX: hide TMP overlay
            KeyViewer.HideKeyViewer();
        }

        private static void OnFixedGUI(UnityModManager.ModEntry modEntry)
        {
            if (settings == null || !modEnabled)
            {
            Overlay.HideOverlay();      // <-- FIX: ensure hidden when disabled
            KeyViewer.HideKeyViewer();
                return;
            }

            KeyViewer.KeyViewerPollEvent();
            LevelName.AdjustLevelNameUi();

            float progress = GetLevelProgress();
            if (progress < 0f)
            {
            if (Overlay.overlayBuilt)
                Overlay.HideOverlay();
            KeyViewer.HideKeyViewer();
                return;
            }

             if (settings.progressBarOn) ProgressBar.DrawTopProgressBar(progress);
            
             bool useTmp = Overlay.TryUseTmpOverlay();
             if (useTmp)
             {
                 Overlay.ShowOverlay();
                 Overlay.TickOverlay(progress);
             }
             else
             {
                 if (Overlay.overlayBuilt) Overlay.HideOverlay();
                 if (settings.statusOn || settings.bpmOn)
                     Status.DrawStatusText(progress, settings.statusOn, settings.bpmOn);
                if (settings.comboOn) Combo.DrawPerfectCombo();
                if (settings.judgementOn) Judgement.DrawJudgementDisplay();
                if (settings.holdOn) Hold.DrawHoldBehaviorLabel();
                if (settings.attemptOn) Attempt.DrawAttempt();
                if (settings.timingScaleOn) TimingScale.DrawTimingScale();
            }

             if (settings.keyViewerOn && runVisible)
                 KeyViewer.DrawKeyViewer();
             else
                 KeyViewer.HideKeyViewer();
        }

        private static MemberInfo _editorStrictEditingMember;

        private static bool IsEditorStrictlyEditing()
        {
            try
            {
                if (scnGame.instance != null)
                    return false;
                scnEditor ed = scnEditor.instance;
                if (ed == null)
                    return false;
                if (_editorStrictEditingMember == null)
                {
                    const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                    _editorStrictEditingMember = (MemberInfo)typeof(scnEditor).GetField("inStrictlyEditingMode", bf)
                        ?? typeof(scnEditor).GetProperty("inStrictlyEditingMode", bf);
                }

                FieldInfo fi = _editorStrictEditingMember as FieldInfo;
                if (fi != null)
                {
                    object v = fi.GetValue(ed);
                    if (v is bool)
                        return (bool)v;
                }
                else
                {
                    PropertyInfo pi = _editorStrictEditingMember as PropertyInfo;
                    if (pi != null)
                    {
                        object v = pi.GetValue(ed, null);
                        if (v is bool)
                            return (bool)v;
                    }
                }
            }
            catch { }
            return false;
        }

        private static float GetLevelProgress()
        {
            try
            {
                if (!modEnabled)
                    return -1f;

                if (IsEditorStrictlyEditing())
                    return -1f;

                scrController controller = scrController.instance;

                if (!runVisible || controller == null || scrLevelMaker.instance == null || scrLevelMaker.instance.listFloors == null)
                    return -1f;

                if (controller.paused)
                    return -1f;

                if (scrLevelMaker.instance.listFloors.Count <= 1)
                    return -1f;

                return Mathf.Clamp01(controller.percentComplete);
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Warning] Progress read failed: " + ex.Message);
                return -1f;
            }
        }

        internal static void SetRunVisible(bool visible, string reason)
        {
            if (!modEnabled)
                return;

            if (runVisible == visible)
                return;

            runVisible = visible;
            mod?.Logger?.Log("[State] " + (visible ? "Show" : "Hide") + " via " + reason);
        }

        internal static void ResetRunData(string reason)
        {
            perfectCombo = 0;
            Judgement.ResetJudgementDisplay();
            KeyViewer.ResetKeyViewerStats();
            TimingScale.CurrentMarginScale = 1f;
            Combo.comboPulseStartTime = -1f;
            ProgressTracker.RunStartProgress = 0f;
            ProgressTracker.RunStartedFromFirstTile = true;
            mod?.Logger?.Log("[State] Reset run data via " + reason);
        }

        private static bool DetectActiveRun()
        {
            try
            {
                scrController controller = scrController.instance;
                return controller != null
                       && !controller.paused
                       && scrLevelMaker.instance != null
                       && scrLevelMaker.instance.listFloors != null
                       && scrLevelMaker.instance.listFloors.Count > 1;
            }
            catch
            {
                return false;
            }
        }

        public static void OnRunHide()
        {
            PlayCount.OnRunHide();
        }

        public static void OnRunDeath()
        {
            PlayCount.OnRunDeath();
        }

        public static void InvalidatePercentCaches()
        {
            Status.InvalidatePercentCaches();
        }

        public static Font GetPreferredHudFont() { return preferredHudFont; }

        public static void EnsureBundledFontsLoaded() { }

        public static List<string> bundledFontNames = new List<string>();

        public static void InvalidateOverlayFontCache() { }

        public static float hudCachedProgress;

        public static bool fontDropdownOpen;

        public static void ImportKeyViewerPreset() { }

        public static int GetKeyViewerTotal() => 0;

        public static void SetKeyViewerTotal(int total) { }

        public static List<KeyValuePair<string, int>> EnumerateKeyViewerCounters() => new List<KeyValuePair<string, int>>();

        public static void SetKeyViewerCount(string name, int count) { }

        public static void ResetAllKeyViewerCounters() { }
    }
}
