using System;

namespace Casino.Blackjack
{
    public sealed class BlackjackRules
    {
        public static readonly BlackjackRules Standard = new BlackjackRules(
            deckCount: 6,
            shuffleThreshold: 0.25m,
            dealerHitsSoft17: false,
            blackjackPayout: new ChipRatio(3, 2),
            standardWinPayout: new ChipRatio(1, 1),
            insurancePayout: new ChipRatio(2, 1),
            insuranceStakeRatio: new ChipRatio(1, 2),
            allowDoubleDown: true,
            allowSplit: true,
            allowDoubleAfterSplit: true,
            maxSplitCount: 1);

        public BlackjackRules(
            int deckCount,
            decimal shuffleThreshold,
            bool dealerHitsSoft17,
            ChipRatio blackjackPayout,
            ChipRatio standardWinPayout,
            ChipRatio insurancePayout,
            ChipRatio insuranceStakeRatio,
            bool allowDoubleDown,
            bool allowSplit,
            bool allowDoubleAfterSplit,
            int maxSplitCount)
        {
            if (deckCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deckCount));
            }

            if (shuffleThreshold < 0m || shuffleThreshold > 1m)
            {
                throw new ArgumentOutOfRangeException(nameof(shuffleThreshold));
            }

            if (maxSplitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSplitCount));
            }

            DeckCount = deckCount;
            ShuffleThreshold = shuffleThreshold;
            DealerHitsSoft17 = dealerHitsSoft17;
            BlackjackPayout = blackjackPayout;
            StandardWinPayout = standardWinPayout;
            InsurancePayout = insurancePayout;
            InsuranceStakeRatio = insuranceStakeRatio;
            AllowDoubleDown = allowDoubleDown;
            AllowSplit = allowSplit;
            AllowDoubleAfterSplit = allowDoubleAfterSplit;
            MaxSplitCount = maxSplitCount;
        }

        public int DeckCount { get; }

        public decimal ShuffleThreshold { get; }

        public bool DealerHitsSoft17 { get; }

        public ChipRatio BlackjackPayout { get; }

        public ChipRatio StandardWinPayout { get; }

        public ChipRatio InsurancePayout { get; }

        public ChipRatio InsuranceStakeRatio { get; }

        public bool AllowDoubleDown { get; }

        public bool AllowSplit { get; }

        public bool AllowDoubleAfterSplit { get; }

        public int MaxSplitCount { get; }
    }

    public readonly struct ChipRatio
    {
        public ChipRatio(int numerator, int denominator)
        {
            if (numerator < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numerator));
            }

            if (denominator <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            Numerator = numerator;
            Denominator = denominator;
        }

        public int Numerator { get; }

        public int Denominator { get; }

        public int ApplyTo(int chips)
        {
            if (TryApplyTo(chips, out var result))
            {
                return result;
            }

            throw new InvalidOperationException("Chip ratio cannot be represented as whole chip units.");
        }

        public bool TryApplyTo(int chips, out int result)
        {
            if (chips < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chips));
            }

            var product = (long)chips * Numerator;
            if (product % Denominator != 0)
            {
                result = 0;
                return false;
            }

            result = checked((int)(product / Denominator));
            return true;
        }
    }
}
