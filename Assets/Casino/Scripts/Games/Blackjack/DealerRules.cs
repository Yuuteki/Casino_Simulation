using System;

namespace Casino.Blackjack
{
    public static class DealerRules
    {
        public static bool ShouldHit(BlackjackHand dealerHand, BlackjackRules rules)
        {
            if (dealerHand == null)
            {
                throw new ArgumentNullException(nameof(dealerHand));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var evaluation = BlackjackHandEvaluator.Evaluate(dealerHand);
            if (evaluation.IsBust)
            {
                return false;
            }

            if (evaluation.Total < 17)
            {
                return true;
            }

            if (evaluation.Total > 17)
            {
                return false;
            }

            return evaluation.IsSoft && rules.DealerHitsSoft17;
        }
    }
}
