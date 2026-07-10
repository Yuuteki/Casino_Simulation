using System;

namespace Casino.Core.Identifiers
{
    public readonly struct RoundId : IEquatable<RoundId>
    {
        public RoundId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identifier cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool Equals(RoundId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RoundId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public readonly struct TransactionId : IEquatable<TransactionId>
    {
        public TransactionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identifier cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool Equals(TransactionId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TransactionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
