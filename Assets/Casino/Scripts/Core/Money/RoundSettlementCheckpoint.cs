using System;
using Casino.Core.Identifiers;

namespace Casino.Core.Money
{
    public readonly struct RoundSettlementCheckpoint
    {
        public RoundSettlementCheckpoint(
            RoundId roundId,
            TransactionId transactionId,
            int startingBalance,
            int finalBalance)
        {
            if (startingBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingBalance));
            }

            if (finalBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finalBalance));
            }

            RoundId = roundId;
            TransactionId = transactionId;
            StartingBalance = startingBalance;
            FinalBalance = finalBalance;
            NetChipDelta = checked(finalBalance - startingBalance);
        }

        public RoundId RoundId { get; }

        public TransactionId TransactionId { get; }

        public int StartingBalance { get; }

        public int FinalBalance { get; }

        public int NetChipDelta { get; }

        public ChipTransaction ToTransaction()
        {
            return new ChipTransaction(TransactionId, NetChipDelta);
        }
    }
}
