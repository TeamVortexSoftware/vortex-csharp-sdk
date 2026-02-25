using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeamVortexSoftware.VortexSDK
{
    /// <summary>
    /// Webhook event type constants for Vortex state changes.
    /// </summary>
    public static class WebhookEventType
    {
        // Invitation Lifecycle
        public const string InvitationCreated = "invitation.created";
        public const string InvitationAccepted = "invitation.accepted";
        public const string InvitationDeactivated = "invitation.deactivated";
        public const string InvitationEmailDelivered = "invitation.email.delivered";
        public const string InvitationEmailBounced = "invitation.email.bounced";
        public const string InvitationEmailOpened = "invitation.email.opened";
        public const string InvitationLinkClicked = "invitation.link.clicked";
        public const string InvitationReminderSent = "invitation.reminder.sent";

        // Deployment Lifecycle
        public const string DeploymentCreated = "deployment.created";
        public const string DeploymentDeactivated = "deployment.deactivated";

        // A/B Testing
        public const string ABTestStarted = "abtest.started";
        public const string ABTestWinnerDeclared = "abtest.winner_declared";

        // Member/Group
        public const string MemberCreated = "member.created";
        public const string GroupMemberAdded = "group.member.added";

        // Email
        public const string EmailComplained = "email.complained";
    }

    /// <summary>
    /// Analytics event type constants for behavioral telemetry.
    /// </summary>
    public static class AnalyticsEventType
    {
        public const string WidgetLoaded = "widget_loaded";
        public const string InvitationSent = "invitation_sent";
        public const string InvitationClicked = "invitation_clicked";
        public const string InvitationAccepted = "invitation_accepted";
        public const string ShareTriggered = "share_triggered";
    }

    /// <summary>
    /// A Vortex webhook event representing a server-side state change.
    /// </summary>
    public class VortexWebhookEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [JsonPropertyName("environmentId")]
        public string? EnvironmentId { get; set; }

        [JsonPropertyName("sourceTable")]
        public string SourceTable { get; set; } = string.Empty;

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public Dictionary<string, object>? Data { get; set; }
    }

    /// <summary>
    /// An analytics event representing client-side behavioral telemetry.
    /// </summary>
    public class VortexAnalyticsEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("environmentId")]
        public string EnvironmentId { get; set; } = string.Empty;

        [JsonPropertyName("deploymentId")]
        public string? DeploymentId { get; set; }

        [JsonPropertyName("widgetConfigurationId")]
        public string? WidgetConfigurationId { get; set; }

        [JsonPropertyName("foreignUserId")]
        public string? ForeignUserId { get; set; }

        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, object>? Payload { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        [JsonPropertyName("segmentation")]
        public string? Segmentation { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;
    }
}
