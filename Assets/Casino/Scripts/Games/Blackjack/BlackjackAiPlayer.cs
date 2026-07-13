using System;
using Casino.Core.Cards;
using Casino.Core.Identifiers;

namespace Casino.Blackjack
{
    public enum BlackjackAiPlayerAction
    {
        Hit,
        Stand,
        DoubleDown,
        Split,
        BuyInsurance,
        DeclineInsurance
    }

    public readonly struct BlackjackAiDecision
    {
        public BlackjackAiDecision(BlackjackAiPlayerAction action)
        {
            if (!Enum.IsDefined(typeof(BlackjackAiPlayerAction), action))
            {
                throw new ArgumentOutOfRangeException(nameof(action));
            }

            Action = action;
        }

        public BlackjackAiPlayerAction Action { get; }

        public static BlackjackAiDecision Hit()
        {
            return new BlackjackAiDecision(BlackjackAiPlayerAction.Hit);
        }

        public static BlackjackAiDecision Stand()
        {
            return new BlackjackAiDecision(BlackjackAiPlayerAction.Stand);
        }

        public static BlackjackAiDecision DoubleDown()
        {
            return new BlackjackAiDecision(BlackjackAiPlayerAction.DoubleDown);
        }

        public static BlackjackAiDecision Split()
        {
            return new BlackjackAiDecision(BlackjackAiPlayerAction.Split);
        }

        public static BlackjackAiDecision BuyInsurance()
        {
            return new BlackjackAiDecision(BlackjackAiPlayerAction.BuyInsurance);
        }

        public static BlackjackAiDecision DeclineInsurance()
        {
            return new BlackjackAiDecision(BlackjackAiPlayerAction.DeclineInsurance);
        }
    }

    public sealed class BlackjackAiDecisionContext
    {
        private BlackjackAiDecisionContext(
            BlackjackRoundPhase phase,
            BlackjackHand hand,
            Card dealerUpCard,
            LegalBlackjackActions legalActions,
            int wager,
            int availableChips)
        {
            if (phase != BlackjackRoundPhase.InsuranceOrDealerPeek && phase != BlackjackRoundPhase.PlayerTurns)
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            if (wager <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wager));
            }

