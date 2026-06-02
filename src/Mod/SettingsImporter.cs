using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;
using JkvSettings = JipperKeyViewer.KeyViewer.KeyViewerSettings;

namespace KorenResourcePack
{
    internal enum SettingsImportSource
    {
        KeyboardChatterBlocker,
        JipperKeyViewer,
        JipperResourcePack,
        AdofaiTweaks,
        EnhancedEffectRemover
    }

    internal enum SettingsImportReplaceMode
    {
        ReplaceAll,
        ReplaceCertain,
        KeepOld
    }

    [Flags]
    internal enum SettingsImportKeyViewerPart
    {
        None = 0,
        KeysLayout = 1 << 0,
        Labels = 1 << 1,
        Colors = 1 << 2,
        Rain = 1 << 3,
        Counts = 1 << 4,
        PositionSize = 1 << 5,
        Display = 1 << 6,
        Font = 1 << 7,
        All = KeysLayout | Labels | Colors | Rain | Counts | PositionSize | Display | Font
    }

    internal sealed class SettingsImportOption
    {
        internal SettingsImportSource Source;
        internal UnityModManager.ModEntry Entry;
        internal string OptionId;
        internal string Label;
    }

    internal sealed class SettingsImportResult
    {
        internal bool Success;
        internal int ImportedCount;
        internal string Message;
    }

    internal static class SettingsImporter
    {
        private const BindingFlags AllMembers =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private sealed class ImportSpec
        {
            internal readonly SettingsImportSource Source;
            internal readonly string DisplayName;
            internal readonly string[] Aliases;

            internal ImportSpec(SettingsImportSource source, string displayName, params string[] aliases)
            {
                Source = source;
                DisplayName = displayName;
                Aliases = aliases;
            }
        }

        private static readonly ImportSpec[] Specs =
        {
            new ImportSpec(SettingsImportSource.KeyboardChatterBlocker, "KeyboardChatterBlocker",
                "KeyboardChatterBlocker", "Keyboard Chatter Blocker"),
            new ImportSpec(SettingsImportSource.JipperKeyViewer, "JipperKeyViewer",
                "JipperKeyViewer", "JipperKeyViewer-FileBased", "Jipper Key Viewer", "Jipper Key Viewer File Based"),
            new ImportSpec(SettingsImportSource.JipperResourcePack, "JipperResourcePack",
                "JipperResourcePack", "Jipper Resource Pack"),
            new ImportSpec(SettingsImportSource.AdofaiTweaks, "ADOFAI Tweaks",
                "AdofaiTweaks", "ADOFAI Tweaks"),
            new ImportSpec(SettingsImportSource.EnhancedEffectRemover, "EnhancedEffectRemover",
                "EnhancedEffectRemover", "Enhanced Effect Remover")
        };

        internal static List<SettingsImportOption> GetAvailableOptions()
        {
            List<SettingsImportOption> options = new List<SettingsImportOption>();
            List<UnityModManager.ModEntry> entries = UnityModManager.modEntries;
            if (entries == null || entries.Count == 0) return options;

            for (int i = 0; i < Specs.Length; i++)
            {
                ImportSpec spec = Specs[i];
                for (int j = 0; j < entries.Count; j++)
                {
                    UnityModManager.ModEntry entry = entries[j];
                    if (!EntryMatches(entry, spec)) continue;

                    string id = SafeString(entry.Info?.Id);
                    string display = StripRichText(SafeString(entry.Info?.DisplayName));
                    if (string.IsNullOrEmpty(display)) display = spec.DisplayName;
                    if (string.IsNullOrEmpty(id)) id = spec.DisplayName;

                    options.Add(new SettingsImportOption
                    {
                        Source = spec.Source,
                        Entry = entry,
                        OptionId = spec.Source + ":" + id,
                        Label = display
                    });
                    break;
                }
            }

            return options;
        }

        internal static SettingsImportOption FindOption(List<SettingsImportOption> options, string optionId)
        {
            if (options == null || options.Count == 0) return null;
            if (!string.IsNullOrEmpty(optionId))
            {
                for (int i = 0; i < options.Count; i++)
                    if (string.Equals(options[i].OptionId, optionId, StringComparison.Ordinal))
                        return options[i];
            }
            return null;
        }

        internal static bool HasKeyViewerPayload(SettingsImportSource source)
        {
            return source == SettingsImportSource.JipperKeyViewer || source == SettingsImportSource.JipperResourcePack;
        }

        internal static SettingsImportKeyViewerPart GetSupportedKeyViewerParts(SettingsImportSource source)
        {
            if (source == SettingsImportSource.JipperKeyViewer)
                return SettingsImportKeyViewerPart.All;
            if (source == SettingsImportSource.JipperResourcePack)
                return SettingsImportKeyViewerPart.KeysLayout
                    | SettingsImportKeyViewerPart.Labels
                    | SettingsImportKeyViewerPart.Colors
                    | SettingsImportKeyViewerPart.Rain
                    | SettingsImportKeyViewerPart.Counts
                    | SettingsImportKeyViewerPart.PositionSize;
            return SettingsImportKeyViewerPart.None;
        }

        internal static SettingsImportResult Import(SettingsImportOption option, UnityModManager.ModEntry ownerEntry)
        {
            return Import(option, ownerEntry, SettingsImportReplaceMode.ReplaceAll, SettingsImportKeyViewerPart.All);
        }

        internal static SettingsImportResult Import(
            SettingsImportOption option,
            UnityModManager.ModEntry ownerEntry,
            SettingsImportReplaceMode keyViewerMode,
            SettingsImportKeyViewerPart keyViewerParts)
        {
            SettingsImportResult result = new SettingsImportResult();
            if (option == null || Main.settings == null)
            {
                result.Message = "No import target selected.";
                return result;
            }

            SettingsImportKeyViewerPart supportedParts = GetSupportedKeyViewerParts(option.Source);
            if (HasKeyViewerPayload(option.Source))
            {
                keyViewerParts &= supportedParts;
                if (keyViewerMode == SettingsImportReplaceMode.ReplaceCertain
                    && keyViewerParts == SettingsImportKeyViewerPart.None)
                {
                    result.Message = "Select at least one KeyViewer setting group.";
                    return result;
                }
            }

            try
            {
                int count = 0;
                switch (option.Source)
                {
                    case SettingsImportSource.KeyboardChatterBlocker:
                        count = ImportKeyboardChatterBlocker(option.Entry);
                        break;
                    case SettingsImportSource.JipperKeyViewer:
                        count = ImportJipperKeyViewer(option.Entry, keyViewerMode, keyViewerParts);
                        break;
                    case SettingsImportSource.JipperResourcePack:
                        count = ImportJipperResourcePack(option.Entry, keyViewerMode, keyViewerParts);
                        break;
                    case SettingsImportSource.AdofaiTweaks:
                        count = ImportAdofaiTweaks(option.Entry);
                        break;
                    case SettingsImportSource.EnhancedEffectRemover:
                        count = ImportEnhancedEffectRemover(option.Entry);
                        break;
                }

                if (count <= 0)
                {
                    if (HasKeyViewerPayload(option.Source) && keyViewerMode == SettingsImportReplaceMode.KeepOld)
                    {
                        result.Success = true;
                        result.ImportedCount = 0;
                        return result;
                    }
                    result.Message = "No readable settings found.";
                    return result;
                }

                PostImportRefresh(ownerEntry);
                result.Success = true;
                result.ImportedCount = count;
                return result;
            }
            catch (Exception ex)
            {
                try { Main.mod?.Logger?.Log("[ImportSettings] " + ex); } catch { }
                result.Message = ex.Message;
                return result;
            }
        }

        private static bool EntryMatches(UnityModManager.ModEntry entry, ImportSpec spec)
        {
            if (entry == null || spec == null) return false;
            if (Main.mod != null && ReferenceEquals(entry, Main.mod)) return false;

            string entryId = NormalizeModToken(entry.Info?.Id);
            string display = NormalizeModToken(StripRichText(entry.Info?.DisplayName));
            string pathName = NormalizeModToken(Path.GetFileName(GetEntryDirectory(entry) ?? ""));
            for (int i = 0; i < spec.Aliases.Length; i++)
            {
                string alias = NormalizeModToken(spec.Aliases[i]);
                if (entryId == alias || display == alias || pathName == alias)
                    return true;
            }
            return false;
        }

        private static int ImportKeyboardChatterBlocker(UnityModManager.ModEntry entry)
        {
            int count = 0;
            object setting = GetStaticMember(FindTypeInEntryAssembly(entry, "KeyboardChatterBlocker.Main"), "setting");
            object profile = GetStaticMember(FindTypeInEntryAssembly(entry, "KeyboardChatterBlocker.Main"), "selectedKeyLimiterProfile");

            if (setting != null)
            {
                bool enabled;
                int interval;
                if (TryGetInt(setting, "inputInterval", out interval))
                    Assign(ref Main.settings.KCBThresholdMs, Mathf.Max(0, interval), ref count);
                if (TryGetBool(setting, "enableKeyLimiter", out enabled))
                    Assign(ref Main.settings.KeyLimiterOn, enabled, ref count);
                if (profile == null)
                    profile = FindSelectedProfile(GetMemberValue(setting, "keyLimiterProfiles"));
                int[] keys = ReadKeyCodesFromMember(profile, "allowedKeys");
                if (keys.Length > 0)
                    Assign(ref Main.settings.KeyLimiterAllowed, keys, ref count);
            }

            if (count == 0)
                count += ImportKeyboardChatterBlockerXml(entry);

            if (count > 0)
                Assign(ref Main.settings.KCBOn, true, ref count);

            return count;
        }

