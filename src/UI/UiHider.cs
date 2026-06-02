using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    internal static class UiHider
    {
        internal static readonly Vector3 HiddenPosition = new Vector3(123456f, 123456f, 123456f);

        private static PropertyInfo isEditingLevelProperty;
        private static bool reflectionReady;

        internal static UiHidingProfile SelectedProfile
        {
            get
            {
                EnsureProfiles();
                Settings s = Main.settings;
                return s.UiHidingRecordingMode ? s.UiHidingRecordingProfile : s.UiHidingPlayingProfile;
            }
        }

        internal static void EnsureProfiles()
        {
            Settings s = Main.settings;
            if (s == null) return;

            if (s.UiHidingPlayingProfile == null) s.UiHidingPlayingProfile = new UiHidingProfile();
            if (s.UiHidingRecordingProfile == null) s.UiHidingRecordingProfile = new UiHidingProfile();
            if (s.UiHidingShortcutKey == (int)KeyCode.None) s.UiHidingShortcutKey = (int)KeyCode.F8;
            s.UiHidingShortcutKey = KeyCodeCompat.NormalizeKeyCode(s.UiHidingShortcutKey);

            if (!s.UiHidingProfilesMigrated)
            {
                MigrateOldSettings(s.UiHidingPlayingProfile);
                s.UiHidingProfilesMigrated = true;
            }
        }

        internal static void Tick()
        {
            EnsureProfiles();

            if (Main.settings == null || !Main.modEnabled)
            {
                Restore();
                return;
            }

            if (ShouldToggleRecordingMode())
                ToggleRecordingMode();

            ShowOrHideElements();
        }

        internal static void ApplyNow()
        {
            EnsureProfiles();
            ShowOrHideElements();
        }

        internal static void Restore()
        {
            ShowOrHideElements(true);
        }

        internal static void ToggleRecordingMode()
        {
            if (Main.settings == null) return;
            Main.settings.UiHidingRecordingMode = !Main.settings.UiHidingRecordingMode;
            ShowOrHideElements();
        }

        internal static void ShowOrHideElements(bool forceDisabled = false)
        {
            EnsureProfiles();

            Settings s = Main.settings;
            bool tweakEnabled = !forceDisabled && Main.modEnabled && s != null && s.UiHidingOn;
            UiHidingProfile selectedProfile = tweakEnabled ? SelectedProfile : null;

            bool hideEverything = tweakEnabled && selectedProfile.HideEverything;
            bool hideOtto = tweakEnabled && (hideEverything || selectedProfile.HideOtto);
            bool hideTimingTarget = tweakEnabled && (hideEverything || selectedProfile.HideTimingTarget);
            bool hideNoFail = tweakEnabled && (hideEverything || selectedProfile.HideNoFailIcon);
            bool hideBeta = tweakEnabled && (hideEverything || selectedProfile.HideBeta);
            bool hideTitle = tweakEnabled && (hideEverything || selectedProfile.HideTitle);

            RDC.noHud = hideEverything;

            scrUIController uiController = scrUIController.instance;
            if (uiController == null) return;

            if (IsEditingLevel())
            {
                object editor = scnEditor.instance;
                SetEnabled(GetMemberValue(editor, "autoImage"), !hideOtto);
                SetEnabled(GetMemberValue(editor, "buttonAuto"), !hideOtto);
                SetMemberGameObjectActiveIfMatches(editor, "editorDifficultySelector", hideTimingTarget);
                SetMemberGameObjectActiveIfMatches(editor, "buttonNoFail", hideNoFail);
            }
            else
            {
                SetEnabled(uiController.noFailImage, !hideNoFail);
                SetEnabled(uiController.difficultyImage, !hideTimingTarget);
                SetGameObjectActiveIfMatches(
                    uiController.difficultyContainer != null ? uiController.difficultyContainer.gameObject : null,
                    hideTimingTarget);
                SetMemberGameObjectActiveIfMatches(uiController, "difficultyFadeContainer", hideTimingTarget);
            }

            if (HasSteamBranchName())
                SetBetaObjectsActiveIfMatches(hideBeta);

            SetGameObjectActiveIfMatches(
                uiController.txtLevelName != null ? uiController.txtLevelName.gameObject : null,
                hideTitle);
        }

        internal static bool ShouldHideJudgementText()
        {
            return IsFeatureActive() && (SelectedProfile.HideEverything || SelectedProfile.HideJudgment);
        }

        internal static bool ShouldHideMissIndicators()
        {
            return IsFeatureActive() && (SelectedProfile.HideEverything || SelectedProfile.HideMissIndicators);
        }

        internal static bool ShouldHideOtto()
        {
            return IsFeatureActive() && (SelectedProfile.HideEverything || SelectedProfile.HideOtto);
        }

        internal static bool ShouldHideResult()
        {
            return IsFeatureActive() && (SelectedProfile.HideEverything || SelectedProfile.HideResult);
        }

        internal static bool ShouldHideHitErrorMeter()
        {
            return IsFeatureActive() && (SelectedProfile.HideEverything || SelectedProfile.HideHitErrorMeter);
        }

        internal static bool ShouldHideLastFloorFlash()
        {
            return IsFeatureActive() && (SelectedProfile.HideEverything || SelectedProfile.HideLastFloorFlash);
        }

        private static void MigrateOldSettings(UiHidingProfile profile)
        {
            Settings s = Main.settings;
            if (s == null || profile == null) return;

            profile.HideEverything |= s.UiHideAll;
            profile.HideTitle |= s.UiHideLevelName;
            profile.HideOtto |= s.UiHideAutoplayButton;
            profile.HideTimingTarget |= s.UiHideDifficulty;
            profile.HideNoFailIcon |= s.UiHideModifiers;
            profile.HideBeta |= s.UiHideDebug;
        }

        private static bool IsFeatureActive()
        {
            return Main.modEnabled && Main.settings != null && Main.settings.UiHidingOn;
        }

        private static bool ShouldToggleRecordingMode()
        {
            Settings s = Main.settings;
            if (s == null || !s.UiHidingOn || !s.UiHidingUseRecordingModeShortcut)
                return false;

            KeyCode key = (KeyCode)KeyCodeCompat.NormalizeKeyCode(s.UiHidingShortcutKey);
            if (key == KeyCode.None) return false;
            if (!SafeGetKeyDown(key)) return false;
            if (s.UiHidingShortcutCtrl && !IsCtrlDown()) return false;
            if (s.UiHidingShortcutShift && !IsShiftDown()) return false;
            if (s.UiHidingShortcutAlt && !IsAltDown()) return false;
            return true;
        }

        private static bool SafeGetKeyDown(KeyCode key)
        {
            try { return Input.GetKeyDown(key); }
            catch { return false; }
        }

        private static bool SafeGetKey(KeyCode key)
        {
            try { return Input.GetKey(key); }
            catch { return false; }
        }

        private static bool IsCtrlDown()
        {
            return SafeGetKey(KeyCode.LeftControl) || SafeGetKey(KeyCode.RightControl);
        }

        private static bool IsShiftDown()
        {
            return SafeGetKey(KeyCode.LeftShift) || SafeGetKey(KeyCode.RightShift);
        }

        private static bool IsAltDown()
        {
            return SafeGetKey(KeyCode.LeftAlt) || SafeGetKey(KeyCode.RightAlt) || SafeGetKey(KeyCode.AltGr);
        }

        private static bool IsEditingLevel()
        {
            EnsureReflection();

            if (isEditingLevelProperty != null)
            {
                try
                {
                    MethodInfo getter = isEditingLevelProperty.GetGetMethod(true);
                    object target = getter != null && getter.IsStatic ? null : (object)scnEditor.instance;
                    if (getter == null || getter.IsStatic || target != null)
                        return Convert.ToBoolean(isEditingLevelProperty.GetValue(target, null));
                }
                catch { }
            }

            return scnEditor.instance != null && scnGame.instance == null;
        }

        private static void EnsureReflection()
        {
            if (reflectionReady) return;
            reflectionReady = true;

            Type adoBase = AccessTools.TypeByName("ADOBase");
            if (adoBase != null)
                isEditingLevelProperty = AccessTools.Property(adoBase, "isEditingLevel");
        }

        internal static object GetMemberValue(object owner, string memberName)
        {
            if (owner == null || string.IsNullOrEmpty(memberName)) return null;

            Type type = owner.GetType();
            FieldInfo field = AccessTools.Field(type, memberName);
            if (field != null)
            {
                try { return field.GetValue(owner); }
                catch { }
            }

            PropertyInfo property = AccessTools.Property(type, memberName);
            if (property != null)
            {
                try { return property.GetValue(owner, null); }
                catch { }
            }

            return null;
        }

        internal static GameObject GetGameObject(object value)
        {
            if (value == null) return null;
            GameObject gameObject = value as GameObject;
            if (gameObject != null) return gameObject;
            Component component = value as Component;
            return component != null ? component.gameObject : null;
        }

        private static void SetEnabled(object value, bool enabled)
        {
            if (value == null) return;

            Behaviour behaviour = value as Behaviour;
            if (behaviour != null)
            {
                behaviour.enabled = enabled;
                return;
            }

            PropertyInfo property = AccessTools.Property(value.GetType(), "enabled");
            if (property == null || !property.CanWrite) return;
            try { property.SetValue(value, enabled, null); }
            catch { }
        }

        private static void SetMemberGameObjectActiveIfMatches(object owner, string memberName, bool hide)
        {
            SetGameObjectActiveIfMatches(GetGameObject(GetMemberValue(owner, memberName)), hide);
        }

        private static void SetGameObjectActiveIfMatches(GameObject gameObject, bool hide)
        {
            if (gameObject == null) return;
            if (gameObject.activeSelf == hide)
                gameObject.SetActive(!hide);
        }

        private static bool HasSteamBranchName()
        {
            try { return !string.IsNullOrEmpty(GCS.steamBranchName); }
            catch { return false; }
        }

        private static void SetBetaObjectsActiveIfMatches(bool hide)
        {
            Type type = AccessTools.TypeByName("scrEnableIfBeta");
            if (type == null) return;

            UnityEngine.Object[] objects;
            try { objects = Resources.FindObjectsOfTypeAll(type); }
            catch { return; }
            if (objects == null) return;

            for (int i = 0; i < objects.Length; i++)
                SetGameObjectActiveIfMatches(GetGameObject(objects[i]), hide);
        }
    }
}
