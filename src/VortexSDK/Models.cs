using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeamVortexSoftware.VortexSDK
{
    /// <summary>
    /// User data for JWT generation
    /// </summary>
    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("adminScopes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? AdminScopes { get; set; }

        public User() { }

        public User(string id, string email, List<string>? adminScopes = null)
        {
            Id = id;
            Email = email;
            AdminScopes = adminScopes;
        }
    }

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
    /// Group information for JWT generation (input)
    /// Supports both 'id' (legacy) and 'groupId' (preferred) for backward compatibility
    /// </summary>
    public class Group
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        [JsonPropertyName("groupId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? GroupId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public Group() { }

        public Group(string type, string name, string? id = null, string? groupId = null)
        {
            Type = type;
            Name = name;
            Id = id;
            GroupId = groupId;
        }
    }

    /// <summary>
    /// Invitation group from API responses
    /// This matches the MemberGroups table structure from the API
    /// </summary>
    public class InvitationGroup
    {
        /// <summary>Vortex internal UUID</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>Vortex account ID</summary>
        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        /// <summary>Customer's group ID (the ID they provided to Vortex)</summary>
        [JsonPropertyName("groupId")]
        public string GroupId { get; set; } = string.Empty;

        /// <summary>Group type (e.g., "workspace", "team")</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Group name</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>ISO 8601 timestamp when the group was created</summary>
        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        public InvitationGroup() { }

        public InvitationGroup(string id, string accountId, string groupId, string type, string name, string createdAt)
        {
            Id = id;
            AccountId = accountId;
            GroupId = groupId;
            Type = type;
            Name = name;
            CreatedAt = createdAt;
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
        public List<InvitationGroup> Groups { get; set; } = new();

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
