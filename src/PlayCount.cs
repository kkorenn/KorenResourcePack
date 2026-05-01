using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ADOFAI;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static Dictionary<PlayCountHash, PlayData> playDatas;
        private static PlayCountHash lastMapHash;
        private static float startProgress;
        private static float savedStartProgress = -1f;
        private static float lastMultiplier = 1f;
        private static bool autoOnceEnabled;
        private static float curBest = -1f;

        private static string PlayCountFilePath
        {
            get { return Path.Combine(mod.Path, "Plays.dat"); }
        }

        private static void LoadPlayCount()
        {
            playDatas = new Dictionary<PlayCountHash, PlayData>();
            string path = PlayCountFilePath;
            if (File.Exists(path))
            {
                try { LoadPlayCountFile(path); return; }
                catch (Exception e)
                {
                    mod.Logger.Log("[Warning] PlayCount load failed: " + e.Message);
                    playDatas.Clear();
                }
            }
            string bak = path + ".bak";
            if (File.Exists(bak))
            {
                try { LoadPlayCountFile(bak); }
                catch (Exception e)
                {
                    mod.Logger.Log("[Warning] PlayCount backup load failed: " + e.Message);
                }
            }
        }

        private static void LoadPlayCountFile(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int version = br.ReadByte();
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    byte[] hashBytes = br.ReadBytes(16);
                    PlayCountHash key = new PlayCountHash(hashBytes);
                    PlayData data = new PlayData();
                    data.Read(br, version);
                    playDatas[key] = data;
                }
            }
        }

        private static void SavePlayCount()
        {
            try
            {
                string path = PlayCountFilePath;
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                using (FileStream fs = File.Create(path))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write((byte)1);
                    bw.Write(playDatas.Count);
                    foreach (var pair in playDatas)
                    {
                        if (pair.Value == null) continue;
                        bw.Write(pair.Key.data, 0, pair.Key.data.Length);
                        pair.Value.Write(bw);
                    }
                }
            }
            catch (Exception e)
            {
                mod?.Logger?.Log("[Warning] PlayCount save failed: " + e.Message);
            }
        }

        private static void DisposePlayCount()
        {
            playDatas = null;
        }

        private static PlayData GetPlayData(PlayCountHash hash)
        {
            PlayData data;
            if (!playDatas.TryGetValue(hash, out data))
            {
                data = new PlayData();
                playDatas[hash] = data;
            }
            return data;
        }

        private static float GetCurrentMultiplier()
        {
            try { return (float)(ADOBase.conductor.song.pitch * ADOBase.controller.speed); }
            catch { return 1f; }
        }

        private static PlayCountHash GetMapHash()
        {
            using (MD5 md5 = MD5.Create())
            {
                try
                {
                    if (ADOBase.isOfficialLevel)
                        return new PlayCountHash(md5.ComputeHash(Encoding.UTF8.GetBytes(ADOBase.currentLevel ?? "")));
                }
                catch { }
                return new PlayCountHash(md5.ComputeHash(GetMapHashBytes()));
            }
        }

        private static byte[] GetMapHashBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                scrLevelMaker lm = scrLevelMaker.instance;
                if (lm.isOldLevel)
                {
                    bw.Write(lm.leveldata ?? "");
                }
                else
                {
                    var anglesArr = System.Linq.Enumerable.ToArray(lm.floorAngles);
                    bw.Write(anglesArr.Length);
                    for (int ai = 0; ai < anglesArr.Length; ai++) bw.Write((float)anglesArr[ai]);
                }

                try
                {
                    foreach (var levelEvent in ADOBase.customLevel.events)
                    {
                        switch (levelEvent.eventType)
                        {
                            case LevelEventType.SetSpeed:
                                bw.Write(levelEvent.floor);
                                bw.Write((byte)0);
                                var speedType = levelEvent.Get<SpeedType>("speedType");
                                bw.Write((byte)speedType);
                                bw.Write((float)levelEvent[speedType == SpeedType.Bpm ? "beatsPerMinute" : "bpmMultiplier"]);
                                break;
                            case LevelEventType.Twirl:
                                bw.Write(levelEvent.floor);
                                bw.Write((byte)1);
                                break;
                            case LevelEventType.Hold:
                                bw.Write(levelEvent.floor);
                                bw.Write((byte)2);
                                bw.Write((int)levelEvent["duration"]);
                                break;
                            case LevelEventType.MultiPlanet:
                                bw.Write(levelEvent.floor);
                                bw.Write((byte)3);
                                bw.Write((byte)levelEvent.Get<PlanetCount>("planets"));
                                break;
                            case LevelEventType.Pause:
                                bw.Write(levelEvent.floor);
                                bw.Write((byte)4);
                                bw.Write((float)levelEvent["duration"]);
                                break;
                            case LevelEventType.AutoPlayTiles:
                                bw.Write(levelEvent.floor);
                                bw.Write((byte)5);
                                bw.Write((bool)levelEvent["enabled"]);
                                break;
                            case LevelEventType.ScaleMargin:
                                bw.Write(levelEvent.floor);
                                bw.Write((byte)6);
                                bw.Write((float)levelEvent["scale"]);
                                break;
                            case LevelEventType.Multitap:
                                bw.Write(levelEvent.floor);
                                bw.Write((byte)7);
                                bw.Write((float)levelEvent["taps"]);
                                break;
                            case LevelEventType.KillPlayer:
                                bw.Write(levelEvent.floor);
                                bw.Write((byte)8);
                                break;
                        }
                    }
                }
                catch { }

                return ms.ToArray();
            }
        }

        private static void OnRunShow()
        {
            try
            {
                bool wasVisible = runVisible;
                autoOnceEnabled = false;
                try { autoOnceEnabled = RDC.auto || ADOBase.controller.noFail; } catch { }

                scrController ctrl = scrController.instance;
                if (ctrl == null) return;

                if (!wasVisible || (ADOBase.isScnGame && scrController.checkpointsUsed == 0))
                {
                    startProgress = ctrl.percentComplete;
                    curBest = -1f;
                }

                lastMapHash = GetMapHash();
                savedStartProgress = startProgress;
                lastMultiplier = GetCurrentMultiplier();

                if (!autoOnceEnabled && playDatas != null)
                {
                    GetPlayData(lastMapHash).AddAttempts(startProgress, lastMultiplier);
                }
            }
            catch (Exception e)
            {
                mod?.Logger?.Log("[Warning] OnRunShow failed: " + e.Message);
            }
        }

        private static void OnRunHide()
        {
            try
            {
                if (playDatas == null) return;

                if (savedStartProgress != -1f && !autoOnceEnabled)
                {
                    float progress = 0f;
                    try { progress = scrController.instance.percentComplete; } catch { }
                    GetPlayData(lastMapHash).SetBest(savedStartProgress, progress, lastMultiplier);
                }

                float curProgress = 0f;
                try { curProgress = scrController.instance.percentComplete; } catch { }
                if (curProgress == startProgress && !autoOnceEnabled && savedStartProgress != -1f)
                {
                    GetPlayData(lastMapHash).RemoveAttempts(startProgress, lastMultiplier);
                }

                savedStartProgress = -1f;
            }
            catch (Exception e)
            {
                mod?.Logger?.Log("[Warning] OnRunHide failed: " + e.Message);
            }
        }

        private static void OnRunDeath()
        {
            try
            {
                if (autoOnceEnabled || savedStartProgress == -1f || playDatas == null) return;
                float progress = 0f;
                try { progress = scrController.instance.percentComplete; } catch { }
                GetPlayData(lastMapHash).SetBest(savedStartProgress, progress, lastMultiplier);
                savedStartProgress = -1f;
                curBest = progress;
            }
            catch { }
        }

        public struct PlayCountHash : IEquatable<PlayCountHash>
        {
            public readonly byte[] data;

            public PlayCountHash(byte[] data) { this.data = data; }

            public override bool Equals(object obj)
            {
                if (obj is PlayCountHash h) return Equals(h);
                return false;
            }

            public bool Equals(PlayCountHash other)
            {
                if (data == null || other.data == null) return data == other.data;
                if (data.Length != other.data.Length) return false;
                for (int i = 0; i < data.Length; i++)
                    if (data[i] != other.data[i]) return false;
                return true;
            }

            public override int GetHashCode()
            {
                if (data == null) return 0;
                int hash = 17;
                foreach (byte b in data) hash = hash * 31 + b;
                return hash;
            }

            public static bool operator ==(PlayCountHash left, PlayCountHash right) { return left.Equals(right); }
            public static bool operator !=(PlayCountHash left, PlayCountHash right) { return !left.Equals(right); }
        }

        public class PlayData
        {
            public int totalAttempts;
            public Dictionary<long, int> attempts = new Dictionary<long, int>();
            public Dictionary<long, float> best = new Dictionary<long, float>();

            private static long MakeKey(float a, float b)
            {
                return ((long)BitConverter.ToInt32(BitConverter.GetBytes(a), 0) << 32)
                     | (long)(uint)BitConverter.ToInt32(BitConverter.GetBytes(b), 0);
            }

            private static void SplitKey(long key, out float a, out float b)
            {
                a = BitConverter.ToSingle(BitConverter.GetBytes((int)(key >> 32)), 0);
                b = BitConverter.ToSingle(BitConverter.GetBytes((int)(key & 0xFFFFFFFF)), 0);
            }

            public void AddAttempts(float progress, float multiplier)
            {
                long key = MakeKey(progress, multiplier);
                int val;
                if (attempts.TryGetValue(key, out val))
                    attempts[key] = val + 1;
                else
                    attempts[key] = 1;
                totalAttempts++;
                SavePlayCount();
            }

            public void RemoveAttempts(float progress, float multiplier)
            {
                long key = MakeKey(progress, multiplier);
                int val;
                if (!attempts.TryGetValue(key, out val)) return;
                if (val == 1) attempts.Remove(key);
                else attempts[key] = val - 1;
                totalAttempts--;
                SavePlayCount();
            }

            public void SetBest(float start, float cur, float multiplier)
            {
                long key = MakeKey(start, multiplier);
                float existing;
                if (!best.TryGetValue(key, out existing))
                {
                    best[key] = cur;
                    SavePlayCount();
                }
                else if (existing < cur)
                {
                    best[key] = cur;
                    SavePlayCount();
                }
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(totalAttempts);
                bw.Write(attempts.Count);
                foreach (var pair in attempts)
                {
                    float a, b;
                    SplitKey(pair.Key, out a, out b);
                    bw.Write(a);
                    bw.Write(b);
                    bw.Write(pair.Value);
                }
                bw.Write(best.Count);
                foreach (var pair in best)
                {
                    float a, b;
                    SplitKey(pair.Key, out a, out b);
                    bw.Write(a);
                    bw.Write(b);
                    bw.Write(pair.Value);
                }
            }

            public void Read(BinaryReader br, int version)
            {
                totalAttempts = br.ReadInt32();
                int size = br.ReadInt32();
                for (int i = 0; i < size; i++)
                {
                    if (version == 0) br.ReadByte();
                    float p = br.ReadSingle();
                    float m = br.ReadSingle();
                    attempts[MakeKey(p, m)] = br.ReadInt32();
                }
                size = br.ReadInt32();
                for (int i = 0; i < size; i++)
                {
                    if (version == 0) br.ReadByte();
                    float p = br.ReadSingle();
                    float m = br.ReadSingle();
                    best[MakeKey(p, m)] = br.ReadSingle();
                }
            }

            public float GetBest(float start, float multiplier)
            {
                float val;
                return best.TryGetValue(MakeKey(start, multiplier), out val) ? val : 0f;
            }

            public int GetAttempts(float progress, float multiplier)
            {
                int val;
                return attempts.TryGetValue(MakeKey(progress, multiplier), out val) ? val : 0;
            }

            public int GetAllAttempts()
            {
                int total = 0;
                foreach (var v in attempts.Values) total += v;
                return total;
            }
        }
    }
}