        private static int ImportKeyboardChatterBlockerXml(UnityModManager.ModEntry entry)
        {
            XDocument doc = LoadXml(entry, "Setting.xml");
            if (doc == null) return 0;

            int count = 0;
            int interval;
            bool enabled;
            if (TryReadXmlInt(doc, "inputInterval", out interval))
                Assign(ref Main.settings.KCBThresholdMs, Mathf.Max(0, interval), ref count);
            if (TryReadXmlBool(doc, "enableKeyLimiter", out enabled))
                Assign(ref Main.settings.KeyLimiterOn, enabled, ref count);

            XElement profile = FindSelectedProfileElement(doc, "KeyLimiterProfile");
            int[] keys = ReadKeyCodesFromXml(profile, "allowedKeys");
            if (keys.Length > 0)
                Assign(ref Main.settings.KeyLimiterAllowed, keys, ref count);
            return count;
        }

        private static int ImportJipperKeyViewer(
            UnityModManager.ModEntry entry,
            SettingsImportReplaceMode keyViewerMode,
            SettingsImportKeyViewerPart keyViewerParts)
        {
            if (keyViewerMode == SettingsImportReplaceMode.KeepOld)
                return 0;

            string json = null;
            Type viewerType = FindTypeInEntryAssembly(entry, "JipperKeyViewer.KeyViewer.KeyViewer");
            object runtimeSettings = GetStaticMember(viewerType, "Settings");
            if (runtimeSettings != null)
                json = JsonUtility.ToJson(runtimeSettings, false);
            if (string.IsNullOrEmpty(json))
                json = ReadFirstText(CandidateJipperKeyViewerConfigPaths(entry));
            if (string.IsNullOrEmpty(json))
                return 0;

            JkvSettings imported = JsonUtility.FromJson<JkvSettings>(json);
            if (imported == null)
                return 0;
            NormalizeJkvSettings(imported);
            return ApplyJkvImport(imported, keyViewerMode, keyViewerParts, SettingsImportSource.JipperKeyViewer);
        }

        private static int ImportJipperResourcePack(
            UnityModManager.ModEntry entry,
            SettingsImportReplaceMode keyViewerMode,
            SettingsImportKeyViewerPart keyViewerParts)
        {
            int count = 0;
            Type mainType = FindTypeInEntryAssembly(entry, "JipperResourcePack.Main");
            if (mainType == null) return 0;

            object mainSettings = GetStaticMember(mainType, "Settings");
            float size;
            if (TryGetFloat(mainSettings, "Size", out size))
                Assign(ref Main.settings.size, Mathf.Clamp(size, 0.5f, 2f), ref count);

            count += ImportJrpStatus(entry);
            count += ImportJrpBpm(entry);
            count += ImportJrpCombo(entry);
            count += ImportJrpJudgement(entry);
            count += ImportJrpAttempt(entry);
            count += ImportJrpResourceChanger(entry);
            count += ImportJrpKeyViewer(entry, keyViewerMode, keyViewerParts);
            return count;
        }

        private static int ImportJrpStatus(UnityModManager.ModEntry entry)
        {
            object settings = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.OverlayContents.Status"), "Settings");
            if (settings == null)
                settings = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.Jongyeol.JStatus"), "Settings");
            if (settings == null) return 0;

            int count = 0;
            bool b;
            Assign(ref Main.settings.statusOn, true, ref count);
            if (TryGetBool(settings, "ShowProgress", out b)) Assign(ref Main.settings.ShowProgress, b, ref count);
            if (TryGetBool(settings, "ShowAccuracy", out b)) Assign(ref Main.settings.ShowAccuracy, b, ref count);
            if (TryGetBool(settings, "ShowXAccuracy", out b)) Assign(ref Main.settings.ShowXAccuracy, b, ref count);
            if (TryGetBool(settings, "ShowMusicTime", out b)) Assign(ref Main.settings.ShowMusicTime, b, ref count);
            if (TryGetBool(settings, "ShowMapTime", out b)) Assign(ref Main.settings.ShowMapTime, b, ref count);
            if (TryGetBool(settings, "ShowCheckpoint", out b)) Assign(ref Main.settings.ShowCheckpoint, b, ref count);
            if (TryGetBool(settings, "ShowBest", out b)) Assign(ref Main.settings.ShowBest, b, ref count);
            if (TryGetBool(settings, "ShowFPS", out b)) Assign(ref Main.settings.ShowFPS, b, ref count);
            if (TryGetBool(settings, "HideDebugText", out b)) Assign(ref Main.settings.HideDebugText, b, ref count);
            if (TryGetBool(settings, "ShowProgressBar", out b)) Assign(ref Main.settings.progressBarOn, b, ref count);

            ColorRange range;
            if (TryGetColorRangeFromMember(settings, "ProgressColor", out range)) Assign(ref Main.settings.ProgressColor, range, ref count);
            if (TryGetColorRangeFromMember(settings, "AccuracyColor", out range)) Assign(ref Main.settings.AccuracyColor, range, ref count);
            if (TryGetColorRangeFromMember(settings, "XAccuracyColor", out range)) Assign(ref Main.settings.XAccuracyColor, range, ref count);
            if (TryGetColorRangeFromMember(settings, "MusicTimeColor", out range)) Assign(ref Main.settings.MusicTimeColor, range, ref count);
            if (TryGetColorRangeFromMember(settings, "MapTimeColor", out range)) Assign(ref Main.settings.MapTimeColor, range, ref count);
            if (TryGetColorRangeFromMember(settings, "BestColor", out range)) Assign(ref Main.settings.BestColor, range, ref count);
            if (TryGetColorRangeFromMember(settings, "ProgressBarColor", out range)) Assign(ref Main.settings.ProgressBarFillColor, range, ref count);
            if (TryGetColorRangeFromMember(settings, "ProgressBarBackgroundColor", out range)) Assign(ref Main.settings.ProgressBarBackColor, range, ref count);
            if (TryGetColorRangeFromMember(settings, "ProgressBarBorderColor", out range)) Assign(ref Main.settings.ProgressBarBorderColor, range, ref count);
            return count;
        }

        private static int ImportJrpBpm(UnityModManager.ModEntry entry)
        {
            object settings = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.OverlayContents.Bpm"), "Settings");
            if (settings == null)
                settings = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.Jongyeol.Jbpm"), "Settings");
            if (settings == null) return 0;

            int count = 0;
            float f;
            Assign(ref Main.settings.bpmOn, true, ref count);
            if (TryGetFloat(settings, "BpmColorMax", out f)) Assign(ref Main.settings.BpmColorMax, f, ref count);
            ColorRange range;
            if (TryGetColorRangeFromMember(settings, "BpmColor", out range)) Assign(ref Main.settings.BpmColor, range, ref count);
            return count;
        }

        private static int ImportJrpCombo(UnityModManager.ModEntry entry)
        {
            object settings = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.OverlayContents.Combo"), "Settings");
            if (settings == null)
                settings = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.Jongyeol.JCombo"), "Settings");
            if (settings == null) return 0;

            int count = 0;
            bool b;
            int i;
            Assign(ref Main.settings.comboOn, true, ref count);
            if (TryGetBool(settings, "EnableAutoCombo", out b)) Assign(ref Main.settings.EnableAutoCombo, b, ref count);
            if (TryGetInt(settings, "ComboColorMax", out i)) Assign(ref Main.settings.ComboColorMax, i, ref count);
            ColorRange range;
            if (TryGetColorRangeFromMember(settings, "ComboColor", out range)) Assign(ref Main.settings.ComboColor, range, ref count);
            return count;
        }

        private static int ImportJrpJudgement(UnityModManager.ModEntry entry)
        {
            object settings = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.OverlayContents.Judgement"), "Settings");
            if (settings == null) return 0;

            int count = 0;
            bool b;
            Assign(ref Main.settings.judgementOn, true, ref count);
            if (TryGetBool(settings, "LocationUp", out b))
                Assign(ref Main.settings.judgementPositionY, b ? 90f : 0f, ref count);
            return count;
        }

        private static int ImportJrpAttempt(UnityModManager.ModEntry entry)
        {
            int count = 0;
            object settings = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.OverlayContents.Attempt"), "Settings");
            if (settings != null)
            {
                bool b;
                Assign(ref Main.settings.attemptOn, true, ref count);
                if (TryGetBool(settings, "ShowAttempt", out b)) Assign(ref Main.settings.ShowAttempt, b, ref count);
                if (TryGetBool(settings, "ShowFullAttempt", out b)) Assign(ref Main.settings.ShowFullAttempt, b, ref count);
            }

            int playData = ImportJrpPlayData(entry);
            if (playData > 0)
            {
                Assign(ref Main.settings.attemptOn, true, ref count);
                count += playData;
            }
            return count;
        }

        private static int ImportJrpPlayData(UnityModManager.ModEntry entry)
        {
            string dir = GetEntryDirectory(entry);
            if (string.IsNullOrEmpty(dir)) return 0;

            string[] candidates =
            {
                Path.Combine(dir, "Plays.dat"),
                Path.Combine(dir, "Plays.dat.bak")
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                try
                {
                    int imported = PlayCount.ImportPlayDataFromFile(candidates[i]);
                    if (imported > 0) return imported;
                }
                catch { }
            }
            return 0;
        }

        private static int ImportJrpResourceChanger(UnityModManager.ModEntry entry)
        {
            Type type = FindTypeInEntryAssembly(entry, "JipperResourcePack.ResourceChanger");
            object settings = GetStaticMember(type, "_settings");
            if (settings == null) return 0;

            int count = 0;
            bool b;
            Assign(ref Main.settings.ResourceChangerOn, true, ref count);
            if (TryGetBool(settings, "ChangeRabbit", out b)) Assign(ref Main.settings.ChangeOttoIcon, b, ref count);
            if (TryGetBool(settings, "ChangeBallColor", out b)) Assign(ref Main.settings.ChangeBallColor, b, ref count);
            if (TryGetBool(settings, "ChangeTileColor", out b)) Assign(ref Main.settings.ChangeTileColor, b, ref count);
            return count;
        }

