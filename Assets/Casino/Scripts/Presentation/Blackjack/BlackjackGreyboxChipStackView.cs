using System;
using System.Collections.Generic;
using UnityEngine;

namespace Casino.Presentation.Blackjack
{
    internal sealed class BlackjackGreyboxChipStackView
    {
        private const string LocalChipAnchorPath = "Blackjack_Greybox_Root/Seats/Seat_0_Local/ChipStackAnchor";
        private const string StackName = "Chip_Sample_Stack";
        private const int MaxDisplayedChips = 12;

        private readonly Transform stackRoot;
        private readonly GameObject chipTemplate;
        private readonly List<GameObject> chips = new List<GameObject>();

        private BlackjackGreyboxChipStackView(Transform stackRoot, GameObject chipTemplate)
        {
            this.stackRoot = stackRoot;
            this.chipTemplate = chipTemplate;
            CollectExistingChips();
        }

        public int VisibleChipCount { get; private set; }

        public static BlackjackGreyboxChipStackView TryFindLocalPlayerStack()
        {
            var anchor = GameObject.Find(LocalChipAnchorPath);
            if (anchor == null)
            {
                return null;
            }

            var stack = anchor.transform.Find(StackName);
            if (stack == null)
            {
                return null;
            }

            var template = FindFirstChip(stack);
            if (template == null)
            {
                return null;
            }

            return new BlackjackGreyboxChipStackView(stack, template);
        }

        public void SetWager(int wager, int chipUnit)
        {
            var chipCount = CalculateChipCount(wager, chipUnit);
            EnsureChipCount(chipCount);

            for (var index = 0; index < chips.Count; index++)
            {
                var chip = chips[index];
                if (chip == null)
                {
                    continue;
                }

                var isVisible = index < chipCount;
                chip.SetActive(isVisible);
                if (isVisible)
                {
                    chip.transform.localPosition = PositionForChip(index);
                    chip.transform.localRotation = Quaternion.Euler(0f, index * 11f, 0f);
                }
            }

            VisibleChipCount = chipCount;
        }

        private void CollectExistingChips()
        {
            chips.Clear();
            for (var index = 0; index < stackRoot.childCount; index++)
            {
                var child = stackRoot.GetChild(index);
                if (child != null)
                {
                    chips.Add(child.gameObject);
                }
            }
        }

        private void EnsureChipCount(int chipCount)
        {
            while (chips.Count < chipCount)
            {
                var chip = UnityEngine.Object.Instantiate(chipTemplate, stackRoot);
                chip.name = "Chip_" + chips.Count;
                chips.Add(chip);
            }
        }

        private static int CalculateChipCount(int wager, int chipUnit)
        {
            if (wager <= 0 || chipUnit <= 0)
            {
                return 0;
            }

            return Math.Min(MaxDisplayedChips, Mathf.CeilToInt((float)wager / chipUnit));
        }

        private static Vector3 PositionForChip(int index)
        {
            var rowOffset = index % 3;
            return new Vector3(rowOffset * 0.018f, index * 0.035f, -rowOffset * 0.012f);
        }

        private static GameObject FindFirstChip(Transform stack)
        {
            for (var index = 0; index < stack.childCount; index++)
            {
                var child = stack.GetChild(index);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }
}
