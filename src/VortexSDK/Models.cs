using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeamVortexSoftware.VortexSDK
{
    /// <summary>
    /// Identifier for a user (email, sms, etc.)
    /// </summary>
    public class Identifier
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        public Identifier() { }

        public Identifier(string type, string value)
        {
            Type = type;
            Value = value;
        }
    }

    /// <summary>
    /// Group information
    /// </summary>
    public class Group
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public Group() { }

        public Group(string type, string id, string name)
        {
            Type = type;
            Id = id;
            Name = name;
        }
    }

    /// <summary>
    /// Invitation target (email or sms)
    /// </summary>
    public class InvitationTarget
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        public InvitationTarget() { }

        public InvitationTarget(string type, string value)
        {
            Type = type;
            Value = value;
        }
    }

    /// <summary>
    /// Invitation acceptance information
    /// </summary>
    public class InvitationAcceptance
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("acceptedAt")]
        public string AcceptedAt { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public InvitationTarget? Target { get; set; }
    }

    /// <summary>
    /// Full invitation details
    /// </summary>
    public class Invitation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [JsonPropertyName("clickThroughs")]
        public int ClickThroughs { get; set; }

        [JsonPropertyName("configurationAttributes")]
        public Dictionary<string, object>? ConfigurationAttributes { get; set; }

        [JsonPropertyName("attributes")]
        public Dictionary<string, object>? Attributes { get; set; }

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("deactivated")]
        public bool Deactivated { get; set; }

        [JsonPropertyName("deliveryCount")]
        public int DeliveryCount { get; set; }

        [JsonPropertyName("deliveryTypes")]
        public List<string> DeliveryTypes { get; set; } = new();

        [JsonPropertyName("foreignCreatorId")]
        public string ForeignCreatorId { get; set; } = string.Empty;

        [JsonPropertyName("invitationType")]
        public string InvitationType { get; set; } = string.Empty;

        [JsonPropertyName("modifiedAt")]
        public string? ModifiedAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public List<InvitationTarget> Target { get; set; } = new();

        [JsonPropertyName("views")]
        public int Views { get; set; }

        [JsonPropertyName("widgetConfigurationId")]
        public string WidgetConfigurationId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("groups")]
        public List<Group> Groups { get; set; } = new();

        [JsonPropertyName("accepts")]
        public List<InvitationAcceptance> Accepts { get; set; } = new();
    }

    /// <summary>
    /// Response containing multiple invitations
    /// </summary>
    public class InvitationsResponse
    {
        [JsonPropertyName("invitations")]
        public List<Invitation>? Invitations { get; set; }
    }
}
