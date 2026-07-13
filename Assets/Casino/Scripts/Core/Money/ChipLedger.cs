using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Casino.Core.Identifiers;

namespace Casino.Core.Money
{
    public readonly struct ChipTransaction
    {
        public ChipTransaction(TransactionId id, int chipDelta)
        {
            Id = id;
            ChipDelta = chipDelta;
        }

        public TransactionId Id { get; }

        public int ChipDelta { get; }
    }

    public sealed class ChipLedger
    {
        private readonly HashSet<TransactionId> appliedTransactions;

        public ChipLedger(int startingBalance)
            : this(startingBalance, new HashSet<TransactionId>())
        {
        }

        public ChipLedger(int startingBalance, IEnumerable<TransactionId> appliedTransactions)
            : this(startingBalance, CopyAppliedTransactions(appliedTransactions))
        {
        }

        private ChipLedger(int balance, HashSet<TransactionId> appliedTransactions)
        {
            if (balance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(balance), "Chip balance cannot be negative.");
            }

            Balance = balance;
            this.appliedTransactions = appliedTransactions;
        }

        public int Balance { get; }

        public IReadOnlyCollection<TransactionId> AppliedTransactionIds
        {
            get
            {
                return new ReadOnlyCollection<TransactionId>(new List<TransactionId>(appliedTransactions));
            }
        }

        public bool HasApplied(TransactionId transactionId)
        {
            return appliedTransactions.Contains(transactionId);
        }

        public ChipLedger Apply(ChipTransaction transaction, out bool applied)
        {
            if (appliedTransactions.Contains(transaction.Id))
            {
                applied = false;
                return this;
            }

            var nextBalance = checked(Balance + transaction.ChipDelta);
            if (nextBalance < 0)
            {
                throw new InvalidOperationException("Transaction would make chip balance negative.");
            }

            var nextAppliedTransactions = new HashSet<TransactionId>(appliedTransactions)
            {
                transaction.Id
            };

            applied = true;
            return new ChipLedger(nextBalance, nextAppliedTransactions);
        }

        private static HashSet<TransactionId> CopyAppliedTransactions(IEnumerable<TransactionId> appliedTransactions)
        {
            if (appliedTransactions == null)
            {
                throw new ArgumentNullException(nameof(appliedTransactions));
            }

            return new HashSet<TransactionId>(appliedTransactions);
        }
    }
}
