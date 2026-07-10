using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Casino.Core.Cards
{
    public enum CardSuit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3
    }

    public enum CardRank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }

    public readonly struct Card : IEquatable<Card>
    {
        public Card(CardSuit suit, CardRank rank)
        {
            if (!Enum.IsDefined(typeof(CardSuit), suit))
            {
                throw new ArgumentOutOfRangeException(nameof(suit));
            }

            if (!Enum.IsDefined(typeof(CardRank), rank))
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            Suit = suit;
            Rank = rank;
        }

        public CardSuit Suit { get; }

        public CardRank Rank { get; }

        public int BlackjackValue => CardValues.GetBlackjackValue(Rank);

        public bool IsAce => Rank == CardRank.Ace;

        public bool IsTenValue => BlackjackValue == 10;

        public bool Equals(Card other)
        {
            return Suit == other.Suit && Rank == other.Rank;
        }

        public override bool Equals(object obj)
        {
            return obj is Card other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)Suit * 397) ^ (int)Rank;
        }

        public override string ToString()
        {
            return Rank + " of " + Suit;
        }

        public static bool operator ==(Card left, Card right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Card left, Card right)
        {
            return !left.Equals(right);
        }
    }

    public static class CardValues
    {
        public static int GetBlackjackValue(CardRank rank)
        {
            if (!Enum.IsDefined(typeof(CardRank), rank))
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            if (rank == CardRank.Ace)
            {
                return 11;
            }

            return rank >= CardRank.Ten ? 10 : (int)rank;
        }

        public static bool HaveSameBlackjackSplitValue(Card left, Card right)
        {
            return GetBlackjackValue(left.Rank) == GetBlackjackValue(right.Rank);
        }
    }

    public static class StandardDeck
    {
        private static readonly ReadOnlyCollection<CardSuit> Suits =
            new ReadOnlyCollection<CardSuit>((CardSuit[])Enum.GetValues(typeof(CardSuit)));

        private static readonly ReadOnlyCollection<CardRank> Ranks =
            new ReadOnlyCollection<CardRank>((CardRank[])Enum.GetValues(typeof(CardRank)));

        public static IReadOnlyList<Card> Create()
        {
            var cards = new List<Card>(52);

            for (var suitIndex = 0; suitIndex < Suits.Count; suitIndex++)
            {
                for (var rankIndex = 0; rankIndex < Ranks.Count; rankIndex++)
                {
                    cards.Add(new Card(Suits[suitIndex], Ranks[rankIndex]));
                }
            }

            return new ReadOnlyCollection<Card>(cards);
        }
    }
}
