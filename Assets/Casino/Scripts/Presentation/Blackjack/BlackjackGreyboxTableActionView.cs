using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Casino.Presentation.Blackjack
{
    internal sealed class BlackjackGreyboxTableActionView
    {
        private const string LocalActionAnchorPath = "Blackjack_Greybox_Root/Seats/Seat_0_Local/ActionPromptAnchor";
        private const string GeneratedRootName = "Greybox_Table_Actions";

        private readonly Button dealButton;
        private readonly Button hitButton;
        private readonly Button standButton;

        private BlackjackGreyboxTableActionView(Transform anchor, Font font, UnityAction onDeal, UnityAction onHit, UnityAction onStand)
        {
            var canvasObject = new GameObject(GeneratedRootName);
            canvasObject.transform.SetParent(anchor, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 5;
            canvasObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(360f, 86f);
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            canvasRect.localScale = Vector3.one * 0.007f;

            var background = canvasObject.AddComponent<Image>();
            background.color = new Color32(20, 31, 34, 225);
            background.raycastTarget = false;

            var layout = canvasObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            dealButton = AddButton("Table Deal Button", canvasRect, font, "DEAL", onDeal);
            hitButton = AddButton("Table Hit Button", canvasRect, font, "HIT", onHit);
            standButton = AddButton("Table Stand Button", canvasRect, font, "STAND", onStand);
        }

        public Button DealButton => dealButton;

        public Button HitButton => hitButton;

        public Button StandButton => standButton;

        public static BlackjackGreyboxTableActionView TryFindLocalSeat(Font font, UnityAction onDeal, UnityAction onHit, UnityAction onStand)
        {
            if (font == null)
            {
                throw new ArgumentNullException(nameof(font));
            }

            var anchorObject = GameObject.Find(LocalActionAnchorPath);
            if (anchorObject == null)
            {
                return null;
            }

            var existing = anchorObject.transform.Find(GeneratedRootName);
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    existing.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(existing.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }
            }

            return new BlackjackGreyboxTableActionView(anchorObject.transform, font, onDeal, onHit, onStand);
        }

        public void SetState(bool canDeal, bool canHit, bool canStand, bool isNextRound)
        {
            dealButton.interactable = canDeal;
            hitButton.interactable = canHit;
            standButton.interactable = canStand;
            SetLabel(dealButton, isNextRound ? "NEXT" : "DEAL");
        }

        private static Button AddButton(string objectName, Transform parent, Font font, string label, UnityAction callback)
        {
            var buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(104f, 64f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color32(231, 188, 78, 255);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(callback);
            var colors = button.colors;
            colors.normalColor = new Color32(231, 188, 78, 255);
            colors.highlightedColor = new Color32(255, 218, 119, 255);
            colors.pressedColor = new Color32(187, 139, 45, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(72, 82, 82, 210);
            button.colors = colors;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 25;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color32(24, 29, 31, 255);
            text.raycastTarget = false;
            text.text = label;
            return button;
        }

        private static void SetLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }
    }
}
