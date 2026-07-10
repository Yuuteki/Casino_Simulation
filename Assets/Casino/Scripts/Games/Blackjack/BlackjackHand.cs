using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Casino.Core.Cards;

namespace Casino.Blackjack
{
    public sealed class BlackjackHand
    {
        private readonly Card[] cards;
        private readonly ReadOnlyCollection<Card> readOnlyCards;

        public BlackjackHand(IEnumerable<Card> cards, bool isFromSplit = false, bool wasSplitFromAces = false)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var copiedCards = new List<Card>(cards);
            if (copiedCards.Count == 0)
            {
                throw new ArgumentException("A blackjack hand must contain at least one card.", nameof(cards));
            }

            this.cards = copiedCards.ToArray();
            readOnlyCards = new ReadOnlyCollection<Card>(this.cards);
            IsFromSplit = isFromSplit;
            WasSplitFromAces = wasSplitFromAces;
        }

        public IReadOnlyList<Card> Cards => readOnlyCards;

        public int Count => cards.Length;

        public bool IsFromSplit { get; }

        public bool WasSplitFromAces { get; }

        public BlackjackHandEvaluation Evaluation => BlackjackHandEvaluator.Evaluate(this);

        public BlackjackHand AddCard(Card card)
        {
            var nextCards = new Card[cards.Length + 1];
            Array.Copy(cards, nextCards, cards.Length);
            nextCards[nextCards.Length - 1] = card;
            return new BlackjackHand(nextCards, IsFromSplit, WasSplitFromAces);
        }

        public static BlackjackHand FromCards(params Card[] cards)
        {
            return new BlackjackHand(cards);
        }

        public static BlackjackHand FromSplit(params Card[] cards)
        {
            return new BlackjackHand(cards, true);
        }

        public static BlackjackHand FromSplitAces(params Card[] cards)
        {
            return new BlackjackHand(cards, true, true);
        }
    }

    public readonly struct BlackjackHandEvaluation
    {
        public BlackjackHandEvaluation(
            int hardTotal,
            int total,
            int softAceCount,
            bool isNaturalBlackjack)
        {
            HardTotal = hardTotal;
            Total = total;
            SoftAceCount = softAceCount;
            IsNaturalBlackjack = isNaturalBlackjack;
        }

        public int HardTotal { get; }

        public int Total { get; }

        public int SoftAceCount { get; }

        public bool IsSoft => SoftAceCount > 0;

        public bool IsBust => Total > 21;

        public bool IsTwentyOne => Total == 21;

        public bool IsNaturalBlackjack { get; }
    }

    public static class BlackjackHandEvaluator
    {
        public static BlackjackHandEvaluation Evaluate(BlackjackHand hand)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand));
            }

            var hardTotal = 0;
            var aceCount = 0;
            var hasTenValue = false;

            for (var index = 0; index < hand.Cards.Count; index++)
            {
                var card = hand.Cards[index];
                if (card.Rank == CardRank.Ace)
                {
                    aceCount++;
                    hardTotal += 1;
                }
                else
                {
                    hardTotal += card.BlackjackValue;
                    hasTenValue |= card.IsTenValue;
                }
            }

            var total = hardTotal;
            var softAceCount = 0;
            for (var index = 0; index < aceCount; index++)
            {
                if (total + 10 > 21)
                {
                    break;
                }

                total += 10;
                softAceCount++;
            }

            var isNaturalBlackjack = !hand.IsFromSplit
                && hand.Count == 2
                && aceCount == 1
                && hasTenValue
                && total == 21;

            return new BlackjackHandEvaluation(hardTotal, total, softAceCount, isNaturalBlackjack);
        }
    }
}
