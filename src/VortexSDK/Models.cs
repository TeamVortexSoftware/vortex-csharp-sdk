using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeamVortexSoftware.VortexSDK
{
    // ============================================================================
    // Enums for type-safe API values
    // ============================================================================

    /// <summary>
    /// Target type for invitation responses
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<InvitationTargetType>))]
    public enum InvitationTargetType
    {
        email,
        phone,
        share,
        @internal  // @ prefix because internal is a C# keyword
    }

    /// <summary>
    /// Target type for creating invitations
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<CreateInvitationTargetTypeEnum>))]
    public enum CreateInvitationTargetTypeEnum
    {
        email,
        phone,
        @internal
    }

    /// <summary>
    /// Type of invitation
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<InvitationType>))]
    public enum InvitationType
    {
        single_use,
        multi_use,
        autojoin
    }

    /// <summary>
    /// Current status of an invitation
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<InvitationStatus>))]
    public enum InvitationStatus
    {
        queued,
        sending,
        sent,
        delivered,
        accepted,
        shared,
        unfurled,
        accepted_elsewhere
    }

    /// <summary>
    /// Delivery type for invitations
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<DeliveryType>))]
    public enum DeliveryType
    {
        email,
        phone,
        share,
        @internal
    }

    // ============================================================================
    // Core types
    // ============================================================================

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
    /// Invitation target (email, phone, share, internal)
    /// </summary>
    public class InvitationTarget
    {
        [JsonPropertyName("type")]
        public InvitationTargetType Type { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the person being invited
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>
        /// Avatar URL for the person being invited (for display in invitation lists)
        /// </summary>
        [JsonPropertyName("avatarUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AvatarUrl { get; set; }

        public InvitationTarget() { }

        public InvitationTarget(InvitationTargetType type, string value)
        {
            Type = type;
            Value = value;
        }

        public InvitationTarget(InvitationTargetType type, string value, string? name = null, string? avatarUrl = null)
        {
            Type = type;
            Value = value;
            Name = name;
            AvatarUrl = avatarUrl;
        }

        public static InvitationTarget Email(string value) => new(InvitationTargetType.email, value);
        public static InvitationTarget Phone(string value) => new(InvitationTargetType.phone, value);
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
        public List<DeliveryType> DeliveryTypes { get; set; } = new();

        [JsonPropertyName("foreignCreatorId")]
        public string ForeignCreatorId { get; set; } = string.Empty;

        [JsonPropertyName("invitationType")]
        public InvitationType InvitationType { get; set; }

        [JsonPropertyName("modifiedAt")]
        public string? ModifiedAt { get; set; }

        [JsonPropertyName("status")]
        public InvitationStatus Status { get; set; }

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

        /// <summary>
        /// Customer-defined subtype for analytics segmentation (e.g., "pymk", "find-friends")
        /// </summary>
        [JsonPropertyName("subtype")]
        public string? Subtype { get; set; }

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
        [JsonPropertyName("type")]
        public CreateInvitationTargetTypeEnum Type { get; set; }

        /// <summary>
        /// Target value: email address, phone number, or internal user ID
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the person being invited
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>
        /// Avatar URL for the person being invited (for display in invitation lists)
        /// </summary>
        [JsonPropertyName("avatarUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AvatarUrl { get; set; }

        public CreateInvitationTarget() { }

        public CreateInvitationTarget(CreateInvitationTargetTypeEnum type, string value)
        {
            Type = type;
            Value = value;
        }

        public CreateInvitationTarget(CreateInvitationTargetTypeEnum type, string value, string? name = null, string? avatarUrl = null)
        {
            Type = type;
            Value = value;
            Name = name;
            AvatarUrl = avatarUrl;
        }

        public static CreateInvitationTarget Email(string email) => new(CreateInvitationTargetTypeEnum.email, email);
        public static CreateInvitationTarget Phone(string phone) => new(CreateInvitationTargetTypeEnum.phone, phone);
        public static CreateInvitationTarget Internal(string internalId) => new(CreateInvitationTargetTypeEnum.@internal, internalId);
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
    /// Configuration for link unfurl (Open Graph) metadata.
    /// Controls how the invitation link appears when shared on social platforms or messaging apps.
    /// </summary>
    public class UnfurlConfig
    {
        /// <summary>The title shown in link previews (og:title)</summary>
        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

        /// <summary>The description shown in link previews (og:description)</summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        /// <summary>The image URL shown in link previews (og:image) - must be HTTPS</summary>
        [JsonPropertyName("image")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Image { get; set; }

        /// <summary>The Open Graph type (og:type) - e.g., 'website', 'article', 'product'</summary>
        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Type { get; set; }

        /// <summary>The site name shown in link previews (og:site_name)</summary>
        [JsonPropertyName("siteName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SiteName { get; set; }
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

        /// <summary>
        /// Customer-defined subtype for analytics segmentation (e.g., "pymk", "find-friends")
        /// </summary>
        [JsonPropertyName("subtype")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Subtype { get; set; }

        [JsonPropertyName("templateVariables")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? TemplateVariables { get; set; }

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Metadata { get; set; }

        [JsonPropertyName("unfurlConfig")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UnfurlConfig? UnfurlConfig { get; set; }

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

    // --- Types for autojoin domain management ---

    /// <summary>
    /// Represents an autojoin domain configuration
    /// </summary>
    public class AutojoinDomain
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = string.Empty;

        public AutojoinDomain() { }

        public AutojoinDomain(string id, string domain)
        {
            Id = id;
            Domain = domain;
        }
    }

    /// <summary>
    /// Response from autojoin API endpoints
    /// </summary>
    public class AutojoinDomainsResponse
    {
        [JsonPropertyName("autojoinDomains")]
        public List<AutojoinDomain> AutojoinDomains { get; set; } = new();

        [JsonPropertyName("invitation")]
        public Invitation? Invitation { get; set; }
    }

    /// <summary>
    /// Request body for configuring autojoin domains
    /// </summary>
    public class ConfigureAutojoinRequest
    {
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonPropertyName("scopeType")]
        public string ScopeType { get; set; } = string.Empty;

        [JsonPropertyName("scopeName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ScopeName { get; set; }

        [JsonPropertyName("domains")]
        public List<string> Domains { get; set; } = new();

        [JsonPropertyName("widgetId")]
        public string WidgetId { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Metadata { get; set; }

        public ConfigureAutojoinRequest() { }

        public ConfigureAutojoinRequest(string scope, string scopeType, List<string> domains, string widgetId)
        {
            Scope = scope;
            ScopeType = scopeType;
            Domains = domains;
            WidgetId = widgetId;
        }
    }
}
