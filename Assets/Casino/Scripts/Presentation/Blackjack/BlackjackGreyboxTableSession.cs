using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Casino.Blackjack;
using Casino.Core.Cards;
using Casino.Core.Identifiers;
using Casino.Core.Randomness;
using Casino.Persistence;

namespace Casino.Presentation.Blackjack
{
    public sealed class BlackjackGreyboxTableSession
    {
        public const int DefaultStartingBalance = 1000;
        public const int DefaultMinimumWager = 10;
        public const int DefaultMaximumWager = 100;
        public const int DefaultWager = 10;

        private readonly List<string> debugLog = new List<string>();
        private List<Card> remainingCards;
        private int shuffleSeed;
        private int roundIndex;
        private int actionIndex;
        private bool currentRoundAppliedToProfile;
        private bool useOrderedCardsForNextRound;

        public BlackjackGreyboxTableSession()
            : this(
                new PlayerId("local-player"),
                DefaultStartingBalance,
                BlackjackRules.Standard,
                DefaultMinimumWager,
                DefaultMaximumWager,
                DefaultWager,
                24681357)
        {
        }

        public BlackjackGreyboxTableSession(
            PlayerId playerId,
            int startingBalance,
            BlackjackRules rules,
            int minimumWager,
            int maximumWager,
            int defaultWager,
            int shuffleSeed)
        {
            if (startingBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingBalance));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (minimumWager <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumWager));
            }

            if (maximumWager > 0 && maximumWager < minimumWager)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumWager));
            }

            if (defaultWager < minimumWager)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultWager));
            }

            Rules = rules;
            MinimumWager = minimumWager;
            MaximumWager = maximumWager;
            DefaultOpeningWager = maximumWager > 0 ? Math.Min(defaultWager, maximumWager) : defaultWager;
            this.shuffleSeed = shuffleSeed;
            Profile = new LocalPlayerProfile(playerId, startingBalance);
            StatusMessage = "Choose a wager and deal.";
            StartNextRound();
        }

        public BlackjackRules Rules { get; }

        public int MinimumWager { get; }

        public int MaximumWager { get; }

        public int DefaultOpeningWager { get; }

        public LocalPlayerProfile Profile { get; private set; }

        public BlackjackLocalRound Round { get; private set; }

        public int LastSettlementDelta { get; private set; }

        public string StatusMessage { get; private set; }

        public IReadOnlyList<string> DebugLog => new ReadOnlyCollection<string>(debugLog);

        public bool CanDeal => Round != null
            && Round.Phase == BlackjackRoundPhase.Betting
            && Round.Balance >= MinimumWager;

        public bool CanStartNextRound => Round == null || Round.Phase == BlackjackRoundPhase.Intermission;

        public LegalBlackjackActions LegalActions
        {
            get
            {
                if (Round == null)
                {
                    return NoLegalActions();
                }

                if (Round.Phase == BlackjackRoundPhase.PlayerTurns)
                {
                    return Round.GetLegalActionsForActiveHand();
                }

                if (Round.Phase == BlackjackRoundPhase.InsuranceOrDealerPeek
                    && Round.DealerUpCard.Rank == CardRank.Ace)
                {
                    return Round.GetLegalActionsForInsurance();
                }

                return NoLegalActions();
            }
        }

        public void UseOrderedCardsForNextRound(IEnumerable<Card> orderedCards)
        {
            if (orderedCards == null)
            {
                throw new ArgumentNullException(nameof(orderedCards));
            }

            if (Round != null
                && Round.Phase != BlackjackRoundPhase.Betting
                && Round.Phase != BlackjackRoundPhase.Intermission)
            {
                throw new InvalidOperationException("The current round must finish before replacing the shoe.");
            }

            remainingCards = new List<Card>(orderedCards);
            Round = null;
            useOrderedCardsForNextRound = true;
            Log("Loaded ordered cards for the next round.");
            StartNextRound();
        }

        public void StartNextRound()
        {
            if (Round != null && Round.Phase != BlackjackRoundPhase.Intermission)
            {
                throw new InvalidOperationException("Cannot start a new round before the current round reaches intermission.");
            }

            ApplyCurrentRoundSettlementIfReady();
            EnsureShoeForNextRound();

            Round = new BlackjackLocalRound(
                new RoundId("greybox-round-" + roundIndex),
                Rules,
                Profile.OfficialChipBalance,
                remainingCards);

            roundIndex++;
            actionIndex = 0;
            currentRoundAppliedToProfile = false;
            useOrderedCardsForNextRound = false;
            LastSettlementDelta = 0;
            StatusMessage = "Choose a wager and deal.";
            Log("Started " + Round.RoundId + " with balance " + Profile.OfficialChipBalance + ".");
        }

        public bool PlaceBet(int wager)
        {
            if (Round == null || Round.Phase == BlackjackRoundPhase.Intermission)
            {
                StartNextRound();
            }

            if (Round.Phase != BlackjackRoundPhase.Betting)
            {
                StatusMessage = "Betting is closed for this round.";
                return false;
            }

            var normalizedWager = NormalizeWager(wager, Round.Balance);
            if (normalizedWager <= 0)
            {
                StatusMessage = "Not enough chips for the table minimum.";
                return false;
            }

            if (!Rules.BlackjackPayout.TryApplyTo(normalizedWager, out _))
            {
                StatusMessage = "Use an even wager so 3:2 blackjack payout stays in whole chips.";
                return false;
            }

            var result = Round.PlaceBet(NextActionId("bet"), normalizedWager);
            if (!ApplyCommandResult(result, "Bet " + normalizedWager))
            {
                return false;
            }

            AdvanceAutomaticUntilDecision();
            return true;
        }

        public bool BuyInsurance()
        {
            if (Round == null)
            {
                return RejectMissingRound();
            }

            var result = Round.BuyInsurance(NextActionId("insurance"));
            if (!ApplyCommandResult(result, "Insurance bought"))
            {
                return false;
            }

            AdvanceAutomaticUntilDecision();
            return true;
        }

        public bool DeclineInsurance()
        {
            if (Round == null)
            {
                return RejectMissingRound();
            }

            var result = Round.DeclineInsurance(NextActionId("no-insurance"));
            if (!ApplyCommandResult(result, "Insurance declined"))
            {
                return false;
            }

            AdvanceAutomaticUntilDecision();
            return true;
        }

        public bool Hit()
        {
            return ApplyPlayerCommand(Round != null ? Round.Hit(NextActionId("hit")) : BlackjackCommandResult.Rejected("No active round."), "Hit");
        }

        public bool Stand()
        {
            return ApplyPlayerCommand(Round != null ? Round.Stand(NextActionId("stand")) : BlackjackCommandResult.Rejected("No active round."), "Stand");
        }

        public bool DoubleDown()
        {
            return ApplyPlayerCommand(Round != null ? Round.DoubleDown(NextActionId("double")) : BlackjackCommandResult.Rejected("No active round."), "Double down");
        }

        public bool Split()
        {
            return ApplyPlayerCommand(Round != null ? Round.Split(NextActionId("split")) : BlackjackCommandResult.Rejected("No active round."), "Split");
        }

        public bool AutoPlayCurrentRound()
        {
            if (Round == null || Round.Phase == BlackjackRoundPhase.Intermission)
            {
                StartNextRound();
            }

            if (Round.Phase == BlackjackRoundPhase.Betting && Round.Balance < MinimumWager)
            {
                StatusMessage = "Not enough chips to auto-play this table.";
                return false;
            }

            var driver = new BlackjackAiRoundDriver(new BasicBlackjackAiStrategy());
            driver.PlayToIntermission(
                Round,
                MinimumWager,
                Round.RoundId + ":greybox-auto");
            ApplyCurrentRoundSettlementIfReady();
            StatusMessage = "AI completed " + Round.RoundId + " (" + FormatSigned(LastSettlementDelta) + " chips).";
            return true;
        }

        public int RunAiRounds(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var completed = 0;
            for (var index = 0; index < count; index++)
            {
                if (Round == null || Round.Phase == BlackjackRoundPhase.Intermission)
                {
                    StartNextRound();
                }

                if (Round.Balance < MinimumWager)
                {
                    StatusMessage = "Stopped AI run: not enough chips for the table minimum.";
                    break;
                }

                AutoPlayCurrentRound();
                completed++;
            }

            StatusMessage = "AI completed " + completed + " round(s). Profile balance: " + Profile.OfficialChipBalance + ".";
            return completed;
        }

        public string BuildRoundSummary()
        {
            if (Round == null)
            {
                return "No active round.";
            }

            var builder = new StringBuilder();
            builder.Append("Round: ").Append(Round.RoundId)
                .Append(" | Phase: ").Append(Round.Phase)
                .Append(" | Shoe: ").Append(GetShoeHint())
                .AppendLine();
            builder.Append("Profile balance: ").Append(Profile.OfficialChipBalance)
                .Append(" | Round balance: ").Append(Round.Balance)
                .Append(" | Table: min ").Append(MinimumWager);
            if (MaximumWager > 0)
            {
                builder.Append(" max ").Append(MaximumWager);
            }
            else
            {
                builder.Append(" no-limit variant");
            }

            return builder.ToString();
        }

        private bool ApplyPlayerCommand(BlackjackCommandResult result, string actionName)
        {
            if (!ApplyCommandResult(result, actionName))
            {
                return false;
            }

            AdvanceAutomaticUntilDecision();
            return true;
        }

        private bool ApplyCommandResult(BlackjackCommandResult result, string actionName)
        {
            if (result.WasApplied)
            {
                StatusMessage = actionName + ".";
                Log(actionName + " applied in " + Round.RoundId + ".");
                return true;
            }

            StatusMessage = result.RejectionReason;
            Log(actionName + " rejected: " + result.RejectionReason);
            return false;
        }

        private bool RejectMissingRound()
        {
            StatusMessage = "No active round.";
            return false;
        }

        private void AdvanceAutomaticUntilDecision()
        {
            for (var safety = 0; safety < 30; safety++)
            {
                if (Round == null)
                {
                    return;
                }

                switch (Round.Phase)
                {
                    case BlackjackRoundPhase.InitialDeal:
                    case BlackjackRoundPhase.DealerTurn:
                    case BlackjackRoundPhase.Settlement:
                        Round.AdvanceAutomatic();
                        break;
                    case BlackjackRoundPhase.InsuranceOrDealerPeek:
                        if (!Round.AdvanceAutomatic())
                        {
                            StatusMessage = "Dealer shows ace. Choose insurance or no insurance.";
                            return;
                        }

                        break;
                    case BlackjackRoundPhase.Intermission:
                        ApplyCurrentRoundSettlementIfReady();
                        return;
                    case BlackjackRoundPhase.PlayerTurns:
                    case BlackjackRoundPhase.Betting:
                        return;
                    default:
                        throw new InvalidOperationException("Unsupported blackjack phase: " + Round.Phase);
                }
            }

            throw new InvalidOperationException("Automatic advancement exceeded the safety limit.");
        }

        private void ApplyCurrentRoundSettlementIfReady()
        {
            if (Round == null || Round.Phase != BlackjackRoundPhase.Intermission || currentRoundAppliedToProfile)
            {
                return;
            }

            var checkpoint = Round.CreateSettlementCheckpoint();
            Profile = Profile.ApplySettlementCheckpoint(checkpoint);
            remainingCards = new List<Card>(Round.SnapshotRemainingCards());
            currentRoundAppliedToProfile = true;
            LastSettlementDelta = checkpoint.NetChipDelta;
            StatusMessage = "Round settled: " + FormatSigned(LastSettlementDelta) + " chips.";
            Log("Settled " + checkpoint.RoundId + " with delta " + FormatSigned(LastSettlementDelta) + ".");
        }

        private void EnsureShoeForNextRound()
        {
            var shuffleThreshold = (int)(Rules.DeckCount * CardShoe.CardsPerDeck * Rules.ShuffleThreshold);
            if (useOrderedCardsForNextRound && remainingCards != null)
            {
                return;
            }

            if (remainingCards != null && remainingCards.Count >= shuffleThreshold)
            {
                return;
            }

            var shoe = CardShoe.CreateShuffled(Rules.DeckCount, new SystemRandomSource(shuffleSeed));
            shuffleSeed++;
            remainingCards = new List<Card>(shoe.SnapshotRemainingCards());
            Log("Shuffled a " + Rules.DeckCount + "-deck shoe.");
        }

        private int NormalizeWager(int requestedWager, int availableBalance)
        {
            if (requestedWager < MinimumWager || availableBalance < MinimumWager)
            {
                return 0;
            }

            var capped = Math.Min(requestedWager, availableBalance);
            if (MaximumWager > 0)
            {
                capped = Math.Min(capped, MaximumWager);
            }

            return capped;
        }

        private ActionId NextActionId(string actionName)
        {
            var actionId = new ActionId(Round.RoundId + ":" + actionName + ":" + actionIndex);
            actionIndex++;
            return actionId;
        }

        private string GetShoeHint()
        {
            if (Round == null)
            {
                return "none";
            }

            var remaining = Round.RemainingCardCount;
            var total = Rules.DeckCount * CardShoe.CardsPerDeck;
            if (remaining > total / 2)
            {
                return "plenty";
            }

            if (remaining >= total / 4)
            {
                return "over quarter";
            }

            return "shuffle soon";
        }

        private void Log(string message)
        {
            debugLog.Add(message);
            if (debugLog.Count > 20)
            {
                debugLog.RemoveAt(0);
            }
        }

        private static string FormatSigned(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        private static LegalBlackjackActions NoLegalActions()
        {
            return new LegalBlackjackActions(false, false, false, false, false, 0);
        }
    }

    public static class BlackjackGreyboxFormatting
    {
        public static string FormatDealer(BlackjackLocalRound round)
        {
            if (round == null || round.DealerCards.Count == 0)
            {
                return "Dealer: waiting for deal.";
            }

            var revealHole = IsDealerHoleRevealed(round.Phase);
            var builder = new StringBuilder();
            builder.Append("Dealer: ");
            for (var index = 0; index < round.DealerCards.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                if (index == 1 && !revealHole)
                {
                    builder.Append("[hole]");
                }
                else
                {
                    builder.Append(FormatCard(round.DealerCards[index]));
                }
            }

            builder.Append(" | Value: ");
            builder.Append(revealHole ? FormatHandValue(round.GetDealerHand()) : "?");
            return builder.ToString();
        }

        public static string FormatPlayerHands(BlackjackLocalRound round)
        {
            if (round == null || round.PlayerHands.Count == 0)
            {
                return "Player: no hand yet.";
            }

            var builder = new StringBuilder();
            for (var index = 0; index < round.PlayerHands.Count; index++)
            {
                var handState = round.PlayerHands[index];
                if (index > 0)
                {
                    builder.AppendLine();
                }

                var active = round.Phase == BlackjackRoundPhase.PlayerTurns && index == round.ActiveHandIndex;
                builder.Append(active ? "> " : "  ");
                builder.Append("Hand ").Append(index + 1).Append(": ")
                    .Append(FormatHand(handState.Hand))
                    .Append(" | Value: ").Append(FormatHandValue(handState.Hand))
                    .Append(" | Wager: ").Append(handState.Wager);

                if (handState.HasSettlement)
                {
                    builder.Append(" | ").Append(handState.Settlement.Outcome)
                        .Append(" ").Append(FormatSigned(handState.Settlement.NetChips));
                }
                else if (handState.IsComplete)
                {
                    builder.Append(" | complete");
                }
            }

            return builder.ToString();
        }

        public static string FormatInsurance(BlackjackLocalRound round)
        {
            if (round == null)
            {
                return string.Empty;
            }

            if (round.HasInsuranceSettlement)
            {
                return "Insurance: " + round.InsuranceSettlement.Outcome + " "
                    + FormatSigned(round.InsuranceSettlement.NetChips);
            }

            if (round.InsuranceWager > 0)
            {
                return "Insurance wager: " + round.InsuranceWager;
            }

            return "Insurance: none";
        }

        public static string FormatCard(Card card)
        {
            return FormatRank(card.Rank) + FormatSuit(card.Suit);
        }

        private static string FormatHand(BlackjackHand hand)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < hand.Cards.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(" ");
                }

                builder.Append(FormatCard(hand.Cards[index]));
            }

            return builder.ToString();
        }

        private static string FormatHandValue(BlackjackHand hand)
        {
            var evaluation = hand.Evaluation;
            if (evaluation.IsNaturalBlackjack)
            {
                return "Blackjack";
            }

            if (evaluation.IsBust)
            {
                return evaluation.Total + " bust";
            }

            return evaluation.IsSoft ? evaluation.Total + " soft" : evaluation.Total.ToString();
        }

        private static bool IsDealerHoleRevealed(BlackjackRoundPhase phase)
        {
            return phase == BlackjackRoundPhase.DealerTurn
                || phase == BlackjackRoundPhase.Settlement
                || phase == BlackjackRoundPhase.Intermission;
        }

        private static string FormatRank(CardRank rank)
        {
            switch (rank)
            {
                case CardRank.Ace:
                    return "A";
                case CardRank.Jack:
                    return "J";
                case CardRank.Queen:
                    return "Q";
                case CardRank.King:
                    return "K";
                default:
                    return ((int)rank).ToString();
            }
        }

        private static string FormatSuit(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Clubs:
                    return "C";
                case CardSuit.Diamonds:
                    return "D";
                case CardSuit.Hearts:
                    return "H";
                case CardSuit.Spades:
                    return "S";
                default:
                    throw new ArgumentOutOfRangeException(nameof(suit));
            }
        }

        private static string FormatSigned(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }
    }
}
