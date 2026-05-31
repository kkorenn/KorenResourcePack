namespace KorenResourcePack
{

    internal static partial class JudgementRestriction
    {
        internal static void OnAddHitPostfix(HitMargin hit) => AfterAddHit(hit);


        private static int missCount;
        private static int overloadCount;
        private static bool failTriggered;

        internal static void ResetCounters()
        {
            missCount = 0;
            overloadCount = 0;
            failTriggered = false;
        }

        // Force a real death, overriding No Fail. We call scrPlayer.Die with hitbox:true
        // rather than scrController.FailAction directly: FailAction is only step one of the
        // game's death (state -> Fail, stop song). The part that explodes the planet and
        // schedules Fail2Action (the actual death/restart screen) lives in scrPlayer.Die via
        // planetarySystem.Die(anim, Fail2Action). Calling FailAction alone left the player
        // hovering in States.Fail with the planet still orbiting and nothing ever killing it.
        // hitbox:true also skips Die's No Fail / auto / debug guards and the no-fail branch's
        // inner Hit() call, so it's safe to invoke from inside the AddHit postfix.
        private static void TriggerFail(string reason)
        {
            try
            {
                scrController c = scrController.instance;
                if (c == null || failTriggered) return;
                scrPlayer p = c.playerOne;
                if (p == null) return;
                failTriggered = true;
                p.DieByHitbox(reason ?? "");
            }
            catch { }
        }

        private static bool ShouldFailFor(HitMargin margin)
        {
            int marginInt = (int)margin;
            switch (Main.settings.JRestrictMode)
            {
                case 1: 
                    return marginInt != (int)HitMargin.Perfect;
                case 2: 
                {
                    
                    if (marginInt != (int)HitMargin.Perfect) return true;
                    
                    XPerfectBridge.Judge xj = XPerfectBridge.LastJudge();
                    if (xj == XPerfectBridge.Judge.None) return false; 
                    return xj != XPerfectBridge.Judge.X;
                }
                case 4: 
                    return margin == HitMargin.TooEarly;
                case 3: 
                {
                    int mask = Main.settings.JRestrictAllowedMask;
                    if (mask == 0) return false; 
                    int bit = 1 << marginInt;
                    return (mask & bit) == 0;
                }
                case 0:
                default: 
                {
                    try
                    {
                        scrMistakesManager m = MistakesAccess.Get();
                        if (m == null) return false;
                        float acc = MistakesAccess.PercentAcc(m);
                        if (float.IsNaN(acc) || float.IsInfinity(acc)) return false;
                        return acc * 100f < Main.settings.JRestrictAccuracy;
                    }
                    catch { return false; }
                }
            }
        }

        private static void AfterAddHit(HitMargin hit)
        {
            if (!Main.modEnabled || Main.settings == null) return;
            if (hit == HitMargin.Auto) return;

            bool jrOn = Main.settings.JRestrictOn;
            bool dlOn = Main.settings.DeathLimitOn;
            if (!jrOn && !dlOn) return;

            if (hit == HitMargin.FailMiss) missCount++;
            else if (hit == HitMargin.FailOverload) overloadCount++;

            if (jrOn && ShouldFailFor(hit))
            {
                TriggerFail("Broke the judgement restriction!!");
                return;
            }

            if (dlOn)
            {
                int deaths = missCount + overloadCount;
                if (Main.settings.DeathLimitMaxDeathsOn && deaths > Main.settings.DeathLimitMaxDeaths)
                {
                    TriggerFail("Exceeded death limit!!");
                    return;
                }
                if (Main.settings.DeathLimitMaxMissesOn && missCount > Main.settings.DeathLimitMaxMisses)
                {
                    TriggerFail("Exceeded miss limit!!");
                    return;
                }
                if (Main.settings.DeathLimitMaxOverloadsOn && overloadCount > Main.settings.DeathLimitMaxOverloads)
                {
                    TriggerFail("Exceeded overload limit!!");
                    return;
                }
            }
        }

    }
}