        private static int ImportJrpKeyViewer(
            UnityModManager.ModEntry entry,
            SettingsImportReplaceMode keyViewerMode,
            SettingsImportKeyViewerPart keyViewerParts)
        {
            if (keyViewerMode == SettingsImportReplaceMode.KeepOld)
                return 0;

            object settings = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.KeyViewerContents.KeyViewer"), "Settings");
            if (settings == null) return 0;

            JkvSettings imported = new JkvSettings();
            int count = 0;
            SettingsImportKeyViewerPart available = SettingsImportKeyViewerPart.None;

            int before = count;
            CopyEnumMember(settings, "KeyViewerStyle", ref imported.KeyViewerStyle, ref count);
            CopyEnumMember(settings, "FootKeyViewerStyle", ref imported.FootKeyViewerStyle, ref count);
            CopyKeyCodeArray(settings, "key10", ref imported.key10, ref count);
            CopyKeyCodeArray(settings, "key12", ref imported.key12, ref count);
            CopyKeyCodeArray(settings, "key16", ref imported.key16, ref count);
            CopyKeyCodeArray(settings, "key20", ref imported.key20, ref count);
            CopyKeyCodeArray(settings, "GhostKey10", ref imported.GhostKey10, ref count);
            CopyKeyCodeArray(settings, "GhostKey12", ref imported.GhostKey12, ref count);
            CopyKeyCodeArray(settings, "GhostKey16", ref imported.GhostKey16, ref count);
            CopyKeyCodeArray(settings, "GhostKey20", ref imported.GhostKey20, ref count);
            CopyKeyCodeArray(settings, "footkey2", ref imported.footkey2, ref count);
            CopyKeyCodeArray(settings, "footkey4", ref imported.footkey4, ref count);
            CopyKeyCodeArray(settings, "footkey6", ref imported.footkey6, ref count);
            CopyKeyCodeArray(settings, "footkey8", ref imported.footkey8, ref count);
            CopyKeyCodeArray(settings, "footkey16", ref imported.footkey16, ref count);
            if (count > before) available |= SettingsImportKeyViewerPart.KeysLayout;

            before = count;
            CopyStringArray(settings, "key10Text", ref imported.key10Text, ref count);
            CopyStringArray(settings, "key12Text", ref imported.key12Text, ref count);
            CopyStringArray(settings, "key16Text", ref imported.key16Text, ref count);
            CopyStringArray(settings, "key20Text", ref imported.key20Text, ref count);
            if (count > before) available |= SettingsImportKeyViewerPart.Labels;

            float f;
            bool b;
            before = count;
            if (TryGetFloat(settings, "Size", out f)) { imported.Size = Mathf.Clamp(f, 0.1f, 3f); count++; }
            if (TryGetFloat(settings, "YLocation", out f)) { imported.DownLocation = f <= 100f; count++; }
            if (count > before) available |= SettingsImportKeyViewerPart.PositionSize;

            before = count;
            if (TryGetBool(settings, "useRain", out b)) { imported.EnableRainEffect = b; count++; }
            if (TryGetBool(settings, "useGhostRain", out b)) { imported.EnableGhostRain = b; count++; }
            if (TryGetFloat(settings, "rainSpeed", out f))
            {
                imported.RainSpeedRow1 = imported.RainSpeedRow2 = imported.RainSpeedRow3 = f;
                count++;
            }
            if (TryGetFloat(settings, "rainHeight", out f))
            {
                imported.RainHeightRow1 = imported.RainHeightRow2 = imported.RainHeightRow3 = f;
                count++;
            }
            if (count > before) available |= SettingsImportKeyViewerPart.Rain;

            before = count;
            CopyColorMember(settings, "Background", ref imported.Background, ref count);
            CopyColorMember(settings, "BackgroundClicked", ref imported.BackgroundClicked, ref count);
            CopyColorMember(settings, "Outline", ref imported.Outline, ref count);
            CopyColorMember(settings, "OutlineClicked", ref imported.OutlineClicked, ref count);
            CopyColorMember(settings, "Text", ref imported.Text, ref count);
            CopyColorMember(settings, "TextClicked", ref imported.TextClicked, ref count);
            CopyColorMember(settings, "RainColor", ref imported.RainColor, ref count);
            CopyColorMember(settings, "RainColor2", ref imported.RainColor2, ref count);
            CopyColorMember(settings, "RainColor3", ref imported.RainColor3, ref count);
            if (count > before) available |= SettingsImportKeyViewerPart.Colors;

            int countData = ImportJrpKeyCountData(entry, imported);
            if (countData > 0)
            {
                count += countData;
                available |= SettingsImportKeyViewerPart.Counts;
            }

            if (count <= 0) return 0;
            NormalizeJkvSettings(imported);
            if (keyViewerMode == SettingsImportReplaceMode.ReplaceCertain)
                keyViewerParts &= available;
            return ApplyJkvImport(imported, keyViewerMode, keyViewerParts, SettingsImportSource.JipperResourcePack);
        }

        private static int ApplyJkvImport(
            JkvSettings imported,
            SettingsImportReplaceMode keyViewerMode,
            SettingsImportKeyViewerPart keyViewerParts,
            SettingsImportSource source)
        {
            if (imported == null || keyViewerMode == SettingsImportReplaceMode.KeepOld)
                return 0;

            JkvSettings target = imported;
            int count = 0;

            if (keyViewerMode == SettingsImportReplaceMode.ReplaceCertain)
            {
                target = ReadCurrentJkvSettings();
                int copied = CopySelectedJkvSettings(imported, target, keyViewerParts, source);
                if (copied <= 0) return 0;
                count += copied;
            }
            else
            {
                count++;
            }

            NormalizeJkvSettings(target);
            Assign(ref Main.settings.KeyViewerJkvJson, JsonUtility.ToJson(target, false), ref count);
            Assign(ref Main.settings.KeyViewerMode, "simple", ref count);
            Assign(ref Main.settings.keyViewerOn, target.Enabled, ref count);
            JkvBridge.ApplyImportedSettings(target);
            return count;
        }

        private static JkvSettings ReadCurrentJkvSettings()
        {
            string json = null;
            try
            {
                JkvSettings live = JipperKeyViewer.KeyViewer.KeyViewer.Settings;
                if (live != null)
                    json = JsonUtility.ToJson(live, false);
            }
            catch { }

            if (string.IsNullOrEmpty(json) && Main.settings != null)
                json = Main.settings.KeyViewerJkvJson;

            JkvSettings settings = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { settings = JsonUtility.FromJson<JkvSettings>(json); }
                catch { settings = null; }
            }

            if (settings == null)
                settings = new JkvSettings();
            NormalizeJkvSettings(settings);
            return settings;
        }

