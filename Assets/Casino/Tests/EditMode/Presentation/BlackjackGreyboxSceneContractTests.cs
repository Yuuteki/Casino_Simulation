using Casino.Presentation.Blackjack;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Casino.Tests.EditMode.Presentation
{
    public sealed class BlackjackGreyboxSceneContractTests
    {
        private const string ScenePath = "Assets/Casino/Scenes/BlackjackGreybox.unity";

        [Test]
        public void BlackjackGreyboxSceneContainsModelIntegrationAnchors()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);

            Assert.That(scene.IsValid(), Is.True);
            var root = GameObject.Find("Blackjack_Greybox_Root");
            Assert.That(root, Is.Not.Null);

            AssertRequiredTransform(root.transform, "Room/Floor_Placeholder");
            AssertRequiredTransform(root.transform, "Table/Blackjack_Felt_Surface");
            AssertRequiredTransform(root.transform, "Dealer/Dealer_Anchor");
            AssertRequiredTransform(root.transform, "Dealer/Dealer_CardZone");
            AssertRequiredTransform(root.transform, "Dealer/Dealer_HoleCardZone");
            AssertRequiredTransform(root.transform, "Dealer/Dealer_ChipTray");
            AssertRequiredTransform(root.transform, "Dealer/Discard_Tray");
            AssertRequiredTransform(root.transform, "Camera/LocalSeat_CameraAnchor");

            AssertSeat(root.transform, "Seat_0_Local");
            AssertSeat(root.transform, "Seat_1_LeftNear");
            AssertSeat(root.transform, "Seat_2_LeftFar");
            AssertSeat(root.transform, "Seat_3_FarLeft");
            AssertSeat(root.transform, "Seat_4_FarRight");
            AssertSeat(root.transform, "Seat_5_RightNear");

            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Casino/Prefabs/Blackjack/Chip.prefab"),
                Is.Not.Null);
            var chipStack = AssertRequiredTransform(root.transform, "Seats/Seat_0_Local/ChipStackAnchor/Chip_Sample_Stack");
            Assert.That(chipStack.childCount, Is.EqualTo(3));

            var screen = root.transform.Find("UI/BlackjackGreyboxScreen");
            Assert.That(screen, Is.Not.Null);
            Assert.That(screen.GetComponent<BlackjackGreyboxScreen>(), Is.Not.Null);
        }

        private static void AssertSeat(Transform root, string seatName)
        {
            var seat = AssertRequiredTransform(root, "Seats/" + seatName);
            AssertRequiredTransform(seat, "Seat_Placeholder");
            AssertRequiredTransform(seat, "AvatarAnchor");
            AssertRequiredTransform(seat, "CameraAnchor");
            AssertRequiredTransform(seat, "CardZone");
            AssertRequiredTransform(seat, "BetSpot");
            AssertRequiredTransform(seat, "ChipStackAnchor");
            AssertRequiredTransform(seat, "ActionPromptAnchor");
        }

        private static Transform AssertRequiredTransform(Transform root, string relativePath)
        {
            var found = root.Find(relativePath);
            Assert.That(found, Is.Not.Null, relativePath + " is missing.");
            return found;
        }
    }
}
