using System.Collections;
using Casino.Blackjack;
using Casino.Core.Identifiers;
using Casino.Presentation.Blackjack;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Casino.Tests.PlayMode.Presentation
{
    public sealed class BlackjackGreyboxPlayModeTests
    {
        [UnityTest]
        public IEnumerator RunHundredAiButtonCompletesOneHundredUiDrivenRounds()
        {
            DestroyExistingGreyboxScreens();
            yield return null;

            var screenObject = new GameObject("Greybox UI PlayMode Verification");
            screenObject.SetActive(false);
            var screen = screenObject.AddComponent<BlackjackGreyboxScreen>();
            screen.ConfigureStartup(
                newStartingBalance: 10000,
                newMinimumWager: 10,
                newMaximumWager: 100,
                newDefaultWager: 10,
                newRandomSeed: 97531);

            screenObject.SetActive(true);
            yield return null;

            Assert.That(screen.RunHundredButton, Is.Not.Null);
            Assert.That(screen.RunHundredButton.interactable, Is.True);

            screen.RunHundredButton.onClick.Invoke();
            yield return null;

            Assert.That(screen.Session.Round.Phase, Is.EqualTo(BlackjackRoundPhase.Intermission));
            Assert.That(screen.Session.Profile.LatestConfirmedRoundId.HasValue, Is.True);
            Assert.That(screen.Session.Profile.LatestConfirmedRoundId.Value, Is.EqualTo(new RoundId("greybox-round-99")));
            Assert.That(screen.Session.Profile.OfficialChipBalance, Is.GreaterThanOrEqualTo(0));
            Assert.That(screen.RunHundredButton.interactable, Is.True);

            Object.Destroy(screenObject);
            CleanupEventSystem();
            yield return null;
        }

        private static void DestroyExistingGreyboxScreens()
        {
            var screens = Object.FindObjectsByType<BlackjackGreyboxScreen>(FindObjectsSortMode.None);
            for (var index = 0; index < screens.Length; index++)
            {
                Object.Destroy(screens[index].gameObject);
            }
        }

        private static void CleanupEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                Object.Destroy(eventSystem.gameObject);
            }
        }
    }
}
