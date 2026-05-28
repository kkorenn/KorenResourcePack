using UnityEngine;

namespace KorenResourcePack
{
    
    internal static partial class Bpm
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

                tileBpm = (float)(conductor.bpm * conductor.song.pitch * GetControllerSpeed(controller));
                actualBpm = floor.nextfloor ? (float)(60.0 / (floor.nextfloor.entryTime - floor.entryTime) * conductor.song.pitch) : tileBpm;
            }
            catch
            {
                tileBpm = 0f;
                actualBpm = 0f;
            }
        }

        private static double GetControllerSpeed(scrController controller)
        {
            return controller != null && controller.planetarySystem != null ? controller.planetarySystem.speed : 1.0;
        }
    }
}