        private static int CopySelectedJkvSettings(
            JkvSettings sourceSettings,
            JkvSettings target,
            SettingsImportKeyViewerPart parts,
            SettingsImportSource source)
        {
            bool fullSource = source == SettingsImportSource.JipperKeyViewer;
            int count = 0;

            if ((parts & SettingsImportKeyViewerPart.KeysLayout) != 0)
            {
                target.KeyViewerStyle = sourceSettings.KeyViewerStyle;
                target.FootKeyViewerStyle = sourceSettings.FootKeyViewerStyle;
                if (fullSource) target.key8 = Copy(sourceSettings.key8);
                target.key10 = Copy(sourceSettings.key10);
                target.key12 = Copy(sourceSettings.key12);
                if (fullSource) target.key14 = Copy(sourceSettings.key14);
                target.key16 = Copy(sourceSettings.key16);
                target.key20 = Copy(sourceSettings.key20);

                target.footkey2 = Copy(sourceSettings.footkey2);
                target.footkey4 = Copy(sourceSettings.footkey4);
                target.footkey6 = Copy(sourceSettings.footkey6);
                target.footkey8 = Copy(sourceSettings.footkey8);
                if (fullSource)
                {
                    target.footkey10 = Copy(sourceSettings.footkey10);
                    target.footkey12 = Copy(sourceSettings.footkey12);
                    target.footkey14 = Copy(sourceSettings.footkey14);
                }
                target.footkey16 = Copy(sourceSettings.footkey16);

                if (fullSource) target.GhostKey8 = Copy(sourceSettings.GhostKey8);
                target.GhostKey10 = Copy(sourceSettings.GhostKey10);
                target.GhostKey12 = Copy(sourceSettings.GhostKey12);
                if (fullSource) target.GhostKey14 = Copy(sourceSettings.GhostKey14);
                target.GhostKey16 = Copy(sourceSettings.GhostKey16);
                target.GhostKey20 = Copy(sourceSettings.GhostKey20);
                count++;
            }

            if ((parts & SettingsImportKeyViewerPart.Labels) != 0)
            {
                if (fullSource) target.key8Text = Copy(sourceSettings.key8Text);
                target.key10Text = Copy(sourceSettings.key10Text);
                target.key12Text = Copy(sourceSettings.key12Text);
                if (fullSource) target.key14Text = Copy(sourceSettings.key14Text);
                target.key16Text = Copy(sourceSettings.key16Text);
                target.key20Text = Copy(sourceSettings.key20Text);

                if (fullSource)
                {
                    target.footkey2Text = Copy(sourceSettings.footkey2Text);
                    target.footkey4Text = Copy(sourceSettings.footkey4Text);
                    target.footkey6Text = Copy(sourceSettings.footkey6Text);
                    target.footkey8Text = Copy(sourceSettings.footkey8Text);
                    target.footkey10Text = Copy(sourceSettings.footkey10Text);
                    target.footkey12Text = Copy(sourceSettings.footkey12Text);
                    target.footkey14Text = Copy(sourceSettings.footkey14Text);
                    target.footkey16Text = Copy(sourceSettings.footkey16Text);
                }
                count++;
            }

            if ((parts & SettingsImportKeyViewerPart.Colors) != 0)
            {
                target.Background = sourceSettings.Background;
                target.BackgroundClicked = sourceSettings.BackgroundClicked;
                target.Outline = sourceSettings.Outline;
                target.OutlineClicked = sourceSettings.OutlineClicked;
                target.Text = sourceSettings.Text;
                target.TextClicked = sourceSettings.TextClicked;
                target.RainColor = sourceSettings.RainColor;
                target.RainColor2 = sourceSettings.RainColor2;
                target.RainColor3 = sourceSettings.RainColor3;

                if (fullSource)
                {
                    target.GhostRainColor = sourceSettings.GhostRainColor;
                    target.EnableSeparateKpsTotalColors = sourceSettings.EnableSeparateKpsTotalColors;
                    target.KpsBackground = sourceSettings.KpsBackground;
                    target.KpsOutline = sourceSettings.KpsOutline;
                    target.KpsText = sourceSettings.KpsText;
                    target.TotalBackground = sourceSettings.TotalBackground;
                    target.TotalOutline = sourceSettings.TotalOutline;
                    target.TotalText = sourceSettings.TotalText;
                    target.EnablePerKeyColors = sourceSettings.EnablePerKeyColors;
                    target.PerKeyBackground = Copy(sourceSettings.PerKeyBackground);
                    target.PerKeyBackgroundClicked = Copy(sourceSettings.PerKeyBackgroundClicked);
                    target.PerKeyOutline = Copy(sourceSettings.PerKeyOutline);
                    target.PerKeyOutlineClicked = Copy(sourceSettings.PerKeyOutlineClicked);
                    target.PerKeyText = Copy(sourceSettings.PerKeyText);
                    target.PerKeyTextClicked = Copy(sourceSettings.PerKeyTextClicked);
                    target.PerKeyRainColor = Copy(sourceSettings.PerKeyRainColor);
                }
                count++;
            }

            if ((parts & SettingsImportKeyViewerPart.Rain) != 0)
            {
                target.EnableRainEffect = sourceSettings.EnableRainEffect;
                target.EnableRainFade = sourceSettings.EnableRainFade;
                target.EnableRainOpacityGradient = sourceSettings.EnableRainOpacityGradient;
                target.RainOpacityGradientLength = sourceSettings.RainOpacityGradientLength;
                target.EnableGhostRain = sourceSettings.EnableGhostRain;
                target.RainFadeDuration = sourceSettings.RainFadeDuration;
                target.EnableRainForRow1 = sourceSettings.EnableRainForRow1;
                target.EnableRainForRow2 = sourceSettings.EnableRainForRow2;
                target.EnableRainForRow3 = sourceSettings.EnableRainForRow3;
                target.RainSpeedRow1 = sourceSettings.RainSpeedRow1;
                target.RainSpeedRow2 = sourceSettings.RainSpeedRow2;
                target.RainSpeedRow3 = sourceSettings.RainSpeedRow3;
                target.RainHeightRow1 = sourceSettings.RainHeightRow1;
                target.RainHeightRow2 = sourceSettings.RainHeightRow2;
                target.RainHeightRow3 = sourceSettings.RainHeightRow3;
                count++;
            }

            if ((parts & SettingsImportKeyViewerPart.Counts) != 0)
            {
                target.Count = Copy(sourceSettings.Count);
                target.TotalCount = sourceSettings.TotalCount;
                count++;
            }

            if ((parts & SettingsImportKeyViewerPart.PositionSize) != 0)
            {
                target.DownLocation = sourceSettings.DownLocation;
                target.Size = sourceSettings.Size;
                if (fullSource)
                {
                    target.MainKeyViewerPosition = sourceSettings.MainKeyViewerPosition;
                    target.FootKeyViewerPosition = sourceSettings.FootKeyViewerPosition;
                    target.CustomPositionEnabled = sourceSettings.CustomPositionEnabled;
                }
                count++;
            }

            if (fullSource && (parts & SettingsImportKeyViewerPart.Display) != 0)
            {
                target.Enabled = sourceSettings.Enabled;
                target.SyncToKeyLimiter = sourceSettings.SyncToKeyLimiter;
                target.EnableCountFormatting = sourceSettings.EnableCountFormatting;
                target.HideMainKeyCount = sourceSettings.HideMainKeyCount;
                target.EnablePerKeyKps = sourceSettings.EnablePerKeyKps;
                target.StreamerMode = sourceSettings.StreamerMode;
                count++;
            }

            if (fullSource && (parts & SettingsImportKeyViewerPart.Font) != 0)
            {
                target.KeyFontSize = sourceSettings.KeyFontSize;
                target.CounterFontSize = sourceSettings.CounterFontSize;
                target.CounterTextSpacing = sourceSettings.CounterTextSpacing;
                target.EnablePerKeyFontSizes = sourceSettings.EnablePerKeyFontSizes;
                target.PerKeyFontSize = Copy(sourceSettings.PerKeyFontSize);
                target.PerKeyCounterFontSize = Copy(sourceSettings.PerKeyCounterFontSize);
                target.FontIndex = sourceSettings.FontIndex;
                target.FontName = sourceSettings.FontName;
                target.FontStyleFlags = sourceSettings.FontStyleFlags;
                target.Language = sourceSettings.Language;
                count++;
            }

            return count;
        }

        private static KeyCode[] Copy(KeyCode[] value)
        {
            return value == null ? null : (KeyCode[])value.Clone();
        }

        private static string[] Copy(string[] value)
        {
            return value == null ? null : (string[])value.Clone();
        }

        private static int[] Copy(int[] value)
        {
            return value == null ? null : (int[])value.Clone();
        }

        private static float[] Copy(float[] value)
        {
            return value == null ? null : (float[])value.Clone();
        }

        private static Color[] Copy(Color[] value)
        {
            return value == null ? null : (Color[])value.Clone();
        }

        private static int ImportJrpKeyCountData(UnityModManager.ModEntry entry, JkvSettings imported)
        {
            int count = 0;
            bool hasRuntimeCounts = false;
            object countData = GetStaticMember(FindTypeInEntryAssembly(entry, "JipperResourcePack.KeyViewerContents.KeyCountData"), "Instance");
            if (countData != null)
            {
                int total;
                if (TryGetInt(countData, "TotalCount", out total))
                {
                    imported.TotalCount = Mathf.Max(0, total);
                    if (total > 0) hasRuntimeCounts = true;
                    count++;
                }

                int[] counts = ReadIntArray(GetMemberValue(countData, "Count"));
                if (counts.Length > 0)
                {
                    ApplyJrpKeyCounts(imported, counts);
                    if (HasAnyPositive(counts)) hasRuntimeCounts = true;
                    count++;
                }
            }

            if (!hasRuntimeCounts)
            {
                int fileCount = ImportJrpKeyCountFile(entry, imported);
                if (fileCount > 0) return fileCount;
            }
            return count;
        }

        private static int ImportJrpKeyCountFile(UnityModManager.ModEntry entry, JkvSettings imported)
        {
            string dir = GetEntryDirectory(entry);
            if (string.IsNullOrEmpty(dir)) return 0;

            string[] paths =
            {
                Path.Combine(dir, "KeyCount.dat"),
                Path.Combine(dir, "KeyCount.dat.bak")
            };

            for (int p = 0; p < paths.Length; p++)
            {
                string path = paths[p];
                try
                {
                    if (!File.Exists(path)) continue;
                    byte[] data = File.ReadAllBytes(path);
                    if (data.Length < 8) continue;

                    imported.TotalCount = Mathf.Max(0, BitConverter.ToInt32(data, 4));
                    int valueCount = Math.Min(36, (data.Length - 8) / 4);
                    int[] counts = new int[valueCount];
                    for (int i = 0; i < valueCount; i++)
                        counts[i] = Math.Max(0, BitConverter.ToInt32(data, 8 + i * 4));
                    ApplyJrpKeyCounts(imported, counts);
                    return 2;
                }
                catch { }
            }

            return 0;
        }

        private static void ApplyJrpKeyCounts(JkvSettings imported, int[] counts)
        {
            if (imported.Count == null || imported.Count.Length != 36)
                imported.Count = new int[36];

            if (counts == null) return;
            if (counts.Length == 24)
            {
                for (int i = 0; i < 16 && i < counts.Length; i++)
                    imported.Count[i] = Math.Max(0, counts[i]);
                for (int i = 16; i < 24 && i < counts.Length; i++)
                    imported.Count[i + 4] = Math.Max(0, counts[i]);
                return;
            }

            int limit = Math.Min(imported.Count.Length, counts.Length);
            for (int i = 0; i < limit; i++)
                imported.Count[i] = Math.Max(0, counts[i]);
        }

        private static bool HasAnyPositive(int[] values)
        {
            if (values == null) return false;
            for (int i = 0; i < values.Length; i++)
                if (values[i] > 0) return true;
            return false;
        }

        private static int ImportAdofaiTweaks(UnityModManager.ModEntry entry)
        {
            int count = 0;
            List<object> runtimeSettings = GetAdofaiTweaksRuntimeSettings(entry);
            for (int i = 0; i < runtimeSettings.Count; i++)
                count += ImportAdofaiTweaksSettingsObject(runtimeSettings[i]);

            count += ImportAdofaiTweaksXml(entry);
            return count;
        }

        private static int ImportAdofaiTweaksSettingsObject(object settings)
        {
            if (settings == null) return 0;
            string name = settings.GetType().Name;
            if (name == "KeyLimiterSettings") return ImportAdofaiKeyLimiterObject(settings);
            if (name == "KeyViewerSettings") return ImportAdofaiKeyViewerObject(settings);
            if (name == "MiscellaneousSettings") return ImportAdofaiMiscObject(settings);
            if (name == "JudgmentVisualsSettings") return ImportAdofaiJudgmentObject(settings);
            if (name == "HideUiElementsSettings") return ImportAdofaiHideUiObject(settings);
            if (name == "RestrictGameplaySettings") return ImportAdofaiRestrictObject(settings);
            return 0;
        }

