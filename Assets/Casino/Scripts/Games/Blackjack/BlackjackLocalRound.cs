using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Casino.Core.Cards;
using Casino.Core.Identifiers;
using Casino.Core.Money;

namespace Casino.Blackjack
{
    public enum BlackjackRoundPhase
    {
        Betting,
        InitialDeal,
        InsuranceOrDealerPeek,
        PlayerTurns,
        DealerTurn,
        Settlement,
        Intermission
    }

    public enum BlackjackCommandStatus
    {
        Applied,
        Duplicate,
        Rejected
    }

    public readonly struct BlackjackCommandResult
    {
        private BlackjackCommandResult(BlackjackCommandStatus status, string rejectionReason)
        {
            Status = status;
            RejectionReason = rejectionReason ?? string.Empty;
        }

        public BlackjackCommandStatus Status { get; }

        public string RejectionReason { get; }

        public bool WasApplied => Status == BlackjackCommandStatus.Applied;

        public static BlackjackCommandResult Applied()
        {
            return new BlackjackCommandResult(BlackjackCommandStatus.Applied, string.Empty);
        }

        public static BlackjackCommandResult Duplicate()
        {
            return new BlackjackCommandResult(BlackjackCommandStatus.Duplicate, string.Empty);
        }

        public static BlackjackCommandResult Rejected(string reason)
        {
            return new BlackjackCommandResult(BlackjackCommandStatus.Rejected, reason);
        }
    }

