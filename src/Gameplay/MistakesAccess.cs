namespace KorenResourcePack
{
    internal static class MistakesAccess
    {
        internal static scrMistakesManager Get()
        {
            scrController ctrl = scrController.instance;
            return ctrl != null ? ctrl.mistakesManager : null;
        }

        internal static float PercentAcc(scrMistakesManager m)
        {
            return m != null ? m.percentAcc : 1f;
        }

        internal static float PercentXAcc(scrMistakesManager m)
        {
            return m != null ? m.percentXAcc : 1f;
        }
    }
}
