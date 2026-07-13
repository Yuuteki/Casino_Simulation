using System.Collections.Generic;
using Casino.Blackjack;
using Casino.Core.Cards;
using Casino.Core.Identifiers;
using Casino.Core.Money;
using Casino.Persistence;
using NUnit.Framework;

namespace Casino.Tests.EditMode
{
    public sealed class LocalProfilePersistenceTests
    {
        private static readonly BlackjackRules Rules = BlackjackRules.Standard;

        [Test]
        public void ProfileSnapshotRestoresBalanceAndAppliedTransactions()
        {
            var playerId = new PlayerId("local-player");
            var profile = new LocalPlayerProfile(playerId, 100)
                .ApplyTransaction(new ChipTransaction(new TransactionId("daily:grant:1"), 25));

            var restored = LocalPlayerProfile.FromSnapshot(profile.ToSnapshot());

            Assert.That(restored.PlayerId, Is.EqualTo(playerId));
            Assert.That(restored.OfficialChipBalance, Is.EqualTo(125));
            Assert.That(restored.HasApplied(new TransactionId("daily:grant:1")), Is.True);
            Assert.That(restored.LatestConfirmedRoundId.HasValue, Is.False);
        }

        [Test]
        public void ProfileStoreRoundTripPreservesSettlementCheckpoint()
        {
            var store = new MemoryProfileStore();
            var playerId = new PlayerId("local-player");
            var profile = new LocalPlayerProfile(playerId, 100);
            var round = WinningRoundFrom(profile);

            var checkpoint = round.CreateSettlementCheckpoint();
            profile = profile.ApplySettlementCheckpoint(checkpoint);
            store.Save(profile);

            var loaded = store.Load(playerId).Profile;

            Assert.That(loaded.OfficialChipBalance, Is.EqualTo(110));
            Assert.That(loaded.LatestConfirmedRoundId.HasValue, Is.True);
            Assert.That(loaded.LatestConfirmedRoundId.Value, Is.EqualTo(new RoundId("profile-win-round")));
            Assert.That(loaded.HasApplied(checkpoint.TransactionId), Is.True);
        }

        [Test]
        public void ReapplyingSettlementCheckpointAfterReloadDoesNotChangeBalance()
        {
            var store = new MemoryProfileStore();
            var playerId = new PlayerId("local-player");
            var profile = new LocalPlayerProfile(playerId, 100);
            var round = WinningRoundFrom(profile);
            var checkpoint = round.CreateSettlementCheckpoint();

            store.Save(profile.ApplySettlementCheckpoint(checkpoint));
            var loaded = store.Load(playerId).Profile;
            var replayed = loaded.ApplySettlementCheckpoint(checkpoint);

            Assert.That(loaded.OfficialChipBalance, Is.EqualTo(110));
            Assert.That(replayed.OfficialChipBalance, Is.EqualTo(110));
            Assert.That(replayed.HasApplied(checkpoint.TransactionId), Is.True);
        }

        [Test]
        public void SettlementCheckpointRejectsMismatchedProfileBalance()
        {
            var profile = new LocalPlayerProfile(new PlayerId("local-player"), 95);
            var checkpoint = new RoundSettlementCheckpoint(
                new RoundId("round-1"),
                new TransactionId("round-1:settlement:checkpoint"),
                100,
                110);

            Assert.Throws<System.InvalidOperationException>(() => profile.ApplySettlementCheckpoint(checkpoint));
            Assert.That(profile.OfficialChipBalance, Is.EqualTo(95));
            Assert.That(profile.HasApplied(checkpoint.TransactionId), Is.False);
        }

        [Test]
        public void BlackjackRoundCannotCreateCheckpointBeforeIntermission()
        {
            var round = new BlackjackLocalRound(
                new RoundId("unfinished-round"),
                Rules,
                100,
                new[]
                {
                    C(CardRank.Ten),
                    C(CardRank.Six),
                    C(CardRank.Nine),
                    C(CardRank.Ten)
                });

            Assert.Throws<System.InvalidOperationException>(() => round.CreateSettlementCheckpoint());
        }

        [Test]
        public void BlackjackSettlementCheckpointAppliesRoundNetToProfile()
        {
            var profile = new LocalPlayerProfile(new PlayerId("local-player"), 100);
            var round = WinningRoundFrom(profile);
            var checkpoint = round.CreateSettlementCheckpoint();

            var settled = profile.ApplySettlementCheckpoint(checkpoint);

            Assert.That(checkpoint.StartingBalance, Is.EqualTo(100));
            Assert.That(checkpoint.FinalBalance, Is.EqualTo(110));
            Assert.That(checkpoint.NetChipDelta, Is.EqualTo(10));
            Assert.That(settled.OfficialChipBalance, Is.EqualTo(110));
            Assert.That(settled.LatestConfirmedRoundId.Value, Is.EqualTo(new RoundId("profile-win-round")));
        }

        private static BlackjackLocalRound WinningRoundFrom(LocalPlayerProfile profile)
        {
            var round = new BlackjackLocalRound(
                new RoundId("profile-win-round"),
                Rules,
                profile.OfficialChipBalance,
                new[]
                {
                    C(CardRank.Ten),
                    C(CardRank.Six),
                    C(CardRank.Nine),
                    C(CardRank.Ten),
                    C(CardRank.King)
                });

            Assert.That(round.PlaceBet(new ActionId("profile-win-round:bet"), 10).WasApplied, Is.True);
            Assert.That(round.AdvanceAutomatic(), Is.True);
            Assert.That(round.AdvanceAutomatic(), Is.True);
            Assert.That(round.Stand(new ActionId("profile-win-round:stand")).WasApplied, Is.True);
            Assert.That(round.AdvanceAutomatic(), Is.True);
            Assert.That(round.AdvanceAutomatic(), Is.True);
            Assert.That(round.Phase, Is.EqualTo(BlackjackRoundPhase.Intermission));
            Assert.That(round.Balance, Is.EqualTo(110));
            return round;
        }

        private static Card C(CardRank rank, CardSuit suit = CardSuit.Spades)
        {
            return new Card(suit, rank);
        }

        private sealed class MemoryProfileStore : ILocalPlayerProfileStore
        {
            private readonly Dictionary<PlayerId, LocalPlayerProfileSnapshot> snapshots =
                new Dictionary<PlayerId, LocalPlayerProfileSnapshot>();

            public ProfileLoadResult Load(PlayerId playerId)
            {
                if (!snapshots.TryGetValue(playerId, out var snapshot))
                {
                    return ProfileLoadResult.Missing();
                }

                return ProfileLoadResult.FoundProfile(LocalPlayerProfile.FromSnapshot(snapshot));
            }

            public void Save(LocalPlayerProfile profile)
            {
                snapshots[profile.PlayerId] = profile.ToSnapshot();
            }
        }
    }
}
