using Casino.Blackjack;
using Casino.Core.Cards;
using Casino.Core.Identifiers;
using Casino.Core.Randomness;
using NUnit.Framework;

namespace Casino.Tests.EditMode
{
    public sealed class BlackjackAiTests
    {
        private static readonly BlackjackRules Rules = BlackjackRules.Standard;

        [Test]
        public void BasicAiDeclinesInsuranceFromPublicContext()
        {
            var strategy = new BasicBlackjackAiStrategy();
            var hand = BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Seven));
            var legal = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForInsurance(hand, 10, 90, C(CardRank.Ace)),
                Rules);
            var context = BlackjackAiDecisionContext.ForInsurance(
                hand,
                C(CardRank.Ace),
                legal,
                10,
                90);

            var decision = strategy.ChooseInsurance(context);

            Assert.That(legal.CanBuyInsurance, Is.True);
            Assert.That(decision.Action, Is.EqualTo(BlackjackAiPlayerAction.DeclineInsurance));
        }

        [Test]
        public void BasicAiChoosesOnlyLegalRepresentativeActions()
        {
            var strategy = new BasicBlackjackAiStrategy();

            AssertLegalDecision(strategy, BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Six)), C(CardRank.Ten), 10, 100);
            AssertLegalDecision(strategy, BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Seven)), C(CardRank.Ten), 10, 100);
            AssertLegalDecision(strategy, BlackjackHand.FromCards(C(CardRank.Five), C(CardRank.Six)), C(CardRank.Six), 10, 100);
            AssertLegalDecision(strategy, BlackjackHand.FromCards(C(CardRank.Eight), C(CardRank.Eight)), C(CardRank.Six), 10, 100);
            AssertLegalDecision(strategy, BlackjackHand.FromCards(C(CardRank.Five), C(CardRank.Six)), C(CardRank.Six), 10, 0);
        }

        [Test]
        public void BasicAiUsesExpectedSimpleStrategyChoices()
        {
            var strategy = new BasicBlackjackAiStrategy();

            Assert.That(Choose(strategy, BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Six)), C(CardRank.Ten), 10, 100), Is.EqualTo(BlackjackAiPlayerAction.Hit));
            Assert.That(Choose(strategy, BlackjackHand.FromCards(C(CardRank.Ten), C(CardRank.Seven)), C(CardRank.Ten), 10, 100), Is.EqualTo(BlackjackAiPlayerAction.Stand));
            Assert.That(Choose(strategy, BlackjackHand.FromCards(C(CardRank.Five), C(CardRank.Six)), C(CardRank.Six), 10, 100), Is.EqualTo(BlackjackAiPlayerAction.DoubleDown));
            Assert.That(Choose(strategy, BlackjackHand.FromCards(C(CardRank.Eight), C(CardRank.Eight)), C(CardRank.Six), 10, 100), Is.EqualTo(BlackjackAiPlayerAction.Split));
            Assert.That(Choose(strategy, BlackjackHand.FromCards(C(CardRank.Five), C(CardRank.Six)), C(CardRank.Six), 10, 0), Is.EqualTo(BlackjackAiPlayerAction.Hit));
        }

        [Test]
        public void AiRoundDriverCompletesDealerBustRoundAndCreatesCheckpoint()
        {
            var round = LocalRound(
                100,
                C(CardRank.Ten),
                C(CardRank.Six),
                C(CardRank.Nine),
                C(CardRank.Ten),
                C(CardRank.King));
            var driver = new BlackjackAiRoundDriver(new BasicBlackjackAiStrategy());

            driver.PlayToIntermission(round, 10, "dealer-bust");
            var checkpoint = round.CreateSettlementCheckpoint();

            Assert.That(round.Phase, Is.EqualTo(BlackjackRoundPhase.Intermission));
            Assert.That(round.Balance, Is.EqualTo(110));
            Assert.That(checkpoint.NetChipDelta, Is.EqualTo(10));
            Assert.That(round.PlayerHands[0].Settlement.Outcome, Is.EqualTo(BlackjackMainBetOutcome.DealerBust));
        }

        [Test]
        public void AiRoundDriverDeclinesInsuranceAndCompletesRound()
        {
            var round = LocalRound(
                100,
                C(CardRank.Ten),
                C(CardRank.Ace),
                C(CardRank.Seven),
                C(CardRank.Nine));
            var driver = new BlackjackAiRoundDriver(new BasicBlackjackAiStrategy());

            driver.PlayToIntermission(round, 10, "insurance-decline");

            Assert.That(round.Phase, Is.EqualTo(BlackjackRoundPhase.Intermission));
            Assert.That(round.InsuranceWager, Is.EqualTo(0));
            Assert.That(round.HasInsuranceSettlement, Is.False);
            Assert.That(round.Balance, Is.EqualTo(90));
            Assert.That(round.PlayerHands[0].Settlement.Outcome, Is.EqualTo(BlackjackMainBetOutcome.PlayerLoss));
        }

        [Test]
        public void AiRoundDriverCompletesTenThousandSeededRounds()
        {
            var driver = new BlackjackAiRoundDriver(new BasicBlackjackAiStrategy());

            for (var roundIndex = 0; roundIndex < 10000; roundIndex++)
            {
                var shoe = CardShoe.CreateSixDeckShoe(new SystemRandomSource(roundIndex + 1000));
                var round = new BlackjackLocalRound(
                    new RoundId("ai-simulation-" + roundIndex),
                    Rules,
                    1000,
                    shoe.SnapshotRemainingCards());

                driver.PlayToIntermission(round, 2, "ai-simulation-" + roundIndex);

                Assert.That(round.Phase, Is.EqualTo(BlackjackRoundPhase.Intermission));
                Assert.That(round.PlayerHands[0].HasSettlement, Is.True);
                Assert.That(round.Balance, Is.GreaterThanOrEqualTo(0));
            }
        }

        private static BlackjackAiPlayerAction Choose(
            BasicBlackjackAiStrategy strategy,
            BlackjackHand hand,
            Card dealerUpCard,
            int wager,
            int availableChips)
        {
            var legal = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForPlayerTurn(hand, wager, availableChips),
                Rules);
            var context = BlackjackAiDecisionContext.ForPlayerTurn(
                hand,
                dealerUpCard,
                legal,
                wager,
                availableChips);

            return strategy.ChoosePlayerAction(context).Action;
        }

        private static void AssertLegalDecision(
            BasicBlackjackAiStrategy strategy,
            BlackjackHand hand,
            Card dealerUpCard,
            int wager,
            int availableChips)
        {
            var legal = BlackjackActionValidator.GetLegalActions(
                BlackjackActionContext.ForPlayerTurn(hand, wager, availableChips),
                Rules);
            var context = BlackjackAiDecisionContext.ForPlayerTurn(
                hand,
                dealerUpCard,
                legal,
                wager,
                availableChips);

            var decision = strategy.ChoosePlayerAction(context);

            Assert.That(IsLegal(decision.Action, legal), Is.True, decision.Action + " should be legal.");
        }

        private static bool IsLegal(BlackjackAiPlayerAction action, LegalBlackjackActions legal)
        {
            switch (action)
            {
                case BlackjackAiPlayerAction.Hit:
                    return legal.CanHit;
                case BlackjackAiPlayerAction.Stand:
                    return legal.CanStand;
                case BlackjackAiPlayerAction.DoubleDown:
                    return legal.CanDoubleDown;
                case BlackjackAiPlayerAction.Split:
                    return legal.CanSplit;
                case BlackjackAiPlayerAction.BuyInsurance:
                    return legal.CanBuyInsurance;
                case BlackjackAiPlayerAction.DeclineInsurance:
                    return true;
                default:
                    return false;
            }
        }

        private static Card C(CardRank rank, CardSuit suit = CardSuit.Spades)
        {
            return new Card(suit, rank);
        }

        private static BlackjackLocalRound LocalRound(int startingBalance, params Card[] orderedCards)
        {
            return new BlackjackLocalRound(new RoundId("ai-test-round"), Rules, startingBalance, orderedCards);
        }
    }
}
