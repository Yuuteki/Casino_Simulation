using UnityEngine;

namespace Casino.Presentation.Blackjack
{
    public sealed class BlackjackGreyboxCardMotion : MonoBehaviour
    {
        private const float MoveDuration = 0.24f;
        private const float ArcHeight = 0.16f;

        private Vector3 startWorldPosition;
        private Vector3 targetWorldPosition;
        private float delayRemaining;
        private float elapsed;

        public bool IsAnimating => enabled;

        public void Begin(Vector3 sourceWorldPosition, Vector3 destinationWorldPosition, float delay)
        {
            targetWorldPosition = destinationWorldPosition;
            if (!Application.isPlaying)
            {
                transform.position = targetWorldPosition;
                enabled = false;
                return;
            }

            startWorldPosition = sourceWorldPosition;
            delayRemaining = Mathf.Max(0f, delay);
            elapsed = 0f;
            transform.position = startWorldPosition;
            enabled = true;
        }

        private void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            if (delayRemaining > 0f)
            {
                delayRemaining -= deltaTime;
                return;
            }

            elapsed += deltaTime;
            var progress = Mathf.Clamp01(elapsed / MoveDuration);
            var eased = progress * progress * (3f - 2f * progress);
            var position = Vector3.Lerp(startWorldPosition, targetWorldPosition, eased);
            position.y += Mathf.Sin(progress * Mathf.PI) * ArcHeight;
            transform.position = position;

            if (progress >= 1f)
            {
                transform.position = targetWorldPosition;
                enabled = false;
            }
        }
    }
}
