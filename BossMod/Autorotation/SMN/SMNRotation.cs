﻿namespace BossMod.SMN
{
    public static class Rotation
    {
        public enum Attunement { None, Ifrit, Titan, Garuda }

        // full state needed for determining next action
        public class State : CommonRotation.PlayerState
        {
            public bool PetSummoned;
            public bool IfritReady;
            public bool TitanReady;
            public bool GarudaReady;
            public Attunement Attunement;
            public int AttunementStacks;
            public float AttunementLeft;
            public float SummonLockLeft;
            public int AetherflowStacks; // 0-2
            public float SwiftcastLeft; // 0 if buff not up, max 10

            public State(float[] cooldowns) : base(cooldowns) { }

            public override string ToString()
            {
                return $"RB={RaidBuffsLeft:f1}, Att={Attunement}/{AttunementStacks}/{AttunementLeft:f1}, SummLock={SummonLockLeft:f1}, IfritR={IfritReady}, TitanR={TitanReady}, GarudaR={GarudaReady}, Aetherflow={AetherflowStacks}, PotCD={PotionCD:f1}, GCD={GCD:f3}, ALock={AnimationLock:f3}+{AnimationLockDelay:f3}, lvl={Level}";
            }
        }

        // strategy configuration
        public class Strategy : CommonRotation.Strategy
        {
        }

        // TODO: this is valid up to ~L30
        public static AID GetNextBestGCD(State state, Strategy strategy, bool aoe, bool moving)
        {
            // make sure pet is summoned
            if (!state.PetSummoned && state.Unlocked(MinLevel.SummonCarbuncle))
                return AID.SummonCarbuncle;

            if (state.AttunementLeft > state.GCD)
            {
                AID action;
                if (aoe && state.Unlocked(MinLevel.Outburst))
                {
                    action = state.Attunement switch
                    {
                        Attunement.Ifrit => moving ? AID.None : AID.RubyOutburst,
                        Attunement.Titan => AID.TopazOutburst,
                        Attunement.Garuda => AID.EmeraldOutburst,
                        _ => AID.None
                    };
                }
                else
                {
                    action = state.Attunement switch
                    {
                        Attunement.Ifrit => moving ? AID.None : state.Unlocked(MinLevel.Ruin2) ? AID.RubyRuin2 : AID.RubyRuin1,
                        Attunement.Titan => state.Unlocked(MinLevel.Ruin2) ? AID.TopazRuin2 : AID.TopazRuin1,
                        Attunement.Garuda => state.Unlocked(MinLevel.Ruin2) ? AID.EmeraldRuin2 : AID.EmeraldRuin1,
                        _ => AID.None
                    };
                }
                if (action != AID.None)
                    return action;
            }

            if (!strategy.Prepull && state.Unlocked(MinLevel.SummonRuby) && state.Attunement == Attunement.None && !state.IfritReady && !state.TitanReady && !state.GarudaReady && state.CD(CDGroup.Aethercharge) <= state.GCD)
                return AID.Aethercharge;

            if (state.SummonLockLeft <= state.GCD)
            {
                if (state.TitanReady && state.Unlocked(MinLevel.SummonTopaz))
                    return AID.SummonTopaz;
                if (state.IfritReady && state.Unlocked(MinLevel.SummonRuby))
                    return AID.SummonRuby;
                if (state.GarudaReady && state.Unlocked(MinLevel.SummonEmerald))
                    return AID.SummonEmerald;
            }

            if (moving)
                return AID.None;
            else if (aoe && state.Unlocked(MinLevel.Outburst))
                return AID.Outburst;
            else
                return state.Unlocked(MinLevel.Ruin2) ? AID.Ruin2 : AID.Ruin1;
        }

        public static ActionID GetNextBestOGCD(State state, Strategy strategy, float windowEnd, bool aoe)
        {
            // TODO: reconsider priorities, this kinda works at low level
            if (state.Unlocked(MinLevel.EnergyDrainFester) && state.AetherflowStacks == 0 && state.CanWeave(CDGroup.EnergyDrain, 0.6f, windowEnd))
                return ActionID.MakeSpell(AID.EnergyDrain);

            if (state.Unlocked(MinLevel.EnergyDrainFester) && state.AetherflowStacks > 0 && state.CanWeave(CDGroup.Fester, 0.6f, windowEnd))
                return ActionID.MakeSpell(AID.Fester);

            return new();
        }
    }
}
