using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        public static KeyViewerSettings Settings;

        public static readonly Color Background = new(0.5607843f, 0.2352941f, 1, 0.1960784f);
        public static readonly Color BackgroundClicked = Color.white;
        public static readonly Color Outline = new(0.5529412f, 0.2431373f, 1);
        public static readonly Color OutlineClicked = Color.white;
        public static readonly Color Text = Color.white;
        public static readonly Color TextClicked = Color.black;
        public static readonly Color RainColor = new(0.5137255f, 0.1254902f, 0.858823538f);
        public static readonly Color RainColor2 = Color.white;
        public static readonly Color RainColor3 = Color.magenta;

        public static readonly byte[] BackSequence8 = Array.Empty<byte>();
        public static readonly byte[] BackSequence10 = new byte[] { 8, 9 };
        public static readonly byte[] BackSequence12 = new byte[] { 9, 8, 10, 11 };
        public static readonly byte[] BackSequence14 = new byte[] { 9, 8, 10, 11, 12, 13 };
        public static readonly byte[] BackSequence16 = new byte[] { 12, 13, 9, 8, 10, 11, 14, 15 };
        public static readonly byte[] BackSequence20 = new byte[] { 12, 13, 9, 8, 10, 11, 14, 15, 17, 16, 18, 19 };

        static readonly string[] KeyLayoutNames = { "12K", "16K", "20K", "10K", "8K", "14K" };
        static readonly string[] FootKeyLayoutNames = { "Off", "2K", "4K", "6K", "8K", "10K", "12K", "14K", "16K" };

        static KeyViewer()
        {
            var all = (KeyCode[])Enum.GetValues(typeof(KeyCode));
            AllKeyCodes = Array.FindAll(all, k => !k.ToString().StartsWith("Joystick"));
        }


        GameObject KeyViewerObject;
        GameObject KeyViewerSizeObject;
        Canvas Canvas;
        Key[] Keys;
        Key Kps;
        int lastKps;
        int lastTotal;
        Key Total;
        Queue<long> PressTimes;
        Queue<long>[] keyPressTimes;
        int[] lastPerKeyKps;
        Stopwatch Stopwatch;
        bool KeyChangeExpanded;
        bool GhostRainChangeExpanded;
        bool TextChangeExpanded;
        bool[] ColorExpanded;
        int SelectedKey = -1;
        int changeState;

        static string ConfigPath
        {
            get
            {
                if (configPath == null)
                {
                    string modPath = Path.GetDirectoryName(Main.Mod?.Path);
                    configPath = Path.Combine(modPath ?? Application.persistentDataPath, "config", "settings.json");
                }
                return configPath;
            }
        }
        static string configPath;

        Sprite keyBackgroundSprite;
        Sprite keyOutlineSprite;
        Sprite ghostRainSprite;
        public static KeyViewer instance;
        private RainSystem rainSystem;
        static Dictionary<string, int> fontNameIndex;
        private static readonly KeyCode[] AllKeyCodes;
        private KeyviewerStyle cachedKeyStyle = (KeyviewerStyle)(-1);
        private KeyCode[] cachedMainKeys;
        private FootKeyviewerStyle cachedFootStyle = (FootKeyviewerStyle)(-1);
        private KeyCode[] cachedFootKeys;
        private KeyCode[] cachedGhostKeys;
        private bool[] ghostKeyStates;
        private Dictionary<TMP_FontAsset, Material> shadowMaterials = new Dictionary<TMP_FontAsset, Material>();
        static readonly List<FontEntry> fontList = new List<FontEntry>();
        private bool wasEnabled;
        private bool fontRestored;
        private string lastAppliedFontName;


        void Awake()
        {
            instance = this;
            LoadSettings();
            I18n.Load();
            I18n.Lang = Settings.Language;
            rainSystem = new RainSystem(Settings);
            TryLoadResources();
            rainSystem.GhostRainSprite = ghostRainSprite;
            wasEnabled = HostKeyViewerEnabled();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void RestoreFontOnce()
        {
            if (fontNameIndex == null || fontRestored || string.IsNullOrEmpty(Settings.FontName)) return;
            if (fontNameIndex.TryGetValue(Settings.FontName, out int idx))
            {
                Settings.FontIndex = idx;
                UpdateAllFonts();
                SaveSettings();
            }
            fontRestored = true;
        }

        void OnEnable()
        {
            if (HostKeyViewerEnabled()) EnableKeyViewer();
            else DisableKeyViewer();
            if (!IsDmNoteMode() && Settings.CustomPositionEnabled)
            {
                ResetKeyViewerPosition();
                ResetFootKeyViewerPosition();
            }
        }

        void OnDisable()
        {
            DisableKeyViewer();
        }

        void OnDestroy()
        {
            SaveSettings();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            rainSystem?.ClearAll(Keys);
            foreach (var mat in shadowMaterials.Values)
                Destroy(mat);
            shadowMaterials.Clear();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SaveSettings();
            for (int i = fontList.Count - 1; i >= 0; i--)
                if (fontList[i].font == null) fontList.RemoveAt(i);
            if (fontList.Count == 0 || Settings.FontIndex >= fontList.Count)
                Settings.FontIndex = 0;
            fontRestored = false;
            LinkFallbackFonts();
            rainSystem.ClearActiveDrops(Keys);
        }

        void Start()
        {
            RestoreFontOnce();
        }

        void Update()
        {
            if (!Application.isFocused) return;

            bool enabled = HostKeyViewerEnabled();
            bool dmNoteMode = IsDmNoteMode();
            if (wasEnabled != enabled || (KeyViewerObject != null && dmNoteLayoutActive != dmNoteMode))
            {
                if (KeyViewerObject != null)
                    DisableKeyViewer();
                if (enabled)
                {
                    EnableKeyViewer();
                    if (!dmNoteMode && Settings.CustomPositionEnabled)
                    {
                        ResetKeyViewerPosition();
                        ResetFootKeyViewerPosition();
                    }
                }
                wasEnabled = enabled;
            }
            if (KeyViewerObject != null && enabled)
            {
                if (dmNoteMode && IsDmNoteLayoutDirty())
                {
                    DisableKeyViewer();
                    EnableKeyViewer();
                    if (KeyViewerObject == null) return;
                }
                string krpFont = KorenResourcePack.Main.settings != null ? KorenResourcePack.Main.settings.fontName : null;
                if (krpFont != lastAppliedFontName)
                {
                    lastAppliedFontName = krpFont;
                    UpdateAllFonts();
                }

                long now = Stopwatch.ElapsedMilliseconds;
                if (dmNoteMode)
                {
                    UpdateDmNoteTransform();
                    ProcessDmNoteKeysInUpdate(now);
                    if (DmNoteRainEnabled()) rainSystem.UpdateEffects(Keys, now);
                }
                else
                {
                    ProcessKeySelection();
                    ProcessMainAndFootKeysInUpdate(now);
                    ProcessKpsInUpdate(now);
                    ProcessPerKeyKpsInUpdate(now);
                    ProcessGhostKeysInUpdate(now);
                    if (Settings.EnableRainEffect) rainSystem.UpdateEffects(Keys, now);
                }
            }
        }


        private void LoadSettings()
        {
            string json = KorenResourcePack.Main.settings != null ? KorenResourcePack.Main.settings.KeyViewerJkvJson : null;
            if (!string.IsNullOrEmpty(json))
            {
                try { Settings = JsonUtility.FromJson<KeyViewerSettings>(json); }
                catch (Exception e)
                {
                    Main.Mod?.Logger?.Error($"Failed to parse JKV settings: {e.Message}");
                    Settings = null;
                }
            }

            if (Settings == null)
            {
                Settings = new KeyViewerSettings();
                SaveSettings();
                return;
            }

            if (Settings.Version < 2)
            {
                const float refW = 1920f, refH = 1080f;
                float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
                Settings.MainKeyViewerPosition = new Vector2(
                    Clamp01(Settings.MainKeyViewerPosition.x / refW),
                    1f - Clamp01(Settings.MainKeyViewerPosition.y / refH));
                Settings.FootKeyViewerPosition = new Vector2(
                    Clamp01(Settings.FootKeyViewerPosition.x / refW),
                    1f - Clamp01(Settings.FootKeyViewerPosition.y / refH));
                Settings.Version = 2;
            }
            Settings.key8Text = Settings.key8Text ?? new string[8];
            Settings.key10Text = Settings.key10Text ?? new string[10];
            Settings.key12Text = Settings.key12Text ?? new string[12];
            Settings.key14Text = Settings.key14Text ?? new string[14];
            Settings.key16Text = Settings.key16Text ?? new string[16];
            Settings.key20Text = Settings.key20Text ?? new string[20];
            Settings.footkey2Text = Settings.footkey2Text ?? new string[2];
            Settings.footkey4Text = Settings.footkey4Text ?? new string[4];
            Settings.footkey6Text = Settings.footkey6Text ?? new string[6];
            Settings.footkey8Text = Settings.footkey8Text ?? new string[8];
            Settings.footkey10Text = Settings.footkey10Text ?? new string[10];
            Settings.footkey12Text = Settings.footkey12Text ?? new string[12];
            Settings.footkey14Text = Settings.footkey14Text ?? new string[14];
            Settings.footkey16Text = Settings.footkey16Text ?? new string[16];
            Settings.Count = Settings.Count ?? new int[36];
            Settings.EnsurePerKeyFontSizes();
            if (Settings.PerKeyBackground == null || Settings.PerKeyBackground.Length != 38)
                Settings.InitPerKeyColors();

            SaveSettings();
        }

        public void SaveSettings()
        {
            try
            {
                if (KorenResourcePack.Main.settings == null) return;
                KorenResourcePack.Main.settings.KeyViewerJkvJson = JsonUtility.ToJson(Settings, false);
                SyncKeysToKeyLimiter();
                KorenResourcePack.SettingsGui.MarkSettingsDirty();
            }
            catch (Exception e)
            {
                Main.Mod?.Logger?.Error($"Failed to save JKV settings: {e.Message}");
            }
        }

        public void ApplyImportedSettings(KeyViewerSettings imported)
        {
            if (imported == null) return;

            try
            {
                bool wasVisible = KeyViewerObject != null;
                if (wasVisible)
                    DisableKeyViewer();
                else
                    rainSystem?.ClearAll(Keys);

                Settings = imported;
                EnsureImportedSettingsReady();
                rainSystem = new RainSystem(Settings) { GhostRainSprite = ghostRainSprite };

                SelectedKey = -1;
                changeState = 0;
                lastKps = -1;
                lastTotal = -1;
                cachedKeyStyle = (KeyviewerStyle)(-1);
                cachedFootStyle = (FootKeyviewerStyle)(-1);
                cachedMainKeys = null;
                cachedFootKeys = null;
                cachedGhostKeys = null;

                if (wasVisible && Settings.Enabled)
                {
                    EnableKeyViewer();
                    if (Settings.CustomPositionEnabled)
                    {
                        ResetKeyViewerPosition();
                        ResetFootKeyViewerPosition();
                    }
                    RefreshAllCountDisplay();
                }

                wasEnabled = Settings.Enabled;
                SaveSettings();
            }
            catch (Exception e)
            {
                Main.Mod?.Logger?.Error($"Failed to apply imported JKV settings: {e.Message}");
            }
        }

        private static void EnsureImportedSettingsReady()
        {
            if (Settings.Count == null || Settings.Count.Length != 36)
            {
                int[] old = Settings.Count;
                Settings.Count = new int[36];
                if (old != null)
                    Array.Copy(old, Settings.Count, Math.Min(old.Length, Settings.Count.Length));
            }
            Settings.key8Text = Settings.key8Text ?? new string[8];
            Settings.key10Text = Settings.key10Text ?? new string[10];
            Settings.key12Text = Settings.key12Text ?? new string[12];
            Settings.key14Text = Settings.key14Text ?? new string[14];
            Settings.key16Text = Settings.key16Text ?? new string[16];
            Settings.key20Text = Settings.key20Text ?? new string[20];
            Settings.footkey2Text = Settings.footkey2Text ?? new string[2];
            Settings.footkey4Text = Settings.footkey4Text ?? new string[4];
            Settings.footkey6Text = Settings.footkey6Text ?? new string[6];
            Settings.footkey8Text = Settings.footkey8Text ?? new string[8];
            Settings.footkey10Text = Settings.footkey10Text ?? new string[10];
            Settings.footkey12Text = Settings.footkey12Text ?? new string[12];
            Settings.footkey14Text = Settings.footkey14Text ?? new string[14];
            Settings.footkey16Text = Settings.footkey16Text ?? new string[16];
            Settings.EnsurePerKeyFontSizes();
            if (Settings.PerKeyBackground == null || Settings.PerKeyBackground.Length != 38)
                Settings.InitPerKeyColors();
        }
    }
}
