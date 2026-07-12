using System.Collections.Generic;
using Casino.Blackjack;
using Casino.Core.Cards;
using Casino.Core.Identifiers;
using Casino.Core.Money;
using Casino.Core.Randomness;
using NUnit.Framework;

namespace Casino.Tests.EditMode
{
    public sealed class BlackjackCoreTests
    {
        private static readonly BlackjackRules Rules = BlackjackRules.Standard;

        [Test]
        public void SixDeckShoeContainsThreeHundredTwelveCards()
        {
            var shoe = CardShoe.CreateSixDeckShoe(new SystemRandomSource(1234));
            var counts = new Dictionary<Card, int>();

            while (!shoe.IsEmpty)
            {
                var card = shoe.Deal();
                counts.TryGetValue(card, out var count);
                counts[card] = count + 1;
            }

            Assert.That(counts.Count, Is.EqualTo(52));
            Assert.That(shoe.InitialCardCount, Is.EqualTo(312));
            foreach (var count in counts.Values)
            {
                Assert.That(count, Is.EqualTo(6));
            }
        }

        [Test]
        public void FisherYatesShuffleUsesInjectedRandomSource()
        {
            var values = new List<int> { 1, 2, 3 };

            FisherYatesShuffle.Shuffle(values, new ScriptedRandomSource(new[] { 0, 0 }));

            CollectionAssert.AreEqual(new[] { 2, 3, 1 }, values);
        }

        [Test]
        public void HandEvaluationHandlesMultipleAces()
        {
            var softTwentyOne = BlackjackHand.FromCards(
                C(CardRank.Ace),
                C(CardRank.Ace),
                C(CardRank.Nine));

            var hardTwentyOne = softTwentyOne.AddCard(C(CardRank.King));

            Assert.That(softTwentyOne.Evaluation.Total, Is.EqualTo(21));
            Assert.That(softTwentyOne.Evaluation.IsSoft, Is.True);
            Assert.That(hardTwentyOne.Evaluation.Total, Is.EqualTo(21));
            Assert.That(hardTwentyOne.Evaluation.IsSoft, Is.False);
        }

        [Test]
        public void NaturalBlackjackRequiresInitialUnsplitAceAndTenValueCard()
        {
            var natural = BlackjackHand.FromCards(C(CardRank.Ace), C(CardRank.King));
            var splitTwentyOne = BlackjackHand.FromSplit(C(CardRank.Ace), C(CardRank.King));

            Assert.That(natural.Evaluation.IsNaturalBlackjack, Is.True);
            Assert.That(splitTwentyOne.Evaluation.Total, Is.EqualTo(21));
            Assert.That(splitTwentyOne.Evaluation.IsNaturalBlackjack, Is.False);
        }

