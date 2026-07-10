using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Casino.Blackjack
{
    public enum BlackjackMainBetOutcome
    {
        PlayerBust,
        DealerBust,
        PlayerWin,
        PlayerLoss,
        Push,
        PlayerBlackjack
    }

    public enum BlackjackInsuranceOutcome
    {
        NoBet,
        Won,
        Lost
    }

    public readonly struct BlackjackHandBet
    {
        public BlackjackHandBet(BlackjackHand hand, int wager)
        {
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));

            if (wager <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wager));
            }

            Wager = wager;
        }

        public BlackjackHand Hand { get; }

        public int Wager { get; }
    }

    public readonly struct MainBetSettlement
    {
        public MainBetSettlement(BlackjackMainBetOutcome outcome, int wager, int netChips, int payoutChips)
        {
            Outcome = outcome;
            Wager = wager;
            NetChips = netChips;
            PayoutChips = payoutChips;
        }

        public BlackjackMainBetOutcome Outcome { get; }

        public int Wager { get; }

        public int NetChips { get; }

        public int PayoutChips { get; }
    }

    public readonly struct InsuranceBetSettlement
    {
        public InsuranceBetSettlement(BlackjackInsuranceOutcome outcome, int wager, int netChips, int payoutChips)
        {
            Outcome = outcome;
            Wager = wager;
            NetChips = netChips;
            PayoutChips = payoutChips;
        }

        public BlackjackInsuranceOutcome Outcome { get; }

        public int Wager { get; }

        public int NetChips { get; }

        public int PayoutChips { get; }
    }

    public static class BlackjackSettlementCalculator
    {
        public static MainBetSettlement SettleMainBet(
            BlackjackHand playerHand,
            BlackjackHand dealerHand,
            int wager,
            BlackjackRules rules)
        {
            if (playerHand == null)
            {
                throw new ArgumentNullException(nameof(playerHand));
            }

            if (dealerHand == null)
            {
                throw new ArgumentNullException(nameof(dealerHand));
            }

            if (wager <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wager));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var player = BlackjackHandEvaluator.Evaluate(playerHand);
            var dealer = BlackjackHandEvaluator.Evaluate(dealerHand);

            if (player.IsBust)
            {
                return new MainBetSettlement(BlackjackMainBetOutcome.PlayerBust, wager, -wager, 0);
            }

            if (player.IsNaturalBlackjack && dealer.IsNaturalBlackjack)
            {
                return Push(wager);
            }

            if (player.IsNaturalBlackjack)
            {
                var profit = rules.BlackjackPayout.ApplyTo(wager);
                return new MainBetSettlement(BlackjackMainBetOutcome.PlayerBlackjack, wager, profit, wager + profit);
            }

            if (dealer.IsNaturalBlackjack)
            {
                return new MainBetSettlement(BlackjackMainBetOutcome.PlayerLoss, wager, -wager, 0);
            }

            if (dealer.IsBust)
            {
                return Win(BlackjackMainBetOutcome.DealerBust, wager, rules);
            }

            if (player.Total > dealer.Total)
            {
                return Win(BlackjackMainBetOutcome.PlayerWin, wager, rules);
            }

            if (player.Total < dealer.Total)
            {
                return new MainBetSettlement(BlackjackMainBetOutcome.PlayerLoss, wager, -wager, 0);
            }

            return Push(wager);
        }

        public static IReadOnlyList<MainBetSettlement> SettleMainBets(
            IEnumerable<BlackjackHandBet> playerHands,
            BlackjackHand dealerHand,
            BlackjackRules rules)
        {
            if (playerHands == null)
            {
                throw new ArgumentNullException(nameof(playerHands));
            }

            var settlements = new List<MainBetSettlement>();
            foreach (var playerHand in playerHands)
            {
                settlements.Add(SettleMainBet(playerHand.Hand, dealerHand, playerHand.Wager, rules));
            }

            return new ReadOnlyCollection<MainBetSettlement>(settlements);
        }

        public static InsuranceBetSettlement SettleInsurance(
            bool dealerHasNaturalBlackjack,
            int insuranceWager,
            BlackjackRules rules)
        {
            if (insuranceWager < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(insuranceWager));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (insuranceWager == 0)
            {
                return new InsuranceBetSettlement(BlackjackInsuranceOutcome.NoBet, 0, 0, 0);
            }

            if (!dealerHasNaturalBlackjack)
            {
                return new InsuranceBetSettlement(
                    BlackjackInsuranceOutcome.Lost,
                    insuranceWager,
                    -insuranceWager,
                    0);
            }

            var profit = rules.InsurancePayout.ApplyTo(insuranceWager);
            return new InsuranceBetSettlement(
                BlackjackInsuranceOutcome.Won,
                insuranceWager,
                profit,
                insuranceWager + profit);
        }

        private static MainBetSettlement Win(BlackjackMainBetOutcome outcome, int wager, BlackjackRules rules)
        {
            var profit = rules.StandardWinPayout.ApplyTo(wager);
            return new MainBetSettlement(outcome, wager, profit, wager + profit);
        }

        private static MainBetSettlement Push(int wager)
        {
            return new MainBetSettlement(BlackjackMainBetOutcome.Push, wager, 0, wager);
        }
    }
}
