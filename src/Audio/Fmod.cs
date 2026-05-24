using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;
using FMOD;
using Object = UnityEngine.Object;

namespace KorenResourcePack.Audio
{
    
    public class FmodStartAct : MonoBehaviour
    {
        public Action act;

        private void Update()
        {
            if (act == null) return;
            act.Invoke();
            act = null;
            Destroy(gameObject);
        }
    }

    public class FmodDummy : MonoBehaviour
    {
    }

    public static class Fmod
    {
        internal const string HarmonyId = "koren.koren_resource_pack.fmod";
        internal const string PatchCategory = "KrpFmod";
        internal const uint MinBufferSize = 256;
        private const int MaxFmodChannels = 1024;
        private const int MaxSoftwareChannels = 1024;

        private static Harmony harmony;
        internal static FMOD.System fmodsys;
        private static UnityModManager.ModEntry entry;
        private static Texture2D fmodLogoTex;
        private const string FmodLogoBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAL4AAAAyCAYAAADiBmE+AAAAAXNSR0IArs4c6QAACO5JREFUeF7tXU2rZDUQrfonzkJE3SroQpz5JToKfqKIoLibmaUiyIgoIuj4S2ZEBMWFC1E3woy/JM65JE06nZvUyU363vfebXgw81769knlpOqkKkmrDHg5554QkVdE5Lp/PP6PH+vrmqo+sjbe211+C3hOPTT29Kaq3iu1VeODTM2cc7dE5FWS5Lln78Q3WfzqNNok8T3hb3cchp34HY15GR61KeJ7MD9EkqaXjXfi97LkJXnOZojvgdzvIGt2qXNJyDmyG5sgPgmixR67x2+x2iV+D8m5/ovbwZ4+DN1O/EtM4paubYH4kDchTdnSB8t7duJbrHSF2qxKfOccUpVYzI5+7cQfbeEL9vy1iY8CAlOIajXvTvxWy13S961G/AW5elRg8fOAGJMf98otYa0r0HRN4rPeHmTH6poh/BUYwr2LLRZYhfjOOSxmsai1vh6o6g1r473dboGaBdYiPha0WNhaXo9U9Zql4d5mt4DVAheB+DfOJW+8MabF9rk+szRQW8MTsHpc+G+cmICDWnUHbIIL2IApK43XIr5Z36tq1x2fMdGS7c65WkJYSGNxXNyWavU0BqKH7dclPJB+d3p8puUZBjuFx4Skw08bsVeMa8IW7LYW8Z3F4I89yj1VvZm2dc49KSLPi8jT/m9/i8jvqvqv5bnRQDI7QIcurhuyXMBzZyTBFm4aBD6MX/cJ2jh+J5OTqCHVtyw45yyLVmul9nZqOOfcm/5QyosJyX8REXjm7wxe1XoAIfeoE0yWyTbXpsOWjaxz6IAJkYdxDHMf2XUCNDiIJaYI7zUR3+rNLYCOSOace8tEvq68S1V/TbXhgxvpY/pQv6FHjXG1438HSbinN0WZeY62srCu7TNesR3zj3rQxMkTun1q4i8pqr/xI06kj48dhH5BwzkIjzoVEOamSURvD+SFdQi+Ay4av1YlfhviEjWk2dQv66q3yfEH7EZrjnj5JxjUrq1gQl/X4KHra1YMaXtqPT0AIfVgntV4t8VkfeNqL9Q1Q9D24Eeoyl8DxxMilSRfZD6W7LuMQ7LoZnZbn7NaF0Tsjis7Vcl/lci8q4R6V1V/SAa2BHevtnLDvL2S/CMtM/ckFWl2UZID/yrEv89EfnSSPx3VPUbr1tHezOz94omYs8EQGoSCs8Zt4afSJ6S3h8YpY0UOmq2KvGfQ15YRLDILb3+RLpTVf/wxB+555+SF+cgGlP0c86Zi4ktjKm8ZzYbtSFvjy5Ut7Xr4z2x4U=";

        private static Texture2D GetFmodLogo()
        {
            if (fmodLogoTex != null) return fmodLogoTex;
            try
            {
                fmodLogoTex = new Texture2D(1, 1);
                fmodLogoTex.LoadImage(Convert.FromBase64String(FmodLogoBase64));
            }
            catch { fmodLogoTex = null; }
            return fmodLogoTex;
        }

        public static void DrawFmodBranding()
        {
            var tex = GetFmodLogo();
            if (tex != null) GUILayout.Label(tex);
            GUILayout.Label("Made using FMOD by Firelight Technologies Pty Ltd.");
        }
        private static GameObject driverObject;
        private static bool patchesApplied;
        private static bool sceneHooksRegistered;
        internal static Dictionary<int, Channel> channels = new Dictionary<int, Channel>();
        internal static Dictionary<int, AudioSource> idToAudioSource = new Dictionary<int, AudioSource>();
        internal static Dictionary<int, List<Channel>> playOneShotChannels = new Dictionary<int, List<Channel>>();
        internal static Dictionary<int, float> volCache = new Dictionary<int, float>();
        internal static Dictionary<int, float> positionCache = new Dictionary<int, float>();
        internal static uint bufferSize = MinBufferSize;
        internal static Dictionary<int, Sound> cache = new Dictionary<int, Sound>();
        internal static Dictionary<int, Sound> staticCache = new Dictionary<int, Sound>();
        private static readonly HashSet<string> externalLoadedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, DSP> spectrumDsps = new Dictionary<int, DSP>();
        private static readonly Dictionary<int, float> channelFrequencies = new Dictionary<int, float>();
        private static readonly Dictionary<int, uint> channelLengthsPcm = new Dictionary<int, uint>();
        private static readonly Dictionary<int, string> mixerParamCache = new Dictionary<int, string>();
        private static readonly Dictionary<string, float> mixerScalarFrameCache = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<int> deadAudioSourceIds = new List<int>(32);
        private static readonly List<int> deadOneShotSourceIds = new List<int>(32);
        private static int mixerScalarCacheFrame = -1;
        private static float nextMaintenanceTime;
        private const float MaintenanceSeconds = 0.5f;

        internal static ChannelGroup general;
        internal static ChannelGroup nonpause;
        internal static ChannelGroup master;

        public static bool Initialized { get; private set; }
        public static bool UseASIO { get; set; }
        public static int SelectedDriver { get; set; }
        private static bool curUseAsio;
        private static readonly SYSTEM_CALLBACK systemCallback = OnSystemCallback;
        private static readonly List<FmodDriverInfo> driverCache = new List<FmodDriverInfo>();
        private static volatile bool driverRefreshRequested;
        private static string driverSignature = "";
        private static Guid selectedDriverGuid = Guid.Empty;
        private static string selectedDriverName = "";
        private static float nextDriverPollTime;
        private const float DriverPollSeconds = 2.5f;

        private static IntPtr nativeHandle;

        private enum NativePlatform { Unknown, Windows, Linux, Osx }

        private sealed class FmodDriverInfo
        {
            public int Index;
            public string Name;
            public Guid Guid;
            public int SystemRate;
            public SPEAKERMODE SpeakerMode;
            public int SpeakerModeChannels;
        }

        private static NativePlatform GetPlatform()
        {
            var p = Application.platform;
            if (p == RuntimePlatform.WindowsPlayer || p == RuntimePlatform.WindowsEditor) return NativePlatform.Windows;
            if (p == RuntimePlatform.LinuxPlayer || p == RuntimePlatform.LinuxEditor) return NativePlatform.Linux;
            if (p == RuntimePlatform.OSXPlayer || p == RuntimePlatform.OSXEditor) return NativePlatform.Osx;
            return NativePlatform.Unknown;
        }

        private static string GetNativeRelPath(NativePlatform p)
        {
            switch (p)
            {
                case NativePlatform.Windows: return "fmod.dll";
                case NativePlatform.Linux: return "libfmod.so";
                case NativePlatform.Osx: return "libfmod.dylib";
                default: return null;
            }
        }

