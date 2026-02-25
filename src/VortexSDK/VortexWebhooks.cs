using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TeamVortexSoftware.VortexSDK
{
    /// <summary>
    /// Exception thrown when webhook signature verification fails.
    /// </summary>
    public class VortexWebhookSignatureException : VortexException
    {
        public VortexWebhookSignatureException(string message) : base(message) { }
    }

    /// <summary>
    /// Core webhook verification and parsing.
    /// 
    /// This class is framework-agnostic — use it directly or with
    /// ASP.NET Core middleware.
    /// </summary>
    /// <example>
    /// <code>
    /// var webhooks = new VortexWebhooks("whsec_your_secret");
    /// 
    /// // In your controller:
    /// var (isWebhook, webhookEvent, analyticsEvent) = webhooks.ConstructEvent(body, signature);
    /// </code>
    /// </example>
    public class VortexWebhooks
    {
        private readonly byte[] _secretBytes;

        /// <summary>
        /// Create a new VortexWebhooks instance.
        /// </summary>
        /// <param name="secret">The webhook signing secret from your Vortex dashboard</param>
        public VortexWebhooks(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("VortexWebhooks requires a secret", nameof(secret));

            _secretBytes = Encoding.UTF8.GetBytes(secret);
        }

        /// <summary>
        /// Verify the HMAC-SHA256 signature of an incoming webhook payload.
        /// </summary>
        /// <param name="payload">The raw request body</param>
        /// <param name="signature">The value of the X-Vortex-Signature header</param>
        /// <returns>true if the signature is valid</returns>
        public bool VerifySignature(string payload, string signature)
        {
            if (string.IsNullOrEmpty(signature))
                return false;

            using var hmac = new HMACSHA256(_secretBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expected = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            // Timing-safe comparison
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expected));
        }

        /// <summary>
        /// Verify and parse an incoming webhook payload.
        /// Returns a VortexWebhookEvent or VortexAnalyticsEvent (as object).
        /// Use IsWebhookEvent/IsAnalyticsEvent to check type.
        /// </summary>
        /// <param name="payload">The raw request body</param>
        /// <param name="signature">The value of the X-Vortex-Signature header</param>
        /// <returns>A VortexWebhookEvent or VortexAnalyticsEvent</returns>
        /// <exception cref="VortexWebhookSignatureException">If signature is invalid</exception>
        public object ConstructEvent(string payload, string signature)
        {
            if (!VerifySignature(payload, signature))
            {
                throw new VortexWebhookSignatureException(
                    "Webhook signature verification failed. Ensure you are using " +
                    "the raw request body and the correct signing secret.");
            }

            var node = JsonNode.Parse(payload);
            if (node?["name"] != null)
            {
                return JsonSerializer.Deserialize<VortexAnalyticsEvent>(payload)!;
            }
            else
            {
                return JsonSerializer.Deserialize<VortexWebhookEvent>(payload)!;
            }
        }

        /// <summary>Check if an event is a webhook event.</summary>
        public static bool IsWebhookEvent(object evt) => evt is VortexWebhookEvent;

        /// <summary>Check if an event is an analytics event.</summary>
        public static bool IsAnalyticsEvent(object evt) => evt is VortexAnalyticsEvent;
    }
}