    public sealed class BlackjackPlayerHandState
    {
        internal BlackjackPlayerHandState(BlackjackHand hand, int wager)
        {
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));
            if (wager <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wager));
            }

            Wager = wager;
        }

        public BlackjackHand Hand { get; internal set; }

        public int Wager { get; internal set; }

        public bool IsComplete { get; internal set; }

        public bool HasSettlement { get; internal set; }

        public MainBetSettlement Settlement { get; internal set; }
    }

    public sealed class BlackjackLocalRound
    {
        private readonly BlackjackRules rules;
        private readonly Queue<Card> shoe;
        private readonly HashSet<ActionId> appliedActions = new HashSet<ActionId>();
        private readonly List<BlackjackPlayerHandState> playerHands = new List<BlackjackPlayerHandState>();
        private readonly ReadOnlyCollection<BlackjackPlayerHandState> readOnlyPlayerHands;
        private readonly List<Card> dealerCards = new List<Card>();
        private readonly int startingBalance;
        private ChipLedger ledger;
        private int openingWager;
        private int splitCount;
        private int activeHandIndex = -1;
        private bool insuranceDecisionResolved;
        private int insuranceWager;
        private bool hasInsuranceSettlement;
        private InsuranceBetSettlement insuranceSettlement;
        private bool settlementApplied;

        public BlackjackLocalRound(
            RoundId roundId,
            BlackjackRules rules,
            int startingBalance,
            IEnumerable<Card> orderedCards)
        {
            RoundId = roundId;
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            if (orderedCards == null)
            {
                throw new ArgumentNullException(nameof(orderedCards));
            }

            ledger = new ChipLedger(startingBalance);
            this.startingBalance = startingBalance;
            shoe = new Queue<Card>(orderedCards);
            readOnlyPlayerHands = new ReadOnlyCollection<BlackjackPlayerHandState>(playerHands);
            Phase = BlackjackRoundPhase.Betting;
        }

        public RoundId RoundId { get; }

        public BlackjackRules Rules => rules;

        public BlackjackRoundPhase Phase { get; private set; }

        public int Balance => ledger.Balance;

        public int StartingBalance => startingBalance;

        public IReadOnlyList<BlackjackPlayerHandState> PlayerHands => readOnlyPlayerHands;

        public int ActiveHandIndex => activeHandIndex;

        public BlackjackPlayerHandState ActiveHand
        {
            get
            {
                if (Phase != BlackjackRoundPhase.PlayerTurns || activeHandIndex < 0)
                {
                    throw new InvalidOperationException("There is no active player hand.");
                }

                return playerHands[activeHandIndex];
            }
        }

        public IReadOnlyList<Card> DealerCards => new ReadOnlyCollection<Card>(dealerCards);

        public Card DealerUpCard
        {
            get
            {
                if (dealerCards.Count == 0)
                {
                    throw new InvalidOperationException("Dealer has no up card yet.");
                }

                return dealerCards[0];
            }
        }

        public int InsuranceWager => insuranceWager;

        public int RemainingCardCount => shoe.Count;

        public bool HasInsuranceSettlement => hasInsuranceSettlement;

        public InsuranceBetSettlement InsuranceSettlement => insuranceSettlement;

        public bool DealerHasNaturalBlackjack
        {
            get
            {
                if (dealerCards.Count < 2)
                {
                    return false;
                }

                return GetDealerHand().Evaluation.IsNaturalBlackjack;
            }
        }

        public BlackjackCommandResult PlaceBet(ActionId actionId, int wager)
        {
            if (appliedActions.Contains(actionId))
            {
                return BlackjackCommandResult.Duplicate();
            }

            if (Phase != BlackjackRoundPhase.Betting)
            {
                return BlackjackCommandResult.Rejected("Bets can only be placed during betting.");
            }

            if (wager <= 0)
            {
                return BlackjackCommandResult.Rejected("Wager must be positive.");
            }

            if (wager > Balance)
            {
                return BlackjackCommandResult.Rejected("Insufficient balance.");
            }

            appliedActions.Add(actionId);
            ApplyLedgerDelta(TransactionSuffix("bet:" + actionId), -wager);
            openingWager = wager;
            Phase = BlackjackRoundPhase.InitialDeal;
            return BlackjackCommandResult.Applied();
        }

        public BlackjackCommandResult BuyInsurance(ActionId actionId)
        {
            if (appliedActions.Contains(actionId))
            {
                return BlackjackCommandResult.Duplicate();
            }

            var validation = ValidateInsuranceAction();
            if (!string.IsNullOrEmpty(validation))
            {
                return BlackjackCommandResult.Rejected(validation);
            }

            if (!rules.InsuranceStakeRatio.TryApplyTo(openingWager, out var requiredWager))
            {
                return BlackjackCommandResult.Rejected("Insurance wager cannot be represented in whole chips.");
            }

            if (requiredWager > Balance)
            {
                return BlackjackCommandResult.Rejected("Insufficient balance for insurance.");
            }

            appliedActions.Add(actionId);
            ApplyLedgerDelta(TransactionSuffix("insurance:" + actionId), -requiredWager);
            insuranceWager = requiredWager;
            insuranceDecisionResolved = true;
            return BlackjackCommandResult.Applied();
        }

        public BlackjackCommandResult DeclineInsurance(ActionId actionId)
        {
            if (appliedActions.Contains(actionId))
            {
                return BlackjackCommandResult.Duplicate();
            }

            var validation = ValidateInsuranceAction();
            if (!string.IsNullOrEmpty(validation))
            {
                return BlackjackCommandResult.Rejected(validation);
            }

            appliedActions.Add(actionId);
            insuranceDecisionResolved = true;
            return BlackjackCommandResult.Applied();
        }

        public BlackjackCommandResult Hit(ActionId actionId)
        {
            return ApplyPlayerAction(actionId, legal => legal.CanHit, hand =>
            {
                hand.Hand = hand.Hand.AddCard(DealCard());
                if (hand.Hand.Evaluation.IsBust || hand.Hand.Evaluation.IsTwentyOne)
                {
                    hand.IsComplete = true;
                    MoveToNextPlayerHandOrDealer();
                }
            }, "Hit is not legal now.");
        }

        public BlackjackCommandResult Stand(ActionId actionId)
        {
            return ApplyPlayerAction(actionId, legal => legal.CanStand, hand =>
            {
                hand.IsComplete = true;
                MoveToNextPlayerHandOrDealer();
            }, "Stand is not legal now.");
        }

        public BlackjackCommandResult DoubleDown(ActionId actionId)
        {
            return ApplyPlayerAction(actionId, legal => legal.CanDoubleDown, hand =>
            {
                var extraWager = hand.Wager;
                ApplyLedgerDelta(TransactionSuffix("double:" + actionId), -extraWager);
                hand.Wager += extraWager;
                hand.Hand = hand.Hand.AddCard(DealCard());
                hand.IsComplete = true;
                MoveToNextPlayerHandOrDealer();
            }, "Double down is not legal now.");
        }

        public BlackjackCommandResult Split(ActionId actionId)
        {
            return ApplyPlayerAction(actionId, legal => legal.CanSplit, hand =>
            {
                ApplyLedgerDelta(TransactionSuffix("split:" + actionId), -hand.Wager);

                var firstCard = hand.Hand.Cards[0];
                var secondCard = hand.Hand.Cards[1];
                var splitAces = firstCard.Rank == CardRank.Ace && secondCard.Rank == CardRank.Ace;
                var firstHand = CreateSplitHand(firstCard, DealCard(), splitAces, hand.Wager);
                var secondHand = CreateSplitHand(secondCard, DealCard(), splitAces, hand.Wager);

                playerHands[activeHandIndex] = firstHand;
                playerHands.Insert(activeHandIndex + 1, secondHand);
                splitCount++;
                MoveToNextPlayerHandOrDealer(activeHandIndex);
            }, "Split is not legal now.");
        }

        public bool AdvanceAutomatic()
        {
            switch (Phase)
            {
                case BlackjackRoundPhase.InitialDeal:
                    DealInitialCards();
                    Phase = BlackjackRoundPhase.InsuranceOrDealerPeek;
                    return true;
                case BlackjackRoundPhase.InsuranceOrDealerPeek:
                    return AdvanceInsuranceOrDealerPeek();
                case BlackjackRoundPhase.DealerTurn:
                    PlayDealerTurn();
                    Phase = BlackjackRoundPhase.Settlement;
                    return true;
                case BlackjackRoundPhase.Settlement:
                    ApplySettlement();
                    Phase = BlackjackRoundPhase.Intermission;
                    return true;
                default:
                    return false;
            }
        }

        public int AdvanceAutomaticUntilBlocked(int maxSteps = 100)
        {
            if (maxSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSteps));
            }

            var steps = 0;
            while (steps < maxSteps && AdvanceAutomatic())
            {
                steps++;
            }

            return steps;
        }

        public BlackjackHand GetDealerHand()
        {
            if (dealerCards.Count == 0)
            {
                throw new InvalidOperationException("Dealer has no cards yet.");
            }

            return new BlackjackHand(dealerCards);
        }

        public IReadOnlyList<Card> SnapshotRemainingCards()
        {
            return new ReadOnlyCollection<Card>(new List<Card>(shoe));
        }

        public LegalBlackjackActions GetLegalActionsForActiveHand()
        {
            if (Phase != BlackjackRoundPhase.PlayerTurns || activeHandIndex < 0)
            {
                throw new InvalidOperationException("There is no active player hand.");
            }

            var hand = playerHands[activeHandIndex];
            return BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForPlayerTurn(
                    hand.Hand,
                    hand.Wager,
                    Balance,
                    splitCount,
                    hand.IsComplete),
                rules);
        }

        public LegalBlackjackActions GetLegalActionsForInsurance()
        {
            if (Phase != BlackjackRoundPhase.InsuranceOrDealerPeek)
            {
                throw new InvalidOperationException("Insurance actions are only queried during the insurance phase.");
            }

            if (playerHands.Count == 0)
            {
                throw new InvalidOperationException("There is no player hand for insurance.");
            }

            var hand = playerHands[0];
            return BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForInsurance(
                    hand.Hand,
                    hand.Wager,
                    Balance,
                    DealerUpCard,
                    insuranceWager > 0),
                rules);
        }

        public RoundSettlementCheckpoint CreateSettlementCheckpoint()
        {
            if (Phase != BlackjackRoundPhase.Intermission)
            {
                throw new InvalidOperationException("Settlement checkpoints can only be created after settlement.");
            }

            return new RoundSettlementCheckpoint(
                RoundId,
                new TransactionId(TransactionSuffix("settlement:checkpoint")),
                startingBalance,
                Balance);
        }

        private BlackjackCommandResult ApplyPlayerAction(
            ActionId actionId,
            Func<LegalBlackjackActions, bool> isLegal,
            Action<BlackjackPlayerHandState> apply,
            string rejectionReason)
        {
            if (appliedActions.Contains(actionId))
            {
                return BlackjackCommandResult.Duplicate();
            }

            if (Phase != BlackjackRoundPhase.PlayerTurns)
            {
                return BlackjackCommandResult.Rejected("Player actions are only legal during player turns.");
            }

            var hand = playerHands[activeHandIndex];
            var legal = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForPlayerTurn(
                    hand.Hand,
                    hand.Wager,
                    Balance,
                    splitCount,
                    hand.IsComplete),
                rules);

            if (!isLegal(legal))
            {
                return BlackjackCommandResult.Rejected(rejectionReason);
            }

            appliedActions.Add(actionId);
            apply(hand);
            return BlackjackCommandResult.Applied();
        }

        private string ValidateInsuranceAction()
        {
            if (Phase != BlackjackRoundPhase.InsuranceOrDealerPeek)
            {
                return "Insurance is only available during the insurance phase.";
            }

            if (dealerCards.Count == 0 || DealerUpCard.Rank != CardRank.Ace)
            {
                return "Insurance is only available against a dealer ace.";
            }

            if (insuranceDecisionResolved)
            {
                return "Insurance has already been resolved.";
            }

            return string.Empty;
        }

        private bool AdvanceInsuranceOrDealerPeek()
        {
            if (DealerUpCard.Rank == CardRank.Ace && !insuranceDecisionResolved)
            {
                return false;
            }

            ResolveInsuranceIfNeeded();

            if (DealerHasNaturalBlackjack || AllPlayerHandsAreInitialBlackjacks())
            {
                CompleteAllHands();
                Phase = BlackjackRoundPhase.Settlement;
                return true;
            }

            activeHandIndex = FindNextIncompleteHand(0);
            Phase = activeHandIndex >= 0 ? BlackjackRoundPhase.PlayerTurns : BlackjackRoundPhase.DealerTurn;
            return true;
        }

        private void DealInitialCards()
        {
            if (openingWager <= 0)
            {
                throw new InvalidOperationException("Cannot deal before a bet is placed.");
            }

            var firstPlayerCard = DealCard();
            dealerCards.Add(DealCard());
            var secondPlayerCard = DealCard();
            dealerCards.Add(DealCard());

            var playerHand = BlackjackHand.FromCards(firstPlayerCard, secondPlayerCard);
            var handState = new BlackjackPlayerHandState(playerHand, openingWager)
            {
                IsComplete = playerHand.Evaluation.IsNaturalBlackjack
            };

            playerHands.Add(handState);
        }

        private Card DealCard()
        {
            if (shoe.Count == 0)
            {
                throw new InvalidOperationException("Cannot deal from an empty local round shoe.");
            }

            return shoe.Dequeue();
        }

        private BlackjackPlayerHandState CreateSplitHand(Card originalCard, Card dealtCard, bool splitAces, int wager)
        {
            var hand = splitAces
                ? BlackjackHand.FromSplitAces(originalCard, dealtCard)
                : BlackjackHand.FromSplit(originalCard, dealtCard);

            return new BlackjackPlayerHandState(hand, wager)
            {
                IsComplete = splitAces || hand.Evaluation.IsTwentyOne || hand.Evaluation.IsBust
            };
        }

        private void MoveToNextPlayerHandOrDealer(int startIndex = -1)
        {
            var searchStart = startIndex < 0 ? activeHandIndex + 1 : startIndex;
            activeHandIndex = FindNextIncompleteHand(searchStart);
            if (activeHandIndex < 0)
            {
                Phase = BlackjackRoundPhase.DealerTurn;
            }
        }

        private int FindNextIncompleteHand(int startIndex)
        {
            for (var index = Math.Max(0, startIndex); index < playerHands.Count; index++)
            {
                if (!playerHands[index].IsComplete)
                {
                    return index;
                }
            }

            return -1;
        }

        private bool AllPlayerHandsAreInitialBlackjacks()
        {
            if (playerHands.Count == 0)
            {
                return false;
            }

            for (var index = 0; index < playerHands.Count; index++)
            {
                if (!playerHands[index].Hand.Evaluation.IsNaturalBlackjack)
                {
                    return false;
                }
            }

            return true;
        }

        private void CompleteAllHands()
        {
            for (var index = 0; index < playerHands.Count; index++)
            {
                playerHands[index].IsComplete = true;
            }

            activeHandIndex = -1;
        }

        private void PlayDealerTurn()
        {
            if (!AnyUnbustedPlayerHand())
            {
                return;
            }

            while (DealerRules.ShouldHit(GetDealerHand(), rules))
            {
                dealerCards.Add(DealCard());
            }
        }

        private bool AnyUnbustedPlayerHand()
        {
            for (var index = 0; index < playerHands.Count; index++)
            {
                if (!playerHands[index].Hand.Evaluation.IsBust)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveInsuranceIfNeeded()
        {
            if (hasInsuranceSettlement || insuranceWager <= 0)
            {
                return;
            }

            insuranceSettlement = BlackjackSettlementCalculator.SettleInsurance(
                DealerHasNaturalBlackjack,
                insuranceWager,
                rules);

            if (insuranceSettlement.PayoutChips > 0)
            {
                ApplyLedgerDelta(TransactionSuffix("settlement:insurance"), insuranceSettlement.PayoutChips);
            }

            hasInsuranceSettlement = true;
        }

        private void ApplySettlement()
        {
            if (settlementApplied)
            {
                return;
            }

            var dealerHand = GetDealerHand();
            for (var index = 0; index < playerHands.Count; index++)
            {
                var hand = playerHands[index];
                var settlement = BlackjackSettlementCalculator.SettleMainBet(hand.Hand, dealerHand, hand.Wager, rules);
                hand.Settlement = settlement;
                hand.HasSettlement = true;
                if (settlement.PayoutChips > 0)
                {
                    ApplyLedgerDelta(TransactionSuffix("settlement:main:" + index), settlement.PayoutChips);
                }
            }

            settlementApplied = true;
        }

        private void ApplyLedgerDelta(string transactionId, int chipDelta)
        {
            ledger = ledger.Apply(new ChipTransaction(new TransactionId(transactionId), chipDelta), out _);
        }

        private string TransactionSuffix(string suffix)
        {
            return RoundId + ":" + suffix;
        }
    }
}
