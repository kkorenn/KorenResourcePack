using System;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private const string HarmonyId = "koren.koren_resource_pack";

        private static Settings settings;
        private static UnityModManager.ModEntry mod;
        private static Harmony harmony;
        private static GUIStyle percentStyle;
        private static GUIStyle percentShadowStyle;
        private static GUIStyle rightStatusStyle;
        private static GUIStyle rightStatusShadowStyle;
        private static GUIStyle comboValueStyle;
        private static GUIStyle comboValueShadowStyle;
        private static GUIStyle judgementStyle;
        private static GUIStyle judgementShadowStyle;
        private static Font preferredHudFont;
        private static bool modEnabled = true;
        private static bool runVisible;
        private static int perfectCombo;
        private static readonly int[] judgementCounts = new int[12];
        private static int lastJudgementSlot = 4;
        private static float comboPulseStartTime = -1f;
        private static float comboPulsePeakScale = 1.24f;
        private static float comboPulseOutDuration = 0.075f;
        private static float comboPulseSettleDuration = 0.18f;
        private static Text trackedLevelNameText;
        private static Vector2 trackedLevelNameOriginalPosition;
        private static int trackedLevelNameOriginalFontSize;
        private static readonly float[] JudgementSlotWeights = { 0.85f, 1f, 1.1f, 1.2f, 1.7f, 1.2f, 1.1f, 1f, 0.85f };
        private static readonly Color[] JudgementSlotColors =
        {
            new Color(0.78f, 0.35f, 1f, 1f),
            new Color(1f, 0.22f, 0.22f, 1f),
            new Color(1f, 0.44f, 0.31f, 1f),
            new Color(0.63f, 1f, 0.31f, 1f),
            new Color(0.38f, 1f, 0.31f, 1f),
            new Color(0.63f, 1f, 0.31f, 1f),
            new Color(1f, 0.44f, 0.31f, 1f),
            new Color(1f, 0.22f, 0.22f, 1f),
            new Color(0.78f, 0.35f, 1f, 1f)
        };

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            modEnabled = true;
            runVisible = false;
            perfectCombo = 0;
            ResetJudgementDisplay();
            comboPulseStartTime = -1f;

            try
            {
                settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Warning] Settings load failed, using defaults: " + ex.Message);
                settings = new Settings();
            }

            try { LoadPlayCount(); }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Warning] PlayCount load failed: " + ex.Message);
            }

            modEntry.OnToggle = OnToggle;
            modEntry.OnFixedGUI = OnFixedGUI;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnUnload = OnUnload;

            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(Main).Assembly);
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            modEntry.Logger.Log("koren resource pack loaded.");

            try
            {
                Thread updateThread = new Thread(() => CheckForUpdates(modEntry));
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
                DisableRuntimeState();
                modEntry.Logger.Log("koren resource pack disabled at runtime.");
                return true;
            }

            perfectCombo = 0;
            ResetJudgementDisplay();
            comboPulseStartTime = -1f;
            runVisible = DetectActiveRun();
            if (runVisible)
            {
                AdjustLevelNameUi();
            }

            modEntry.Logger.Log("koren resource pack enabled at runtime.");
            return true;
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            harmony?.UnpatchAll(HarmonyId);
            RestoreLevelNameUi();
            DisposePlayCount();
            modEntry.Logger.Log("koren resource pack unloaded.");
            return true;
        }

        private static void OnSceneUnloaded(Scene _)
        {
            OnRunHide();
            SetRunVisible(false, "sceneUnloaded");
        }

        private static void OnFixedGUI(UnityModManager.ModEntry modEntry)
        {
            if (settings == null || !modEnabled)
            {
                return;
            }

            AdjustLevelNameUi();

            float progress = GetLevelProgress();
            if (progress < 0f)
            {
                return;
            }

            if (settings.progressBarOn) DrawTopProgressBar(progress);
            if (settings.statusOn || settings.bpmOn)
            {
                DrawStatusText(progress, settings.statusOn, settings.bpmOn);
            }
            if (settings.comboOn)
            {
                DrawPerfectCombo();
            }
            if (settings.judgementOn)
            {
                DrawJudgementDisplay();
            }
            if (settings.holdOn) DrawHoldBehaviorLabel();
            if (settings.attemptOn) DrawAttempt();
            if (settings.timingScaleOn) DrawTimingScale();
        }

        private static float GetLevelProgress()
        {
            try
            {
                if (!modEnabled)
                {
                    return -1f;
                }

                scrController controller = scrController.instance;

                if (!runVisible || controller == null || scrLevelMaker.instance == null || scrLevelMaker.instance.listFloors == null)
                {
                    return -1f;
                }

                if (controller.paused)
                {
                    return -1f;
                }

                if (scrLevelMaker.instance.listFloors.Count <= 1)
                {
                    return -1f;
                }

                return Mathf.Clamp01(controller.percentComplete);
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Warning] Progress read failed: " + ex.Message);
                return -1f;
            }
        }

        private static void SetRunVisible(bool visible, string reason)
        {
            if (!modEnabled)
            {
                return;
            }

            if (runVisible == visible)
            {
                return;
            }

            runVisible = visible;
            mod?.Logger?.Log("[State] " + (visible ? "Show" : "Hide") + " via " + reason);
        }

        private static void ResetRunData(string reason)
        {
            perfectCombo = 0;
            ResetJudgementDisplay();
            currentMarginScale = 1f;
            comboPulseStartTime = -1f;
            mod?.Logger?.Log("[State] Reset run data via " + reason);
        }

        private static void DisableRuntimeState()
        {
            runVisible = false;
            perfectCombo = 0;
            ResetJudgementDisplay();
            comboPulseStartTime = -1f;
            RestoreLevelNameUi();
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
    }
}
