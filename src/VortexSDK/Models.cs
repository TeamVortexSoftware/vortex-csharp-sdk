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
    /// User data for JWT generation - represents the authenticated user sending invitations.
    /// Only Id is required. Email is optional but recommended for invitation attribution.
    /// </summary>
    public class User
    {
        /// <summary>Your internal user ID (required for invitation attribution)</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>User's email address (optional, used for reply-to in invitation emails)</summary>
        [JsonPropertyName("email")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Email { get; set; }

        /// <summary>Display name shown to recipients (e.g., "John invited you")</summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>Avatar URL displayed in invitation emails and widgets (must be HTTPS)</summary>
        [JsonPropertyName("avatarUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AvatarUrl { get; set; }

        /// <summary>DEPRECATED: Use Name instead</summary>
        [JsonPropertyName("userName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [Obsolete("Use Name instead")]
        public string? UserName { get; set; }

        /// <summary>DEPRECATED: Use AvatarUrl instead</summary>
        [JsonPropertyName("userAvatarUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [Obsolete("Use AvatarUrl instead")]
        public string? UserAvatarUrl { get; set; }

        /// <summary>List of scopes where user has admin privileges (e.g., ["autojoin"])</summary>
        [JsonPropertyName("adminScopes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? AdminScopes { get; set; }

        /// <summary>Restrict invitations to these email domains (e.g., ["acme.com"])</summary>
        [JsonPropertyName("allowedEmailDomains")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? AllowedEmailDomains { get; set; }

        public User() { }

        /// <summary>
        /// Create a new User with only the required id field
        /// </summary>
        /// <param name="id">User's unique identifier in your system</param>
        public User(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Create a new User with id and optional email
        /// </summary>
        /// <param name="id">User's unique identifier in your system</param>
        /// <param name="email">User's email address (optional but recommended for reply-to)</param>
        /// <param name="adminScopes">List of admin scopes (e.g., ["autojoin"])</param>
        /// <param name="name">User's display name</param>
        /// <param name="avatarUrl">User's avatar URL</param>
        public User(string id, string? email = null, List<string>? adminScopes = null, string? name = null, string? avatarUrl = null)
        {
            Id = id;
            Email = email;
            Name = name;
            AvatarUrl = avatarUrl;
            AdminScopes = adminScopes;
        }
    }

    /// <summary>
    /// User data for accepting invitations - identifies who accepted the invitation
    /// </summary>
    public class AcceptUser
    {
        /// <summary>Email address of the user accepting. At least one of Email or Phone is required.</summary>
        [JsonPropertyName("email")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Email { get; set; }

        /// <summary>Phone number with country code (e.g., "+15551234567"). At least one of Email or Phone is required.</summary>
        [JsonPropertyName("phone")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Phone { get; set; }

        /// <summary>Display name of the accepting user (shown in notifications to inviter)</summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>Whether user was already registered. true=existing, false=new signup, null=unknown. Used for conversion analytics.</summary>
        [JsonPropertyName("isExisting")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsExisting { get; set; }

        public AcceptUser() { }

        public AcceptUser(string? email = null, string? phone = null, string? name = null, bool? isExisting = null)
        {
            Email = email;
            Phone = phone;
            Name = name;
            IsExisting = isExisting;
        }
    }

    /// <summary>
    /// Identifier for a user - used in JWT generation to link user across channels
    /// </summary>
    public class Identifier
    {
        /// <summary>Identifier type: "email", "phone", "username", or custom type</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>The identifier value (email address, phone number, etc.)</summary>
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
    /// Scope/group for JWT generation - defines user's team/org membership in tokens
    /// </summary>
    public class Group
    {
        /// <summary>Scope type (e.g., "team", "organization", "workspace")</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Legacy scope identifier. Use Scope/groupId instead.</summary>
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        /// <summary>Your internal scope/group identifier (preferred)</summary>
        [JsonPropertyName("groupId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Scope { get; set; }

        /// <summary>Display name for the scope (e.g., "Engineering Team")</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public Group() { }

        public Group(string type, string name, string? id = null, string? groupId = null)
        {
            Type = type;
            Name = name;
            Id = id;
            Scope = groupId;
        }
    }

    /// <summary>
    /// Invitation group from API responses
    /// This matches the MemberGroups table structure from the API
    /// </summary>
    public class InvitationScope
    {
        /// <summary>Vortex internal UUID</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>Vortex account ID</summary>
        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        /// <summary>Customer's group ID (the ID they provided to Vortex)</summary>
        [JsonPropertyName("groupId")]
        public string Scope { get; set; } = string.Empty;

        /// <summary>Preferred alias for Scope/groupId</summary>
        [JsonIgnore]
        public string ScopeId
        {
            get => Scope;
            set => Scope = value;
        }

        /// <summary>Group type (e.g., "workspace", "team")</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Group name</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>ISO 8601 timestamp when the group was created</summary>
        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        public InvitationScope() { }

        public InvitationScope(string id, string accountId, string groupId, string type, string name, string createdAt)
        {
            Id = id;
            AccountId = accountId;
            Scope = groupId;
            Type = type;
            Name = name;
            CreatedAt = createdAt;
        }
    }

    /// <summary>
    /// Represents the target recipient of an invitation
    /// </summary>
    public class InvitationTarget
    {
        /// <summary>Delivery channel: email, phone, share (link), or internal (in-app)</summary>
        [JsonPropertyName("type")]
        public InvitationTargetType Type { get; set; }

        /// <summary>Target address: email, phone number with country code, or share link ID</summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>Display name of the recipient (e.g., "John Doe")</summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>Avatar URL for the recipient, shown in invitation lists and widgets</summary>
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
    /// Record of an invitation being accepted
    /// </summary>
    public class InvitationAcceptance
    {
        /// <summary>Unique identifier for this acceptance record</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>Your Vortex account ID</summary>
        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        /// <summary>ISO 8601 timestamp when the invitation was accepted</summary>
        [JsonPropertyName("acceptedAt")]
        public string AcceptedAt { get; set; } = string.Empty;

        /// <summary>The target that accepted the invitation</summary>
        [JsonPropertyName("target")]
        public InvitationTarget? Target { get; set; }
    }

    /// <summary>
    /// Complete invitation details as returned by the Vortex API
    /// </summary>
    public class Invitation
    {
        /// <summary>Unique identifier for this invitation</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>Your Vortex account ID</summary>
        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        /// <summary>Number of times the invitation link was clicked</summary>
        [JsonPropertyName("clickThroughs")]
        public int ClickThroughs { get; set; }

        /// <summary>Invitation form data submitted by the user, including email addresses of invitees and the values of any custom fields.</summary>
        [JsonPropertyName("formSubmissionData")]
        public Dictionary<string, object>? FormSubmissionData { get; set; }

        /// <summary>Deprecated: Use FormSubmissionData instead. Contains the same data.</summary>
        [Obsolete("Use FormSubmissionData instead")]
        [JsonPropertyName("configurationAttributes")]
        public Dictionary<string, object>? ConfigurationAttributes { get; set; }

        /// <summary>Custom attributes attached to this invitation</summary>
        [JsonPropertyName("attributes")]
        public Dictionary<string, object>? Attributes { get; set; }

        /// <summary>ISO 8601 timestamp when the invitation was created</summary>
        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>Whether this invitation has been revoked or expired</summary>
        [JsonPropertyName("deactivated")]
        public bool Deactivated { get; set; }

        /// <summary>Number of times the invitation was sent (including reminders)</summary>
        [JsonPropertyName("deliveryCount")]
        public int DeliveryCount { get; set; }

        /// <summary>Channels used to deliver this invitation (email, sms, share link)</summary>
        [JsonPropertyName("deliveryTypes")]
        public List<DeliveryType> DeliveryTypes { get; set; } = new();

        /// <summary>Your internal user ID for the person who created this invitation</summary>
        [JsonPropertyName("foreignCreatorId")]
        public string ForeignCreatorId { get; set; } = string.Empty;

        /// <summary>Type of invitation: single_use (1:1), multi_use (1:many), or autojoin</summary>
        [JsonPropertyName("invitationType")]
        public InvitationType InvitationType { get; set; }

        /// <summary>ISO 8601 timestamp of last modification</summary>
        [JsonPropertyName("modifiedAt")]
        public string? ModifiedAt { get; set; }

        /// <summary>Current status: queued, sending, sent, delivered, accepted, shared, unfurled</summary>
        [JsonPropertyName("status")]
        public InvitationStatus Status { get; set; }

        /// <summary>List of invitation recipients with their contact info and status</summary>
        [JsonPropertyName("target")]
        public List<InvitationTarget> Target { get; set; } = new();

        /// <summary>Number of times the invitation page was viewed</summary>
        [JsonPropertyName("views")]
        public int Views { get; set; }

        /// <summary>Widget configuration ID used for this invitation</summary>
        [JsonPropertyName("widgetConfigurationId")]
        public string WidgetConfigurationId { get; set; } = string.Empty;

        /// <summary>Deployment ID this invitation belongs to</summary>
        [JsonPropertyName("deploymentId")]
        public string DeploymentId { get; set; } = string.Empty;

        /// <summary>Scopes (teams/orgs) this invitation grants access to</summary>
        [JsonPropertyName("groups")]
        public List<InvitationScope> Groups { get; set; } = new();

        /// <summary>Preferred alias for Groups. Each element also has ScopeId.</summary>
        [JsonIgnore]
        public List<InvitationScope> Scopes
        {
            get => Groups;
            set => Groups = value;
        }

        /// <summary>List of acceptance records if the invitation was accepted (optional)</summary>
        [JsonPropertyName("accepts")]
        public List<InvitationAcceptance>? Accepts { get; set; }

        /// <summary>Primary scope identifier (e.g., "team-123")</summary>
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        /// <summary>Type of the primary scope (e.g., "team", "organization")</summary>
        [JsonPropertyName("scopeType")]
        public string? ScopeType { get; set; }

        /// <summary>Whether this invitation has passed its expiration date</summary>
        [JsonPropertyName("expired")]
        public bool Expired { get; set; }

        /// <summary>ISO 8601 timestamp when this invitation expires</summary>
        [JsonPropertyName("expires")]
        public string? Expires { get; set; }

        /// <summary>Custom metadata attached to this invitation</summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>Pass-through data returned unchanged in webhooks and callbacks</summary>
        [JsonPropertyName("passThrough")]
        public string? PassThrough { get; set; }

        /// <summary>Source identifier for tracking (e.g., "ios-app", "web-dashboard")</summary>
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>Subtype for analytics segmentation (e.g., "pymk", "find-friends")</summary>
        [JsonPropertyName("subtype")]
        public string? Subtype { get; set; }

        /// <summary>Display name of the user who created this invitation</summary>
        [JsonPropertyName("creatorName")]
        public string? CreatorName { get; set; }

        /// <summary>Avatar URL of the user who created this invitation</summary>
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
    /// Target recipient when creating an invitation
    /// </summary>
    public class CreateInvitationTarget
    {
        /// <summary>Delivery channel: email, phone, or internal (in-app)</summary>
        [JsonPropertyName("type")]
        public CreateInvitationTargetTypeEnum Type { get; set; }

        /// <summary>Target address: email, phone number (with country code), or internal user ID</summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>Display name of the recipient (shown in invitation emails and UI)</summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>Avatar URL for the recipient (displayed in invitation lists)</summary>
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
    /// Information about the user sending the invitation - used for attribution and display
    /// </summary>
    public class Inviter
    {
        /// <summary>Your internal user ID for the inviter (required for attribution)</summary>
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>Inviter's email address (used for reply-to and identification)</summary>
        [JsonPropertyName("userEmail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UserEmail { get; set; }

        /// <summary>Display name shown to recipients (e.g., "John invited you to...")</summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>Avatar URL displayed in invitation emails and widgets</summary>
        [JsonPropertyName("avatarUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AvatarUrl { get; set; }

        /// <summary>DEPRECATED: Use Name instead</summary>
        [JsonPropertyName("userName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [Obsolete("Use Name instead")]
        public string? UserName { get; set; }

        /// <summary>DEPRECATED: Use AvatarUrl instead</summary>
        [JsonPropertyName("userAvatarUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [Obsolete("Use AvatarUrl instead")]
        public string? UserAvatarUrl { get; set; }

        public Inviter() { }

        public Inviter(string userId, string? userEmail = null, string? name = null, string? avatarUrl = null)
        {
            UserId = userId;
            UserEmail = userEmail;
            Name = name;
            AvatarUrl = avatarUrl;
        }
    }

    /// <summary>
    /// Group information for creating invitations
    /// </summary>
    public class CreateInvitationScope
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
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Preferred alias for Scope/groupId
        /// </summary>
        [JsonIgnore]
        public string ScopeId
        {
            get => Scope;
            set => Scope = value;
        }

        /// <summary>
        /// Display name of the group
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public CreateInvitationScope() { }

        public CreateInvitationScope(string type, string groupId, string name)
        {
            Type = type;
            Scope = groupId;
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
        public List<CreateInvitationScope>? Groups { get; set; }

        /// <summary>Preferred: flat scope ID for single scope (takes priority over Groups/Scopes)</summary>
        [JsonIgnore]
        public string? ScopeId { get; set; }

        /// <summary>Scope type when using flat ScopeId param</summary>
        [JsonIgnore]
        public string? ScopeType { get; set; }

        /// <summary>Scope name when using flat ScopeId param</summary>
        [JsonIgnore]
        public string? ScopeName { get; set; }

        /// <summary>Deprecated: use ScopeId/ScopeType/ScopeName or Groups</summary>
        [JsonIgnore]
        public List<CreateInvitationScope>? Scopes { get; set; }

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

    // --- Types for syncing internal invitation actions ---

    /// <summary>
    /// Request body for syncing an internal invitation action
    /// </summary>
    public class SyncInternalInvitationRequest
    {
        /// <summary>The inviter's user ID</summary>
        [JsonPropertyName("creatorId")]
        public string CreatorId { get; set; } = string.Empty;

        /// <summary>The invitee's user ID</summary>
        [JsonPropertyName("targetValue")]
        public string TargetValue { get; set; } = string.Empty;

        /// <summary>The action taken: "accepted" or "declined"</summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>The widget component UUID</summary>
        [JsonPropertyName("componentId")]
        public string ComponentId { get; set; } = string.Empty;

        public SyncInternalInvitationRequest() { }

        public SyncInternalInvitationRequest(string creatorId, string targetValue, string action, string componentId)
        {
            CreatorId = creatorId;
            TargetValue = targetValue;
            Action = action;
            ComponentId = componentId;
        }
    }

    /// <summary>
    /// Response from syncing an internal invitation action
    /// </summary>
    public class SyncInternalInvitationResponse
    {
        /// <summary>Number of invitations processed</summary>
        [JsonPropertyName("processed")]
        public int Processed { get; set; }

        /// <summary>IDs of the invitations that were processed</summary>
        [JsonPropertyName("invitationIds")]
        public List<string> InvitationIds { get; set; } = new();
    }

    // --- Types for autojoin domain management ---

    /// <summary>
    /// Autojoin domain - users with matching email domains automatically join the scope
    /// </summary>
    public class AutojoinDomain
    {
        /// <summary>Unique identifier for this autojoin configuration</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>Email domain that triggers autojoin (e.g., "acme.com")</summary>
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

        [JsonPropertyName("componentId")]
        public string ComponentId { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Metadata { get; set; }

        public ConfigureAutojoinRequest() { }

        public ConfigureAutojoinRequest(string scope, string scopeType, List<string> domains, string componentId)
        {
            Scope = scope;
            ScopeType = scopeType;
            Domains = domains;
            ComponentId = componentId;
        }
    }

    // ─── GenerateToken types ────────────────────────────────────

    /// <summary>
    /// User data for GenerateToken - represents the authenticated user sending invitations
    /// </summary>
    public class TokenUser
    {
        /// <summary>Unique identifier for the user in your system. Used to attribute invitations and track referral chains.</summary>
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Id { get; set; }

        /// <summary>Display name shown to invitation recipients (e.g., "John invited you")</summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>User's email address. Used for reply-to in invitation emails.</summary>
        [JsonPropertyName("email")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Email { get; set; }

        /// <summary>URL to user's avatar image. Displayed in invitation emails and widgets.</summary>
        [JsonPropertyName("avatarUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AvatarUrl { get; set; }

        /// <summary>List of scope IDs where this user has admin privileges (e.g., ["team:team-123"])</summary>
        [JsonPropertyName("adminScopes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? AdminScopes { get; set; }

        public TokenUser() { }

        public TokenUser(string id)
        {
            Id = id;
        }

        public TokenUser(string? id, string? name = null, string? email = null)
        {
            Id = id;
            Name = name;
            Email = email;
        }
    }

    /// <summary>
    /// Payload for GenerateToken - used to generate secure tokens for Vortex components
    /// </summary>
    public class GenerateTokenPayload
    {
        /// <summary>The authenticated user who will be using the Vortex component</summary>
        [JsonPropertyName("user")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TokenUser? User { get; set; }

        /// <summary>Component ID to generate token for (from your Vortex dashboard)</summary>
        [JsonPropertyName("component")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Component { get; set; }

        /// <summary>Scope identifier to restrict invitations to a specific team/org (format: "scopeType:scopeId")</summary>
        [JsonPropertyName("scope")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Scope { get; set; }

        /// <summary>Custom variables to pass to the component for template rendering</summary>
        [JsonPropertyName("vars")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Vars { get; set; }

        /// <summary>Additional properties for forward compatibility</summary>
        [JsonExtensionData]
        public Dictionary<string, object>? Extra { get; set; }

        public GenerateTokenPayload() { }

        public GenerateTokenPayload(TokenUser user)
        {
            User = user;
        }
    }

    /// <summary>
    /// Options for GenerateToken
    /// </summary>
    public class GenerateTokenOptions
    {
        /// <summary>
        /// Token expiry. String format like "5m", "1h", "24h", "7d" or integer seconds.
        /// Default: 30 days.
        /// </summary>
        public object? ExpiresIn { get; set; }

        public GenerateTokenOptions() { }

        public GenerateTokenOptions(string expiresIn)
        {
            ExpiresIn = expiresIn;
        }

        public GenerateTokenOptions(int expiresInSeconds)
        {
            ExpiresIn = expiresInSeconds;
        }
    }
}