        [Test]
        public void DealerStandsOnHardAndSoftSeventeen()
        {
            var hardSixteen = BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Six));
            var hardSeventeen = BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Seven));
            var softSeventeen = BlackjackHand.FromCards(C(CardRank.Ace), C(CardRank.Six));

            Assert.That(DealerRules.ShouldHit(hardSixteen, Rules), Is.True);
            Assert.That(DealerRules.ShouldHit(hardSeventeen, Rules), Is.False);
            Assert.That(DealerRules.ShouldHit(softSeventeen, Rules), Is.False);
        }

        [Test]
        public void LegalActionsRespectDoubleSplitAndBalanceRules()
        {
            var tenValuePair = BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Queen));
            var legal = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForPlayerTurn(tenValuePair, 10, 10),
                Rules);

            var afterHit = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForPlayerTurn(tenValuePair.AddCard(C(CardRank.Two)), 10, 10),
                Rules);

            var insufficientBalance = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForPlayerTurn(tenValuePair, 10, 9),
                Rules);

            var alreadySplit = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForPlayerTurn(tenValuePair, 10, 10, previousSplitCount: 1),
                Rules);

            Assert.That(legal.CanDoubleDown, Is.True);
            Assert.That(legal.CanSplit, Is.True);
            Assert.That(afterHit.CanDoubleDown, Is.False);
            Assert.That(afterHit.CanSplit, Is.False);
            Assert.That(insufficientBalance.CanDoubleDown, Is.False);
            Assert.That(insufficientBalance.CanSplit, Is.False);
            Assert.That(alreadySplit.CanSplit, Is.False);
        }

        [Test]
        public void SplitAcesReceiveNoFurtherPlayerActions()
        {
            var splitAceHand = BlackjackHand.FromSplitAces(C(CardRank.Ace), C(CardRank.Ten));

            var legal = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForPlayerTurn(splitAceHand, 10, 100),
                Rules);

            Assert.That(legal.CanHit, Is.False);
            Assert.That(legal.CanStand, Is.False);
            Assert.That(legal.CanDoubleDown, Is.False);
            Assert.That(legal.CanSplit, Is.False);
        }

        [Test]
        public void InsuranceIsAvailableOnlyAgainstDealerAceWithEnoughChips()
        {
            var playerHand = BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Six));

            var legal = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForInsurance(playerHand, 10, 5, C(CardRank.Ace)),
                Rules);

            var insufficient = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForInsurance(playerHand, 10, 4, C(CardRank.Ace)),
                Rules);

            var dealerTen = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForInsurance(playerHand, 10, 5, C(CardRank.Ten)),
                Rules);

            Assert.That(legal.CanHit, Is.False);
            Assert.That(legal.CanStand, Is.False);
            Assert.That(legal.CanDoubleDown, Is.False);
            Assert.That(legal.CanSplit, Is.False);
            Assert.That(legal.CanBuyInsurance, Is.True);
            Assert.That(legal.InsuranceWager, Is.EqualTo(5));
            Assert.That(insufficient.CanBuyInsurance, Is.False);
            Assert.That(dealerTen.CanBuyInsurance, Is.False);
        }

        [Test]
        public void MainSettlementCoversWinLossPushBlackjackAndBust()
        {
            var dealerTwenty = BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Queen));
            var dealerEighteen = BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Eight));

            var win = BlackjackSettlementCalculator.SettleMainBet(
                BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Nine)),
                dealerEighteen,
                10,
                Rules);

            var blackjack = BlackjackSettlementCalculator.SettleMainBet(
                BlackjackHand.FromCards(C(CardRank.Ace), C(CardRank.King)),
                dealerTwenty,
                10,
                Rules);

            var push = BlackjackSettlementCalculator.SettleMainBet(
                BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Queen)),
                dealerTwenty,
                10,
                Rules);

            var bust = BlackjackSettlementCalculator.SettleMainBet(
                BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Nine), C(CardRank.Five)),
                dealerEighteen,
                10,
                Rules);

            Assert.That(win.Outcome, Is.EqualTo(BlackjackMainBetOutcome.PlayerWin));
            Assert.That(win.NetChips, Is.EqualTo(10));
            Assert.That(win.PayoutChips, Is.EqualTo(20));
            Assert.That(blackjack.Outcome, Is.EqualTo(BlackjackMainBetOutcome.PlayerBlackjack));
            Assert.That(blackjack.NetChips, Is.EqualTo(15));
            Assert.That(blackjack.PayoutChips, Is.EqualTo(25));
            Assert.That(push.Outcome, Is.EqualTo(BlackjackMainBetOutcome.Push));
            Assert.That(push.NetChips, Is.EqualTo(0));
            Assert.That(push.PayoutChips, Is.EqualTo(10));
            Assert.That(bust.Outcome, Is.EqualTo(BlackjackMainBetOutcome.PlayerBust));
            Assert.That(bust.NetChips, Is.EqualTo(-10));
            Assert.That(bust.PayoutChips, Is.EqualTo(0));
        }

        [Test]
        public void DealerBustPaysAllUnbustedPlayerHands()
        {
            var dealerBust = BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Six), C(CardRank.King));
            var hands = new[]
            {
                new BlackjackHandBet(BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Seven)), 10),
                new BlackjackHandBet(BlackjackHand.FromSplit(C(CardRank.Nine), C(CardRank.Two)), 20)
            };

            var settlements = BlackjackSettlementCalculator.SettleMainBets(hands, dealerBust, Rules);

            Assert.That(settlements[0].Outcome, Is.EqualTo(BlackjackMainBetOutcome.DealerBust));
            Assert.That(settlements[0].NetChips, Is.EqualTo(10));
            Assert.That(settlements[1].Outcome, Is.EqualTo(BlackjackMainBetOutcome.DealerBust));
            Assert.That(settlements[1].NetChips, Is.EqualTo(20));
        }

        [Test]
        public void InsuranceSettlementPaysTwoToOneOrLosesStake()
        {
            var won = BlackjackSettlementCalculator.SettleInsurance(true, 5, Rules);
            var lost = BlackjackSettlementCalculator.SettleInsurance(false, 5, Rules);

            Assert.That(won.Outcome, Is.EqualTo(BlackjackInsuranceOutcome.Won));
            Assert.That(won.NetChips, Is.EqualTo(10));
            Assert.That(won.PayoutChips, Is.EqualTo(15));
            Assert.That(lost.Outcome, Is.EqualTo(BlackjackInsuranceOutcome.Lost));
            Assert.That(lost.NetChips, Is.EqualTo(-5));
            Assert.That(lost.PayoutChips, Is.EqualTo(0));
        }

        [Test]
        public void DuplicateSettlementTransactionIsIgnored()
        {
            var ledger = new ChipLedger(100);
            var transaction = new ChipTransaction(new TransactionId("round-1:settlement:seat-1"), 15);

            ledger = ledger.Apply(transaction, out var firstApplied);
            ledger = ledger.Apply(transaction, out var secondApplied);

            Assert.That(firstApplied, Is.True);
            Assert.That(secondApplied, Is.False);
            Assert.That(ledger.Balance, Is.EqualTo(115));
        }

        private static Card C(CardRank rank, CardSuit suit = CardSuit.Spades)
        {
            return new Card(suit, rank);
        }
    }
}
