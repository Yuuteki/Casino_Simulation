using System;
using System.Text;
using Casino.Core.Identifiers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Casino.Presentation.Blackjack
{
    public sealed class BlackjackGreyboxRuntimeBootstrap
    {
        private const string GreyboxSceneName = "BlackjackGreybox";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGreyboxScreen()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, GreyboxSceneName, StringComparison.Ordinal))
            {
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<BlackjackGreyboxScreen>() != null)
            {
                return;
            }

            var screenObject = new GameObject("Blackjack Greybox Screen");
            UnityEngine.Object.DontDestroyOnLoad(screenObject);
            screenObject.AddComponent<BlackjackGreyboxScreen>();
        }
    }

    public sealed class BlackjackGreyboxScreen : MonoBehaviour
    {
        [SerializeField] private int startingBalance = BlackjackGreyboxTableSession.DefaultStartingBalance;
        [SerializeField] private int minimumWager = BlackjackGreyboxTableSession.DefaultMinimumWager;
        [SerializeField] private int maximumWager = BlackjackGreyboxTableSession.DefaultMaximumWager;
        [SerializeField] private int defaultWager = BlackjackGreyboxTableSession.DefaultWager;
        [SerializeField] private int randomSeed = 24681357;

        private BlackjackGreyboxTableSession session;
        private Font defaultFont;
        private RectTransform canvasRoot;
        private Text headerText;
        private Text statusText;
        private Text dealerText;
        private Text playerText;
        private Text settlementText;
        private Text wagerText;
        private Text logText;
        private Button lowerWagerButton;
        private Button raiseWagerButton;
        private Button dealButton;
        private Button hitButton;
        private Button standButton;
        private Button doubleButton;
        private Button splitButton;
        private Button buyInsuranceButton;
        private Button declineInsuranceButton;
        private Button aiFinishButton;
        private Button runHundredButton;
        private BlackjackGreyboxChipStackView chipStackView;
        private BlackjackGreyboxCardTableView cardTableView;
        private int selectedWager;
        private bool initialized;

        public BlackjackGreyboxTableSession Session => session;

        public Button DealButton => dealButton;

        public Button HitButton => hitButton;

        public Button StandButton => standButton;

        public Button DoubleButton => doubleButton;

        public Button SplitButton => splitButton;

        public Button BuyInsuranceButton => buyInsuranceButton;

        public Button DeclineInsuranceButton => declineInsuranceButton;

        public Button RunHundredButton => runHundredButton;

        public Button RaiseWagerButton => raiseWagerButton;

        public Button LowerWagerButton => lowerWagerButton;

        public int VisibleWagerChipCount => chipStackView != null ? chipStackView.VisibleChipCount : 0;

        public int VisibleTablePlayerCardCount => cardTableView != null ? cardTableView.VisiblePlayerCardCount : 0;

        public int VisibleTableDealerCardCount => cardTableView != null ? cardTableView.VisibleDealerCardCount : 0;

        public int HiddenTableDealerCardCount => cardTableView != null ? cardTableView.HiddenDealerCardCount : 0;

        private void Awake()
        {
            Initialize();
        }

        public void ConfigureStartup(int newStartingBalance, int newMinimumWager, int newMaximumWager, int newDefaultWager, int newRandomSeed)
        {
            if (initialized)
            {
                throw new InvalidOperationException("Greybox screen startup values can only be configured before initialization.");
            }

            if (newStartingBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newStartingBalance));
            }

            if (newMinimumWager <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newMinimumWager));
            }

            if (newMaximumWager > 0 && newMaximumWager < newMinimumWager)
            {
                throw new ArgumentOutOfRangeException(nameof(newMaximumWager));
            }

            if (newDefaultWager < newMinimumWager)
            {
                throw new ArgumentOutOfRangeException(nameof(newDefaultWager));
            }

            startingBalance = newStartingBalance;
            minimumWager = newMinimumWager;
            maximumWager = newMaximumWager;
            defaultWager = newDefaultWager;
            randomSeed = newRandomSeed;
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            selectedWager = defaultWager;
            defaultFont = LoadDefaultFont();
            session = new BlackjackGreyboxTableSession(
                new PlayerId("local-player"),
                startingBalance,
                Casino.Blackjack.BlackjackRules.Standard,
                minimumWager,
                maximumWager,
                defaultWager,
                randomSeed);

            EnsureEventSystem();
            BuildInterface();
            chipStackView = BlackjackGreyboxChipStackView.TryFindLocalPlayerStack();
            cardTableView = BlackjackGreyboxCardTableView.TryFindLocalTable();
            Refresh();
        }

        public void Refresh()
        {
            if (!initialized || session == null)
            {
                return;
            }

            var round = session.Round;
            headerText.text = session.BuildRoundSummary();
            statusText.text = session.StatusMessage;
            dealerText.text = BlackjackGreyboxFormatting.FormatDealer(round);
            playerText.text = BlackjackGreyboxFormatting.FormatPlayerHands(round);
            settlementText.text = BuildSettlementText();
            wagerText.text = "下注: " + selectedWager;
            logText.text = BuildLogText();

            var legal = session.LegalActions;
            var canAdjustWager = round != null && round.Phase == Casino.Blackjack.BlackjackRoundPhase.Betting;
            var wagerCap = GetCurrentWagerCap();
            lowerWagerButton.interactable = canAdjustWager && selectedWager > session.MinimumWager;
            raiseWagerButton.interactable = canAdjustWager && selectedWager + 10 <= wagerCap;
            dealButton.interactable = session.CanDeal
                && selectedWager >= session.MinimumWager
                && selectedWager <= wagerCap;

            hitButton.interactable = legal.CanHit;
            standButton.interactable = legal.CanStand;
            doubleButton.interactable = legal.CanDoubleDown;
            splitButton.interactable = legal.CanSplit;
            buyInsuranceButton.interactable = legal.CanBuyInsurance;
            declineInsuranceButton.interactable = CanDeclineInsurance();
            aiFinishButton.interactable = round != null
                && (round.Phase != Casino.Blackjack.BlackjackRoundPhase.Intermission || session.Profile.OfficialChipBalance >= session.MinimumWager);
            runHundredButton.interactable = session.Profile.OfficialChipBalance >= session.MinimumWager;

            SetButtonLabel(dealButton, round != null && round.Phase == Casino.Blackjack.BlackjackRoundPhase.Intermission ? "下一局" : "发牌");
            chipStackView?.SetWager(GetVisibleTableWager(round), session.MinimumWager);
            cardTableView?.Render(round);
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("Blackjack Greybox Canvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasRoot = canvasObject.GetComponent<RectTransform>();

            var topPanel = AddPanel(
                "Status Panel",
                canvasRoot,
                new Vector2(0.02f, 0.84f),
                new Vector2(0.98f, 0.98f),
                Vector2.zero,
                Vector2.zero,
                new Color32(32, 47, 51, 210));
            AddVerticalLayout(topPanel, 12, 6);
            headerText = AddText("Header", topPanel, 24, TextAnchor.MiddleLeft, new Color32(239, 234, 215, 255));
            statusText = AddText("Status", topPanel, 20, TextAnchor.MiddleLeft, new Color32(241, 191, 94, 255));

            var tablePanel = AddPanel(
                "Table Panel",
                canvasRoot,
                new Vector2(0.02f, 0.32f),
                new Vector2(0.39f, 0.82f),
                Vector2.zero,
                Vector2.zero,
                new Color32(24, 89, 69, 185));
            AddVerticalLayout(tablePanel, 12, 8);
            dealerText = AddText("Dealer", tablePanel, 24, TextAnchor.MiddleLeft, new Color32(255, 248, 221, 255));
            playerText = AddText("Player", tablePanel, 22, TextAnchor.MiddleLeft, new Color32(226, 239, 235, 255));
            settlementText = AddText("Settlement", tablePanel, 18, TextAnchor.MiddleLeft, new Color32(241, 191, 94, 255));

            var actionPanel = AddPanel(
                "Action Panel",
                canvasRoot,
                new Vector2(0.02f, 0.02f),
                new Vector2(0.98f, 0.24f),
                Vector2.zero,
                Vector2.zero,
                new Color32(38, 38, 45, 220));
            AddVerticalLayout(actionPanel, 12, 8);

            var wagerRow = AddRow("Wager Row", actionPanel);
            lowerWagerButton = AddButton("Lower Wager", wagerRow, "-10", OnLowerWager);
            wagerText = AddText("Wager Text", wagerRow, 22, TextAnchor.MiddleCenter, new Color32(239, 234, 215, 255));
            raiseWagerButton = AddButton("Raise Wager", wagerRow, "+10", OnRaiseWager);
            dealButton = AddButton("Deal Button", wagerRow, "发牌", OnDeal);

            var actionRow = AddRow("Player Action Row", actionPanel);
            hitButton = AddButton("Hit Button", actionRow, "要牌", OnHit);
            standButton = AddButton("Stand Button", actionRow, "停牌", OnStand);
            doubleButton = AddButton("Double Button", actionRow, "加倍", OnDouble);
            splitButton = AddButton("Split Button", actionRow, "分牌", OnSplit);
            buyInsuranceButton = AddButton("Insurance Button", actionRow, "保险", OnBuyInsurance);
            declineInsuranceButton = AddButton("No Insurance Button", actionRow, "不保险", OnDeclineInsurance);

            var debugRow = AddRow("Debug Row", actionPanel);
            aiFinishButton = AddButton("AI Finish Button", debugRow, "AI 打完", OnAiFinish);
            runHundredButton = AddButton("Run 100 Button", debugRow, "调试 100 局", OnRunHundred);
            logText = AddText("Debug Log", debugRow, 16, TextAnchor.MiddleLeft, new Color32(203, 213, 205, 255));
        }

        private string BuildSettlementText()
        {
            var round = session.Round;
            if (round == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append(BlackjackGreyboxFormatting.FormatInsurance(round));
            if (round.Phase == Casino.Blackjack.BlackjackRoundPhase.Intermission)
            {
                builder.Append(" | Round delta: ");
                builder.Append(session.LastSettlementDelta >= 0 ? "+" : string.Empty);
                builder.Append(session.LastSettlementDelta);
                builder.Append(" | Press Deal Next for another round.");
            }

            return builder.ToString();
        }

        private string BuildLogText()
        {
            var log = session.DebugLog;
            var builder = new StringBuilder();
            var first = Math.Max(0, log.Count - 5);
            for (var index = first; index < log.Count; index++)
            {
                if (index > first)
                {
                    builder.AppendLine();
                }

                builder.Append(log[index]);
            }

            return builder.ToString();
        }

        private bool CanDeclineInsurance()
        {
            var round = session.Round;
            return round != null
                && round.Phase == Casino.Blackjack.BlackjackRoundPhase.InsuranceOrDealerPeek
                && round.DealerUpCard.Rank == Casino.Core.Cards.CardRank.Ace;
        }

        private int GetCurrentWagerCap()
        {
            var balance = session.Round != null ? session.Round.Balance : session.Profile.OfficialChipBalance;
            var cap = session.MaximumWager > 0 ? Math.Min(session.MaximumWager, balance) : balance;
            return Math.Max(session.MinimumWager, cap);
        }

        private int GetVisibleTableWager(Casino.Blackjack.BlackjackLocalRound round)
        {
            if (round == null)
            {
                return 0;
            }

            if (round.Phase == Casino.Blackjack.BlackjackRoundPhase.Betting)
            {
                return Math.Min(selectedWager, GetCurrentWagerCap());
            }

            var totalWager = 0;
            for (var index = 0; index < round.PlayerHands.Count; index++)
            {
                totalWager += round.PlayerHands[index].Wager;
            }

            return totalWager > 0 ? totalWager : selectedWager;
        }

        private void OnLowerWager()
        {
            selectedWager = Math.Max(session.MinimumWager, selectedWager - 10);
            Refresh();
        }

        private void OnRaiseWager()
        {
            selectedWager = Math.Min(GetCurrentWagerCap(), selectedWager + 10);
            Refresh();
        }

        private void OnDeal()
        {
            session.PlaceBet(selectedWager);
            selectedWager = Math.Min(selectedWager, GetCurrentWagerCap());
            Refresh();
        }

        private void OnHit()
        {
            session.Hit();
            Refresh();
        }

        private void OnStand()
        {
            session.Stand();
            Refresh();
        }

        private void OnDouble()
        {
            session.DoubleDown();
            Refresh();
        }

        private void OnSplit()
        {
            session.Split();
            Refresh();
        }

        private void OnBuyInsurance()
        {
            session.BuyInsurance();
            Refresh();
        }

        private void OnDeclineInsurance()
        {
            session.DeclineInsurance();
            Refresh();
        }

        private void OnAiFinish()
        {
            session.AutoPlayCurrentRound();
            selectedWager = Math.Min(selectedWager, GetCurrentWagerCap());
            Refresh();
        }

        private void OnRunHundred()
        {
            session.RunAiRounds(100);
            selectedWager = Math.Min(selectedWager, GetCurrentWagerCap());
            Refresh();
        }

        private RectTransform AddPanel(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var panelObject = new GameObject(objectName);
            panelObject.transform.SetParent(parent, false);
            var rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = panelObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private RectTransform AddRow(string objectName, Transform parent)
        {
            var rowObject = new GameObject(objectName);
            rowObject.transform.SetParent(parent, false);
            var rect = rowObject.AddComponent<RectTransform>();
            var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            rowObject.AddComponent<LayoutElement>().preferredHeight = 50;
            return rect;
        }

        private void AddVerticalLayout(Transform target, int padding, int spacing)
        {
            var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        private Text AddText(string objectName, Transform parent, int fontSize, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            textObject.AddComponent<LayoutElement>().minHeight = Math.Max(34, fontSize + 10);
            return text;
        }

        private Button AddButton(string objectName, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color32(67, 78, 83, 255);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.normalColor = new Color32(67, 78, 83, 255);
            colors.highlightedColor = new Color32(94, 110, 116, 255);
            colors.pressedColor = new Color32(171, 68, 55, 255);
            colors.disabledColor = new Color32(46, 48, 52, 180);
            button.colors = colors;

            var rect = buttonObject.AddComponent<LayoutElement>();
            rect.preferredWidth = 150;
            rect.minWidth = 110;
            rect.preferredHeight = 44;

            var labelText = AddText("Label", buttonObject.transform, 18, TextAnchor.MiddleCenter, new Color32(245, 243, 230, 255));
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelText.text = label;
            return button;
        }

        private void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Font.CreateDynamicFontFromOSFont("Arial", 18);
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
    }
}
