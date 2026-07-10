using System;
using Casino.Core.Cards;

namespace Casino.Blackjack
{
    public sealed class BlackjackActionContext
    {
        private BlackjackActionContext(
            BlackjackHand hand,
            int wager,
            int availableChips,
            int previousSplitCount,
            bool isHandComplete,
            bool isInsurancePhase,
            Card? dealerUpCard,
            bool insuranceAlreadyTaken)
        {
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));

            if (wager <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wager), "Wager must be positive.");
            }

            if (availableChips < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableChips));
            }

            if (previousSplitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(previousSplitCount));
            }

            Hand = hand;
            Wager = wager;
            AvailableChips = availableChips;
            PreviousSplitCount = previousSplitCount;
            IsHandComplete = isHandComplete;
            IsInsurancePhase = isInsurancePhase;
            DealerUpCard = dealerUpCard;
            InsuranceAlreadyTaken = insuranceAlreadyTaken;
        }

        public BlackjackHand Hand { get; }

        public int Wager { get; }

        public int AvailableChips { get; }

        public int PreviousSplitCount { get; }

        public bool IsHandComplete { get; }

        public bool IsInsurancePhase { get; }

        public Card? DealerUpCard { get; }

        public bool InsuranceAlreadyTaken { get; }

        public static BlackjackActionContext ForPlayerTurn(
            BlackjackHand hand,
            int wager,
            int availableChips,
            int previousSplitCount = 0,
            bool isHandComplete = false)
        {
            return new BlackjackActionContext(
                hand,
                wager,
                availableChips,
                previousSplitCount,
                isHandComplete,
                false,
                null,
                false);
        }

        public static BlackjackActionContext ForInsurance(
            BlackjackHand hand,
            int wager,
            int availableChips,
            Card dealerUpCard,
            bool insuranceAlreadyTaken = false)
        {
            return new BlackjackActionContext(
                hand,
                wager,
                availableChips,
                0,
                false,
                true,
                dealerUpCard,
                insuranceAlreadyTaken);
        }
    }

    public sealed class LegalBlackjackActions
    {
        public LegalBlackjackActions(
            bool canHit,
            bool canStand,
            bool canDoubleDown,
            bool canSplit,
            bool canBuyInsurance,
            int insuranceWager)
        {
            CanHit = canHit;
            CanStand = canStand;
            CanDoubleDown = canDoubleDown;
            CanSplit = canSplit;
            CanBuyInsurance = canBuyInsurance;
            InsuranceWager = insuranceWager;
        }

        public bool CanHit { get; }

        public bool CanStand { get; }

        public bool CanDoubleDown { get; }

        public bool CanSplit { get; }

        public bool CanBuyInsurance { get; }

        public int InsuranceWager { get; }
    }

    public static class BlackjackActionValidator
    {
        public static LegalBlackjackActions GetLegalActions(BlackjackActionContext context, BlackjackRules rules)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var evaluation = BlackjackHandEvaluator.Evaluate(context.Hand);
            var canTakeHandAction = !context.IsHandComplete
                && !context.Hand.WasSplitFromAces
                && !evaluation.IsBust
                && !evaluation.IsTwentyOne;

            var canDouble = canTakeHandAction
                && rules.AllowDoubleDown
                && context.Hand.Count == 2
                && context.AvailableChips >= context.Wager
                && (!context.Hand.IsFromSplit || rules.AllowDoubleAfterSplit);

            var canSplit = canTakeHandAction
                && rules.AllowSplit
                && context.Hand.Count == 2
                && context.PreviousSplitCount < rules.MaxSplitCount
                && context.AvailableChips >= context.Wager
                && CardValues.HaveSameBlackjackSplitValue(context.Hand.Cards[0], context.Hand.Cards[1]);

            var insuranceWager = 0;
            var canBuyInsurance = false;
            if (context.IsInsurancePhase
                && !context.InsuranceAlreadyTaken
                && context.DealerUpCard.HasValue
                && context.DealerUpCard.Value.Rank == CardRank.Ace
                && rules.InsuranceStakeRatio.TryApplyTo(context.Wager, out insuranceWager))
            {
                canBuyInsurance = context.AvailableChips >= insuranceWager;
            }

            return new LegalBlackjackActions(
                canTakeHandAction,
                canTakeHandAction,
                canDouble,
                canSplit,
                canBuyInsurance,
                insuranceWager);
        }
    }
}
