using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
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
        private const int FmodLogoWidth = 220;
        private const int FmodLogoHeight = 58;
        // Raw RGBA pixels (top-down), deflate-compressed (no zlib wrapper), base64.
        private const string FmodLogoRawDeflateBase64 =
            "7V0LkBbFEb4HCgIHaoBThHAk+AgYsXKEpzwuJQqpKAnmuKicIZQiGikiRriIBAQ0QY1AxRDKishDokSJxKhcTspIKBE0PhAlIipGFIM8NFEi+Op01zaVv9Z/99/umd1///+mq7qEku35/pn+dmZ6umdLSuIVAKhAPQ/1VtS/oO5EPYD6OcjklRInThIQ9lOJXJECzMNQ70X9L9gRxzcnjm9fxDoI9UmwL45vThzf/o+xLeqdinWi45sTxzcZvl7EB4hXHN+cNHu+YVtncfwDHN+cOL7Fiqsv6n8gGXF8c9Js+YZtVKHug+TE8c1Js+Qb2j8a9WlIVhzfnDRXvs2A5MXxzUmz4xva7ob6keObE8e3RPh2B+RHHN+cNCu+od2OqIcNOLMF9TrUGtSeqF8RaFfnCU6aGd+uUfJsL+oY1FI3mk4c3yLj0ORFvkbzkxtFJ45vIgzHo34qxPEB5Xq5EXTi+CbGcK5ibpuR536jc8JjC2B825AWkD+Woh7HWlYgmI/gbVUgfJPu3Q6htku4T6nmbh7qZtR/Z2D5BPVt1D+iTkI9MY/j3hr1YtQlqDt89YEHUV/mGHAdassU+CnVfYxCXYi6gffifqE8o42ot6NegNo+z/1L9c3zUdej7gnA+xzqItSxtHZLId9uE2JoTPBdewnqVgE2irHejXpygn7QHvUXqPsFOMlXZuVj7sM2+6AuR/1Qsa6hd8g9VAuZIN4zUe9S5vOSP6xBHRE33wTx+NVCDAtC+PE91D+jvsPv9Ff43dhD2MeE6zGD8wk6t29ALY/ZF+h9+5YBztdRhyfkt3RO02jx3PSvxIUY8Z7CvmSr7nKTorZMwre45PosbbVDfTjkmY9RJ0bE3Q/1XUtY6d3WOiZ/mIz6mQWM5E8/jdFvW6DewGNgWyjOdgvtpy3iLUOdzvuWfEsa+Hadr51y1EcjPntJDsxDLd6LckQet71f4vWjbWmIgWudUP+WgF/SvvokS7HydZAeSSPfLhM8+z5qhwC83SzOa35ZZtGHx8aEkea5MRZxUn9uT9A330A91QDvSagvQrokjXx7Xvj85IC934aY+67Okg8fjBEjvY8qLc1r2/PgnxQrrlLg7YD6D0ifpIpvHFOWyv0JzhmZsss0HojP35cAzjsNMbZCfTaPPrqN/EJ4lvokpFPSxrdumrhWlrnt5YT6b5JhfC8JoRhMNwOcv02Bn94d43lUc+ZbG0W8dpUP5+AE+2+LgR/fmiDOmUqMQyC+ewulcn4EvH0txXib0/5ts8lvAC8XIEk5XXn2/naCGHco4/5p2gPtDIsLgxf3fx7SLZenkG+SvRflDLX3Pf9Gwn04VeHL38jDWJ+SkripiUwMwTsa0i+jUsg3ek+tifAcrXNG+549XdH+c+z/lKN6JXg5lBJ5XMG36QqcNG+fiNod9Q+K568WYjSZK7bzb6Rcg86Mm/K+poFZjP5VCKiNNIyR/BP1Rl4/V7EvdEEdCF7d86sW/Jz8qkPa+MZtHQNe3mKQUP3OhVmem6pov6fPxmLh85RncazQl58QtrE10884Biedx5uEOYYaoRwOyvNukWPddynoz0GGZLF5mtLWp8ynVhFyVCaD2dnNCqGPHIioh035ltEm3aewgvcR/0J9CnUuBOTtg5f7IZEXs9gYrujLWmHOg7Q+cFYWOwuENmhcKiJivEnRB+SLgwX9UA3e+aBUfp3FluYeOOqPkUIOEK81ZyOUT26cLxOA6V5bfBO22w7kOX03B5zfSHPHlwhwXqwYr35Z7GjqDEdFxLhZYXuMYsy+rYh/brPwniWZoPQz8o+fgHf+GkVonqiOMZ81X3yrVfT5sABba4R23oGId67kWCdnE8pJK8tipyWvqyWyOAK+loo9bJPBuK1WjNtxvrWeNEd2ExjekcO8o9pDqk98M2DPQ/WJlSUxSh75dpewXaozPSrA1gSFD1RHwFgG2esaw2RZiL0/CW3tyuVnoDuHH2EwbgMV7fXPeL57EnNxhN/RhrH047jdMQnVRSXONz7P2i1s974Qe10U65wZEXAOUPjGD0LsTVTYOyMHxpFCex+CQb0Ev4P2avsEvFp9aZww0fsEipBvfRR+Nz6HzS1Cexsj4JytiJ99KcReV8XvbsiBsU5o7yUL4yeN5V+W8ez50jm+pIgkT3z7ubBNmrs657AprUmjPKJOOWw+I7S5IcJv3yq0uT6HvXrpXsjC+EnvJZiU8ez3k34/ZLT9ddTfgFeLQjH0Jj4PaV3kfJO+H5+JmDsolbEh9k5QrFF/FgHnPMWceXyIvQuE9nZaGD/pWmKcwfp3rwW8VBN9c47z8+pi5Bt4tU7S86zZEft0v9DuyhB74xX87R0B51Cb8QI+85TO65UG41cB8rsORmU831/x+6sMfS5KrjmdRfcoQr7VK/p7QETbq4R2iZ/lAbaktW67o8Sswcsrfk9oe2mOWJFUrjQYP02eZk/f+1Yq1xrgHQTRaxAeKUK+/d4WJ7LY/qFiLAda4sQdgj6Q5lNmPdPLiPVKsb6piX/TeQzI68YP+eOhfP4pzfeoUOBtIcwFoP1D12LhG6/5pN8qltQudgR5PdWcLHaGKXj7XQHOcQr73wyx96DC3lLF+C1UtPOYBZ8DXm+UCvHOV7Qzuoj4Nkjx+y8StvGUaSxGEdM4LHn/gne/iPS9MDPE3o9BJwuirB34zG2uso1pWez9SGlrcVDOQxa8s5Rt1BcR36S5UaHnWQFtSPv5c1/+A+V1Su9wXafoC+l30l8LilvzvK69X/KJsP0xeLVP2rt36Z3y5Sw26U5q7b2HtD48KwRvX47za2VwIfKNz1lqWceB7q6djYrf01fRDu0RZ4JXw7FV8fwUBc4bFO08y3NZbYZ2Uu4J/bKN14v0PYmreS22xdDm2pDfv9QCXpqfKReZ6iB/ifp3Q5v7/PMn749HcN88AF79yxRQfqsiRr59AuZyvTLfaA8kK6cpcPa31HYN2zsT0nN3Sc65Av/f1yB9d5dc68P4VV6HNHFcnXJHzwGv/onu17imyPh2hvIdsiTBMdquxFgG8hzSQL6xzXtS5LsPRuiD36UIL31b4OgMbFXMqXG8ZjqbdRCfQVYyF+cWCd+eNlgjD0lwnBoMcM6zzDe6C+H9FPgu5UR3j5j7sDcFeGldMMKH7dEjMSreM67j90Mjrzt7gZeDtC8sdlxAfLvUwI9pzf1SAuP0ERh8cw6fPdlCP9X4bNamwH/HCfpAU8NqW27yYaL6HKphbpPBtzrfGfJs/jPFbVcWON+IKy0M40DDExinORbiVbfb5JsgfykuWZRQ7MiWrPOfiXBMam3G34lvlN9H9xKdyv55RcZa6q0C5hudAQy1FHt9IMZxeh0sfC8RvPtRdlvmG83vy/Pgu6tB+c09kN/7ZEPoXoe2WbBQ/cpyH9/28NqX1o+zj+z1wLsj5VAB821KiSXhfW0c31z5QBvLCcBZbXAeVROypv5Vgr67DCKcRefYA9ySIF6qKQo6z6TzhUYf36jOkHJV382MR4OXf76rQPl2Wwzniz3Au+vXlhwEg/sIQnDS/aeHbPHNtzaK89uFdM5OZ3allvphPMT7DaLPeH4qD8HQK2j/hv+9Crz6uTL+O92DuaLA+EZryDi/8Un3l9r4Zgy92/rHiJPelftt8o3t9oZ4vkFD9bh9YugHOptbHwNeqjH9VkQMTb74ZF3GOc4mfo+dwONVSPHJFyDL/XExjCHltk8zeNdT/kbHBHBWCvdeNRHtkp/Ug51vDOzgeag8xn4o5TXcCxbwUn7eBEkMDry7hGhfXR+SA0s8vLFAzt8ob2+MaRxSMY5VvE94L+JaaaXk/WUR5wAem49t8M3Hu5H8uyRndbS+ovrC8+LkWQDvzub94QEBXtoP34/6Ha2P8XkNzeFrUS/ic+8aXo8Sh6eCvGZhOp/tRdULI9ptzHiG/ky5yvStL7o/tXNJnoXnO8rPaQDv3kG6w/Ih8O7pm8M+2TYFOCv4jGouY3vINx69DWxTfRTdB0f3hs3nOfwR9i/680KOHQxM+r0YgpfumrqcY0HE/4e5T5bzb7iK/81RltosY84u4rZWMc+6ZP67/wE=";

        private static Texture2D GetFmodLogo()
        {
            if (fmodLogoTex != null) return fmodLogoTex;
            try
            {
                byte[] comp = Convert.FromBase64String(FmodLogoRawDeflateBase64);
                int expected = FmodLogoWidth * FmodLogoHeight * 4;
                byte[] raw = new byte[expected];
                using (var ms = new MemoryStream(comp))
                using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    int off = 0;
                    while (off < expected)
                    {
                        int n = ds.Read(raw, off, expected - off);
                        if (n <= 0) break;
                        off += n;
                    }
                    if (off != expected)
                    {
                        entry?.Logger?.Log($"[Fmod] logo deflate short: {off}/{expected}");
                        fmodLogoTex = null;
                        return null;
                    }
                }
                // Source pixels are top-down RGBA; Unity texture origin is bottom-left.
                Color32[] pixels = new Color32[FmodLogoWidth * FmodLogoHeight];
                for (int y = 0; y < FmodLogoHeight; y++)
                {
                    int srcRow = (FmodLogoHeight - 1 - y) * FmodLogoWidth * 4;
                    int dstRow = y * FmodLogoWidth;
                    for (int x = 0; x < FmodLogoWidth; x++)
                    {
                        int s = srcRow + x * 4;
                        pixels[dstRow + x] = new Color32(raw[s], raw[s + 1], raw[s + 2], raw[s + 3]);
                    }
                }
                var t = new Texture2D(FmodLogoWidth, FmodLogoHeight, TextureFormat.RGBA32, false);
                t.SetPixels32(pixels);
                t.filterMode = FilterMode.Bilinear;
                t.wrapMode = TextureWrapMode.Clamp;
                t.Apply(false, false);
                entry?.Logger?.Log($"[Fmod] logo built {t.width}x{t.height}");
                fmodLogoTex = t;
            }
            catch (Exception ex)
            {
                entry?.Logger?.Log("[Fmod] logo build exception: " + ex.Message);
                fmodLogoTex = null;
            }
            return fmodLogoTex;
        }

        public static void DrawFmodLogo()
        {
            var tex = GetFmodLogo();
            if (tex == null) return;
            int w = tex.width > 1 ? tex.width : 220;
            int h = tex.height > 1 ? tex.height : 58;
            entry?.Logger?.Log($"[Fmod] logo size {tex.width}x{tex.height} drawing {w}x{h}");
            Rect r = GUILayoutUtility.GetRect(w, h, GUIStyle.none, GUILayout.ExpandWidth(false), GUILayout.Width(w), GUILayout.Height(h));
            var prevColor = GUI.color;
            var prevContent = GUI.contentColor;
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            GUI.color = prevColor;
            GUI.contentColor = prevContent;
        }

        public static void DrawFmodBranding()
        {
            DrawFmodLogo();
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
        internal static int fmodSampleRate = 48000;
        internal static Dictionary<int, Sound> cache = new Dictionary<int, Sound>();
        internal static Dictionary<int, Sound> staticCache = new Dictionary<int, Sound>();
        private static readonly Dictionary<int, Sound> assetParentCache = new Dictionary<int, Sound>();
        private static readonly HashSet<string> externalLoadedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> externalClipIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, UnityAudioAssetInfo> unityAudioAssetIndex = new Dictionary<string, UnityAudioAssetInfo>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<int> loggedUnityAssetSounds = new HashSet<int>();
        private static readonly HashSet<int> loggedChannelStarts = new HashSet<int>();
        private static readonly HashSet<int> loggedChannelVolumes = new HashSet<int>();
        private static bool unityAudioAssetIndexBuilt;
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

        private sealed class UnityAudioAssetInfo
        {
            public string ClipName;
            public string AssetPath;
            public string ResourcePath;
            public ulong Offset;
            public ulong Size;
            public int SubsoundIndex;
            public int Frequency;
            public int Channels;
            public float LengthSeconds;
            public int CompressionFormat;
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

        private static double ReadUnityDspTime()
        {
            try
            {
                return AudioSettings.dspTime;
            }
            catch
            {
                return Time.realtimeSinceStartup;
            }
        }

        private static double ConvertUnityDspTimeToFmodTime(double unityTime)
        {
            double unityNow = ReadUnityDspTime();
            double delay = Math.Max(0d, unityTime - unityNow);
            return GetDspTime() + delay;
        }

        private static bool IsConductorMusicSource(AudioSource source)
        {
            if (!source) return false;
            try
            {
                var conductor = scrConductor.instance;
                return conductor && (source == conductor.song || source == conductor.song2 || source == conductor.song3);
            }
            catch
            {
                return false;
            }
        }

        private static AudioOutputType GetPlatformAudioOutputType()
        {
            try
            {
                var helper = ADOFAI.Common.Platform.PlatformHelper.instance;
                if (helper == null) return AudioOutputType.Other;
                var outputType = helper.GetActiveAudioDeviceType();
                return outputType == AudioOutputType.NotSet ? AudioOutputType.Other : outputType;
            }
            catch
            {
                return AudioOutputType.Other;
            }
        }

        private static bool HasCachedFmodSound(AudioClip clip)
        {
            if (!clip) return false;
            int id = clip.GetInstanceID();
            return cache.ContainsKey(id) || staticCache.ContainsKey(id);
        }

        private static uint ReadUInt32BE(byte[] data, int offset)
        {
            return (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);
        }

        private static int ReadInt32BE(byte[] data, int offset)
        {
            return unchecked((int)ReadUInt32BE(data, offset));
        }

        private static ulong ReadUInt64BE(byte[] data, int offset)
        {
            return ((ulong)ReadUInt32BE(data, offset) << 32) | ReadUInt32BE(data, offset + 4);
        }

        private static void Align4(Stream stream)
        {
            long aligned = (stream.Position + 3L) & ~3L;
            if (aligned != stream.Position)
                stream.Position = aligned;
        }

        private static string ReadNullTerminatedString(BinaryReader reader)
        {
            var bytes = new List<byte>(32);
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte b = reader.ReadByte();
                if (b == 0)
                    break;
                bytes.Add(b);
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        private static string ReadAlignedString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0 || length > 1024 * 1024)
                throw new InvalidDataException("invalid Unity string length: " + length);

            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
                throw new EndOfStreamException();

            Align4(reader.BaseStream);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void RememberUnityAudioAsset(UnityAudioAssetInfo info)
        {
            if (info == null || string.IsNullOrEmpty(info.ClipName)) return;
            unityAudioAssetIndex[info.ClipName] = info;

            string fileName = Path.GetFileName(info.ClipName);
            if (!string.IsNullOrEmpty(fileName))
                unityAudioAssetIndex[fileName] = info;

            string noExt = Path.GetFileNameWithoutExtension(info.ClipName);
            if (!string.IsNullOrEmpty(noExt))
                unityAudioAssetIndex[noExt] = info;
        }

        private static bool TryParseUnityAudioClip(BinaryReader reader, string assetPath, long objectOffset, uint objectSize, out UnityAudioAssetInfo info)
        {
            info = null;
            if (objectOffset < 0 || objectSize == 0) return false;

            long oldPosition = reader.BaseStream.Position;
            try
            {
                reader.BaseStream.Position = objectOffset;
                long objectEnd = objectOffset + objectSize;

                string clipName = ReadAlignedString(reader);
                int loadType = reader.ReadInt32();
                int channels = reader.ReadInt32();
                int frequency = reader.ReadInt32();
                reader.ReadInt32(); // bits per sample
                float length = reader.ReadSingle();
                reader.ReadByte(); // m_IsTrackerFormat
                Align4(reader.BaseStream);
                int subsoundIndex = reader.ReadInt32();
                reader.ReadByte(); // m_PreloadAudioData
                reader.ReadByte(); // m_LoadInBackground
                reader.ReadByte(); // m_Legacy3D
                Align4(reader.BaseStream);

                string source = ReadAlignedString(reader);
                ulong offset = reader.ReadUInt64();
                ulong size = reader.ReadUInt64();
                int compressionFormat = reader.BaseStream.Position + 4 <= objectEnd ? reader.ReadInt32() : 0;

                if (string.IsNullOrEmpty(clipName) || string.IsNullOrEmpty(source) || size == 0)
                    return false;

                string directory = Path.GetDirectoryName(assetPath) ?? "";
                string resourcePath = Path.IsPathRooted(source) ? source : Path.Combine(directory, source);
                if (!File.Exists(resourcePath))
                    return false;

                info = new UnityAudioAssetInfo
                {
                    ClipName = clipName,
                    AssetPath = assetPath,
                    ResourcePath = resourcePath,
                    Offset = offset,
                    Size = size,
                    SubsoundIndex = Math.Max(0, subsoundIndex),
                    Frequency = frequency,
                    Channels = channels,
                    LengthSeconds = length,
                    CompressionFormat = compressionFormat
                };
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { reader.BaseStream.Position = oldPosition; } catch { }
            }
        }

        private static void TryIndexUnityAssetFile(string assetPath)
        {
            try
            {
                using (var fs = File.OpenRead(assetPath))
                using (var reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    if (fs.Length < 64) return;
                    byte[] header = reader.ReadBytes(48);
                    if (header.Length != 48) return;

                    int version = ReadInt32BE(header, 8);
                    if (version != 22) return;

                    long dataOffset = unchecked((long)ReadUInt64BE(header, 32));
                    if (dataOffset <= 0 || dataOffset >= fs.Length) return;

                    fs.Position = 48;
                    ReadNullTerminatedString(reader);
                    reader.ReadInt32(); // target platform
                    byte hasTypeTree = reader.ReadByte();
                    int typeCount = reader.ReadInt32();
                    if (typeCount <= 0 || typeCount > 4096) return;

                    if (hasTypeTree != 0)
                    {
                        entry?.Logger?.Log("[FMOD] Unity asset index skipped type-tree file: " + Path.GetFileName(assetPath));
                        return;
                    }

                    var classIds = new List<int>(typeCount);
                    for (int i = 0; i < typeCount; i++)
                    {
                        int classId = reader.ReadInt32();
                        reader.ReadByte(); // isStrippedType
                        reader.ReadInt16(); // scriptTypeIndex
                        if (classId == 114)
                            reader.ReadBytes(16); // script id
                        reader.ReadBytes(16); // old type hash
                        classIds.Add(classId);
                    }

                    int objectCount = reader.ReadInt32();
                    if (objectCount <= 0 || objectCount > 1000000) return;

                    for (int i = 0; i < objectCount; i++)
                    {
                        Align4(fs);
                        reader.ReadInt64(); // path id
                        long byteStart = reader.ReadInt64();
                        uint byteSize = reader.ReadUInt32();
                        int typeId = reader.ReadInt32();
                        int classId = (typeId >= 0 && typeId < classIds.Count) ? classIds[typeId] : -1;
                        if (classId != 83)
                            continue;

                        long objectOffset = dataOffset + byteStart;
                        UnityAudioAssetInfo info;
                        if (TryParseUnityAudioClip(reader, assetPath, objectOffset, byteSize, out info))
                            RememberUnityAudioAsset(info);
                    }
                }
            }
            catch (Exception ex)
            {
                entry?.Logger?.Log("[FMOD] Unity asset index failed for " + Path.GetFileName(assetPath) + ": " + ex.Message);
            }
        }

        private static bool LooksLikeUnityDataPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   Directory.Exists(path) &&
                   (File.Exists(Path.Combine(path, "resources.assets")) ||
                    File.Exists(Path.Combine(path, "sharedassets0.assets")) ||
                    File.Exists(Path.Combine(path, "globalgamemanagers.assets")));
        }

        private static string ResolveUnityDataPath()
        {
            string dataPath;
            try { dataPath = Application.dataPath; }
            catch { dataPath = null; }

            if (LooksLikeUnityDataPath(dataPath))
                return dataPath;

            if (!string.IsNullOrEmpty(dataPath))
            {
                string macBundleData = Path.Combine(dataPath, "Resources", "Data");
                if (LooksLikeUnityDataPath(macBundleData))
                    return macBundleData;

                string nestedData = Path.Combine(dataPath, "Data");
                if (LooksLikeUnityDataPath(nestedData))
                    return nestedData;
            }

            try
            {
                string managedDir = Path.GetDirectoryName(typeof(AudioClip).Assembly.Location);
                if (!string.IsNullOrEmpty(managedDir))
                {
                    string fromManaged = Path.GetFullPath(Path.Combine(managedDir, ".."));
                    if (LooksLikeUnityDataPath(fromManaged))
                        return fromManaged;
                }
            }
            catch { }

            return dataPath;
        }

        private static void EnsureUnityAudioAssetIndex()
        {
            if (unityAudioAssetIndexBuilt) return;
            unityAudioAssetIndexBuilt = true;
            unityAudioAssetIndex.Clear();

            string dataPath = ResolveUnityDataPath();
            if (string.IsNullOrEmpty(dataPath) || !Directory.Exists(dataPath))
                return;

            try
            {
                foreach (string assetPath in Directory.GetFiles(dataPath, "*.assets", SearchOption.TopDirectoryOnly))
                    TryIndexUnityAssetFile(assetPath);
                entry?.Logger?.Log("[FMOD] Unity audio asset index ready: " + unityAudioAssetIndex.Count + " keys from " + dataPath);
            }
            catch (Exception ex)
            {
                entry?.Logger?.Log("[FMOD] Unity audio asset index failed: " + ex.Message);
            }
        }

        private static bool TryGetUnityAudioAssetInfo(AudioClip clip, out UnityAudioAssetInfo info)
        {
            info = null;
            if (!clip) return false;
            EnsureUnityAudioAssetIndex();
            return unityAudioAssetIndex.TryGetValue(clip.name, out info);
        }

        private static bool TryReadUnityAudioAssetBytes(UnityAudioAssetInfo info, out byte[] data)
        {
            data = null;
            if (info == null || string.IsNullOrEmpty(info.ResourcePath) || !File.Exists(info.ResourcePath)) return false;
            if (info.Size == 0 || info.Size > int.MaxValue || info.Offset > long.MaxValue) return false;

            try
            {
                using (var fs = File.OpenRead(info.ResourcePath))
                {
                    ulong fileLength = (ulong)fs.Length;
                    if (info.Offset > fileLength || info.Size > fileLength - info.Offset)
                        return false;
                    fs.Position = (long)info.Offset;
                    data = new byte[(int)info.Size];
                    int read = 0;
                    while (read < data.Length)
                    {
                        int n = fs.Read(data, read, data.Length - read);
                        if (n <= 0) return false;
                        read += n;
                    }
                    return true;
                }
            }
            catch
            {
                data = null;
                return false;
            }
        }

        private static bool TryMakeSoundFromUnityAsset(AudioClip clip, out Sound sound)
        {
            sound = default;
            if (!clip) return false;

            int key = clip.GetInstanceID();
            Dictionary<int, Sound> targetCache = clip.loadType != AudioClipLoadType.DecompressOnLoad ? staticCache : cache;
            if (targetCache.TryGetValue(key, out sound))
                return sound.hasHandle();

            UnityAudioAssetInfo info;
            if (!TryGetUnityAudioAssetInfo(clip, out info))
                return false;

            byte[] data;
            if (!TryReadUnityAudioAssetBytes(info, out data) || data.Length < 4)
                return false;

            CREATESOUNDEXINFO soundInfo = new CREATESOUNDEXINFO();
            soundInfo.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
            soundInfo.length = (uint)data.Length;
            soundInfo.suggestedsoundtype = SOUND_TYPE.FSB;
            soundInfo.initialsubsound = info.SubsoundIndex;
            soundInfo.numsubsounds = 1;

            Sound parent;
            MODE mode = MODE.OPENMEMORY | MODE.CREATESAMPLE | MODE.ACCURATETIME;
            RESULT result = fmodsys.createSound(data, mode, ref soundInfo, out parent);
            if (result != RESULT.OK || !parent.hasHandle())
            {
                entry?.Logger?.Log("[FMOD] Unity asset FSB load failed: " + info.ClipName + " (" + result + ")");
                return false;
            }

            Sound playable = parent;
            int subSoundCount;
            if (parent.getNumSubSounds(out subSoundCount) == RESULT.OK && subSoundCount > 0)
            {
                int subIndex = Mathf.Clamp(info.SubsoundIndex, 0, subSoundCount - 1);
                Sound subSound;
                RESULT subResult = parent.getSubSound(subIndex, out subSound);
                if (subResult == RESULT.OK && subSound.hasHandle())
                {
                    playable = subSound;
                    assetParentCache[key] = parent;
                }
            }

            playable.setMode(MODE.LOOP_NORMAL);
            targetCache[key] = playable;
            sound = playable;

            if (loggedUnityAssetSounds.Add(key))
            {
                entry?.Logger?.Log("[FMOD] Unity asset sound loaded: " + info.ClipName +
                                   " (" + Path.GetFileName(info.ResourcePath) + "@" + info.Offset +
                                   "+" + info.Size + ", comp=" + info.CompressionFormat +
                                   ", " + info.Channels + "ch/" + info.Frequency + "Hz)");
            }
            return true;
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
            if (!Initialized)
                return ReadUnityDspTime();
            return GetDspClock() / (double)fmodSampleRate;
        }

        private static int ResolveSelectedDriverSampleRate()
        {
            FmodDriverInfo info;
            if (TryGetDriverInfo(SelectedDriver, out info) && info.SystemRate > 0)
                return Mathf.Clamp(info.SystemRate, 8000, 192000);

            try
            {
                int unityRate = AudioSettings.outputSampleRate;
                if (unityRate > 0)
                    return Mathf.Clamp(unityRate, 8000, 192000);
            }
            catch { }

            return 48000;
        }

        private static ulong SecondsToDspClock(double seconds)
        {
            if (seconds <= 0d) return 0;
            return (ulong)(seconds * fmodSampleRate);
        }

        private static uint SecondsToDspClockDuration(double seconds)
        {
            if (seconds <= 0d) return 0;
            double samples = seconds * fmodSampleRate;
            return samples >= uint.MaxValue ? uint.MaxValue : (uint)samples;
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

            if (staticClip && TryMakeSoundFromUnityAsset(audioclip, out a))
                return a;

            if (audioclip.samples <= 0 || audioclip.channels <= 0 || audioclip.frequency <= 0)
                throw new InvalidOperationException("clip data invalid: " + audioclip.name);

            if (audioclip.loadState == AudioDataLoadState.Unloaded)
                audioclip.LoadAudioData();

            float[] samples = new float[audioclip.samples * audioclip.channels];
            if (!audioclip.GetData(samples, 0))
                throw new InvalidOperationException("clip data unavailable: " + audioclip.name + " (loadType=" + audioclip.loadType + ", loadState=" + audioclip.loadState + ")");

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
            return TryMakeSoundFromAudioClip(clip, out sound, true);
        }

        private static bool TryMakeSoundFromAudioClip(AudioClip clip, out Sound sound, bool logFailure)
        {
            sound = default;
            try
            {
                sound = MakeSoundFromAudioClip(clip);
                return sound.hasHandle();
            }
            catch (Exception ex)
            {
                if (TryMakeSoundFromUnityAsset(clip, out sound))
                    return true;

                if (logFailure)
                    entry?.Logger?.Log("[FMOD] clip unavailable to FMOD: " + ex.Message);
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

        private static bool ShouldFollowSystemDefaultDriver()
        {
            return !UseASIO && Main.settings != null && Main.settings.FmodSelectedDriver == 0;
        }

        private static void CaptureSelectedDriverIdentity()
        {
            selectedDriverGuid = Guid.Empty;
            selectedDriverName = "";
            if (ShouldFollowSystemDefaultDriver()) return;

            FmodDriverInfo info;
            if (!TryGetDriverInfo(SelectedDriver, out info)) return;
            selectedDriverGuid = info.Guid;
            selectedDriverName = info.Name;
        }

        private static void RefreshDriverCache(bool force, bool allowDriverSwitch)
        {
            RefreshDriverCache(force, allowDriverSwitch, false);
        }

        private static void RefreshDriverCache(bool force, bool allowDriverSwitch, bool reapplyDefaultDriver)
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

            ReconcileSelectedDriver(drivers, allowDriverSwitch, reapplyDefaultDriver);
        }

        private static void ReconcileSelectedDriver(List<FmodDriverInfo> drivers, bool allowDriverSwitch, bool reapplyDefaultDriver)
        {
            if (drivers == null || drivers.Count == 0)
            {
                SelectedDriver = 0;
                Main.settings.FmodSelectedDriver = 0;
                selectedDriverGuid = Guid.Empty;
                selectedDriverName = "";
                return;
            }

            bool followDefault = ShouldFollowSystemDefaultDriver();
            bool hadIdentity = !followDefault && HasSelectedDriverIdentity();
            int resolved = followDefault ? 0 : FindSelectedDriverByIdentity(drivers);
            bool selectedDeviceMissing = hadIdentity && resolved < 0;

            if (resolved < 0 && !hadIdentity && SelectedDriver >= 0 && SelectedDriver < drivers.Count)
                resolved = SelectedDriver;
            if (resolved < 0)
                resolved = 0;

            SelectedDriver = resolved;
            Main.settings.FmodSelectedDriver = resolved;

            int activeDriver;
            bool activeKnown = fmodsys.getDriver(out activeDriver) == RESULT.OK;
            bool shouldReapplyDefault = followDefault && reapplyDefaultDriver;
            if (allowDriverSwitch && (shouldReapplyDefault || selectedDeviceMissing || (activeKnown && activeDriver != resolved)))
            {
                string reason = shouldReapplyDefault ? "system output changed" : (selectedDeviceMissing ? "selected device removed" : "driver index changed");
                if (followDefault)
                {
                    RecreateForDefaultDeviceChange(reason);
                    return;
                }
                ApplyDriverIndex(resolved, !followDefault, reason);
                return;
            }

            CaptureSelectedDriverIdentity();
        }

        private static void RecreateForDefaultDeviceChange(string reason)
        {
            if (!Initialized) return;
            var modEntry = entry;
            if (modEntry == null) return;
            DisableRuntime(false);
            if (EnableRuntime(modEntry))
                modEntry.Logger.Log("[FMOD] " + reason + ": reinitialized to follow system default output");
            else
                modEntry.Logger.Log("[FMOD] " + reason + ": failed to reinit for new default output");
        }

        private static void TickDriverRefresh()
        {
            if (!Initialized) return;
            if (!driverRefreshRequested && Time.realtimeSinceStartup < nextDriverPollTime) return;

            bool deviceEvent = driverRefreshRequested;
            driverRefreshRequested = false;
            nextDriverPollTime = Time.realtimeSinceStartup + DriverPollSeconds;
            RefreshDriverCache(deviceEvent, true, deviceEvent);
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
            loggedChannelStarts.Remove(id);
            loggedChannelVolumes.Remove(id);

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

            if (!hasFreq)
            {
                int priority;
                if (sound.getDefaults(out frequency, out priority) != RESULT.OK || frequency <= 0f)
                    frequency = fmodSampleRate;
                channelFrequencies[id] = frequency;
            }

            if (!hasLength)
            {
                if (sound.getLength(out lengthPcm, TIMEUNIT.PCM) != RESULT.OK || lengthPcm == 0)
                {
                    uint lengthMs;
                    if (sound.getLength(out lengthMs, TIMEUNIT.MS) == RESULT.OK && lengthMs > 0)
                        lengthPcm = (uint)Math.Min(uint.MaxValue, Math.Round(lengthMs / 1000d * frequency));
                    else
                        lengthPcm = 0;
                }
                channelLengthsPcm[id] = lengthPcm;
            }

            return true;
        }

        private static void SetPlaybackDelay(Channel channel, ulong startDspClock, uint lengthPcm, float pitch, float frequency)
        {
            if (lengthPcm == 0 || frequency <= 0f)
            {
                channel.setDelay(startDspClock, 0);
                return;
            }

            double safePitch = Math.Max(0.001d, Math.Abs(pitch));
            double durationSeconds = lengthPcm / safePitch / (double)frequency;
            channel.setDelay(startDspClock, startDspClock + SecondsToDspClockDuration(durationSeconds));
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
            float mixerScalar = GetMixerScalar(source);
            float finalVolume = sourceVolume * mixerScalar;
            channel.setVolume(finalVolume);

            if (loggedChannelVolumes.Add(id))
            {
                entry?.Logger?.Log("[FMOD] channel volume: source=\"" + (source ? source.name : "<null>") +
                                   "\" clip=\"" + (source && source.clip ? source.clip.name : "<null>") +
                                   "\" sourceVol=" + sourceVolume.ToString("0.###") +
                                   " mixerScalar=" + mixerScalar.ToString("0.###") +
                                   " final=" + finalVolume.ToString("0.###") +
                                   " muted=" + (source && source.mute));
            }
        }

        private static void MuteFmodOnly(AudioSource source)
        {
            if (!source) return;
            int id = source.GetInstanceID();
            Channel channel;
            if (!TryGetChannel(id, out channel)) return;

            volCache[id] = 0f;
            SetChannelVolume(id, source, channel, 0f);
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
            foreach (var pair in sounds)
            {
                Sound parent;
                if (assetParentCache.TryGetValue(pair.Key, out parent))
                {
                    try { parent.release(); } catch { }
                    assetParentCache.Remove(pair.Key);
                }
                else
                {
                    try { pair.Value.release(); } catch { }
                }

                loggedUnityAssetSounds.Remove(pair.Key);
            }
            sounds.Clear();
        }

        private static void RemoveExternalFakeClips()
        {
            if (externalClipIds.Count == 0) return;

            try
            {
                var manager = AudioManager.Instance;
                if (manager == null || manager.audioLib == null) return;

                foreach (var pair in new List<KeyValuePair<string, int>>(externalClipIds))
                {
                    AudioClip clip;
                    if (!manager.audioLib.TryGetValue(pair.Key, out clip)) continue;
                    if (!clip || clip.GetInstanceID() != pair.Value) continue;

                    manager.audioLib.Remove(pair.Key);
                    Object.Destroy(clip);
                    entry?.Logger?.Log("[FMOD] external fake clip removed: " + pair.Key);
                }
            }
            catch (Exception ex)
            {
                entry?.Logger?.Log("[FMOD] external fake clip cleanup failed: " + ex.Message);
            }
        }

        private static void ReleaseSceneSoundCache()
        {
            RemoveExternalFakeClips();
            ReleaseSoundCache(cache);
            externalLoadedKeys.Clear();
            externalClipIds.Clear();
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
            public float Volume;
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
                    freq = fmodSampleRate;
                bool paused;
                chnl.getPaused(out paused);

                playbackSnapshots.Add(new PlaybackSnapshot
                {
                    Source = pair.Value,
                    Clip = pair.Value.clip,
                    Position = pcm / freq,
                    Volume = pair.Value.volume,
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
                volCache[id] = snap.Volume;
                if (!pa(snap.Source, snap.Clip))
                    continue;
                Channel chnl;
                if (!channels.TryGetValue(id, out chnl)) continue;

                chnl.getDSPClock(out _, out var dspClock);
                TryGetChannelInfo(id, chnl, out var freq, out var length);
                float pitch = Mathf.Approximately(snap.Source.pitch, 0f) ? 1f : snap.Source.pitch;
                chnl.setPosition((uint)(snap.Position * freq), TIMEUNIT.PCM);
                SetPlaybackDelay(chnl, dspClock, length, pitch, freq);
                chnl.setLoopCount(snap.Source.loop ? -1 : 0);
                chnl.setPitch(pitch);
                SetChannelVolume(id, snap.Source, chnl, snap.Volume);
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
            loggedChannelStarts.Clear();
            loggedChannelVolumes.Clear();
            RemoveExternalFakeClips();
            ReleaseSoundCache(cache);
            ReleaseSoundCache(staticCache);
            foreach (var parent in assetParentCache.Values)
                try { parent.release(); } catch { }
            assetParentCache.Clear();
            loggedUnityAssetSounds.Clear();
            externalLoadedKeys.Clear();
            externalClipIds.Clear();
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

            RegisterSystemCallback(modEntry, context);
            ResolveSelectedDriverFromIdentity();
            ClampSelectedDriver();
            bool followDefault = ShouldFollowSystemDefaultDriver();
            int requestedRate = followDefault ? 0 : ResolveSelectedDriverSampleRate();
            fmodSampleRate = requestedRate > 0 ? requestedRate : 48000;
            if (fmodsys.setSoftwareFormat(requestedRate, SPEAKERMODE.DEFAULT, 0) != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": setSoftwareFormat(" + requestedRate + ") failed"); ReleaseFmodSystem(); return false; }
            if (fmodsys.setSoftwareChannels(MaxSoftwareChannels) != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": setSoftwareChannels failed"); ReleaseFmodSystem(); return false; }
            if (fmodsys.setDSPBufferSize(bufferSize, 2) != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": setDSPBufferSize failed"); ReleaseFmodSystem(); return false; }

            if (!followDefault)
            {
                RESULT setDriverResult = fmodsys.setDriver(SelectedDriver);
                if (setDriverResult != RESULT.OK) { modEntry.Logger.Error("[FMOD] " + context + ": setDriver(" + SelectedDriver + ") failed: " + setDriverResult); ReleaseFmodSystem(); return false; }
            }
            if (!InitFmodSystem()) { ReleaseFmodSystem(); return false; }

            int actualRate;
            SPEAKERMODE actualMode;
            int actualChannels;
            if (fmodsys.getSoftwareFormat(out actualRate, out actualMode, out actualChannels) == RESULT.OK && actualRate > 0)
                fmodSampleRate = actualRate;

            int activeDriverIdx;
            if (fmodsys.getDriver(out activeDriverIdx) == RESULT.OK)
            {
                FmodDriverInfo activeInfo;
                string activeName = TryGetDriverInfo(activeDriverIdx, out activeInfo) ? activeInfo.Name : "<unknown>";
                int activeRate = activeInfo != null ? activeInfo.SystemRate : 0;
                modEntry.Logger.Log("[FMOD] " + context + ": active driver index=" + activeDriverIdx + " name=\"" + activeName + "\" deviceRate=" + activeRate + " mixRate=" + fmodSampleRate);

                int driverCount;
                if (fmodsys.getNumDrivers(out driverCount) == RESULT.OK)
                {
                    for (int i = 0; i < driverCount; i++)
                    {
                        FmodDriverInfo di;
                        if (TryGetDriverInfo(i, out di))
                            modEntry.Logger.Log("[FMOD]   driver[" + i + "] name=\"" + di.Name + "\" rate=" + di.SystemRate + " channels=" + di.SpeakerModeChannels);
                    }
                }
            }

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
            modEntry.Logger.Log("[FMOD] initialized (buffer=" + bufferSize + ", rate=" + fmodSampleRate + ", driver=" + SelectedDriver + ", device=" + (string.IsNullOrEmpty(selectedDriverName) ? "system default" : selectedDriverName) + ", asio=" + UseASIO + ")");
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
            modEntry.Logger.Log("[FMOD] re-enabled (rate=" + fmodSampleRate + ", asio=" + UseASIO + ", driver=" + SelectedDriver + ")");
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

            StopChannel(id, false);
            bool conductorSource = IsConductorMusicSource(__instance);

            Sound sound;
            if (!TryMakeSoundFromAudioClip(value, out sound, !conductorSource))
            {
                if (conductorSource)
                    entry?.Logger?.Log("[FMOD] conductor clip could not be loaded by FMOD: " + value.name);
                return false;
            }

            idToAudioSource[id] = __instance;

            Channel channel;
            RESULT playResult = fmodsys.playSound(sound, __instance.ignoreListenerPause ? nonpause : general, true, out channel);
            if (playResult != RESULT.OK || !channel.hasHandle())
            {
                entry?.Logger?.Log("[FMOD] playSound failed: " + playResult);
                return false;
            }

            channels[id] = channel;
            if (loggedChannelStarts.Add(id))
            {
                entry?.Logger?.Log("[FMOD] channel start: source=\"" + (__instance ? __instance.name : "<null>") +
                                   "\" clip=\"" + (value ? value.name : "<null>") +
                                   "\" sourceVol=" + (__instance ? __instance.volume.ToString("0.###") : "<null>") +
                                   " muted=" + (__instance && __instance.mute) +
                                   " loop=" + (__instance && __instance.loop) +
                                   " conductor=" + conductorSource);
            }
            return true;
        }

        private static bool SetAudioSourceVolumeInternal(AudioSource __instance, float vol)
        {
            int id = __instance.GetInstanceID();
            volCache[id] = vol;
            Channel channel;
            if (TryGetChannel(id, out channel))
            {
                SetChannelVolume(id, __instance, channel, vol);
                return true;
            }

            return false;
        }

        public static void SetAudioSourceVolume(AudioSource __instance, float vol)
        {
            SetAudioSourceVolumeInternal(__instance, vol);
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
                chnl.setDelay(start, SecondsToDspClock(time));
            }
        }

        public static void SetScheduledStartTime(AudioSource __instance, double time)
        {
            Channel chnl;
            if (TryGetChannel(__instance.GetInstanceID(), out chnl))
            {
                chnl.getDelay(out _, out var end);
                chnl.setDelay(SecondsToDspClock(time), end);
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
            positionCache[id] = time;
            Channel channel;
            if (TryGetChannel(id, out channel))
            {
                TryGetChannelInfo(id, channel, out var freq, out _);
                channel.setPosition((uint)(time * freq), TIMEUNIT.PCM);
            }
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
            int sampleRate = Mathf.Clamp((int)frequency, 1000, 44100);
            int sampleCount = Math.Max(1, (int)(sampleRate * Math.Max(duration, 0.001f)));
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
                if (__instance.clip == null) return true;
                float sourceVolume = __instance.volume;
                if (!pa(__instance, __instance.clip))
                    return true;

                int id = __instance.GetInstanceID();
                volCache[id] = sourceVolume;
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
                    SetPlaybackDelay(chnl, dspClock, length, pitch, freq);
                    chnl.setLoopCount(__instance.loop ? -1 : 0);
                    chnl.setPitch(pitch);

                    SetChannelVolume(id, __instance, chnl, sourceVolume);
                    chnl.setPriority(__instance.priority);
                    chnl.setPaused(false);
                }
                return false;
            }
        }

        private static double DelaySamplesToSeconds(ulong delaySamples)
        {
            if (delaySamples == 0) return 0d;

            int sampleRate = fmodSampleRate > 0 ? fmodSampleRate : 48000;
            try
            {
                int unityRate = AudioSettings.outputSampleRate;
                if (unityRate > 0) sampleRate = unityRate;
            }
            catch { }

            return delaySamples / (double)Math.Max(1, sampleRate);
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "Play", typeof(ulong))]
        public static class AudioSource_Play_Delay
        {
            public static bool Prefix(AudioSource __instance, ulong delay)
            {
                if (!Initialized) return true;
                if (__instance.clip == null) return true;

                double time = ReadUnityDspTime() + DelaySamplesToSeconds(delay);
                return AudioSource_PlayScheduled.Prefix(__instance, ref time);
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "PlayDelayed", typeof(float))]
        public static class AudioSource_PlayDelayed
        {
            public static bool Prefix(AudioSource __instance, float delay)
            {
                if (!Initialized) return true;
                if (__instance.clip == null) return true;

                double time = ReadUnityDspTime() + Math.Max(0f, delay);
                return AudioSource_PlayScheduled.Prefix(__instance, ref time);
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
            public static bool Prefix(AudioSource __instance, float value)
            {
                if (!Initialized) return true;
                return !SetAudioSourceVolumeInternal(__instance, value);
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "volume", MethodType.Getter)]
        public static class AudioSource_volume_getter
        {
            public static bool Prefix(AudioSource __instance, ref float __result)
            {
                if (!Initialized) return true;
                if (!channels.ContainsKey(__instance.GetInstanceID())) return true;
                __result = GetAudioSourceVolume(__instance);
                return false;
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
            float sourceVolume = __instance.volume;

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
            SetPlaybackDelay(chnl, dspClock, length, pitch, freq);
            chnl.setLoopCount(0);
            chnl.setPitch(pitch);

            chnl.setVolume(sourceVolume * volumeScale * GetMixerScalar(__instance));
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
            public static bool Prefix(AudioSource __instance, ref double time)
            {
                if (!Initialized) return true;
                if (__instance.clip == null) return true;
                float sourceVolume = __instance.volume;
                if (!pa(__instance, __instance.clip))
                    return true;

                int id = __instance.GetInstanceID();
                volCache[id] = sourceVolume;
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

                        double fmodStartTime = ConvertUnityDspTimeToFmodTime(time);
                        var t = SecondsToDspClock(fmodStartTime);
                        SetPlaybackDelay(chnl, t, length, pitch, freq);
                        chnl.setLoopCount(__instance.loop ? -1 : 0);
                        chnl.setPitch(pitch);

                        SetChannelVolume(id, __instance, chnl, sourceVolume);
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
            public static bool Prefix(AudioSource __instance, ref double time)
            {
                if (!Initialized) return true;
                if (!TryGetChannel(__instance.GetInstanceID(), out _))
                    return true;

                SetScheduledEndTime(__instance, ConvertUnityDspTimeToFmodTime(time));
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "SetScheduledStartTime")]
        public static class AudioSource_SetScheduledStartTime
        {
            public static bool Prefix(AudioSource __instance, ref double time)
            {
                if (!Initialized) return true;
                if (!TryGetChannel(__instance.GetInstanceID(), out _))
                    return true;

                SetScheduledStartTime(__instance, ConvertUnityDspTimeToFmodTime(time));
                return false;
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
                bool handled = channels.ContainsKey(id) || playOneShotChannels.ContainsKey(id);
                if (!handled) return true;
                StopChannel(id, false);
                StopOneShotsForSource(id);
                positionCache.Remove(id);
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "Pause", new Type[] { })]
        public static class AudioSource_Pause
        {
            public static bool Prefix(AudioSource __instance)
            {
                if (!Initialized) return true;
                if (!TryGetChannel(__instance.GetInstanceID(), out _)) return true;
                Pause(__instance);
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "UnPause", new Type[] { })]
        public static class AudioSource_UnPause
        {
            public static bool Prefix(AudioSource __instance)
            {
                if (!Initialized) return true;
                if (!TryGetChannel(__instance.GetInstanceID(), out _)) return true;
                UnPause(__instance);
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "time", MethodType.Getter)]
        public static class AudioSource_time_getter
        {
            public static bool Prefix(AudioSource __instance, ref float __result)
            {
                if (!Initialized) return true;
                if (!TryGetChannel(__instance.GetInstanceID(), out _)) return true;
                __result = GetTime(__instance);
                return false;
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "time", MethodType.Setter)]
        public static class AudioSource_time_setter
        {
            public static bool Prefix(AudioSource __instance, float value)
            {
                if (!Initialized) return true;
                SetTime(__instance, value);
                return !channels.ContainsKey(__instance.GetInstanceID());
            }
        }

        [HarmonyPatchCategory(PatchCategory)]
        [HarmonyPatch(typeof(AudioSource), "isPlaying", MethodType.Getter)]
        public static class AudioSource_isPlaying
        {
            public static bool Prefix(AudioSource __instance, ref bool __result)
            {
                if (!Initialized) return true;
                if (!TryGetChannel(__instance.GetInstanceID(), out _)) return true;
                __result = IsPlaying(__instance);
                return false;
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
                n.sampleRate = fmodSampleRate;
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
            public static void Prefix(AudioManager __instance, string path, bool mp3Streaming, float length = 0f, bool stream = true)
            {
                if (!Initialized && Main.settings != null && Main.settings.FmodEnabled)
                    EnableRuntime(entry ?? Main.mod);
                if (!Initialized || __instance == null || __instance.audioLib == null || string.IsNullOrEmpty(path)) return;
                string cn = Path.GetFileName(path) + "*external";
                AudioClip existingClip;
                if (__instance.audioLib.TryGetValue(cn, out existingClip) && HasCachedFmodSound(existingClip))
                    return;

                if (externalLoadedKeys.Contains(cn) && !__instance.audioLib.ContainsKey(cn))
                    externalLoadedKeys.Remove(cn);

                if (!File.Exists(path))
                    return;

                int oldClipId;
                if (externalClipIds.TryGetValue(cn, out oldClipId))
                {
                    AudioClip oldClip;
                    if (__instance.audioLib.TryGetValue(cn, out oldClip) && oldClip && oldClip.GetInstanceID() == oldClipId)
                    {
                        __instance.audioLib.Remove(cn);
                        Object.Destroy(oldClip);
                    }

                    Sound oldSound;
                    if (cache.TryGetValue(oldClipId, out oldSound))
                    {
                        try { oldSound.release(); } catch { }
                        cache.Remove(oldClipId);
                    }
                    externalClipIds.Remove(cn);
                }

                MODE mode = (stream || mp3Streaming ? MODE.CREATESTREAM : MODE.CREATESAMPLE) | MODE.ACCURATETIME;
                Sound sound;
                RESULT result = fmodsys.createSound(path, mode, out sound);
                if (result != RESULT.OK)
                    result = fmodsys.createSound(path, stream || mp3Streaming ? MODE.CREATESTREAM : MODE.CREATESAMPLE, out sound);

                if (result == RESULT.OK && sound.hasHandle())
                {
                    uint lengthMs;
                    if (sound.getLength(out lengthMs, TIMEUNIT.MS) != RESULT.OK || lengthMs == 0)
                        lengthMs = (uint)Math.Max(1, length * 1000f);

                    sound.setMode(MODE.LOOP_NORMAL);
                    AudioClip fakeClip = CreateFakeAudioClip(cn, 1000, lengthMs / 1000f);
                    __instance.audioLib[cn] = fakeClip;
                    cache[fakeClip.GetInstanceID()] = sound;
                    externalLoadedKeys.Add(cn);
                    externalClipIds[cn] = fakeClip.GetInstanceID();
                    entry?.Logger?.Log("[FMOD] external sound loaded: " + cn + " (" + path + ")");
                }
                else
                {
                    entry?.Logger?.Error("[FMOD] external sound load failed: " + path + " (" + result + ")");
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
                __result = GetPlatformAudioOutputType();
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
