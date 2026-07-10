using System;
using System.Collections.Generic;
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

            var nextBalance = Balance + transaction.ChipDelta;
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
    }
}
