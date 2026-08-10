using Casino.Blackjack;
using Casino.Core.Cards;
using Casino.Core.Identifiers;
using Casino.Presentation.Blackjack;
using NUnit.Framework;
using UnityEditor.SceneManagement;
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
                Assert.That(screenObject.transform.Find("Blackjack Greybox Canvas/Background"), Is.Null);

                var tablePanel = screenObject.transform.Find("Blackjack Greybox Canvas/Table Panel");
                Assert.That(tablePanel, Is.Not.Null);
                Assert.That(tablePanel.GetComponent<RectTransform>().anchorMax.x, Is.LessThan(0.5f));
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

        [Test]
        public void ScreenUpdatesSceneChipStackFromSelectedWager()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = CreateChipStackSceneFixture();
            var screenObject = new GameObject("Greybox Chip Screen Test");
            try
            {
                var screen = screenObject.AddComponent<BlackjackGreyboxScreen>();
                screen.Initialize();

                Assert.That(screen.VisibleWagerChipCount, Is.EqualTo(1));
                AssertVisibleChipCount(root.transform, 1);

                screen.RaiseWagerButton.onClick.Invoke();

                Assert.That(screen.VisibleWagerChipCount, Is.EqualTo(2));
                AssertVisibleChipCount(root.transform, 2);

                screen.DealButton.onClick.Invoke();

                Assert.That(screen.VisibleWagerChipCount, Is.EqualTo(2));
                AssertVisibleChipCount(root.transform, 2);
            }
            finally
            {
                Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(root);
                var eventSystem = Object.FindFirstObjectByType<EventSystem>();
                if (eventSystem != null)
                {
                    Object.DestroyImmediate(eventSystem.gameObject);
                }

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void ScreenDealsCardsOntoSceneTableZones()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = CreateCardZoneSceneFixture();
            var screenObject = new GameObject("Greybox Card Screen Test");
            try
            {
                var screen = screenObject.AddComponent<BlackjackGreyboxScreen>();
                screen.Initialize();
                screen.Session.UseOrderedCardsForNextRound(
                    new[]
                    {
                        C(CardRank.Ten),
                        C(CardRank.Six),
                        C(CardRank.Nine),
                        C(CardRank.King),
                        C(CardRank.King)
                    });
                screen.Refresh();

                screen.DealButton.onClick.Invoke();

                Assert.That(screen.VisibleTablePlayerCardCount, Is.EqualTo(2));
                Assert.That(screen.VisibleTableDealerCardCount, Is.EqualTo(1));
                Assert.That(screen.HiddenTableDealerCardCount, Is.EqualTo(1));
                AssertGeneratedCardCount(root.transform, "Seats/Seat_0_Local/CardZone/Greybox_Card_View", 2);
                AssertGeneratedCardCount(root.transform, "Dealer/Dealer_CardZone/Greybox_Card_View", 2);

                screen.StandButton.onClick.Invoke();

                Assert.That(screen.HiddenTableDealerCardCount, Is.EqualTo(0));
                Assert.That(screen.VisibleTableDealerCardCount, Is.EqualTo(3));
                AssertGeneratedCardCount(root.transform, "Dealer/Dealer_CardZone/Greybox_Card_View", 3);
            }
            finally
            {
                Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(root);
                var eventSystem = Object.FindFirstObjectByType<EventSystem>();
                if (eventSystem != null)
                {
                    Object.DestroyImmediate(eventSystem.gameObject);
                }

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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

        private static GameObject CreateChipStackSceneFixture()
        {
            var root = new GameObject("Blackjack_Greybox_Root");
            var seats = new GameObject("Seats");
            seats.transform.SetParent(root.transform, false);
            var seat = new GameObject("Seat_0_Local");
            seat.transform.SetParent(seats.transform, false);
            var anchor = new GameObject("ChipStackAnchor");
            anchor.transform.SetParent(seat.transform, false);
            var stack = new GameObject("Chip_Sample_Stack");
            stack.transform.SetParent(anchor.transform, false);
            var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.name = "Chip_0";
            chip.transform.SetParent(stack.transform, false);
            return root;
        }

        private static GameObject CreateCardZoneSceneFixture()
        {
            var root = new GameObject("Blackjack_Greybox_Root");
            var dealer = new GameObject("Dealer");
            dealer.transform.SetParent(root.transform, false);
            var dealerCardZone = new GameObject("Dealer_CardZone");
            dealerCardZone.transform.SetParent(dealer.transform, false);

            var seats = new GameObject("Seats");
            seats.transform.SetParent(root.transform, false);
            var seat = new GameObject("Seat_0_Local");
            seat.transform.SetParent(seats.transform, false);
            var playerCardZone = new GameObject("CardZone");
            playerCardZone.transform.SetParent(seat.transform, false);
            return root;
        }

        private static void AssertVisibleChipCount(Transform root, int expected)
        {
            var stack = root.Find("Seats/Seat_0_Local/ChipStackAnchor/Chip_Sample_Stack");
            Assert.That(stack, Is.Not.Null);

            var visible = 0;
            for (var index = 0; index < stack.childCount; index++)
            {
                if (stack.GetChild(index).gameObject.activeSelf)
                {
                    visible++;
                }
            }

            Assert.That(visible, Is.EqualTo(expected));
        }

        private static void AssertGeneratedCardCount(Transform root, string relativePath, int expected)
        {
            var generatedRoot = root.Find(relativePath);
            Assert.That(generatedRoot, Is.Not.Null);
            Assert.That(generatedRoot.childCount, Is.EqualTo(expected));
        }

    }
}
