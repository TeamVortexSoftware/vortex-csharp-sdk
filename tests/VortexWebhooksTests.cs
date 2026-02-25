using System.Security.Cryptography;
using System.Text;
using TeamVortexSoftware.VortexSDK;
using Xunit;

namespace VortexSDK.Tests;

public class VortexWebhooksTests
{
    private const string Secret = "whsec_test_secret_123";

    private const string WebhookPayload =
        "{\"id\":\"evt_123\",\"type\":\"invitation.accepted\",\"timestamp\":\"2025-01-15T12:00:00.000Z\"," +
        "\"accountId\":\"acc_123\",\"environmentId\":\"env_456\",\"sourceTable\":\"invitations\"," +
        "\"operation\":\"update\",\"data\":{\"invitationId\":\"inv_789\"}}";

    private const string AnalyticsPayload =
        "{\"id\":\"evt_456\",\"name\":\"widget_loaded\",\"accountId\":\"acc_123\"," +
        "\"organizationId\":\"org_123\",\"projectId\":\"proj_123\",\"environmentId\":\"env_456\"," +
        "\"timestamp\":\"2025-01-15T12:00:00.000Z\"}";

    private static string Sign(string payload, string secret = Secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    [Fact]
    public void Constructor_RequiresSecret()
    {
        Assert.Throws<ArgumentException>(() => new VortexWebhooks(""));
        Assert.Throws<ArgumentException>(() => new VortexWebhooks(null!));
    }

    [Fact]
    public void VerifySignature_Valid()
    {
        var wh = new VortexWebhooks(Secret);
        Assert.True(wh.VerifySignature(WebhookPayload, Sign(WebhookPayload)));
    }

    [Fact]
    public void VerifySignature_Invalid()
    {
        var wh = new VortexWebhooks(Secret);
        Assert.False(wh.VerifySignature(WebhookPayload, "bad_sig"));
    }

    [Fact]
    public void VerifySignature_Empty()
    {
        var wh = new VortexWebhooks(Secret);
        Assert.False(wh.VerifySignature(WebhookPayload, ""));
    }

    [Fact]
    public void ConstructEvent_WebhookEvent()
    {
        var wh = new VortexWebhooks(Secret);
        var evt = wh.ConstructEvent(WebhookPayload, Sign(WebhookPayload));
        Assert.True(VortexWebhooks.IsWebhookEvent(evt));
        var we = Assert.IsType<VortexWebhookEvent>(evt);
        Assert.Equal("evt_123", we.Id);
        Assert.Equal("invitation.accepted", we.Type);
    }

    [Fact]
    public void ConstructEvent_AnalyticsEvent()
    {
        var wh = new VortexWebhooks(Secret);
        var evt = wh.ConstructEvent(AnalyticsPayload, Sign(AnalyticsPayload));
        Assert.True(VortexWebhooks.IsAnalyticsEvent(evt));
        var ae = Assert.IsType<VortexAnalyticsEvent>(evt);
        Assert.Equal("widget_loaded", ae.Name);
    }

    [Fact]
    public void ConstructEvent_BadSignature()
    {
        var wh = new VortexWebhooks(Secret);
        Assert.Throws<VortexWebhookSignatureException>(
            () => wh.ConstructEvent(WebhookPayload, "bad"));
    }
}
