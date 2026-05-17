using UnityEngine;

namespace KorenResourcePack
{
    // BPM helpers — colour gradient + value reader. Used by both the IMGUI Status block
    // (Status.cs) and the TMP overlay (Overlay.cs).
    internal static class Bpm
    {
        internal static Color LerpBpmColor(float bpm)
        {
            Color low = new Color(Main.settings.BpmColorLowR, Main.settings.BpmColorLowG, Main.settings.BpmColorLowB, Main.settings.BpmColorLowA);
            if (Main.settings.BpmColorMax <= 0f) return low;
            float t = Mathf.Clamp01(bpm / Main.settings.BpmColorMax);
            Color high = new Color(Main.settings.BpmColorHighR, Main.settings.BpmColorHighG, Main.settings.BpmColorHighB, Main.settings.BpmColorHighA);
            return Color.Lerp(low, high, t);
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

                tileBpm = (float)(conductor.bpm * conductor.song.pitch * controller.d_speed);
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
