using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static Color LerpBpmColor(float bpm)
        {
            Color low = new Color(settings.BpmColorLowR, settings.BpmColorLowG, settings.BpmColorLowB, settings.BpmColorLowA);
            if (settings.BpmColorMax <= 0f) return low;
            float t = Mathf.Clamp01(bpm / settings.BpmColorMax);
            Color high = new Color(settings.BpmColorHighR, settings.BpmColorHighG, settings.BpmColorHighB, settings.BpmColorHighA);
            return Color.Lerp(low, high, t);
        }

        private static void GetBpmValues(out float tileBpm, out float actualBpm)
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

                tileBpm = (float)(conductor.bpm * conductor.song.pitch * controller.speed);
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
