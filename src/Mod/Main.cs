using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace KorenResourcePack
{
    public static class Main
    {
        private const string HarmonyId = "koren.koren_resource_pack";

        internal static Settings settings;
        internal static Settings SettingsRef { get { return settings; } }
        internal static UnityModManager.ModEntry mod;
        private static Harmony harmony;
        
        internal static Font preferredHudFont;
        internal static bool modEnabled = true;
        
        internal static bool runVisible;
        internal static int perfectCombo;
        
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
            settings.EnsureColorRanges();
            SettingsGui.SyncSimpleKeysToKeyLimiter();
            Localization.Initialize(modEntry, settings.language);

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
            
            harmony.PatchAllUncategorized(typeof(Main).Assembly);
            XPerfectRecursionGuard.TryApply(harmony, modEntry);
            Tweaks.RefreshTweaks();
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
                ResourceChanger.RestoreChangedResources();
                Tweaks.RestoreTweaks();
                DisableRuntimeState();
                EffectRemover.RestoreEditorSaveButtons();
                modEntry.Logger.Log("koren resource pack disabled at runtime.");
                return true;
            }

            perfectCombo = 0;
            Judgement.ResetJudgementDisplay();
            Combo.comboPulseStartTime = -1f;
            runVisible = DetectActiveRun();
            Tweaks.RefreshTweaks();
            ResourceChanger.RefreshChangedResources();
            EffectRemover.RefreshEditorSaveButtons();
            if (runVisible)
            {
                LevelName.AdjustLevelNameUi();
            }

            modEntry.Logger.Log("koren resource pack enabled at runtime.");
            return true;
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            
            try { if (settings != null) settings.Save(modEntry); } catch { }

            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            ResourceChanger.RestoreChangedResources();
            Tweaks.RestoreTweaks();
            EffectRemover.RestoreEditorSaveButtons();
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
            ResourceChanger.ClearSceneCaches();
            Tweaks.ClearSceneCaches();
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
            Overlay.HideOverlay();      
            KeyViewer.HideKeyViewer();
        }

        private static void OnFixedGUI(UnityModManager.ModEntry modEntry)
        {
            if (settings == null || !modEnabled)
            {
                Overlay.HideOverlay();      
                KeyViewer.HideKeyViewer();
                return;
            }

            SettingsGui.FlushAutosaveIfDue(modEntry);
            PlayCount.FlushSaveIfDue();
            SettingsGui.FlushPendingResourceChangerActions();

            KeyLimiter.RefreshPlayerControlState();
            KeyViewer.KeyViewerPollEvent();
            LevelName.AdjustLevelNameUi();

            if (settings.keyViewerOn)
                KeyViewer.DrawKeyViewer();
            else
                KeyViewer.HideKeyViewer();

            float progress = GetLevelProgress();
            if (progress < 0f)
            {
                if (Overlay.overlayBuilt)
                    Overlay.HideOverlay();
                return;
            }

            PlayCount.ObserveProgress(progress);

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
        }

        private static MemberInfo _editorStrictEditingMember;
        private static bool _editorStrictEditingLookupAttempted;
        private const float ProgressReadWarningInterval = 5f;
        private static float _lastProgressReadWarningTime = -999f;

        private static bool IsEditorStrictlyEditing()
        {
            try
            {
                if (scnGame.instance != null)
                    return false;
                scnEditor ed = scnEditor.instance;
                if (ed == null)
                    return false;
                if (!_editorStrictEditingLookupAttempted)
                {
                    _editorStrictEditingLookupAttempted = true;
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
                scrLevelMaker levelMaker = scrLevelMaker.instance;

                if (!runVisible || controller == null || levelMaker == null || levelMaker.listFloors == null)
                    return -1f;

                if (controller.paused)
                    return -1f;

                if (levelMaker.listFloors.Count <= 1)
                    return -1f;

                return Mathf.Clamp01(controller.percentComplete);
            }
            catch (Exception ex)
            {
                LogProgressReadWarning(ex);
                return -1f;
            }
        }

        private static void LogProgressReadWarning(Exception ex)
        {
            float now = 0f;
            try { now = Time.unscaledTime; } catch { }
            if (now - _lastProgressReadWarningTime < ProgressReadWarningInterval)
                return;

            _lastProgressReadWarningTime = now;
            mod?.Logger?.Log("[Warning] Progress read failed: " + ex.Message);
        }

        internal static void SetRunVisible(bool visible, string reason)
        {
            if (!modEnabled)
                return;

            if (visible)
                ResourceChanger.RefreshChangedResources();

            if (runVisible == visible)
                return;

            runVisible = visible;
            if (!visible)
                KeyLimiter.ResetPlayerControlState();
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
        }

        private static bool DetectActiveRun()
        {
            try
            {
                scrController controller = scrController.instance;
                scrLevelMaker levelMaker = scrLevelMaker.instance;
                return controller != null
                       && !controller.paused
                       && levelMaker != null
                       && levelMaker.listFloors != null
                       && levelMaker.listFloors.Count > 1;
            }
            catch
            {
                return false;
            }
        }

        public static void OnRunHide()
        {
            PlayCount.OnRunHide();
            SetRunVisible(false, "runHide");
            Overlay.HideOverlay();
            KeyViewer.HideKeyViewer();
        }

        public static void OnRunDeath()
        {
            PlayCount.OnRunDeath();
        }

        public static void OnRunClear()
        {
            PlayCount.OnRunClear();
        }

        public static void InvalidatePercentCaches()
        {
            Status.InvalidatePercentCaches();
        }

        public static Font GetPreferredHudFont()
        {
            return FontLoader.GetPreferredHudFont();
        }
    }
}
