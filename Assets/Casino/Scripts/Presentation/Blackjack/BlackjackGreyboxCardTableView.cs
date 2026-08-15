using Casino.Blackjack;
using Casino.Core.Cards;
using UnityEngine;

namespace Casino.Presentation.Blackjack
{
    internal sealed class BlackjackGreyboxCardTableView
    {
        private const string DealerCardZonePath = "Blackjack_Greybox_Root/Dealer/Dealer_CardZone";
        private const string LocalPlayerCardZonePath = "Blackjack_Greybox_Root/Seats/Seat_0_Local/CardZone";
        private const string GeneratedRootName = "Greybox_Card_View";

        private readonly Transform dealerRoot;
        private readonly Transform playerRoot;
        private readonly Material faceMaterial;
        private readonly Material backMaterial;
        private BlackjackLocalRound renderedRound;
        private int renderedPlayerCardCount;
        private int renderedDealerCardCount;

        private BlackjackGreyboxCardTableView(Transform dealerRoot, Transform playerRoot)
        {
            this.dealerRoot = dealerRoot;
            this.playerRoot = playerRoot;
            faceMaterial = CreateMaterial("Greybox Card Face", new Color32(245, 241, 225, 255));
            backMaterial = CreateMaterial("Greybox Card Back", new Color32(49, 69, 104, 255));
        }

        public int VisiblePlayerCardCount { get; private set; }

        public int VisibleDealerCardCount { get; private set; }

        public int HiddenDealerCardCount { get; private set; }

        public static BlackjackGreyboxCardTableView TryFindLocalTable()
        {
            var dealerZone = GameObject.Find(DealerCardZonePath);
            var playerZone = GameObject.Find(LocalPlayerCardZonePath);
            if (dealerZone == null || playerZone == null)
            {
                return null;
            }

            return new BlackjackGreyboxCardTableView(
                FindOrCreateGeneratedRoot(dealerZone.transform),
                FindOrCreateGeneratedRoot(playerZone.transform));
        }

        public void Render(BlackjackLocalRound round)
        {
            var isSameRound = ReferenceEquals(renderedRound, round);
            var previousPlayerCardCount = isSameRound ? renderedPlayerCardCount : 0;
            var previousDealerCardCount = isSameRound ? renderedDealerCardCount : 0;
            ClearGeneratedCards(dealerRoot);
            ClearGeneratedCards(playerRoot);
            VisiblePlayerCardCount = 0;
            VisibleDealerCardCount = 0;
            HiddenDealerCardCount = 0;

            if (round == null || round.Phase == BlackjackRoundPhase.Betting)
            {
                renderedRound = round;
                renderedPlayerCardCount = 0;
                renderedDealerCardCount = 0;
                return;
            }

            var isInitialDeal = previousPlayerCardCount == 0 && previousDealerCardCount == 0;
            RenderDealerCards(round, previousDealerCardCount, isInitialDeal);
            RenderPlayerHands(round, previousPlayerCardCount, isInitialDeal);
            renderedRound = round;
            renderedPlayerCardCount = VisiblePlayerCardCount;
            renderedDealerCardCount = round.DealerCards.Count;
        }

        private void RenderDealerCards(BlackjackLocalRound round, int previousCardCount, bool isInitialDeal)
        {
            var revealHole = IsDealerHoleRevealed(round.Phase);
            for (var index = 0; index < round.DealerCards.Count; index++)
            {
                var isHiddenHole = index == 1 && !revealHole;
                RenderCard(
                    dealerRoot,
                    isHiddenHole ? "Dealer_Hole" : "Dealer_Card_" + index,
                    index,
                    0,
                    isHiddenHole ? null : round.DealerCards[index],
                    isHiddenHole,
                    false,
                    index >= previousCardCount,
                    isInitialDeal ? index * 0.12f + 0.06f : (index - previousCardCount) * 0.08f);

                if (isHiddenHole)
                {
                    HiddenDealerCardCount++;
                }
                else
                {
                    VisibleDealerCardCount++;
                }
            }
        }