            if (availableChips < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableChips));
            }

            Phase = phase;
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));
            DealerUpCard = dealerUpCard;
            LegalActions = legalActions ?? throw new ArgumentNullException(nameof(legalActions));
            Wager = wager;
            AvailableChips = availableChips;
        }

        public BlackjackRoundPhase Phase { get; }

        public BlackjackHand Hand { get; }

        public Card DealerUpCard { get; }

        public LegalBlackjackActions LegalActions { get; }

        public int Wager { get; }

        public int AvailableChips { get; }

        public static BlackjackAiDecisionContext ForInsurance(
            BlackjackHand hand,
            Card dealerUpCard,
            LegalBlackjackActions legalActions,
            int wager,
            int availableChips)
        {
            return new BlackjackAiDecisionContext(
                BlackjackRoundPhase.InsuranceOrDealerPeek,
                hand,
                dealerUpCard,
                legalActions,
                wager,
                availableChips);
        }

        public static BlackjackAiDecisionContext ForPlayerTurn(
            BlackjackHand hand,
            Card dealerUpCard,
            LegalBlackjackActions legalActions,
            int wager,
            int availableChips)
        {
            return new BlackjackAiDecisionContext(
                BlackjackRoundPhase.PlayerTurns,
                hand,
                dealerUpCard,
                legalActions,
                wager,
                availableChips);
        }
    }

    public interface IBlackjackAiStrategy
    {
        int ChooseOpeningWager(int availableChips, int minimumWager);

        BlackjackAiDecision ChooseInsurance(BlackjackAiDecisionContext context);

        BlackjackAiDecision ChoosePlayerAction(BlackjackAiDecisionContext context);
    }

    public sealed class BasicBlackjackAiStrategy : IBlackjackAiStrategy
    {
        public int ChooseOpeningWager(int availableChips, int minimumWager)
        {
            if (availableChips < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableChips));
            }

            if (minimumWager <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumWager));
            }

            return availableChips >= minimumWager ? minimumWager : 0;
        }

        public BlackjackAiDecision ChooseInsurance(BlackjackAiDecisionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Phase != BlackjackRoundPhase.InsuranceOrDealerPeek)
            {
                throw new ArgumentException("Insurance decisions require an insurance context.", nameof(context));
            }

            return BlackjackAiDecision.DeclineInsurance();
        }

        public BlackjackAiDecision ChoosePlayerAction(BlackjackAiDecisionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Phase != BlackjackRoundPhase.PlayerTurns)
            {
                throw new ArgumentException("Player action decisions require a player-turn context.", nameof(context));
            }

            var legal = context.LegalActions;
            if (legal.CanSplit && ShouldSplit(context.Hand))
            {
                return BlackjackAiDecision.Split();
            }

            var evaluation = context.Hand.Evaluation;
            var dealerValue = context.DealerUpCard.BlackjackValue;

            if (legal.CanDoubleDown && ShouldDoubleDown(evaluation, dealerValue))
            {
                return BlackjackAiDecision.DoubleDown();
            }

            if (legal.CanStand && ShouldStand(evaluation, dealerValue))
            {
                return BlackjackAiDecision.Stand();
            }

            if (legal.CanHit)
            {
                return BlackjackAiDecision.Hit();
            }

            if (legal.CanStand)
            {
                return BlackjackAiDecision.Stand();
            }

            throw new InvalidOperationException("No legal blackjack player action is available.");
        }

        private static bool ShouldSplit(BlackjackHand hand)
        {
            if (hand.Count != 2)
            {
                return false;
            }

            var first = hand.Cards[0].Rank;
            var second = hand.Cards[1].Rank;
            if (first != second)
            {
                return false;
            }

            return first == CardRank.Ace || first == CardRank.Eight;
        }

        private static bool ShouldDoubleDown(BlackjackHandEvaluation evaluation, int dealerValue)
        {
            if (evaluation.IsSoft)
            {
                return evaluation.Total == 18 && dealerValue >= 3 && dealerValue <= 6;
            }

            if (evaluation.Total == 11)
            {
                return true;
            }

            if (evaluation.Total == 10)
            {
                return dealerValue >= 2 && dealerValue <= 9;
            }

            return evaluation.Total == 9 && dealerValue >= 3 && dealerValue <= 6;
        }

        private static bool ShouldStand(BlackjackHandEvaluation evaluation, int dealerValue)
        {
            if (evaluation.IsSoft)
            {
                return evaluation.Total >= 19 || (evaluation.Total == 18 && dealerValue >= 2 && dealerValue <= 8);
            }

            if (evaluation.Total >= 17)
            {
                return true;
            }

            if (evaluation.Total >= 13)
            {
                return dealerValue >= 2 && dealerValue <= 6;
            }

            return evaluation.Total == 12 && dealerValue >= 4 && dealerValue <= 6;
        }
    }

    public sealed class BlackjackAiRoundDriver
    {
        private readonly IBlackjackAiStrategy strategy;

        public BlackjackAiRoundDriver(IBlackjackAiStrategy strategy)
        {
            this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public int PlayToIntermission(
            BlackjackLocalRound round,
            int minimumWager,
            string actionIdPrefix,
            int maxSteps = 200)
        {
            if (round == null)
            {
                throw new ArgumentNullException(nameof(round));
            }

            if (minimumWager <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumWager));
            }

            if (string.IsNullOrWhiteSpace(actionIdPrefix))
            {
                throw new ArgumentException("Action id prefix cannot be empty.", nameof(actionIdPrefix));
            }

            if (maxSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSteps));
            }

            var steps = 0;
            var actionIndex = 0;
            while (round.Phase != BlackjackRoundPhase.Intermission)
            {
                if (steps >= maxSteps)
                {
                    throw new InvalidOperationException("AI round driver exceeded the maximum step count.");
                }

                switch (round.Phase)
                {
                    case BlackjackRoundPhase.Betting:
                        PlaceOpeningBet(round, minimumWager, NextActionId(actionIdPrefix, ref actionIndex));
                        break;
                    case BlackjackRoundPhase.InitialDeal:
                    case BlackjackRoundPhase.DealerTurn:
                    case BlackjackRoundPhase.Settlement:
                        RequireAdvanced(round.AdvanceAutomatic(), round.Phase);
                        break;
                    case BlackjackRoundPhase.InsuranceOrDealerPeek:
                        ResolveInsuranceOrPeek(round, NextActionId(actionIdPrefix, ref actionIndex));
                        break;
                    case BlackjackRoundPhase.PlayerTurns:
                        ResolvePlayerTurn(round, NextActionId(actionIdPrefix, ref actionIndex));
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported blackjack phase: " + round.Phase);
                }

                steps++;
            }

            return steps;
        }

        private void PlaceOpeningBet(BlackjackLocalRound round, int minimumWager, ActionId actionId)
        {
            var wager = strategy.ChooseOpeningWager(round.Balance, minimumWager);
            if (wager <= 0)
            {
                throw new InvalidOperationException("AI strategy did not choose a positive opening wager.");
            }

            RequireApplied(round.PlaceBet(actionId, wager), "place opening bet");
        }

        private void ResolveInsuranceOrPeek(BlackjackLocalRound round, ActionId actionId)
        {
            if (round.DealerUpCard.Rank != CardRank.Ace)
            {
                RequireAdvanced(round.AdvanceAutomatic(), round.Phase);
                return;
            }

            var firstHand = round.PlayerHands[0];
            var legal = round.GetLegalActionsForInsurance();
            var context = BlackjackAiDecisionContext.ForInsurance(
                firstHand.Hand,
                round.DealerUpCard,
                legal,
                firstHand.Wager,
                round.Balance);

            var decision = strategy.ChooseInsurance(context);
            var result = decision.Action == BlackjackAiPlayerAction.BuyInsurance && legal.CanBuyInsurance
                ? round.BuyInsurance(actionId)
                : round.DeclineInsurance(actionId);

            RequireApplied(result, "resolve insurance");
            RequireAdvanced(round.AdvanceAutomatic(), round.Phase);
        }

        private void ResolvePlayerTurn(BlackjackLocalRound round, ActionId actionId)
        {
            var activeHand = round.ActiveHand;
            var legal = round.GetLegalActionsForActiveHand();
            var context = BlackjackAiDecisionContext.ForPlayerTurn(
                activeHand.Hand,
                round.DealerUpCard,
                legal,
                activeHand.Wager,
                round.Balance);

            var decision = strategy.ChoosePlayerAction(context);
            var action = EnsureLegalPlayerAction(decision.Action, legal);
            BlackjackCommandResult result;
            switch (action)
            {
                case BlackjackAiPlayerAction.Hit:
                    result = round.Hit(actionId);
                    break;
                case BlackjackAiPlayerAction.Stand:
                    result = round.Stand(actionId);
                    break;
                case BlackjackAiPlayerAction.DoubleDown:
                    result = round.DoubleDown(actionId);
                    break;
                case BlackjackAiPlayerAction.Split:
                    result = round.Split(actionId);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported player action: " + action);
            }

            RequireApplied(result, "apply AI player action");
        }

        private static BlackjackAiPlayerAction EnsureLegalPlayerAction(
            BlackjackAiPlayerAction requested,
            LegalBlackjackActions legal)
        {
            switch (requested)
            {
                case BlackjackAiPlayerAction.Hit:
                    if (legal.CanHit)
                    {
                        return requested;
                    }

                    break;
                case BlackjackAiPlayerAction.Stand:
                    if (legal.CanStand)
                    {
                        return requested;
                    }

                    break;
                case BlackjackAiPlayerAction.DoubleDown:
                    if (legal.CanDoubleDown)
                    {
                        return requested;
                    }

                    break;
                case BlackjackAiPlayerAction.Split:
                    if (legal.CanSplit)
                    {
                        return requested;
                    }

                    break;
            }

            if (legal.CanStand)
            {
                return BlackjackAiPlayerAction.Stand;
            }

            if (legal.CanHit)
            {
                return BlackjackAiPlayerAction.Hit;
            }

            throw new InvalidOperationException("No legal blackjack player action is available.");
        }

        private static ActionId NextActionId(string actionIdPrefix, ref int actionIndex)
        {
            var id = new ActionId(actionIdPrefix + ":ai:" + actionIndex);
            actionIndex++;
            return id;
        }

        private static void RequireApplied(BlackjackCommandResult result, string actionName)
        {
            if (!result.WasApplied)
            {
                throw new InvalidOperationException("Could not " + actionName + ": " + result.RejectionReason);
            }
        }

        private static void RequireAdvanced(bool advanced, BlackjackRoundPhase phase)
        {
            if (!advanced)
            {
                throw new InvalidOperationException("Could not advance blackjack phase: " + phase);
            }
        }
    }
}
