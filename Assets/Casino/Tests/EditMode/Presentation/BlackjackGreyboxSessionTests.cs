using Casino.Blackjack;
using Casino.Core.Cards;
using Casino.Core.Identifiers;
using Casino.Presentation.Blackjack;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Casino.Tests.EditMode.Presentation
{
    public sealed class BlackjackGreyboxSessionTests
    {
        private static readonly BlackjackRules Rules = BlackjackRules.Standard;

        [Test]
        public void SessionDealsIntoPlayerDecisionUsingRulesCore()
        {
            var session = SessionWithCards(
                100,
                C(CardRank.Ten),
                C(CardRank.Six),
                C(CardRank.Nine),
                C(CardRank.Ten),
                C(CardRank.King));

            Assert.That(session.PlaceBet(10), Is.True);

            Assert.That(session.Round.Phase, Is.EqualTo(BlackjackRoundPhase.PlayerTurns));
            Assert.That(session.Round.PlayerHands[0].Hand.Evaluation.Total, Is.EqualTo(19));
            Assert.That(session.LegalActions.CanHit, Is.True);
            Assert.That(session.LegalActions.CanStand, Is.True);
        }

        [Test]
        public void SessionAppliesSettlementCheckpointToProfile()
        {
            var session = SessionWithCards(
                100,
                C(CardRank.Ten),
                C(CardRank.Six),
                C(CardRank.Nine),
                C(CardRank.Ten),
                C(CardRank.King));

            Assert.That(session.PlaceBet(10), Is.True);
            Assert.That(session.Stand(), Is.True);

            Assert.That(session.Round.Phase, Is.EqualTo(BlackjackRoundPhase.Intermission));
            Assert.That(session.Profile.OfficialChipBalance, Is.EqualTo(110));
            Assert.That(session.LastSettlementDelta, Is.EqualTo(10));
            Assert.That(session.Profile.LatestConfirmedRoundId.HasValue, Is.True);
        }

        [Test]
        public void SessionCanRunOneHundredAiRoundsWithoutDeadlock()
        {
            var session = new BlackjackGreyboxTableSession(
                new PlayerId("ai-runner"),
                10000,
                Rules,
                10,
                100,
                10,
                13579);

            var completed = session.RunAiRounds(100);

            Assert.That(completed, Is.EqualTo(100));
            Assert.That(session.Round.Phase, Is.EqualTo(BlackjackRoundPhase.Intermission));
            Assert.That(session.Profile.OfficialChipBalance, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ScreenCreatesInitialGreyboxControls()
        {
            var screenObject = new GameObject("Greybox Screen Test");
            try
            {
                var screen = screenObject.AddComponent<BlackjackGreyboxScreen>();
                screen.Initialize();

                Assert.That(screen.Session, Is.Not.Null);
                Assert.That(screen.DealButton, Is.Not.Null);
                Assert.That(screen.HitButton, Is.Not.Null);
                Assert.That(screen.DealButton.interactable, Is.True);
                Assert.That(screen.HitButton.interactable, Is.False);
                Assert.That(screen.StandButton.interactable, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(screenObject);
                var eventSystem = Object.FindFirstObjectByType<EventSystem>();
                if (eventSystem != null)
                {
                    Object.DestroyImmediate(eventSystem.gameObject);
                }
            }
        }

        private static BlackjackGreyboxTableSession SessionWithCards(int startingBalance, params Card[] cards)
        {
            var session = new BlackjackGreyboxTableSession(
                new PlayerId("greybox-test"),
                startingBalance,
                Rules,
                10,
                100,
                10,
                12345);
            session.UseOrderedCardsForNextRound(cards);
            return session;
        }

        private static Card C(CardRank rank, CardSuit suit = CardSuit.Spades)
        {
            return new Card(suit, rank);
        }
    }
}