        private static uint GetGameBufferSize()
        {
            try
            {
                return (uint)Math.Max(Persistence.audioBufferSize, (int)MinBufferSize);
            }
            catch
            {
                return (uint)Math.Max(AudioSettings.GetConfiguration().dspBufferSize, (int)MinBufferSize);
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        [DllImport("libdl.so.2", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen_linux(string filename, int flags);

        [DllImport("libSystem.dylib", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen_osx(string filename, int flags);

        private const int RTLD_NOW = 2;
        private const int RTLD_GLOBAL = 0x100;

        private static bool LoadNativeFmod(UnityModManager.ModEntry modEntry, NativePlatform plat)
        {
            string rel = GetNativeRelPath(plat);
            if (rel == null)
            {
                modEntry.Logger.Error("[FMOD] unsupported platform: " + Application.platform);
                return false;
            }

            string path = Path.Combine(modEntry.Path, rel);
            if (!File.Exists(path))
            {
                modEntry.Logger.Error("[FMOD] native lib missing at " + path + " — mod install is incomplete.");
                return false;
            }

            try
            {
                switch (plat)
                {
                    case NativePlatform.Windows:
                        nativeHandle = LoadLibraryW(path);
                        if (nativeHandle == IntPtr.Zero)
                        {
                            modEntry.Logger.Error("[FMOD] LoadLibraryW failed for " + path + " (Win32 error " + Marshal.GetLastWin32Error() + ")");
                            return false;
                        }
                        break;
                    case NativePlatform.Linux:
                        nativeHandle = dlopen_linux(path, RTLD_NOW | RTLD_GLOBAL);
                        if (nativeHandle == IntPtr.Zero)
                        {
                            modEntry.Logger.Error("[FMOD] dlopen failed for " + path);
                            return false;
                        }
                        break;
                    case NativePlatform.Osx:
                        nativeHandle = dlopen_osx(path, RTLD_NOW | RTLD_GLOBAL);
                        if (nativeHandle == IntPtr.Zero)
                        {
                            modEntry.Logger.Error("[FMOD] dlopen failed for " + path);
                            return false;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error("[FMOD] preload threw " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }

            modEntry.Logger.Log("[FMOD] preloaded " + path);
            return true;
        }

        public static ulong GetDspClock()
        {
            if (!Initialized) return 0;
            ulong dspClock;
            general.getDSPClock(out dspClock, out _);
            return dspClock;
        }

        public static double GetDspTime()
        {
            if (!Initialized) return Time.realtimeSinceStartup;
            return GetDspClock() / 48000d;
        }

        public static Sound MakeSoundFromAudioClip(AudioClip audioclip)
        {
            if (audioclip == null)
                throw new ArgumentNullException(nameof(audioclip));

            int key = audioclip.GetInstanceID();
            bool staticClip = audioclip.loadType != AudioClipLoadType.DecompressOnLoad;
            Dictionary<int, Sound> targetCache = staticClip ? staticCache : cache;
            Sound a;
            if (targetCache.TryGetValue(key, out a))
                return a;

            Sound b;
            if (staticClip &&
                cache.TryGetValue(key, out b))
                return b;

            float[] samples = new float[audioclip.samples * audioclip.channels];
            audioclip.GetData(samples, 0);

            uint lenbytes = (uint)Buffer.ByteLength(samples);

            CREATESOUNDEXINFO soundinfo = new CREATESOUNDEXINFO();
            soundinfo.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
            soundinfo.length = lenbytes;
            soundinfo.format = SOUND_FORMAT.PCMFLOAT;
            soundinfo.defaultfrequency = audioclip.frequency;
            soundinfo.numchannels = audioclip.channels;

            Sound sound;
            RESULT createResult = fmodsys.createSound("abc", MODE.OPENUSER, ref soundinfo, out sound);
            if (createResult != RESULT.OK || !sound.hasHandle())
                throw new InvalidOperationException("createSound failed: " + createResult);

            IntPtr ptr1, ptr2;
            uint len1, len2;
            RESULT lockResult = sound.@lock(0, lenbytes, out ptr1, out ptr2, out len1, out len2);
            if (lockResult != RESULT.OK || ptr1 == IntPtr.Zero)
            {
                try { sound.release(); } catch { }
                throw new InvalidOperationException("sound lock failed: " + lockResult);
            }

            Marshal.Copy(samples, 0, ptr1, (int)(len1 / sizeof(float)));
            if (len2 > 0)
                Marshal.Copy(samples, (int)(len1 / sizeof(float)), ptr2, (int)(len2 / sizeof(float)));
            sound.unlock(ptr1, ptr2, len1, len2);
            sound.setMode(MODE.LOOP_NORMAL);
            targetCache[key] = sound;
            return sound;
        }

        private static bool TryMakeSoundFromAudioClip(AudioClip clip, out Sound sound)
        {
            sound = default;
            try
            {
                sound = MakeSoundFromAudioClip(clip);
                return sound.hasHandle();
            }
            catch (Exception ex)
            {
                entry?.Logger?.Log("[FMOD] clip fallback to Unity audio: " + ex.Message);
                return false;
            }
        }

        private static IEnumerator Updater()
        {
            while (true)
            {
                if (Initialized)
                {
                    fmodsys.update();
                    TickDriverRefresh();
                    TickMaintenance();
                    SyncUnityAudioState();
                }
                yield return null;
            }
        }

        private static bool lastUnityPause;

        private static void SyncUnityAudioState()
        {
            bool curPause = AudioListener.pause;
            if (curPause != lastUnityPause)
            {
                lastUnityPause = curPause;
                SetPausedAll(curPause);
            }

            // Do not force AudioListener.volume to zero. Patched AudioSource calls
            // return false when FMOD owns playback, and return true on failure so
            // Unity audio can keep working as a fallback.
        }

        private static RESULT OnSystemCallback(IntPtr system, SYSTEM_CALLBACK_TYPE type, IntPtr commanddata1, IntPtr commanddata2, IntPtr userdata)
        {
            const SYSTEM_CALLBACK_TYPE deviceEvents =
                SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED |
                SYSTEM_CALLBACK_TYPE.DEVICELOST |
                SYSTEM_CALLBACK_TYPE.DEVICEREINITIALIZE;

            if ((type & deviceEvents) != 0)
                driverRefreshRequested = true;

            return RESULT.OK;
        }

        private static void RegisterSystemCallback(UnityModManager.ModEntry modEntry, string context)
        {
            const SYSTEM_CALLBACK_TYPE deviceEvents =
                SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED |
                SYSTEM_CALLBACK_TYPE.DEVICELOST |
                SYSTEM_CALLBACK_TYPE.DEVICEREINITIALIZE;

            RESULT result = fmodsys.setCallback(systemCallback, deviceEvents);
            if (result != RESULT.OK)
                modEntry.Logger.Log("[FMOD] " + context + ": setCallback(device hotplug) failed: " + result);
        }

        private static bool TryGetDriverInfo(int index, out FmodDriverInfo info)
        {
            info = null;
            if (!fmodsys.hasHandle() || index < 0) return false;

            string name;
            Guid guid;
            int systemRate;
            SPEAKERMODE speakerMode;
            int speakerModeChannels;
            RESULT result = fmodsys.getDriverInfo(index, out name, 256, out guid, out systemRate, out speakerMode, out speakerModeChannels);
            if (result != RESULT.OK) return false;

            info = new FmodDriverInfo
            {
                Index = index,
                Name = string.IsNullOrEmpty(name) ? "Driver " + index : name,
                Guid = guid,
                SystemRate = systemRate,
                SpeakerMode = speakerMode,
                SpeakerModeChannels = speakerModeChannels
            };
            return true;
        }

        private static bool TryBuildDriverList(out List<FmodDriverInfo> drivers, out string signature)
        {
            drivers = new List<FmodDriverInfo>();
            signature = "";
            if (!fmodsys.hasHandle()) return false;

            int count;
            RESULT result = fmodsys.getNumDrivers(out count);
            if (result != RESULT.OK)
            {
                entry?.Logger?.Log("[FMOD] getNumDrivers failed: " + result);
                return false;
            }

            var sig = new StringBuilder();
            sig.Append(count);
            for (int i = 0; i < count; i++)
            {
                FmodDriverInfo info;
                if (!TryGetDriverInfo(i, out info))
                {
                    info = new FmodDriverInfo
                    {
                        Index = i,
                        Name = "Driver " + i,
                        Guid = Guid.Empty,
                        SystemRate = 0,
                        SpeakerMode = SPEAKERMODE.DEFAULT,
                        SpeakerModeChannels = 0
                    };
                }

                drivers.Add(info);
                sig.Append('|')
                    .Append(info.Guid)
                    .Append(':')
                    .Append(info.Name)
                    .Append(':')
                    .Append(info.SystemRate)
                    .Append(':')
                    .Append(info.SpeakerModeChannels);
            }

            signature = sig.ToString();
            return true;
        }

        private static bool HasSelectedDriverIdentity()
        {
            return selectedDriverGuid != Guid.Empty || !string.IsNullOrEmpty(selectedDriverName);
        }

        private static bool DriverMatchesSelectedIdentity(FmodDriverInfo info)
        {
            if (info == null) return false;
            if (selectedDriverGuid != Guid.Empty && info.Guid == selectedDriverGuid) return true;
            return !string.IsNullOrEmpty(selectedDriverName) &&
                   string.Equals(info.Name, selectedDriverName, StringComparison.OrdinalIgnoreCase);
        }

        private static int FindSelectedDriverByIdentity(List<FmodDriverInfo> drivers)
        {
            if (!HasSelectedDriverIdentity()) return -1;
            for (int i = 0; i < drivers.Count; i++)
                if (DriverMatchesSelectedIdentity(drivers[i]))
                    return i;
            return -1;
        }

        private static void CaptureSelectedDriverIdentity()
        {
            selectedDriverGuid = Guid.Empty;
            selectedDriverName = "";

            FmodDriverInfo info;
            if (!TryGetDriverInfo(SelectedDriver, out info)) return;
            selectedDriverGuid = info.Guid;
            selectedDriverName = info.Name;
        }

        private static void RefreshDriverCache(bool force, bool allowDriverSwitch)
        {
            List<FmodDriverInfo> drivers;
            string signature;
            if (!TryBuildDriverList(out drivers, out signature)) return;
            if (!force && signature == driverSignature) return;

            int oldCount = driverCache.Count;
            driverCache.Clear();
            driverCache.AddRange(drivers);
            bool changed = signature != driverSignature;
            driverSignature = signature;

            if (changed)
                entry?.Logger?.Log("[FMOD] output device list changed (" + oldCount + " -> " + drivers.Count + ")");

            ReconcileSelectedDriver(drivers, allowDriverSwitch);
        }

        private static void ReconcileSelectedDriver(List<FmodDriverInfo> drivers, bool allowDriverSwitch)
        {
            if (drivers == null || drivers.Count == 0)
            {
                SelectedDriver = 0;
                Main.settings.FmodSelectedDriver = 0;
                selectedDriverGuid = Guid.Empty;
                selectedDriverName = "";
                return;
            }

            bool hadIdentity = HasSelectedDriverIdentity();
            int resolved = FindSelectedDriverByIdentity(drivers);
            bool selectedDeviceMissing = hadIdentity && resolved < 0;

            if (resolved < 0 && !hadIdentity && SelectedDriver >= 0 && SelectedDriver < drivers.Count)
                resolved = SelectedDriver;
            if (resolved < 0)
                resolved = 0;

            SelectedDriver = resolved;
            Main.settings.FmodSelectedDriver = resolved;

            int activeDriver;
            bool activeKnown = fmodsys.getDriver(out activeDriver) == RESULT.OK;
            if (allowDriverSwitch && (selectedDeviceMissing || (activeKnown && activeDriver != resolved)))
            {
                string reason = selectedDeviceMissing ? "selected device removed" : "driver index changed";
                ApplyDriverIndex(resolved, true, reason);
                return;
            }

            CaptureSelectedDriverIdentity();
        }

        private static void TickDriverRefresh()
        {
            if (!Initialized) return;
            if (!driverRefreshRequested && Time.realtimeSinceStartup < nextDriverPollTime) return;

            driverRefreshRequested = false;
            nextDriverPollTime = Time.realtimeSinceStartup + DriverPollSeconds;
            RefreshDriverCache(false, true);
        }

        private static void ResolveSelectedDriverFromIdentity()
        {
            List<FmodDriverInfo> drivers;
            string signature;
            if (!TryBuildDriverList(out drivers, out signature)) return;

            int resolved = FindSelectedDriverByIdentity(drivers);
            if (resolved >= 0)
            {
                SelectedDriver = resolved;
                Main.settings.FmodSelectedDriver = resolved;
            }

            driverCache.Clear();
            driverCache.AddRange(drivers);
            driverSignature = signature;
        }

        private static bool ApplyDriverIndex(int driver, bool pauseGame, string reason)
        {
            if (!Initialized || !fmodsys.hasHandle()) return false;

            int count;
            if (fmodsys.getNumDrivers(out count) != RESULT.OK || count <= 0)
            {
                entry?.Logger?.Log("[FMOD] " + reason + ": no output drivers available");
                return false;
            }

            int clamped = Mathf.Clamp(driver, 0, count - 1);
            if (pauseGame)
            {
                try
                {
                    if (scrController.instance != null && !scrController.instance.paused)
                        scrController.instance.TogglePauseGame();
                }
                catch { }
            }

            RESULT result = fmodsys.setDriver(clamped);
            if (result != RESULT.OK)
            {
                entry?.Logger?.Log("[FMOD] " + reason + ": setDriver(" + clamped + ") failed: " + result);
                return false;
            }

            SelectedDriver = clamped;
            Main.settings.FmodSelectedDriver = clamped;
            CaptureSelectedDriverIdentity();
            entry?.Logger?.Log("[FMOD] " + reason + ": selected output " + clamped + " (" + selectedDriverName + ")");
            return true;
        }

        private static void ReleaseSpectrumDsp(int id)
        {
            DSP dsp;
            if (!spectrumDsps.TryGetValue(id, out dsp)) return;
            try { dsp.release(); } catch { }
            spectrumDsps.Remove(id);
        }

        private static void StopChannel(int id, bool removeSourceTracking)
        {
            ReleaseSpectrumDsp(id);

            Channel channel;
            if (channels.TryGetValue(id, out channel))
            {
                try { channel.stop(); } catch { }
                channels.Remove(id);
            }

            channelFrequencies.Remove(id);
            channelLengthsPcm.Remove(id);

            if (!removeSourceTracking) return;
            idToAudioSource.Remove(id);
            volCache.Remove(id);
            positionCache.Remove(id);
        }

        private static void StopOneShotsForSource(int id)
        {
            List<Channel> list;
            if (!playOneShotChannels.TryGetValue(id, out list)) return;
            for (int i = 0; i < list.Count; i++)
                try { list[i].stop(); } catch { }
            playOneShotChannels.Remove(id);
        }

        private static void PruneOneShotList(List<Channel> list)
        {
            if (list == null) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                bool playing;
                RESULT result = list[i].isPlaying(out playing);
                if (result == RESULT.OK && playing) continue;
                try { list[i].stop(); } catch { }
                list.RemoveAt(i);
            }
        }

        private static void PruneOneShotChannels(int id)
        {
            List<Channel> list;
            if (!playOneShotChannels.TryGetValue(id, out list)) return;
            PruneOneShotList(list);
            if (list.Count == 0)
                playOneShotChannels.Remove(id);
        }

        private static bool TryGetChannel(int id, out Channel channel)
        {
            return channels.TryGetValue(id, out channel);
        }

        private static bool TryGetChannelInfo(int id, Channel channel, out float frequency, out uint lengthPcm)
        {
            bool hasFreq = channelFrequencies.TryGetValue(id, out frequency);
            bool hasLength = channelLengthsPcm.TryGetValue(id, out lengthPcm);
            if (hasFreq && hasLength) return true;

            Sound sound;
            if (channel.getCurrentSound(out sound) != RESULT.OK)
                return false;

            if (!hasLength)
            {
                if (sound.getLength(out lengthPcm, TIMEUNIT.PCM) != RESULT.OK)
                    lengthPcm = 0;
                channelLengthsPcm[id] = lengthPcm;
            }

            if (!hasFreq)
            {
                int priority;
                if (sound.getDefaults(out frequency, out priority) != RESULT.OK || frequency <= 0f)
                    frequency = 48000f;
                channelFrequencies[id] = frequency;
            }

            return true;
        }

        private static bool EnsureSpectrumDsp(int id, Channel channel, out DSP dsp)
        {
            if (spectrumDsps.TryGetValue(id, out dsp))
                return true;

            if (fmodsys.createDSPByType(DSP_TYPE.FFT, out dsp) != RESULT.OK)
                return false;

            if (channel.addDSP(CHANNELCONTROL_DSP_INDEX.HEAD, dsp) != RESULT.OK)
            {
                try { dsp.release(); } catch { }
                return false;
            }

            spectrumDsps[id] = dsp;
            return true;
        }

        private static string GetMixerParameterName(AudioSource source)
        {
            var group = source.outputAudioMixerGroup;
            if (group == null) return "Volume";

            int groupId = group.GetInstanceID();
            string parameter;
            if (mixerParamCache.TryGetValue(groupId, out parameter))
                return parameter;

            string name = group.name ?? "";
            if (name.IndexOf("Conductor", StringComparison.Ordinal) >= 0)
                name = name.Replace("Conductor", "");
            if (name.IndexOf("Parent", StringComparison.Ordinal) >= 0)
                name = name.Replace("Parent", "");
            parameter = name + "Volume";
            mixerParamCache[groupId] = parameter;
            return parameter;
        }

        private enum MixerCategory { Music, Sfx, Hitsound, Interface, Hold, Unknown }

        private static MixerCategory ClassifyMixerGroup(AudioSource source)
        {
            var group = source.outputAudioMixerGroup;
            if (group == null) return MixerCategory.Unknown;
            string n = group.name ?? "";
            if (n.IndexOf("Music", StringComparison.OrdinalIgnoreCase) >= 0) return MixerCategory.Music;
            if (n.IndexOf("Hitsound", StringComparison.OrdinalIgnoreCase) >= 0) return MixerCategory.Hitsound;
            if (n.IndexOf("Sfx", StringComparison.OrdinalIgnoreCase) >= 0) return MixerCategory.Sfx;
            if (n.IndexOf("Hold", StringComparison.OrdinalIgnoreCase) >= 0) return MixerCategory.Hold;
            if (n.IndexOf("Interface", StringComparison.OrdinalIgnoreCase) >= 0) return MixerCategory.Interface;
            return MixerCategory.Unknown;
        }

        private static float ReadPersistenceFloat(string propName, float fallback)
        {
            try
            {
                var prop = typeof(Persistence).GetProperty(propName);
                if (prop != null && prop.CanRead) return Convert.ToSingle(prop.GetValue(null));
                var field = typeof(Persistence).GetField(propName);
                if (field != null) return Convert.ToSingle(field.GetValue(null));
            }
            catch { }
            return fallback;
        }

        private static MethodBase FirstPersistenceMethod(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                MethodInfo method = AccessTools.Method(typeof(Persistence), names[i]);
                if (method != null) return method;
            }

            return null;
        }

        private static float GetMixerScalar(AudioSource source)
        {
            int frame = Time.frameCount;
            if (mixerScalarCacheFrame != frame)
            {
                mixerScalarFrameCache.Clear();
                mixerScalarCacheFrame = frame;
            }

            MixerCategory cat = ClassifyMixerGroup(source);
            string cacheKey = cat.ToString();
            float scalar;
            if (mixerScalarFrameCache.TryGetValue(cacheKey, out scalar))
                return scalar;

            float global = Mathf.Clamp01(ReadPersistenceFloat("globalVolume", 1f));
            float category = 1f;
            switch (cat)
            {
                case MixerCategory.Music:    category = ReadPersistenceFloat("musicVolume", 1f); break;
                case MixerCategory.Sfx:      category = ReadPersistenceFloat("sfxVolume", 1f); break;
                case MixerCategory.Hitsound: category = ReadPersistenceFloat("hitsoundVolume", ReadPersistenceFloat("hitSoundVolume", 1f)); break;
                case MixerCategory.Interface:category = ReadPersistenceFloat("interfaceVolume", 1f); break;
                case MixerCategory.Hold:     category = ReadPersistenceFloat("holdSoundVolume", ReadPersistenceFloat("sfxVolume", 1f)); break;
            }
            category = Mathf.Clamp01(category);
            scalar = global * category;
            mixerScalarFrameCache[cacheKey] = scalar;
            return scalar;
        }

        private static void SetChannelVolume(int id, AudioSource source, Channel channel, float sourceVolume)
        {
            channel.setVolume(sourceVolume * GetMixerScalar(source));
        }

        private static void RefreshAllChannelVolumes()
        {
            mixerScalarFrameCache.Clear();
            mixerScalarCacheFrame = -1;

            foreach (var pair in idToAudioSource)
            {
                Channel channel;
                if (!channels.TryGetValue(pair.Key, out channel)) continue;
                float sourceVolume;
                if (!volCache.TryGetValue(pair.Key, out sourceVolume))
                    sourceVolume = 1f;
                SetChannelVolume(pair.Key, pair.Value, channel, sourceVolume);
            }
        }

        private static void ReleaseSoundCache(Dictionary<int, Sound> sounds)
        {
            foreach (var sound in sounds.Values)
                try { sound.release(); } catch { }
            sounds.Clear();
        }

        private static void ReleaseSceneSoundCache()
        {
            ReleaseSoundCache(cache);
            externalLoadedKeys.Clear();
        }

        private static void TickMaintenance()
        {
            if (Time.realtimeSinceStartup < nextMaintenanceTime) return;
            nextMaintenanceTime = Time.realtimeSinceStartup + MaintenanceSeconds;
            Collect();
        }

        internal static void Collect()
        {
            deadAudioSourceIds.Clear();
            foreach (var pair in idToAudioSource)
            {
                if (pair.Value)
                    continue;
                deadAudioSourceIds.Add(pair.Key);
            }

            for (int i = 0; i < deadAudioSourceIds.Count; i++)
            {
                int id = deadAudioSourceIds[i];
                StopChannel(id, true);
                StopOneShotsForSource(id);
            }

            deadOneShotSourceIds.Clear();
            foreach (var pair in playOneShotChannels)
            {
                PruneOneShotList(pair.Value);
                if (pair.Value.Count == 0)
                    deadOneShotSourceIds.Add(pair.Key);
            }

            for (int i = 0; i < deadOneShotSourceIds.Count; i++)
                playOneShotChannels.Remove(deadOneShotSourceIds[i]);
        }

        private static void ApplyPatches()
        {
            if (patchesApplied) return;
            harmony = new Harmony(HarmonyId);
            harmony.PatchCategory(typeof(Fmod).Assembly, PatchCategory);
            patchesApplied = true;
        }

        private static void RemovePatches()
        {
            if (!patchesApplied) return;
            harmony?.UnpatchAll(HarmonyId);
            harmony = null;
            patchesApplied = false;
        }

        private static void HookScenes()
        {
            if (sceneHooksRegistered) return;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneHooksRegistered = true;
        }

        private static void OnSceneUnloaded(Scene a)
        {
            if (a.name is "scnGame" or "scnEditor" or "scnLevelSelect")
            {
                Collect();
                ReleaseSceneSoundCache();
            }
        }

        private static void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            if (!Initialized) return;
            Collect();
            foreach (var ads in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (ads.playOnAwake)
                {
                    var sa = new GameObject().AddComponent<FmodStartAct>();
                    sa.act = () => { ads.Play(); };
                }
            }

            var sa1 = new GameObject().AddComponent<FmodStartAct>();
            sa1.act = () =>
            {
                if (scrConductor.instance) scrConductor.instance.song2.volume = 0;
            };
        }

        private static bool InitFmodSystem()
        {
            RESULT initResult = fmodsys.init(MaxFmodChannels, INITFLAGS.NORMAL, IntPtr.Zero);
            if (initResult != RESULT.OK)
            {
                entry.Logger.Error("[FMOD] init failed: " + initResult);
                return false;
            }

            if (fmodsys.createChannelGroup(null, out general) != RESULT.OK)
            {
                entry.Logger.Error("[FMOD] createChannelGroup(general) failed");
                return false;
            }

            if (fmodsys.createChannelGroup(null, out nonpause) != RESULT.OK)
            {
                entry.Logger.Error("[FMOD] createChannelGroup(nonpause) failed");
                return false;
            }

            if (fmodsys.getMasterChannelGroup(out master) != RESULT.OK)
            {
                entry.Logger.Error("[FMOD] getMasterChannelGroup failed");
                return false;
            }

            RESULT addGeneral = master.addGroup(general);
            if (addGeneral != RESULT.OK)
            {
                entry.Logger.Error("[FMOD] add general group to master failed: " + addGeneral);
                return false;
            }

            RESULT addNonpause = master.addGroup(nonpause);
            if (addNonpause != RESULT.OK)
            {
                entry.Logger.Error("[FMOD] add nonpause group to master failed: " + addNonpause);
                return false;
            }

            return true;
        }

        private static bool HasUsableAsioDriver(FMOD.System system, UnityModManager.ModEntry modEntry)
        {
            int num;
            if (system.getNumDrivers(out num) != RESULT.OK || num <= 0)
            {
                modEntry.Logger.Error("[FMOD] ASIO has no output drivers");
                return false;
            }

            if (num == 1)
            {
                string name;
                system.getDriverInfo(0, out name, 256, out _, out _, out _, out _);
                if (!string.IsNullOrEmpty(name) && name.IndexOf("NoSound", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    modEntry.Logger.Error("[FMOD] ASIO found only NoSound Driver; install/select a real ASIO driver");
                    return false;
                }
            }

            return true;
        }

        private static bool ProbeAsioAvailable(UnityModManager.ModEntry modEntry)
        {
            if (GetPlatform() != NativePlatform.Windows)
            {
                modEntry.Logger.Error("[FMOD] ASIO is only available on Windows");
                return false;
            }

            FMOD.System probe;
            if (Factory.System_Create(out probe) != RESULT.OK)
            {
                modEntry.Logger.Error("[FMOD] ASIO probe: System_Create failed");
                return false;
            }

            bool ok = false;
            try
            {
                if (probe.setOutput(OUTPUTTYPE.ASIO) != RESULT.OK)
                {
                    modEntry.Logger.Error("[FMOD] ASIO probe: setOutput(ASIO) failed");
                    return false;
                }
                ok = HasUsableAsioDriver(probe, modEntry);
            }
            finally
            {
                probe.release();
                probe.clearHandle();
            }

            return ok;
        }

        private static void ClampSelectedDriver()
        {
            int num;
            if (fmodsys.getNumDrivers(out num) != RESULT.OK || num <= 0)
            {
                SelectedDriver = 0;
                Main.settings.FmodSelectedDriver = 0;
                return;
            }

            if (SelectedDriver < 0 || SelectedDriver >= num)
            {
                SelectedDriver = 0;
                Main.settings.FmodSelectedDriver = 0;
            }
        }

        private struct PlaybackSnapshot
        {
            public AudioSource Source;
            public AudioClip Clip;
            public float Position;
            public bool Paused;
        }

        private static readonly List<PlaybackSnapshot> playbackSnapshots = new List<PlaybackSnapshot>();

        private static void CapturePlaybackSnapshots()
        {
            playbackSnapshots.Clear();
            foreach (var pair in idToAudioSource)
            {
                if (!pair.Value) continue;
                Channel chnl;
                if (!channels.TryGetValue(pair.Key, out chnl)) continue;
                bool isPlaying;
                if (chnl.isPlaying(out isPlaying) != RESULT.OK || !isPlaying) continue;

                uint pcm;
                chnl.getPosition(out pcm, TIMEUNIT.PCM);
                float freq;
                if (!channelFrequencies.TryGetValue(pair.Key, out freq) || freq <= 0f)
                    freq = 48000f;
                bool paused;
                chnl.getPaused(out paused);

                playbackSnapshots.Add(new PlaybackSnapshot
                {
                    Source = pair.Value,
                    Clip = pair.Value.clip,
                    Position = pcm / freq,
                    Paused = paused
                });
            }
        }

        private static void RestorePlaybackSnapshots()
        {
            if (!Initialized) { playbackSnapshots.Clear(); return; }
            foreach (var snap in playbackSnapshots)
            {
                if (!snap.Source || !snap.Clip) continue;
                int id = snap.Source.GetInstanceID();
                positionCache[id] = snap.Position;
                if (!pa(snap.Source, snap.Clip))
                    continue;
                Channel chnl;
                if (!channels.TryGetValue(id, out chnl)) continue;

                chnl.getDSPClock(out _, out var dspClock);
                TryGetChannelInfo(id, chnl, out var freq, out var length);
                float pitch = Mathf.Approximately(snap.Source.pitch, 0f) ? 1f : snap.Source.pitch;
                chnl.setPosition((uint)(snap.Position * freq), TIMEUNIT.PCM);
                chnl.setDelay(dspClock, dspClock + (uint)(length / pitch / (double)freq * 48000));
                chnl.setLoopCount(snap.Source.loop ? -1 : 0);
                chnl.setPitch(pitch);
                SetChannelVolume(id, snap.Source, chnl, snap.Source.volume);
                chnl.setPriority(snap.Source.priority);
                chnl.setPaused(snap.Paused);
            }
            playbackSnapshots.Clear();
        }

        private static void ClearFmodRuntimeState()
        {
            foreach (var channel in channels.Values)
                try { channel.stop(); } catch { }
            foreach (var list in playOneShotChannels.Values)
                foreach (var channel in list)
                    try { channel.stop(); } catch { }
            foreach (var dsp in spectrumDsps.Values)
                try { dsp.release(); } catch { }

            channels.Clear();
            playOneShotChannels.Clear();
            spectrumDsps.Clear();
            idToAudioSource.Clear();
            volCache.Clear();
            positionCache.Clear();
            channelFrequencies.Clear();
            channelLengthsPcm.Clear();
            mixerScalarFrameCache.Clear();
            mixerScalarCacheFrame = -1;
            ReleaseSoundCache(cache);
            ReleaseSoundCache(staticCache);
            externalLoadedKeys.Clear();
        }

        private static void ReleaseFmodSystem()
        {
            driverRefreshRequested = false;
            nextDriverPollTime = 0f;
            nextMaintenanceTime = 0f;
            driverCache.Clear();
            driverSignature = "";
            if (!fmodsys.hasHandle()) return;
            try { fmodsys.close(); } catch { }
            try { fmodsys.release(); } catch { }
            fmodsys.clearHandle();
            general.clearHandle();
            nonpause.clearHandle();
            master.clearHandle();
        }

        private static bool CreateConfiguredSystem(UnityModManager.ModEntry modEntry, string context)
        {
            bool asioEffective = UseASIO && GetPlatform() == NativePlatform.Windows;

            if (Factory.System_Create(out fmodsys) != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": System_Create failed"); return false; }

            RESULT outputResult = asioEffective ? fmodsys.setOutput(OUTPUTTYPE.ASIO) : RESULT.OK;
            if (outputResult != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": setOutput(ASIO) failed: " + outputResult); ReleaseFmodSystem(); return false; }
            if (asioEffective && !HasUsableAsioDriver(fmodsys, modEntry)) { ReleaseFmodSystem(); return false; }

            if (asioEffective && curUseAsio != UseASIO)
            {
                selectedDriverGuid = Guid.Empty;
                selectedDriverName = "";
                SelectedDriver = 0;
                Main.settings.FmodSelectedDriver = 0;
            }
            curUseAsio = UseASIO;

            if (fmodsys.setSoftwareFormat(48000, SPEAKERMODE.DEFAULT, 0) != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": setSoftwareFormat failed"); ReleaseFmodSystem(); return false; }
            if (fmodsys.setSoftwareChannels(MaxSoftwareChannels) != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": setSoftwareChannels failed"); ReleaseFmodSystem(); return false; }
            if (fmodsys.setDSPBufferSize(bufferSize, 2) != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": setDSPBufferSize failed"); ReleaseFmodSystem(); return false; }

            RegisterSystemCallback(modEntry, context);
            ResolveSelectedDriverFromIdentity();
            ClampSelectedDriver();
            RESULT setDriverResult = fmodsys.setDriver(SelectedDriver);
            if (setDriverResult != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": setDriver(" + SelectedDriver + ") failed: " + setDriverResult); ReleaseFmodSystem(); return false; }
            if (!InitFmodSystem()) { ReleaseFmodSystem(); return false; }
            CaptureSelectedDriverIdentity();
            RefreshDriverCache(true, false);
            return true;
        }

        public static bool TryInit(UnityModManager.ModEntry modEntry)
        {
            if (Initialized) return true;

            entry = modEntry;
            var plat = GetPlatform();

            bufferSize = GetGameBufferSize();
            UseASIO = Main.settings.FmodUseASIO;
            SelectedDriver = Main.settings.FmodSelectedDriver;
            curUseAsio = UseASIO;

            if (nativeHandle == IntPtr.Zero && !LoadNativeFmod(modEntry, plat)) return false;

            bool asioEffective = UseASIO && plat == NativePlatform.Windows;
            if (asioEffective && !ProbeAsioAvailable(modEntry))
            {
                UseASIO = false;
                Main.settings.FmodUseASIO = false;
                curUseAsio = false;
                asioEffective = false;
            }

            if (!CreateConfiguredSystem(modEntry, "init")) return false;

            ApplyPatches();
            HookScenes();

            driverObject = new GameObject("KrpFmodDriver");
            Object.DontDestroyOnLoad(driverObject);
            var dum = driverObject.AddComponent<FmodDummy>();
            dum.StartCoroutine(Updater());

            Initialized = true;
            modEntry.Logger.Log("[FMOD] initialized (buffer=" + bufferSize + ", driver=" + SelectedDriver + ", asio=" + UseASIO + ")");
            return true;
        }

        public static void SaveRuntimePrefs()
        {
            if (!Initialized) return;
            Main.settings.FmodUseASIO = UseASIO;
            Main.settings.FmodSelectedDriver = SelectedDriver;
        }

        public static void SyncWithSettings(UnityModManager.ModEntry modEntry)
        {
            SetEnabled(Main.settings.FmodEnabled, modEntry);
        }

        public static bool SetEnabled(bool enabled)
        {
            return SetEnabled(enabled, entry ?? Main.mod);
        }

        public static bool SetEnabled(bool enabled, UnityModManager.ModEntry modEntry)
        {
            Main.settings.FmodEnabled = enabled;
            if (enabled)
                return EnableRuntime(modEntry);

            DisableRuntime(true);
            return true;
        }

        public static bool SetASIO(bool useAsio)
        {
            return SetASIO(useAsio, entry ?? Main.mod);
        }

        public static bool SetASIO(bool useAsio, UnityModManager.ModEntry modEntry)
        {
            if (useAsio == UseASIO) return true;
            if (modEntry == null) return false;
            if (useAsio && !ProbeAsioAvailable(modEntry))
            {
                Main.settings.FmodUseASIO = false;
                curUseAsio = false;
                return false;
            }

            bool wasAsio = UseASIO;
            UseASIO = useAsio;
            Main.settings.FmodUseASIO = useAsio;
            curUseAsio = useAsio;
            if (!Initialized) return true;
            DisableRuntime(false);
            if (EnableRuntime(modEntry)) return true;

            if (useAsio)
            {
                UseASIO = false;
                Main.settings.FmodUseASIO = false;
                curUseAsio = false;
                DisableRuntime(false);
                return EnableRuntime(modEntry);
            }

            UseASIO = wasAsio;
            Main.settings.FmodUseASIO = wasAsio;
            curUseAsio = wasAsio;
            return EnableRuntime(modEntry);
        }

        public static void ShutdownRuntime()
        {
            DisableRuntime(true);
        }

        private static bool EnableRuntime(UnityModManager.ModEntry modEntry)
        {
            if (Initialized) return true;
            if (modEntry == null) return false;
            
            if (nativeHandle == IntPtr.Zero)
                return TryInit(modEntry);

            entry = modEntry;
            var plat = GetPlatform();
            bufferSize = GetGameBufferSize();
            UseASIO = Main.settings.FmodUseASIO;
            SelectedDriver = Main.settings.FmodSelectedDriver;
            bool asioEffective = UseASIO && plat == NativePlatform.Windows;
            if (asioEffective && !ProbeAsioAvailable(modEntry))
            {
                UseASIO = false;
                Main.settings.FmodUseASIO = false;
                curUseAsio = false;
                asioEffective = false;
            }

            if (!CreateConfiguredSystem(modEntry, "re-enable")) return false;

            ApplyPatches();

            if (driverObject == null)
            {
                driverObject = new GameObject("KrpFmodDriver");
                Object.DontDestroyOnLoad(driverObject);
                var dum = driverObject.AddComponent<FmodDummy>();
                dum.StartCoroutine(Updater());
            }

            Initialized = true;
            RestorePlaybackSnapshots();
            modEntry.Logger.Log("[FMOD] re-enabled (asio=" + UseASIO + ", driver=" + SelectedDriver + ")");
            return true;
        }

        private static void DisableRuntime(bool restoreUnityAudio)
        {
            if (!Initialized) return;
            lastUnityPause = false;
            if (!restoreUnityAudio) CapturePlaybackSnapshots();
            else playbackSnapshots.Clear();
            try
            {

                ClearFmodRuntimeState();
                ReleaseFmodSystem();
            }
            catch (Exception ex)
            {
                entry?.Logger?.Log("[FMOD] disable: close threw " + ex.Message);
            }
            if (driverObject != null)
            {
                Object.Destroy(driverObject);
                driverObject = null;
            }

            if (restoreUnityAudio)
            {
                RemovePatches();
                AudioListener.pause = false;
                AudioListener.volume = 1f;
            }
            Initialized = false;
            entry?.Logger?.Log("[FMOD] disabled at runtime");
        }

        public static bool ASIOChangedRequiresRestart()
        {
            return false;
        }

        public static int GetDriverCount()
        {
            if (!Initialized) return 0;
            if (driverCache.Count == 0)
                RefreshDriverCache(true, true);
            return driverCache.Count;
        }

        public static string GetDriverName(int index)
        {
            if (!Initialized) return "";
            if (driverCache.Count == 0)
                RefreshDriverCache(true, true);
            if (index < 0 || index >= driverCache.Count) return "";
            return driverCache[index].Name;
        }

        public static void ApplySelectedDriver()
        {
            if (!Initialized) return;
            ApplyDriverIndex(SelectedDriver, true, "manual output change");
            RefreshDriverCache(true, false);
        }

        public static void SetVolume(float volume)
        {
            if (!Initialized) return;
            master.setVolume(volume);
        }

        public static float GetVolume()
        {
            if (!Initialized) return 1f;
            master.getVolume(out var volume);
            return volume;
        }

        public static bool pa(AudioSource __instance, AudioClip value)
        {
            int id = __instance.GetInstanceID();
            if (value == null)
            {
                StopChannel(id, false);
                return true;
            }

            Sound sound;
            if (!TryMakeSoundFromAudioClip(value, out sound))
                return false;

            idToAudioSource[id] = __instance;
            StopChannel(id, false);

            Channel channel;
            RESULT playResult = fmodsys.playSound(sound, __instance.ignoreListenerPause ? nonpause : general, true, out channel);
            if (playResult != RESULT.OK || !channel.hasHandle())
            {
                entry?.Logger?.Log("[FMOD] playSound fallback to Unity audio: " + playResult);
                return false;
            }

            channels[id] = channel;
            return true;
        }

        public static void SetAudioSourceVolume(AudioSource __instance, float vol)
        {
            int id = __instance.GetInstanceID();
            volCache[id] = vol;
            Channel channel;
            if (TryGetChannel(id, out channel))
                SetChannelVolume(id, __instance, channel, vol);
        }

        public static float GetAudioSourceVolume(AudioSource __instance)
        {
            float volume;
            return volCache.TryGetValue(__instance.GetInstanceID(), out volume) ? volume : 1f;
        }

        public static float InverseFunction(float num)
        {
            if (Mathf.Approximately(num, -80f)) return 0f;
            if (num > 0f) return (num / 10f) + 1f;
            return (num / 20f) + 1f;
        }

        public static void SetScheduledEndTime(AudioSource __instance, double time)
        {
            Channel chnl;
            if (TryGetChannel(__instance.GetInstanceID(), out chnl))
            {
                chnl.getDelay(out var start, out _);
                chnl.setDelay(start, (ulong)(time * 48000));
            }
        }

        public static void SetScheduledStartTime(AudioSource __instance, double time)
        {
            Channel chnl;
            if (TryGetChannel(__instance.GetInstanceID(), out chnl))
            {
                chnl.getDelay(out _, out var end);
                chnl.setDelay((ulong)(time * 48000), end);
            }
        }

        public static void Pause(AudioSource __instance)
        {
            Channel channel;
            if (TryGetChannel(__instance.GetInstanceID(), out channel))
                channel.setPaused(true);
        }

        public static void UnPause(AudioSource __instance)
        {
            Channel channel;
            if (TryGetChannel(__instance.GetInstanceID(), out channel))
                channel.setPaused(false);
        }

        public static void SetPausedAll(bool paused)
        {
            if (!Initialized) return;
            general.setPaused(paused);
        }

        public static bool GetPausedAll()
        {
            if (!Initialized) return false;
            general.getPaused(out var paused);
            return paused;
        }

        public static float GetTime(AudioSource __instance)
        {
            int id = __instance.GetInstanceID();
            Channel channel;
            if (TryGetChannel(id, out channel))
            {
                channel.getPosition(out var pos, TIMEUNIT.PCM);
                TryGetChannelInfo(id, channel, out var freq, out _);
                return pos / freq;
            }
            float cached;
            return positionCache.TryGetValue(id, out cached) ? cached : 0f;
        }

        public static void SetTime(AudioSource __instance, float time)
        {
            int id = __instance.GetInstanceID();
            Channel channel;
            if (TryGetChannel(id, out channel))
            {
                TryGetChannelInfo(id, channel, out var freq, out _);
                channel.setPosition((uint)(time * freq), TIMEUNIT.PCM);
            }
            positionCache[id] = time;
        }

        public static bool IsPlaying(AudioSource __instance)
        {
            Channel channel;
            if (TryGetChannel(__instance.GetInstanceID(), out channel))
            {
                channel.isPlaying(out var isPlaying);
                return isPlaying;
            }
            return false;
        }

        private static AudioClip CreateFakeAudioClip(string name, float frequency, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            return AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        }

        public static bool GetSpectrumData(DSP dsp, float[] samples, int channelIndex, FFTWindow window)
        {
            if (!dsp.hasHandle() || samples == null || samples.Length == 0)
                return false;

            if (dsp.setParameterInt((int)DSP_FFT.WINDOWTYPE, (int)window) != RESULT.OK)
                return false;

            IntPtr unmanagedData;
            uint dataLength;
            if (dsp.getParameterData((int)DSP_FFT.SPECTRUMDATA, out unmanagedData, out dataLength) != RESULT.OK
                || unmanagedData == IntPtr.Zero)
            {
                return false;
            }

            DSP_PARAMETER_FFT fftData = Marshal.PtrToStructure<DSP_PARAMETER_FFT>(unmanagedData);
            if (channelIndex < 0 || channelIndex >= fftData.numchannels || fftData.length <= 0)
                return false;

            float[][] spectrum = fftData.spectrum;
            if (spectrum == null || channelIndex >= spectrum.Length || spectrum[channelIndex] == null)
                return false;

            Array.Copy(spectrum[channelIndex], samples, Math.Min(samples.Length, spectrum[channelIndex].Length));
            return true;
        }


        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "Play", new Type[] { })]
        public static class AudioSource_Play
        {
            public static bool Prefix(AudioSource __instance)
            {
                if (!Initialized) return true;
                if (!pa(__instance, __instance.clip))
                    return true;

                int id = __instance.GetInstanceID();
                Channel chnl;
                if (TryGetChannel(id, out chnl))
                {
                    chnl.getDSPClock(out _, out var dspClock);
                    TryGetChannelInfo(id, chnl, out var freq, out var length);
                    float cachedPosition;
                    float pitch = Mathf.Approximately(__instance.pitch, 0f) ? 1f : __instance.pitch;
                    chnl.setPosition(
                        positionCache.TryGetValue(id, out cachedPosition)
                            ? (uint)(cachedPosition * freq)
                            : 0, TIMEUNIT.PCM);
                    chnl.setDelay(dspClock, dspClock + (uint)(length / pitch / (double)freq * 48000));
                    chnl.setLoopCount(__instance.loop ? -1 : 0);
                    chnl.setPitch(pitch);

                    SetChannelVolume(id, __instance, chnl, __instance.volume);
                    chnl.setPriority(__instance.priority);
                    chnl.setPaused(false);
                }
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "pitch", MethodType.Setter)]
        public static class AudioSource_pitch_setter
        {
            public static void Prefix(AudioSource __instance, float value)
            {
                if (!Initialized) return;
                Channel channel;
                if (TryGetChannel(__instance.GetInstanceID(), out channel))
                    channel.setPitch(value);
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "volume", MethodType.Setter)]
        public static class AudioSource_volume_setter
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codes.Add(new CodeInstruction(OpCodes.Ldarg_1));
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("SetAudioSourceVolume")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "volume", MethodType.Getter)]
        public static class AudioSource_volume_getter
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("GetAudioSourceVolume")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }

        private static int hitsoundSrcId = -1;
        private static int hitsoundSrcCacheFrame = -1;

        private static bool IsHitsoundSource(AudioSource src)
        {
            if (src == null) return false;
            int frame = Time.frameCount;
            if (hitsoundSrcCacheFrame != frame)
            {
                hitsoundSrcCacheFrame = frame;
                try
                {
                    var cd = scrConductor.instance;
                    if (cd != null)
                    {
                        var srcField = Traverse.Create(cd).Field("hitsoundSrc").GetValue<AudioSource>();
                        hitsoundSrcId = srcField != null ? srcField.GetInstanceID() : -1;
                    }
                    else hitsoundSrcId = -1;
                }
                catch { hitsoundSrcId = -1; }
            }
            return src.GetInstanceID() == hitsoundSrcId;
        }

        internal static bool PlayOneShotInternal(AudioSource __instance, AudioClip clip, float volumeScale)
        {
            int id = __instance.GetInstanceID();
            idToAudioSource[id] = __instance;
            PruneOneShotChannels(id);

            if (IsHitsoundSource(__instance))
                volumeScale *= Mathf.Clamp01(Main.settings.FmodHitsoundVolume);

            Sound sound;
            if (!TryMakeSoundFromAudioClip(clip, out sound))
                return false;

            Channel chnl;
            RESULT playResult = fmodsys.playSound(sound, __instance.ignoreListenerPause ? nonpause : general, true, out chnl);
            if (playResult != RESULT.OK || !chnl.hasHandle())
            {
                entry?.Logger?.Log("[FMOD] PlayOneShot fallback to Unity audio: " + playResult);
                return false;
            }

            List<Channel> oneShots;
            if (!playOneShotChannels.TryGetValue(id, out oneShots))
            {
                oneShots = new List<Channel>(4);
                playOneShotChannels[id] = oneShots;
            }
            oneShots.Add(chnl);

            chnl.getDSPClock(out _, out var dspClock);
            sound.getLength(out var length, TIMEUNIT.PCM);
            sound.getDefaults(out var freq, out _);
            chnl.setPosition(0, TIMEUNIT.PCM);
            float pitch = Mathf.Approximately(__instance.pitch, 0f) ? 1f : __instance.pitch;
            chnl.setDelay(dspClock, dspClock + (uint)(length / pitch / (double)freq * 48000));
            chnl.setLoopCount(0);
            chnl.setPitch(pitch);

            chnl.setVolume(__instance.volume * volumeScale * GetMixerScalar(__instance));
            chnl.setPriority(__instance.priority);
            chnl.setPaused(false);
            return true;
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "PlayOneShot", typeof(AudioClip))]
        public static class AudioSource_PlayOneShot
        {
            public static bool Prefix(AudioSource __instance, AudioClip clip)
            {
                if (!Initialized) return true;
                if (clip == null) return true;
                return !PlayOneShotInternal(__instance, clip, 1f);
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "PlayOneShot", typeof(AudioClip), typeof(float))]
        public static class AudioSource_PlayOneShot_Volume
        {
            public static bool Prefix(AudioSource __instance, AudioClip clip, float volumeScale)
            {
                if (!Initialized) return true;
                if (clip == null) return true;
                return !PlayOneShotInternal(__instance, clip, volumeScale);
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "PlayScheduled")]
        public static class AudioSource_PlayScheduled
        {
            public static bool Prefix(AudioSource __instance, double time)
            {
                if (!Initialized) return true;
                if (!pa(__instance, __instance.clip))
                    return true;

                int id = __instance.GetInstanceID();
                Channel chnl;
                if (TryGetChannel(id, out chnl))
                {
                    try
                    {
                        TryGetChannelInfo(id, chnl, out var freq, out var length);
                        float cachedPosition;
                        float pitch = Mathf.Approximately(__instance.pitch, 0f) ? 1f : __instance.pitch;
                        chnl.setPosition(
                            positionCache.TryGetValue(id, out cachedPosition)
                                ? (uint)(cachedPosition * freq)
                                : 0, TIMEUNIT.PCM);

                        var t = (ulong)(time * 48000);
                        chnl.setDelay(t, t + (uint)(length / pitch / (double)freq * 48000));
                        chnl.setLoopCount(__instance.loop ? -1 : 0);
                        chnl.setPitch(pitch);

                        SetChannelVolume(id, __instance, chnl, __instance.volume);
                        chnl.setPriority(__instance.priority);
                        chnl.setPaused(false);
                    }
                    catch (Exception ex)
                    {
                        entry.Logger.LogException(ex);
                    }
                }
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "SetScheduledEndTime")]
        public static class AudioSource_SetScheduledEndTime
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codes.Add(new CodeInstruction(OpCodes.Ldarg_1));
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("SetScheduledEndTime")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "SetScheduledStartTime")]
        public static class AudioSource_SetScheduledStartTime
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codes.Add(new CodeInstruction(OpCodes.Ldarg_1));
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("SetScheduledStartTime")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "Stop", new Type[] { })]
        public static class AudioSource_Stop
        {
            public static bool Prefix(AudioSource __instance)
            {
                if (!Initialized) return true;
                int id = __instance.GetInstanceID();
                StopChannel(id, false);
                positionCache.Remove(id);
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "Pause", new Type[] { })]
        public static class AudioSource_Pause
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("Pause")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "UnPause", new Type[] { })]
        public static class AudioSource_UnPause
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("UnPause")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSettings), "dspTime", MethodType.Getter)]
        public static class AudioSettings_dspTime
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("GetDspTime")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "time", MethodType.Getter)]
        public static class AudioSource_time_getter
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("GetTime")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "time", MethodType.Setter)]
        public static class AudioSource_time_setter
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codes.Add(new CodeInstruction(OpCodes.Ldarg_1));
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("SetTime")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "isPlaying", MethodType.Getter)]
        public static class AudioSource_isPlaying
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
            {
                var codes = new List<CodeInstruction>();
                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codes.Add(new CodeInstruction(OpCodes.Call, typeof(Fmod).GetMethod("IsPlaying")));
                codes.Add(new CodeInstruction(OpCodes.Ret));
                return codes;
            }
        }


        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSettings), "GetConfiguration")]
        public static class AudioSettings_GetConfiguration
        {
            public static bool Prefix(ref AudioConfiguration __result)
            {
                if (!Initialized) return true;
                var n = new AudioConfiguration();
                n.sampleRate = 48000;
                n.dspBufferSize = (int)bufferSize;
                n.speakerMode = AudioSettings.speakerMode;
                n.numRealVoices = MaxFmodChannels;
                n.numVirtualVoices = MaxSoftwareChannels;
                __result = n;
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSettings), "Reset")]
        public static class AudioSettings_Reset
        {
            public static bool Prefix(AudioConfiguration config, ref bool __result)
            {
                if (!Initialized) return true;
                
                uint requested = config.dspBufferSize == 0 ? bufferSize : (uint)config.dspBufferSize;
                bufferSize = Math.Max(requested, MinBufferSize);

                CapturePlaybackSnapshots();
                ClearFmodRuntimeState();
                ReleaseFmodSystem();
                if (!CreateConfiguredSystem(entry, "reset"))
                {
                    Initialized = false;
                    playbackSnapshots.Clear();
                    RemovePatches();
                    return true;
                }
                RestorePlaybackSnapshots();
                __result = true;
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioManager), "FindOrLoadAudioClipExternal")]
        public static class AudioManager_FindOrLoadAudioClipExternal
        {
            public static void Prefix(string path)
            {
                if (!Initialized) return;
                entry.Logger.Log("[FMOD] external sound: " + path);
                var cn = Path.GetFileName(path) + "*external";
                if (externalLoadedKeys.Contains(cn)) return;

                if (fmodsys.createSound(path, path.EndsWith(".mp3") ? MODE.CREATESAMPLE : MODE.CREATESTREAM, out var sound) == RESULT.OK)
                {
                    sound.getLength(out var length, TIMEUNIT.MS);
                    AudioClip fakeClip = CreateFakeAudioClip(cn, 440, length / 1000f);
                    AudioManager.Instance.audioLib.Add(cn, fakeClip);
                    cache[fakeClip.GetInstanceID()] = sound;
                    externalLoadedKeys.Add(cn);
                }
                else
                {
                    entry.Logger.Error("[FMOD] external sound load failed: " + path);
                }
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(scrConductor), "GetCurrentAudioOutputType")]
        public static class scrConductor_GetCurrentAudioOutputType
        {
            public static bool Prefix(ref AudioOutputType __result)
            {
                if (!Initialized) return true;
                __result = AudioOutputType.Other;
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(scrConductor), "GetCurrentAudioOutputName")]
        public static class scrConductor_GetCurrentAudioOutputName
        {
            public static bool Prefix(ref string __result)
            {
                if (!Initialized) return true;
                __result = (string.IsNullOrEmpty(selectedDriverName) ? GetDriverName(SelectedDriver) : selectedDriverName) + " with FMOD";
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioManager), "FlushData")]
        public static class AudioManager_FlushData
        {
            public static void Prefix()
            {
                if (!Initialized) return;
                ReleaseSceneSoundCache();
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(RDUtils), "SetMixerParameter")]
        public static class RDUtils_SetMixerParameter
        {
            public static void Postfix()
            {
                if (!Initialized) return;
                RefreshAllChannelVolumes();
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(Persistence), "set_globalVolume")]
        public static class Persistence_set_globalVolume
        {
            public static void Postfix() { if (Initialized) RefreshAllChannelVolumes(); }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(Persistence), "set_musicVolume")]
        public static class Persistence_set_musicVolume
        {
            public static void Postfix() { if (Initialized) RefreshAllChannelVolumes(); }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(Persistence), "set_sfxVolume")]
        public static class Persistence_set_sfxVolume
        {
            public static void Postfix() { if (Initialized) RefreshAllChannelVolumes(); }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch]
        public static class Persistence_set_hitsoundVolume
        {
            [HarmonyPrepare]
            public static bool Prepare()
            {
                return TargetMethod() != null;
            }

            public static MethodBase TargetMethod()
            {
                return FirstPersistenceMethod("set_hitsoundVolume", "set_hitSoundVolume");
            }

            public static void Postfix() { if (Initialized) RefreshAllChannelVolumes(); }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(Persistence), "set_interfaceVolume")]
        public static class Persistence_set_interfaceVolume
        {
            public static void Postfix() { if (Initialized) RefreshAllChannelVolumes(); }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "GetSpectrumData", typeof(float[]), typeof(int), typeof(FFTWindow))]
        public static class AudioSource_GetSpectrumData
        {
            public static bool Prefix(AudioSource __instance, float[] samples, int channel, FFTWindow window)
            {
                if (!Initialized) return true;
                int id = __instance.GetInstanceID();
                Channel channelValue;
                if (TryGetChannel(id, out channelValue))
                {
                    DSP dsp;
                    if (EnsureSpectrumDsp(id, channelValue, out dsp)
                        && GetSpectrumData(dsp, samples, channel, window))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