        private static int ImportAdofaiKeyLimiterObject(object settings)
        {
            int count = 0;
            bool enabled;
            if (TryGetBool(settings, "IsEnabled", out enabled))
                Assign(ref Main.settings.KeyLimiterOn, enabled, ref count);
            int[] keys = ReadKeyCodesFromMember(settings, "ActiveKeys");
            if (keys.Length > 0)
                Assign(ref Main.settings.KeyLimiterAllowed, keys, ref count);
            return count;
        }

        private static int ImportAdofaiKeyViewerObject(object settings)
        {
            object profile = GetActiveIndexedProfile(settings, "Profiles", "ProfileIndex");
            int[] keys = ReadKeyCodesFromMember(profile, "ActiveKeys");
            if (keys.Length == 0) return 0;
            int count = 0;
            Assign(ref Main.settings.KeyLimiterAllowed, keys, ref count);
            return count;
        }

        private static int ImportAdofaiMiscObject(object settings)
        {
            int count = 0;
            bool enabled;
            bool disableEditorZoom;
            if (TryGetBool(settings, "IsEnabled", out enabled)
                && enabled
                && TryGetBool(settings, "DisableEditorZoom", out disableEditorZoom)
                && disableEditorZoom)
            {
                Assign(ref Main.settings.TweaksOn, true, ref count);
                Assign(ref Main.settings.BlockMouseWheelScrollWhilePlaying, true, ref count);
            }
            return count;
        }

        private static int ImportAdofaiJudgmentObject(object settings)
        {
            int count = 0;
            bool enabled;
            bool hidePerfects;
            if (TryGetBool(settings, "IsEnabled", out enabled)
                && enabled
                && TryGetBool(settings, "HidePerfects", out hidePerfects)
                && hidePerfects)
            {
                Assign(ref Main.settings.TweaksOn, true, ref count);
                Assign(ref Main.settings.HideJudgementPopups, true, ref count);
                Main.settings.HiddenJudgementPopupMask |= 1 << (int)HitMargin.Perfect;
                count++;
            }
            return count;
        }

        private static int ImportAdofaiHideUiObject(object settings)
        {
            int count = 0;
            bool enabled;
            if (!TryGetBool(settings, "IsEnabled", out enabled) || !enabled) return 0;

            UiHider.EnsureProfiles();
            count += ApplyAdofaiHideUiProfile(GetMemberValue(settings, "PlayingProfile"), Main.settings.UiHidingPlayingProfile);
            count += ApplyAdofaiHideUiProfile(GetMemberValue(settings, "RecordingProfile"), Main.settings.UiHidingRecordingProfile);

            bool b;
            if (TryGetBool(settings, "RecordingMode", out b)) Assign(ref Main.settings.UiHidingRecordingMode, b, ref count);
            if (TryGetBool(settings, "UseRecordingModeShortcut", out b)) Assign(ref Main.settings.UiHidingUseRecordingModeShortcut, b, ref count);

            object shortcut = GetMemberValue(settings, "RecordingModeShortcut");
            if (shortcut != null)
            {
                int key;
                if (TryGetBool(shortcut, "PressCtrl", out b)) Assign(ref Main.settings.UiHidingShortcutCtrl, b, ref count);
                if (TryGetBool(shortcut, "PressShift", out b)) Assign(ref Main.settings.UiHidingShortcutShift, b, ref count);
                if (TryGetBool(shortcut, "PressAlt", out b)) Assign(ref Main.settings.UiHidingShortcutAlt, b, ref count);
                if (TryGetInt(shortcut, "PressKey", out key))
                    Assign(ref Main.settings.UiHidingShortcutKey, KeyCodeCompat.NormalizeKeyCode(key), ref count);
            }

            if (count > 0)
                Assign(ref Main.settings.UiHidingOn, true, ref count);
            return count;
        }

        private static int ImportAdofaiRestrictObject(object settings)
        {
            int count = 0;
            bool enabled;
            bool restrict;
            if (!TryGetBool(settings, "IsEnabled", out enabled) || !enabled) return 0;
            if (!TryGetBool(settings, "RestrictJudgment", out restrict) || !restrict) return 0;

            bool[] restricted = ReadBoolArray(GetMemberValue(settings, "RestrictedJudgments"));
            if (restricted.Length == 0) return 0;

            int allowedMask = 0;
            for (int i = 0; i < Tweaks.VanillaJudgementPopupCount && i < restricted.Length; i++)
                if (!restricted[i]) allowedMask |= 1 << i;

            Assign(ref Main.settings.JRestrictOn, true, ref count);
            Assign(ref Main.settings.JRestrictMode, 3, ref count);
            Assign(ref Main.settings.JRestrictAllowedMask, allowedMask, ref count);
            return count;
        }

        private static int ImportAdofaiTweaksXml(UnityModManager.ModEntry entry)
        {
            int count = 0;
            XDocument keyLimiter = LoadXml(entry, "KeyLimiterSettings.xml");
            if (keyLimiter != null)
            {
                bool enabled;
                if (TryReadXmlBool(keyLimiter, "IsEnabled", out enabled))
                    Assign(ref Main.settings.KeyLimiterOn, enabled, ref count);
                int[] keys = ReadKeyCodesFromXml(keyLimiter.Root, "ActiveKeys");
                if (keys.Length > 0)
                    Assign(ref Main.settings.KeyLimiterAllowed, keys, ref count);
            }

            XDocument keyViewer = LoadXml(entry, "KeyViewerSettings.xml");
            if (keyViewer != null)
            {
                int[] keys = ReadAdofaiKeyViewerXmlKeys(keyViewer);
                if (keys.Length > 0)
                    Assign(ref Main.settings.KeyLimiterAllowed, keys, ref count);
            }

            XDocument misc = LoadXml(entry, "MiscellaneousSettings.xml");
            bool b;
            if (misc != null
                && TryReadXmlBool(misc, "IsEnabled", out b)
                && b
                && TryReadXmlBool(misc, "DisableEditorZoom", out b)
                && b)
            {
                Assign(ref Main.settings.TweaksOn, true, ref count);
                Assign(ref Main.settings.BlockMouseWheelScrollWhilePlaying, true, ref count);
            }

            XDocument judgement = LoadXml(entry, "JudgmentVisualsSettings.xml");
            if (judgement != null
                && TryReadXmlBool(judgement, "IsEnabled", out b)
                && b
                && TryReadXmlBool(judgement, "HidePerfects", out b)
                && b)
            {
                Assign(ref Main.settings.TweaksOn, true, ref count);
                Assign(ref Main.settings.HideJudgementPopups, true, ref count);
                Main.settings.HiddenJudgementPopupMask |= 1 << (int)HitMargin.Perfect;
                count++;
            }

            XDocument hideUi = LoadXml(entry, "HideUiElementsSettings.xml");
            if (hideUi != null && TryReadXmlBool(hideUi, "IsEnabled", out b) && b)
            {
                UiHider.EnsureProfiles();
                int profileCount = 0;
                profileCount += ApplyAdofaiHideUiProfile(FindFirstDescendant(hideUi, "PlayingProfile"), Main.settings.UiHidingPlayingProfile);
                profileCount += ApplyAdofaiHideUiProfile(FindFirstDescendant(hideUi, "RecordingProfile"), Main.settings.UiHidingRecordingProfile);
                if (TryReadXmlBool(hideUi, "RecordingMode", out b)) Assign(ref Main.settings.UiHidingRecordingMode, b, ref profileCount);
                if (TryReadXmlBool(hideUi, "UseRecordingModeShortcut", out b)) Assign(ref Main.settings.UiHidingUseRecordingModeShortcut, b, ref profileCount);

                XElement shortcut = FindFirstDescendant(hideUi, "RecordingModeShortcut");
                if (shortcut != null)
                {
                    int key;
                    if (TryReadXmlBool(shortcut, "PressCtrl", out b)) Assign(ref Main.settings.UiHidingShortcutCtrl, b, ref profileCount);
                    if (TryReadXmlBool(shortcut, "PressShift", out b)) Assign(ref Main.settings.UiHidingShortcutShift, b, ref profileCount);
                    if (TryReadXmlBool(shortcut, "PressAlt", out b)) Assign(ref Main.settings.UiHidingShortcutAlt, b, ref profileCount);
                    if (TryReadXmlKeyCode(shortcut, "PressKey", out key))
                        Assign(ref Main.settings.UiHidingShortcutKey, KeyCodeCompat.NormalizeKeyCode(key), ref profileCount);
                }

                if (profileCount > 0)
                {
                    count += profileCount;
                    Assign(ref Main.settings.UiHidingOn, true, ref count);
                }
            }

            XDocument restrict = LoadXml(entry, "RestrictGameplaySettings.xml");
            if (restrict != null
                && TryReadXmlBool(restrict, "IsEnabled", out b)
                && b
                && TryReadXmlBool(restrict, "RestrictJudgment", out b)
                && b)
            {
                bool[] restricted = ReadXmlBoolArray(restrict, "RestrictedJudgments");
                if (restricted.Length > 0)
                {
                    int allowedMask = 0;
                    for (int i = 0; i < Tweaks.VanillaJudgementPopupCount && i < restricted.Length; i++)
                        if (!restricted[i]) allowedMask |= 1 << i;
                    Assign(ref Main.settings.JRestrictOn, true, ref count);
                    Assign(ref Main.settings.JRestrictMode, 3, ref count);
                    Assign(ref Main.settings.JRestrictAllowedMask, allowedMask, ref count);
                }
            }

            return count;
        }

