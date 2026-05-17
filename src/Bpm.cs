using UnityEngine;

namespace KorenResourcePack
{
    // BPM helpers — colour gradient + value reader. Used by both the IMGUI Status block
    // (Status.cs) and the TMP overlay (Overlay.cs).
    internal static class Bpm
    {
        internal static Color LerpBpmColor(float bpm)
        {
            if (Main.settings == null) return Color.white;
            Main.settings.EnsureColorRanges();
            float t = Main.settings.BpmColorMax <= 0f ? 0f : bpm / Main.settings.BpmColorMax;
            return Main.settings.BpmColor.GetColor(t);
        }

        internal static void GetBpmValues(out float tileBpm, out float actualBpm)
        {
            tileBpm = 0f;
            actualBpm = 0f;

            try
            {
                scrController controller = scrController.instance;
                scrConductor conductor = scrConductor.instance;
                scrFloor floor = controller != null ? (controller.currFloor ?? controller.firstFloor) : null;

                if (controller == null || conductor == null || floor == null || conductor.song == null)
                {
                    return;
                }

#if LEGACY
                tileBpm = (float)(conductor.bpm * conductor.song.pitch * controller.speed);
#else
                tileBpm = (float)(conductor.bpm * conductor.song.pitch * (controller.planetarySystem != null ? controller.planetarySystem.speed : 1.0));
#endif
                actualBpm = floor.nextfloor ? (float)(60.0 / (floor.nextfloor.entryTime - floor.entryTime) * conductor.song.pitch) : tileBpm;
            }
            catch
            {
                tileBpm = 0f;
                actualBpm = 0f;
            }
        }
    }
}