        private void RenderPlayerHands(BlackjackLocalRound round, int previousCardCount, bool isInitialDeal)
        {
            var renderedCardIndex = 0;
            for (var handIndex = 0; handIndex < round.PlayerHands.Count; handIndex++)
            {
                var hand = round.PlayerHands[handIndex].Hand;
                var isActiveHand = round.Phase == BlackjackRoundPhase.PlayerTurns && handIndex == round.ActiveHandIndex;
                for (var cardIndex = 0; cardIndex < hand.Cards.Count; cardIndex++)
                {
                    RenderCard(
                        playerRoot,
                        "Player_Hand_" + handIndex + "_Card_" + cardIndex,
                        cardIndex,
                        handIndex,
                        hand.Cards[cardIndex],
                        false,
                        isActiveHand,
                        renderedCardIndex >= previousCardCount,
                        isInitialDeal ? cardIndex * 0.12f : (renderedCardIndex - previousCardCount) * 0.08f);
                    VisiblePlayerCardCount++;
                    renderedCardIndex++;
                }
            }
        }

        private void RenderCard(
            Transform parent,
            string objectName,
            int cardIndex,
            int handIndex,
            Card? card,
            bool isFaceDown,
            bool isActiveHand,
            bool animate,
            float animationDelay)
        {
            var cardObject = new GameObject(objectName);
            cardObject.name = objectName;
            cardObject.transform.SetParent(parent, false);
            cardObject.transform.localPosition = CardPosition(cardIndex, handIndex, isActiveHand);
            cardObject.transform.localRotation = Quaternion.Euler(0f, cardIndex * 2f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(cardObject.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.34f, 0.018f, 0.48f);

            var renderer = body.GetComponent<Renderer>();
            var material = isFaceDown ? backMaterial : faceMaterial;
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            AddLabel(cardObject.transform, isFaceDown ? "HOLE" : FormatCard(card.Value), isFaceDown ? Color.white : TextColor(card.Value));

            if (animate)
            {
                var destination = cardObject.transform.position;
                var motion = cardObject.AddComponent<BlackjackGreyboxCardMotion>();
                motion.Begin(GetDealSourceWorldPosition(), destination, animationDelay);
            }
        }

        private Vector3 GetDealSourceWorldPosition()
        {
            return dealerRoot.position + Vector3.right * 0.82f + Vector3.up * 0.34f;
        }

        private static void AddLabel(Transform parent, string label, Color color)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObject.transform.localScale = new Vector3(0.055f, 0.055f, 0.055f);

            var text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.7f;
            text.fontSize = 42;
            text.color = color;
        }

        private static Vector3 CardPosition(int cardIndex, int handIndex, bool isActiveHand)
        {
            var activeLift = isActiveHand ? 0.025f : 0f;
            return new Vector3(cardIndex * 0.42f, 0.03f + activeLift + handIndex * 0.01f, -handIndex * 0.58f);
        }

        private static Transform FindOrCreateGeneratedRoot(Transform zone)
        {
            var existing = zone.Find(GeneratedRootName);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(GeneratedRootName);
            root.transform.SetParent(zone, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root.transform;
        }

        private static void ClearGeneratedCards(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index);
                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static bool IsDealerHoleRevealed(BlackjackRoundPhase phase)
        {
            return phase == BlackjackRoundPhase.DealerTurn
                || phase == BlackjackRoundPhase.Settlement
                || phase == BlackjackRoundPhase.Intermission;
        }

        private static string FormatCard(Card card)
        {
            return FormatRank(card.Rank) + FormatSuit(card.Suit);
        }

        private static string FormatRank(CardRank rank)
        {
            switch (rank)
            {
                case CardRank.Ace:
                    return "A";
                case CardRank.Jack:
                    return "J";
                case CardRank.Queen:
                    return "Q";
                case CardRank.King:
                    return "K";
                default:
                    return ((int)rank).ToString();
            }
        }

        private static string FormatSuit(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Clubs:
                    return "C";
                case CardSuit.Diamonds:
                    return "D";
                case CardSuit.Hearts:
                    return "H";
                case CardSuit.Spades:
                    return "S";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(suit));
            }
        }

        private static Color TextColor(Card card)
        {
            return card.Suit == CardSuit.Diamonds || card.Suit == CardSuit.Hearts
                ? new Color32(166, 45, 42, 255)
                : new Color32(24, 28, 31, 255);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = name,
                color = color
            };
            return material;
        }
    }
}
