using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Casino.Core.Identifiers;
using Casino.Core.Money;

namespace Casino.Persistence
{
    public sealed class LocalPlayerProfile
    {
        private readonly ChipLedger ledger;

        public LocalPlayerProfile(PlayerId playerId, int startingBalance)
            : this(playerId, new ChipLedger(startingBalance), null)
        {
        }

        private LocalPlayerProfile(PlayerId playerId, ChipLedger ledger, RoundId? latestConfirmedRoundId)
        {
            PlayerId = playerId;
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            LatestConfirmedRoundId = latestConfirmedRoundId;
        }

        public PlayerId PlayerId { get; }

        public int OfficialChipBalance => ledger.Balance;

        public RoundId? LatestConfirmedRoundId { get; }

        public IReadOnlyCollection<TransactionId> AppliedTransactionIds => ledger.AppliedTransactionIds;

        public bool HasApplied(TransactionId transactionId)
        {
            return ledger.HasApplied(transactionId);
        }

        public LocalPlayerProfile ApplyTransaction(ChipTransaction transaction)
        {
            var nextLedger = ledger.Apply(transaction, out var applied);
            if (!applied)
            {
                return this;
            }

            return new LocalPlayerProfile(PlayerId, nextLedger, LatestConfirmedRoundId);
        }

        public LocalPlayerProfile ApplySettlementCheckpoint(RoundSettlementCheckpoint checkpoint)
        {
            if (HasApplied(checkpoint.TransactionId))
            {
                return this;
            }

            if (OfficialChipBalance != checkpoint.StartingBalance)
            {
                throw new InvalidOperationException("Settlement checkpoint does not match the current profile balance.");
            }

            var nextLedger = ledger.Apply(checkpoint.ToTransaction(), out _);
            return new LocalPlayerProfile(PlayerId, nextLedger, checkpoint.RoundId);
        }

        public LocalPlayerProfileSnapshot ToSnapshot()
        {
            var appliedTransactionIds = new List<string>();
            foreach (var transactionId in AppliedTransactionIds)
            {
                appliedTransactionIds.Add(transactionId.ToString());
            }

            appliedTransactionIds.Sort(StringComparer.Ordinal);
            return new LocalPlayerProfileSnapshot(
                PlayerId.ToString(),
                OfficialChipBalance,
                LatestConfirmedRoundId.HasValue ? LatestConfirmedRoundId.Value.ToString() : string.Empty,
                appliedTransactionIds);
        }

        public static LocalPlayerProfile FromSnapshot(LocalPlayerProfileSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var appliedTransactionIds = new List<TransactionId>();
            foreach (var transactionId in snapshot.AppliedTransactionIds)
            {
                appliedTransactionIds.Add(new TransactionId(transactionId));
            }

            RoundId? latestConfirmedRoundId = null;
            if (!string.IsNullOrWhiteSpace(snapshot.LatestConfirmedRoundId))
            {
                latestConfirmedRoundId = new RoundId(snapshot.LatestConfirmedRoundId);
            }

            return new LocalPlayerProfile(
                new PlayerId(snapshot.PlayerId),
                new ChipLedger(snapshot.OfficialChipBalance, appliedTransactionIds),
                latestConfirmedRoundId);
        }
    }

    public sealed class LocalPlayerProfileSnapshot
    {
        private readonly ReadOnlyCollection<string> appliedTransactionIds;

        public LocalPlayerProfileSnapshot(
            string playerId,
            int officialChipBalance,
            string latestConfirmedRoundId,
            IEnumerable<string> appliedTransactionIds)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException("Player id cannot be empty.", nameof(playerId));
            }

            if (officialChipBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(officialChipBalance));
            }

            if (appliedTransactionIds == null)
            {
                throw new ArgumentNullException(nameof(appliedTransactionIds));
            }

            var copiedTransactionIds = new List<string>();
            foreach (var transactionId in appliedTransactionIds)
            {
                if (string.IsNullOrWhiteSpace(transactionId))
                {
                    throw new ArgumentException("Applied transaction id cannot be empty.", nameof(appliedTransactionIds));
                }

                copiedTransactionIds.Add(transactionId);
            }

            PlayerId = playerId;
            OfficialChipBalance = officialChipBalance;
            LatestConfirmedRoundId = latestConfirmedRoundId ?? string.Empty;
            this.appliedTransactionIds = new ReadOnlyCollection<string>(copiedTransactionIds);
        }

        public string PlayerId { get; }

        public int OfficialChipBalance { get; }

        public string LatestConfirmedRoundId { get; }

        public IReadOnlyList<string> AppliedTransactionIds => appliedTransactionIds;
    }
}
