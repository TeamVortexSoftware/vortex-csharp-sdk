using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeamVortexSoftware.VortexSDK
{
    /// <summary>
    /// User data for JWT generation
    /// Requires both id and email
    /// Optional fields: userName (max 200 chars), userAvatarUrl (HTTPS URL, max 2000 chars), adminScopes
    /// </summary>
    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("userName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UserName { get; set; }

        [JsonPropertyName("userAvatarUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UserAvatarUrl { get; set; }

        [JsonPropertyName("adminScopes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? AdminScopes { get; set; }

        public User() { }

        public User(string id, string email, List<string>? adminScopes = null, string? userName = null, string? userAvatarUrl = null)
        {
            Id = id;
            Email = email;
            UserName = userName;
            UserAvatarUrl = userAvatarUrl;
            AdminScopes = adminScopes;
        }
    }

    /// <summary>
    /// User data for accepting invitations
    /// Requires either email or phone (or both)
    /// </summary>
    public class AcceptUser
    {
        [JsonPropertyName("email")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Phone { get; set; }

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        public AcceptUser() { }

        public AcceptUser(string? email = null, string? phone = null, string? name = null)
        {
            Email = email;
            Phone = phone;
            Name = name;
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

        /// <summary>
        /// Valid values: "email", "phone", "share", "internal"
        /// </summary>
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

        [JsonPropertyName("deploymentId")]
        public string DeploymentId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("groups")]
        public List<InvitationGroup> Groups { get; set; } = new();

        [JsonPropertyName("accepts")]
        public List<InvitationAcceptance> Accepts { get; set; } = new();

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("scopeType")]
        public string? ScopeType { get; set; }

        [JsonPropertyName("expired")]
        public bool Expired { get; set; }

        [JsonPropertyName("expires")]
        public string? Expires { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        [JsonPropertyName("passThrough")]
        public string? PassThrough { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("creatorName")]
        public string? CreatorName { get; set; }

        [JsonPropertyName("creatorAvatarUrl")]
        public string? CreatorAvatarUrl { get; set; }
    }

    /// <summary>
    /// Response containing multiple invitations
    /// </summary>
    public class InvitationsResponse
    {
        [JsonPropertyName("invitations")]
        public List<Invitation>? Invitations { get; set; }
    }

    // --- Types for creating invitations via backend API ---

    /// <summary>
    /// Target for creating an invitation
    /// </summary>
    public class CreateInvitationTarget
    {
        /// <summary>
        /// Target type: "email", "phone", or "internal"
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Target value: email address, phone number, or internal user ID
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        public CreateInvitationTarget() { }

        public CreateInvitationTarget(string type, string value)
        {
            Type = type;
            Value = value;
        }

        public static CreateInvitationTarget Email(string email) => new("email", email);
        public static CreateInvitationTarget Phone(string phone) => new("phone", phone);
        public static CreateInvitationTarget Internal(string internalId) => new("internal", internalId);
    }

    /// <summary>
    /// Information about the user creating the invitation (the inviter)
    /// </summary>
    public class Inviter
    {
        /// <summary>
        /// Required: Your internal user ID for the inviter
        /// </summary>
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Email of the inviter
        /// </summary>
        [JsonPropertyName("userEmail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UserEmail { get; set; }

        /// <summary>
        /// Optional: Display name of the inviter
        /// </summary>
        [JsonPropertyName("userName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UserName { get; set; }

        /// <summary>
        /// Optional: Avatar URL of the inviter
        /// </summary>
        [JsonPropertyName("userAvatarUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UserAvatarUrl { get; set; }

        public Inviter() { }

        public Inviter(string userId, string? userEmail = null, string? userName = null, string? userAvatarUrl = null)
        {
            UserId = userId;
            UserEmail = userEmail;
            UserName = userName;
            UserAvatarUrl = userAvatarUrl;
        }
    }

    /// <summary>
    /// Group information for creating invitations
    /// </summary>
    public class CreateInvitationGroup
    {
        /// <summary>
        /// Group type (e.g., "team", "organization")
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Your internal group ID
        /// </summary>
        [JsonPropertyName("groupId")]
        public string GroupId { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the group
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public CreateInvitationGroup() { }

        public CreateInvitationGroup(string type, string groupId, string name)
        {
            Type = type;
            GroupId = groupId;
            Name = name;
        }
    }

    /// <summary>
    /// Request body for creating an invitation via the public API (backend SDK use)
    /// </summary>
    public class CreateInvitationRequest
    {
        [JsonPropertyName("widgetConfigurationId")]
        public string WidgetConfigurationId { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public CreateInvitationTarget Target { get; set; } = new();

        [JsonPropertyName("inviter")]
        public Inviter Inviter { get; set; } = new();

        [JsonPropertyName("groups")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<CreateInvitationGroup>? Groups { get; set; }

        [JsonPropertyName("source")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Source { get; set; }

        [JsonPropertyName("templateVariables")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? TemplateVariables { get; set; }

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Metadata { get; set; }

        public CreateInvitationRequest() { }

        public CreateInvitationRequest(string widgetConfigurationId, CreateInvitationTarget target, Inviter inviter)
        {
            WidgetConfigurationId = widgetConfigurationId;
            Target = target;
            Inviter = inviter;
        }
    }

    /// <summary>
    /// Response from creating an invitation
    /// </summary>
    public class CreateInvitationResponse
    {
        /// <summary>
        /// The ID of the created invitation
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The short link for the invitation
        /// </summary>
        [JsonPropertyName("shortLink")]
        public string ShortLink { get; set; } = string.Empty;

        /// <summary>
        /// The status of the invitation
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// When the invitation was created
        /// </summary>
        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;
    }
}
