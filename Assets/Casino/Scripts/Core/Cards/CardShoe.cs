using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Casino.Core.Randomness;

namespace Casino.Core.Cards
{
    public sealed class CardShoe
    {
        public const int StandardBlackjackDeckCount = 6;
        public const int CardsPerDeck = 52;

        private readonly List<Card> cards;
        private int nextCardIndex;

        private CardShoe(List<Card> cards, int deckCount)
        {
            this.cards = cards ?? throw new ArgumentNullException(nameof(cards));
            DeckCount = deckCount;
            InitialCardCount = cards.Count;
        }

        public int DeckCount { get; }

        public int InitialCardCount { get; }

        public int RemainingCount => cards.Count - nextCardIndex;

        public bool IsEmpty => RemainingCount == 0;

        public static CardShoe CreateSixDeckShoe(IRandomSource randomSource)
        {
            return CreateShuffled(StandardBlackjackDeckCount, randomSource);
        }

        public static CardShoe CreateShuffled(int deckCount, IRandomSource randomSource)
        {
            if (deckCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deckCount), "Deck count must be positive.");
            }

            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            var cards = new List<Card>(deckCount * CardsPerDeck);
            var standardDeck = StandardDeck.Create();

            for (var deckIndex = 0; deckIndex < deckCount; deckIndex++)
            {
                for (var cardIndex = 0; cardIndex < standardDeck.Count; cardIndex++)
                {
                    cards.Add(standardDeck[cardIndex]);
                }
            }

            FisherYatesShuffle.Shuffle(cards, randomSource);
            return new CardShoe(cards, deckCount);
        }

        public Card Deal()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Cannot deal from an empty shoe.");
            }

            var card = cards[nextCardIndex];
            nextCardIndex++;
            return card;
        }

        public IReadOnlyList<Card> SnapshotRemainingCards()
        {
            return new ReadOnlyCollection<Card>(cards.GetRange(nextCardIndex, RemainingCount));
        }

        public bool ShouldShuffleBeforeNextRound(decimal remainingThreshold)
        {
            if (remainingThreshold < 0m || remainingThreshold > 1m)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingThreshold));
            }

            return RemainingCount < InitialCardCount * remainingThreshold;
        }
    }
}