        private static int ImportEnhancedEffectRemover(UnityModManager.ModEntry entry)
        {
            int count = 0;
            object settings = GetStaticMember(FindTypeInEntryAssembly(entry, "EnhancedEffectRemover.Settings"), "Instance");
            if (settings != null)
                count += ApplyEnhancedEffectRemoverObject(settings);

            string json = ReadFirstText(new[] { Path.Combine(GetEntryDirectory(entry) ?? "", "Settings.json") });
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    JObject root = JObject.Parse(json);
                    count += ApplyEnhancedEffectRemoverJson(root);
                }
                catch { }
            }

            return count;
        }

        private static int ApplyEnhancedEffectRemoverObject(object settings)
        {
            int count = 0;
            bool b;
            float f;
            Assign(ref Main.settings.EffectRemoverOn, true, ref count);
            if (TryGetFloat(settings, "CameraZoomScale", out f)) Assign(ref Main.settings.EffectRemoverCameraZoomScale, f, ref count);
            if (TryGetBool(settings, "EnableSave", out b)) Assign(ref Main.settings.EffectRemoverEnableSave, b, ref count);
            if (TryGetBool(settings, "ResetTrackAnimation", out b)) Assign(ref Main.settings.EffectRemoverResetTrackAnimation, b, ref count);
            if (TryGetBool(settings, "ResetTrackColor", out b)) Assign(ref Main.settings.EffectRemoverResetTrackColor, b, ref count);
            if (TryGetBool(settings, "RemoveAllDecorations", out b)) Assign(ref Main.settings.EffectRemoverRemoveAllDecorations, b, ref count);
            if (TryGetBool(settings, "ResetTrackOpacity", out b)) Assign(ref Main.settings.EffectRemoverResetTrackOpacity, b, ref count);
            if (TryGetBool(settings, "SetCameraZoomScale", out b)) Assign(ref Main.settings.EffectRemoverSetCameraZoom, b, ref count);
            if (TryGetBool(settings, "Filters", out b)) Assign(ref Main.settings.EffectRemoverFilters, b, ref count);
            if (TryGetBool(settings, "AdvFilters", out b)) Assign(ref Main.settings.EffectRemoverAdvancedFilters, b, ref count);
            if (TryGetBool(settings, "Particles", out b)) Assign(ref Main.settings.EffectRemoverParticles, b, ref count);
            if (TryGetBool(settings, "Decorations", out b)) Assign(ref Main.settings.EffectRemoverDecorations, b, ref count);
            if (TryGetBool(settings, "Backgrounds", out b)) Assign(ref Main.settings.EffectRemoverBackgrounds, b, ref count);
            if (TryGetBool(settings, "Cameras", out b)) Assign(ref Main.settings.EffectRemoverCameras, b, ref count);
            if (TryGetBool(settings, "RepeatEvents", out b)) Assign(ref Main.settings.EffectRemoverRepeatEvents, b, ref count);
            if (TryGetBool(settings, "FrameRate", out b)) Assign(ref Main.settings.EffectRemoverFrameRate, b, ref count);
            if (TryGetBool(settings, "HitSounds", out b)) Assign(ref Main.settings.EffectRemoverHitSounds, b, ref count);
            if (TryGetBool(settings, "PlanetOrbit", out b)) Assign(ref Main.settings.EffectRemoverPlanetOrbit, b, ref count);
            if (TryGetBool(settings, "PlanetScale", out b)) Assign(ref Main.settings.EffectRemoverPlanetScale, b, ref count);
            if (TryGetBool(settings, "PlanetRadius", out b)) Assign(ref Main.settings.EffectRemoverPlanetRadius, b, ref count);
            if (TryGetBool(settings, "TrackAnimations", out b)) Assign(ref Main.settings.EffectRemoverTrackAnimations, b, ref count);
            if (TryGetBool(settings, "TrackPos", out b)) Assign(ref Main.settings.EffectRemoverTrackPositions, b, ref count);
            if (TryGetBool(settings, "TrackMove", out b)) Assign(ref Main.settings.EffectRemoverTrackMoves, b, ref count);
            if (TryGetBool(settings, "TrackColors", out b)) Assign(ref Main.settings.EffectRemoverTrackColors, b, ref count);
            if (TryGetBool(settings, "HoldSounds", out b)) Assign(ref Main.settings.EffectRemoverHoldSounds, b, ref count);
            if (TryGetBool(settings, "HideIcons", out b)) Assign(ref Main.settings.EffectRemoverHideIcons, b, ref count);
            if (TryGetBool(settings, "TrackPanel", out b)) Assign(ref Main.settings.EffectRemoverTrackPanel, b, ref count);
            if (TryGetBool(settings, "PlanetPanel", out b)) Assign(ref Main.settings.EffectRemoverPlanetPanel, b, ref count);
            if (TryGetBool(settings, "CheckPoints", out b))
            {
                Assign(ref Main.settings.TweaksOn, true, ref count);
                Assign(ref Main.settings.RemoveAllCheckpoints, b, ref count);
            }
            return count;
        }

        private static int ApplyEnhancedEffectRemoverJson(JObject root)
        {
            int count = 0;
            bool b;
            float f;
            Assign(ref Main.settings.EffectRemoverOn, true, ref count);
            if (TryReadJsonFloat(root, "CameraZoomScale", out f)) Assign(ref Main.settings.EffectRemoverCameraZoomScale, f, ref count);
            if (TryReadJsonBool(root, "EnableSave", out b)) Assign(ref Main.settings.EffectRemoverEnableSave, b, ref count);
            if (TryReadJsonBool(root, "ResetTrackAnimation", out b)) Assign(ref Main.settings.EffectRemoverResetTrackAnimation, b, ref count);
            if (TryReadJsonBool(root, "ResetTrackColor", out b)) Assign(ref Main.settings.EffectRemoverResetTrackColor, b, ref count);
            if (TryReadJsonBool(root, "RemoveAllDecorations", out b)) Assign(ref Main.settings.EffectRemoverRemoveAllDecorations, b, ref count);
            if (TryReadJsonBool(root, "ResetTrackOpacity", out b)) Assign(ref Main.settings.EffectRemoverResetTrackOpacity, b, ref count);
            if (TryReadJsonBool(root, "SetCameraZoomScale", out b)) Assign(ref Main.settings.EffectRemoverSetCameraZoom, b, ref count);
            if (TryReadJsonBool(root, "Filters", out b)) Assign(ref Main.settings.EffectRemoverFilters, b, ref count);
            if (TryReadJsonBool(root, "AdvFilters", out b)) Assign(ref Main.settings.EffectRemoverAdvancedFilters, b, ref count);
            if (TryReadJsonBool(root, "Particles", out b)) Assign(ref Main.settings.EffectRemoverParticles, b, ref count);
            if (TryReadJsonBool(root, "Decorations", out b)) Assign(ref Main.settings.EffectRemoverDecorations, b, ref count);
            if (TryReadJsonBool(root, "Backgrounds", out b)) Assign(ref Main.settings.EffectRemoverBackgrounds, b, ref count);
            if (TryReadJsonBool(root, "Cameras", out b)) Assign(ref Main.settings.EffectRemoverCameras, b, ref count);
            if (TryReadJsonBool(root, "RepeatEvents", out b)) Assign(ref Main.settings.EffectRemoverRepeatEvents, b, ref count);
            if (TryReadJsonBool(root, "FrameRate", out b)) Assign(ref Main.settings.EffectRemoverFrameRate, b, ref count);
            if (TryReadJsonBool(root, "HitSounds", out b)) Assign(ref Main.settings.EffectRemoverHitSounds, b, ref count);
            if (TryReadJsonBool(root, "PlanetOrbit", out b)) Assign(ref Main.settings.EffectRemoverPlanetOrbit, b, ref count);
            if (TryReadJsonBool(root, "PlanetScale", out b)) Assign(ref Main.settings.EffectRemoverPlanetScale, b, ref count);
            if (TryReadJsonBool(root, "PlanetRadius", out b)) Assign(ref Main.settings.EffectRemoverPlanetRadius, b, ref count);
            if (TryReadJsonBool(root, "TrackAnimations", out b)) Assign(ref Main.settings.EffectRemoverTrackAnimations, b, ref count);
            if (TryReadJsonBool(root, "TrackPos", out b)) Assign(ref Main.settings.EffectRemoverTrackPositions, b, ref count);
            if (TryReadJsonBool(root, "TrackMove", out b)) Assign(ref Main.settings.EffectRemoverTrackMoves, b, ref count);
            if (TryReadJsonBool(root, "TrackColors", out b)) Assign(ref Main.settings.EffectRemoverTrackColors, b, ref count);
            if (TryReadJsonBool(root, "HoldSounds", out b)) Assign(ref Main.settings.EffectRemoverHoldSounds, b, ref count);
            if (TryReadJsonBool(root, "HideIcons", out b)) Assign(ref Main.settings.EffectRemoverHideIcons, b, ref count);
            if (TryReadJsonBool(root, "TrackPanel", out b)) Assign(ref Main.settings.EffectRemoverTrackPanel, b, ref count);
            if (TryReadJsonBool(root, "PlanetPanel", out b)) Assign(ref Main.settings.EffectRemoverPlanetPanel, b, ref count);
            if (TryReadJsonBool(root, "CheckPoints", out b))
            {
                Assign(ref Main.settings.TweaksOn, true, ref count);
                Assign(ref Main.settings.RemoveAllCheckpoints, b, ref count);
            }
            return count;
        }

        private static void PostImportRefresh(UnityModManager.ModEntry ownerEntry)
        {
            if (Main.settings == null) return;
            Main.settings.InvalidateColorRangeCache();
            Main.settings.EnsureColorRanges();
            try { FontLoader.InvalidatePreferredHudFont(); } catch { }
            try { Overlay.InvalidateOverlayFontCache(); } catch { }
            try { SettingsGui.SyncSimpleKeysToKeyLimiter(); } catch { }
            try { Tweaks.RefreshTweaks(); } catch { }
            try { ResourceChanger.RefreshChangedResources(); } catch { }
            try { EffectRemover.RefreshEditorSaveButtons(); } catch { }
            try { JkvBridge.SaveSettings(); } catch { }
            try { Main.settings.Save(ownerEntry); } catch { }
        }

        private static int ApplyAdofaiHideUiProfile(object profile)
        {
            UiHider.EnsureProfiles();
            return ApplyAdofaiHideUiProfile(profile, Main.settings.UiHidingPlayingProfile);
        }

        private static int ApplyAdofaiHideUiProfile(object profile, UiHidingProfile target)
        {
            if (profile == null) return 0;
            int count = 0;
            bool b;
            if (target == null) return 0;
            if (TryGetBool(profile, "HideEverything", out b)) Assign(ref target.HideEverything, b, ref count);
            if (TryGetBool(profile, "HideJudgment", out b)) Assign(ref target.HideJudgment, b, ref count);
            if (TryGetBool(profile, "HideMissIndicators", out b)) Assign(ref target.HideMissIndicators, b, ref count);
            if (TryGetBool(profile, "HideTitle", out b)) Assign(ref target.HideTitle, b, ref count);
            if (TryGetBool(profile, "HideOtto", out b)) Assign(ref target.HideOtto, b, ref count);
            if (TryGetBool(profile, "HideTimingTarget", out b)) Assign(ref target.HideTimingTarget, b, ref count);
            if (TryGetBool(profile, "HideNoFailIcon", out b)) Assign(ref target.HideNoFailIcon, b, ref count);
            if (TryGetBool(profile, "HideBeta", out b)) Assign(ref target.HideBeta, b, ref count);
            if (TryGetBool(profile, "HideResult", out b)) Assign(ref target.HideResult, b, ref count);
            if (TryGetBool(profile, "HideHitErrorMeter", out b)) Assign(ref target.HideHitErrorMeter, b, ref count);
            if (TryGetBool(profile, "HideLastFloorFlash", out b)) Assign(ref target.HideLastFloorFlash, b, ref count);
            return count;
        }

        private static int ApplyAdofaiHideUiProfile(XElement profile)
        {
            UiHider.EnsureProfiles();
            return ApplyAdofaiHideUiProfile(profile, Main.settings.UiHidingPlayingProfile);
        }

        private static int ApplyAdofaiHideUiProfile(XElement profile, UiHidingProfile target)
        {
            if (profile == null) return 0;
            int count = 0;
            bool b;
            if (target == null) return 0;
            if (TryReadXmlBool(profile, "HideEverything", out b)) Assign(ref target.HideEverything, b, ref count);
            if (TryReadXmlBool(profile, "HideJudgment", out b)) Assign(ref target.HideJudgment, b, ref count);
            if (TryReadXmlBool(profile, "HideMissIndicators", out b)) Assign(ref target.HideMissIndicators, b, ref count);
            if (TryReadXmlBool(profile, "HideTitle", out b)) Assign(ref target.HideTitle, b, ref count);
            if (TryReadXmlBool(profile, "HideOtto", out b)) Assign(ref target.HideOtto, b, ref count);
            if (TryReadXmlBool(profile, "HideTimingTarget", out b)) Assign(ref target.HideTimingTarget, b, ref count);
            if (TryReadXmlBool(profile, "HideNoFailIcon", out b)) Assign(ref target.HideNoFailIcon, b, ref count);
            if (TryReadXmlBool(profile, "HideBeta", out b)) Assign(ref target.HideBeta, b, ref count);
            if (TryReadXmlBool(profile, "HideResult", out b)) Assign(ref target.HideResult, b, ref count);
            if (TryReadXmlBool(profile, "HideHitErrorMeter", out b)) Assign(ref target.HideHitErrorMeter, b, ref count);
            if (TryReadXmlBool(profile, "HideLastFloorFlash", out b)) Assign(ref target.HideLastFloorFlash, b, ref count);
            return count;
        }

        private static List<object> GetAdofaiTweaksRuntimeSettings(UnityModManager.ModEntry entry)
        {
            List<object> settings = new List<object>();
            Type type = FindTypeInEntryAssembly(entry, "AdofaiTweaks.AdofaiTweaks");
            object runners = GetStaticMember(type, "tweakRunners");
            IEnumerable enumerable = runners as IEnumerable;
            if (enumerable == null) return settings;
            foreach (object runner in enumerable)
            {
                object value = GetMemberValue(runner, "Settings");
                if (value != null) settings.Add(value);
            }
            return settings;
        }

        private static object GetActiveIndexedProfile(object settings, string listMember, string indexMember)
        {
            object listObj = GetMemberValue(settings, listMember);
            IEnumerable enumerable = listObj as IEnumerable;
            if (enumerable == null) return null;
            int index;
            if (!TryGetInt(settings, indexMember, out index)) index = 0;
            int i = 0;
            object first = null;
            foreach (object item in enumerable)
            {
                if (first == null) first = item;
                if (i == index) return item;
                i++;
            }
            return first;
        }

        private static object FindSelectedProfile(object profiles)
        {
            IEnumerable enumerable = profiles as IEnumerable;
            if (enumerable == null) return null;
            object first = null;
            foreach (object profile in enumerable)
            {
                if (first == null) first = profile;
                bool selected;
                if (TryGetBool(profile, "isSelected", out selected) && selected)
                    return profile;
            }
            return first;
        }

        private static XElement FindSelectedProfileElement(XDocument doc, string profileName)
        {
            if (doc == null) return null;
            XElement first = null;
            foreach (XElement profile in doc.Descendants().Where(e => e.Name.LocalName == profileName))
            {
                if (first == null) first = profile;
                bool selected;
                if (TryReadXmlBool(profile, "isSelected", out selected) && selected)
                    return profile;
            }
            return first;
        }

        private static int[] ReadAdofaiKeyViewerXmlKeys(XDocument doc)
        {
            int profileIndex = 0;
            TryReadXmlInt(doc, "ProfileIndex", out profileIndex);
            XElement profiles = FindFirstDescendant(doc, "Profiles");
            if (profiles == null) return new int[0];
            List<XElement> profileList = profiles.Elements().ToList();
            if (profileList.Count == 0) return new int[0];
            if (profileIndex < 0 || profileIndex >= profileList.Count) profileIndex = 0;
            return ReadKeyCodesFromXml(profileList[profileIndex], "ActiveKeys");
        }

        private static bool[] ReadXmlBoolArray(XDocument doc, string arrayName)
        {
            XElement parent = FindFirstDescendant(doc, arrayName);
            if (parent == null) return new bool[0];
            List<bool> values = new List<bool>();
            foreach (XElement item in parent.Elements())
            {
                bool b;
                if (TryParseBool(item.Value, out b)) values.Add(b);
            }
            return values.ToArray();
        }

        private static bool[] ReadBoolArray(object value)
        {
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null) return new bool[0];
            List<bool> values = new List<bool>();
            foreach (object item in enumerable)
            {
                bool b;
                if (TryConvertBool(item, out b)) values.Add(b);
            }
            return values.ToArray();
        }

        private static int[] ReadIntArray(object value)
        {
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null || value is string) return new int[0];
            List<int> values = new List<int>();
            foreach (object item in enumerable)
            {
                if (item == null) continue;
                try
                {
                    values.Add(Math.Max(0, Convert.ToInt32(item, CultureInfo.InvariantCulture)));
                }
                catch
                {
                    int parsed;
                    if (int.TryParse(item.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                        values.Add(Math.Max(0, parsed));
                }
            }
            return values.ToArray();
        }

        private static int[] ReadKeyCodesFromMember(object obj, string member)
        {
            return ReadKeyCodeEnumerable(GetMemberValue(obj, member));
        }

        private static int[] ReadKeyCodeEnumerable(object value)
        {
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null || value is string) return new int[0];

            List<int> result = new List<int>();
            foreach (object item in enumerable)
            {
                int key;
                if (TryConvertKeyCode(item, out key) && !result.Contains(key))
                    result.Add(key);
            }
            return result.ToArray();
        }

        private static int[] ReadKeyCodesFromXml(XElement parent, string listName)
        {
            if (parent == null) return new int[0];
            XElement list = FindFirstDescendant(parent, listName);
            if (list == null) return new int[0];
            List<int> result = new List<int>();
            foreach (XElement item in list.Elements())
            {
                int key;
                if (TryConvertKeyCode(item.Value, out key) && !result.Contains(key))
                    result.Add(key);
            }
            return result.ToArray();
        }

        private static bool TryConvertKeyCode(object value, out int key)
        {
            key = 0;
            if (value == null) return false;
            if (value is KeyCode)
            {
                key = KeyCodeCompat.NormalizeKeyCode((int)(KeyCode)value);
                return true;
            }
            if (value.GetType().IsEnum)
            {
                try
                {
                    key = KeyCodeCompat.NormalizeKeyCode(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    return true;
                }
                catch { return false; }
            }
            if (value is IConvertible && !(value is string))
            {
                try
                {
                    key = KeyCodeCompat.NormalizeKeyCode(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    return true;
                }
                catch { }
            }
            string text = value.ToString();
            int parsed;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                key = KeyCodeCompat.NormalizeKeyCode(parsed);
                return true;
            }
            KeyCode kc;
            if (Enum.TryParse(text, true, out kc))
            {
                key = KeyCodeCompat.NormalizeKeyCode((int)kc);
                return true;
            }
            return false;
        }

        private static void CopyEnumMember<TEnum>(object source, string member, ref TEnum target, ref int count) where TEnum : struct
        {
            object value = GetMemberValue(source, member);
            if (value == null) return;
            TEnum parsed;
            if (Enum.TryParse(value.ToString(), true, out parsed))
            {
                target = parsed;
                count++;
            }
        }

        private static void CopyKeyCodeArray(object source, string member, ref KeyCode[] target, ref int count)
        {
            int[] codes = ReadKeyCodesFromMember(source, member);
            if (codes.Length == 0) return;
            KeyCode[] arr = new KeyCode[codes.Length];
            for (int i = 0; i < codes.Length; i++) arr[i] = (KeyCode)codes[i];
            target = arr;
            count++;
        }

        private static void CopyStringArray(object source, string member, ref string[] target, ref int count)
        {
            object value = GetMemberValue(source, member);
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null || value is string) return;
            List<string> result = new List<string>();
            foreach (object item in enumerable)
                result.Add(item == null ? null : item.ToString());
            if (result.Count == 0) return;
            target = result.ToArray();
            count++;
        }

        private static void CopyColorMember(object source, string member, ref Color target, ref int count)
        {
            Color color;
            if (TryGetColor(GetMemberValue(source, member), out color))
            {
                target = color;
                count++;
            }
        }

        private static bool TryGetColorRangeFromMember(object obj, string member, out ColorRange range)
        {
            return TryGetColorRange(GetMemberValue(obj, member), out range);
        }

        private static bool TryGetColorRange(object value, out ColorRange range)
        {
            range = null;
            if (value == null) return false;

            List<ColorRangePoint> points = new List<ColorRangePoint>();
            object listObj = GetMemberValue(value, "List");
            IEnumerable list = listObj as IEnumerable;
            if (list != null)
            {
                foreach (object item in list)
                {
                    float progress;
                    Color color;
                    if (TryGetFloat(item, "Progress", out progress) && TryGetColor(item, out color))
                        points.Add(new ColorRangePoint(Mathf.Clamp01(progress), color));
                }
            }

            Color perfect;
            bool hasPerfect = TryGetColor(GetMemberValue(value, "PerfectColor"), out perfect);
            if (points.Count == 0 && !hasPerfect) return false;

            range = hasPerfect ? new ColorRange(points, perfect) : new ColorRange(points);
            range.Normalize();
            return true;
        }

        private static bool TryGetColor(object value, out Color color)
        {
            color = Color.white;
            if (value == null) return false;
            if (value is Color)
            {
                color = (Color)value;
                return true;
            }

            float r, g, b, a;
            if (TryGetFloat(value, "r", out r)
                && TryGetFloat(value, "g", out g)
                && TryGetFloat(value, "b", out b))
            {
                if (!TryGetFloat(value, "a", out a)) a = 1f;
                color = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp01(a));
                return true;
            }
            return false;
        }

        private static void NormalizeJkvSettings(JkvSettings settings)
        {
            if (settings == null) return;
            if (settings.Count == null || settings.Count.Length != 36) settings.Count = new int[36];
            settings.key8Text = settings.key8Text ?? new string[8];
            settings.key10Text = settings.key10Text ?? new string[10];
            settings.key12Text = settings.key12Text ?? new string[12];
            settings.key14Text = settings.key14Text ?? new string[14];
            settings.key16Text = settings.key16Text ?? new string[16];
            settings.key20Text = settings.key20Text ?? new string[20];
            settings.footkey2Text = settings.footkey2Text ?? new string[2];
            settings.footkey4Text = settings.footkey4Text ?? new string[4];
            settings.footkey6Text = settings.footkey6Text ?? new string[6];
            settings.footkey8Text = settings.footkey8Text ?? new string[8];
            settings.footkey10Text = settings.footkey10Text ?? new string[10];
            settings.footkey12Text = settings.footkey12Text ?? new string[12];
            settings.footkey14Text = settings.footkey14Text ?? new string[14];
            settings.footkey16Text = settings.footkey16Text ?? new string[16];
            settings.EnsurePerKeyFontSizes();
            if (settings.PerKeyBackground == null || settings.PerKeyBackground.Length != 38)
                settings.InitPerKeyColors();
        }

        private static Type FindTypeInEntryAssembly(UnityModManager.ModEntry entry, string fullName)
        {
            string entryAssembly = NormalizeAssemblyName(entry?.Info?.AssemblyName);
            string entryDir = GetEntryDirectory(entry);
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly asm = assemblies[i];
                if (asm == typeof(SettingsImporter).Assembly) continue;
                bool matches = false;
                if (!string.IsNullOrEmpty(entryAssembly)
                    && string.Equals(NormalizeAssemblyName(asm.GetName().Name), entryAssembly, StringComparison.OrdinalIgnoreCase))
                {
                    matches = true;
                }
                if (!matches && !string.IsNullOrEmpty(entryDir))
                {
                    try
                    {
                        string location = asm.Location;
                        if (!string.IsNullOrEmpty(location)
                            && location.StartsWith(entryDir, StringComparison.OrdinalIgnoreCase))
                            matches = true;
                    }
                    catch { }
                }
                if (!matches) continue;
                Type type = asm.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }

        private static object GetStaticMember(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            FieldInfo field = type.GetField(name, AllMembers);
            if (field != null) return field.GetValue(null);
            PropertyInfo prop = type.GetProperty(name, AllMembers);
            if (prop != null) return prop.GetValue(null, null);
            return null;
        }

        private static object GetMemberValue(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return null;
            Type type = obj as Type ?? obj.GetType();
            object instance = obj is Type ? null : obj;
            FieldInfo field = type.GetField(name, AllMembers);
            if (field != null) return field.GetValue(instance);
            PropertyInfo prop = type.GetProperty(name, AllMembers);
            if (prop != null) return prop.GetValue(instance, null);
            return null;
        }

        private static bool TryGetBool(object obj, string name, out bool value)
        {
            return TryConvertBool(GetMemberValue(obj, name), out value);
        }

        private static bool TryConvertBool(object obj, out bool value)
        {
            value = false;
            if (obj == null) return false;
            if (obj is bool)
            {
                value = (bool)obj;
                return true;
            }
            string text = obj.ToString();
            if (bool.TryParse(text, out value)) return true;
            int i;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
            {
                value = i != 0;
                return true;
            }
            return false;
        }

        private static bool TryGetInt(object obj, string name, out int value)
        {
            value = 0;
            object raw = GetMemberValue(obj, name);
            if (raw == null) return false;
            try
            {
                value = Convert.ToInt32(raw, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
            }
        }

        private static bool TryGetFloat(object obj, string name, out float value)
        {
            value = 0f;
            object raw = GetMemberValue(obj, name);
            if (raw == null) return false;
            try
            {
                value = Convert.ToSingle(raw, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return float.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }
        }

        private static XDocument LoadXml(UnityModManager.ModEntry entry, string fileName)
        {
            string dir = GetEntryDirectory(entry);
            if (string.IsNullOrEmpty(dir)) return null;
            string path = Path.Combine(dir, fileName);
            if (!File.Exists(path)) return null;
            try { return XDocument.Load(path); }
            catch { return null; }
        }

        private static bool TryReadXmlBool(XContainer root, string name, out bool value)
        {
            value = false;
            XElement element = FindFirstDescendant(root, name);
            return element != null && TryParseBool(element.Value, out value);
        }

        private static bool TryReadXmlInt(XContainer root, string name, out int value)
        {
            value = 0;
            XElement element = FindFirstDescendant(root, name);
            return element != null && int.TryParse(element.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadXmlKeyCode(XContainer root, string name, out int value)
        {
            value = 0;
            XElement element = FindFirstDescendant(root, name);
            if (element == null) return false;
            if (int.TryParse(element.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;

            try
            {
                value = (int)(KeyCode)Enum.Parse(typeof(KeyCode), element.Value, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseBool(string text, out bool value)
        {
            value = false;
            if (bool.TryParse(text, out value)) return true;
            int i;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
            {
                value = i != 0;
                return true;
            }
            return false;
        }

        private static XElement FindFirstDescendant(XContainer root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            return root.Descendants().FirstOrDefault(e => e.Name.LocalName == name);
        }

        private static bool TryReadJsonBool(JObject root, string name, out bool value)
        {
            value = false;
            JToken token;
            if (root == null || !root.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out token)) return false;
            try
            {
                value = token.Value<bool>();
                return true;
            }
            catch { return TryParseBool(token.ToString(), out value); }
        }

        private static bool TryReadJsonFloat(JObject root, string name, out float value)
        {
            value = 0f;
            JToken token;
            if (root == null || !root.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out token)) return false;
            return float.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static IEnumerable<string> CandidateJipperKeyViewerConfigPaths(UnityModManager.ModEntry entry)
        {
            string dir = GetEntryDirectory(entry);
            if (string.IsNullOrEmpty(dir)) yield break;
            string parent = Path.GetDirectoryName(dir);
            yield return Path.Combine(dir, "config", "settings.json");
            yield return Path.Combine(dir, "settings.json");
            if (!string.IsNullOrEmpty(parent))
            {
                yield return Path.Combine(parent, "config", "settings.json");
                yield return Path.Combine(parent, "JipperKeyViewer", "config", "settings.json");
            }
        }

        private static string ReadFirstText(IEnumerable<string> paths)
        {
            if (paths == null) return null;
            foreach (string path in paths)
            {
                try
                {
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        return File.ReadAllText(path);
                }
                catch { }
            }
            return null;
        }

        private static string GetEntryDirectory(UnityModManager.ModEntry entry)
        {
            string path = entry?.Path;
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                if (File.Exists(path)) return Path.GetDirectoryName(path);
                return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path;
            }
        }

        private static string NormalizeAssemblyName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            name = Path.GetFileNameWithoutExtension(name);
            return name ?? "";
        }

        private static string NormalizeModToken(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            StringBuilder sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToLowerInvariant(text[i]);
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        private static string StripRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            StringBuilder sb = new StringBuilder(text.Length);
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<') { inTag = true; continue; }
                if (c == '>') { inTag = false; continue; }
                if (!inTag) sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        private static string SafeString(string value)
        {
            return value ?? "";
        }

        private static void Assign<T>(ref T target, T value, ref int count)
        {
            target = value;
            count++;
        }
    }
}
