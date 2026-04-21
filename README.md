# vortex-csharp-sdk

<!-- AUTO-GENERATED FROM SDK MANIFEST — DO NOT EDIT DIRECTLY -->

![Version](https://img.shields.io/badge/version-1.19.0-blue)
![Language](https://img.shields.io/badge/language-csharp-green)

**Invitation infrastructure for modern apps**

Vortex handles the complete invitation lifecycle — sending invites via email/SMS/share links, tracking clicks and conversions, managing referral programs, and optimizing your invitation flows with A/B testing.
[Learn more about Vortex →](https://tryvortex.com)

## Why This SDK?

This backend SDK securely signs user data for Vortex components. Your API key stays on your server, while the signed token is passed to the frontend where Vortex components render the invitation UI.

- Keep your API key secure — it never touches the browser
- Sign user identity for attribution — know who sent each invitation
- Control what data components can access via scoped tokens
- Verify webhook signatures for secure event handling

## How It Works

Vortex uses a split architecture: your backend signs tokens with the SDK, and your frontend renders components that use those tokens to securely interact with Vortex.

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Your Server   │     │  User Browser   │     │  Vortex Cloud   │
│    (this SDK)   │     │   (component)   │     │                 │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         │  1. GenerateToken()   │                       │
         │◄──────────────────────│                       │
         │                       │                       │
         │  2. Return token      │                       │
         │──────────────────────►│                       │
         │                       │                       │
         │                       │  3. Component calls   │
         │                       │     API with token    │
         │                       │──────────────────────►│
         │                       │                       │
         │                       │  4. Render UI,        │
         │                       │     send invitations  │
         │                       │◄──────────────────────│
         │                       │                       │
```

### Integration Flow

**1. Install the backend SDK** `[backend]`

Add this SDK to your .NET project

```csharp
dotnet add package TeamVortexSoftware.VortexSDK
```

**2. Initialize the client** `[backend]`

Create a Vortex client with your API key (keep this on the server!)

```csharp
using TeamVortexSoftware.VortexSDK;

var client = new VortexClient(
    Environment.GetEnvironmentVariable("VORTEX_API_KEY")
);
```

**3. Generate a token for the current user** `[backend]`

When a user loads a page with a Vortex component, generate a signed token on your server

```csharp
var token = client.GenerateToken(new GenerateTokenPayload
{
    User = new TokenUser { Id = currentUser.Id }
});
```

**4. Pass the token to your frontend** `[backend]`

Include the token in your page response or API response

```csharp
return Ok(new { vortexToken = token });
```

**5. Render a Vortex component with the token** `[frontend]`

Use the React/Angular/Web Component with the token

```csharp
import { VortexInvite } from "@teamvortexsoftware/vortex-react";

<VortexInvite token={vortexToken} />
```

**6. Vortex handles the rest** `[vortex]`

The component securely communicates with Vortex servers, displays the invitation UI, sends emails/SMS, tracks conversions, and reports analytics

### Security Model

> ⚠️ **Important:** Your Vortex API key is a secret that grants full access to your account. It must never be exposed to browsers or client-side code.

By signing tokens on your server, you:

- Keep your API key secret (it never leaves your server)
- Control exactly what user data is shared with components
- Ensure invitations are attributed to real, authenticated users
- Prevent abuse — users can only send invitations as themselves

#### When Signing is Optional

Token signing is controlled by your component configuration in the Vortex dashboard.

---

## Quick Start

Generate a secure token for Vortex components

```csharp
using TeamVortexSoftware.VortexSDK;

var client = new VortexClient(Environment.GetEnvironmentVariable("VORTEX_API_KEY"));

var payload = new GenerateTokenPayload
{
    User = new TokenUser { Id = "user-123", Email = "user@example.com" }
};

var token = client.GenerateToken(payload);
```

## Installation

```bash
dotnet add package TeamVortexSoftware.VortexSDK
```

<details>
<summary>Other package managers</summary>

**PackageManager:**

```bash
Install-Package TeamVortexSoftware.VortexSDK
```

</details>

## Initialization

```csharp
var client = new VortexClient(Environment.GetEnvironmentVariable("VORTEX_API_KEY"));
```

### Environment Variables

| Variable         | Required | Description         |
| ---------------- | -------- | ------------------- |
| `VORTEX_API_KEY` | ✓        | Your Vortex API key |

## Core Methods

These are the methods you'll use most often.

### `GenerateToken()`

Generate a signed token for use with Vortex widgets

**Signature:**

```csharp
GenerateToken(GenerateTokenPayload payload, GenerateTokenOptions? options): string
```

**Parameters:**

| Name      | Type                    | Required | Description                                       |
| --------- | ----------------------- | -------- | ------------------------------------------------- |
| `payload` | `GenerateTokenPayload`  | ✓        | Data to sign (user, component, scope, vars, etc.) |
| `options` | `GenerateTokenOptions?` |          | Optional configuration (ExpiresIn)                |

**Returns:** `string`
— Signed JWT token string

_Added in v0.8.0_

---

### `GetInvitationAsync()`

Get a specific invitation by ID

**Signature:**

```csharp
GetInvitationAsync(string invitationId): Task<Invitation>
```

**Parameters:**

| Name           | Type     | Required | Description       |
| -------------- | -------- | -------- | ----------------- |
| `invitationId` | `string` | ✓        | The invitation ID |

**Returns:** `Task<Invitation>`
— The invitation details

_Added in v0.1.0_

---

### `AcceptInvitationAsync()`

Accept a single invitation (recommended method)

**Signature:**

```csharp
AcceptInvitationAsync(string invitationId, AcceptUser user): Task<Invitation>
```

**Parameters:**

| Name           | Type         | Required | Description                                  |
| -------------- | ------------ | -------- | -------------------------------------------- |
| `invitationId` | `string`     | ✓        | Single invitation ID to accept               |
| `user`         | `AcceptUser` | ✓        | User with email or phone (and optional name) |

**Returns:** `Task<Invitation>`
— Invitation result

_Added in v0.6.0_

---

## All Methods

<details>
<summary>Click to expand full method reference</summary>

### `GetInvitationsByTargetAsync()`

Get invitations by target (email or sms)

**Signature:**

```csharp
GetInvitationsByTargetAsync(string targetType, string targetValue): Task<List<Invitation>>
```

**Parameters:**

| Name          | Type     | Required | Description                   |
| ------------- | -------- | -------- | ----------------------------- |
| `targetType`  | `string` | ✓        | Type of target (email, phone) |
| `targetValue` | `string` | ✓        | The target value              |

**Returns:** `Task<List<Invitation>>`
— List of invitations

_Added in v0.1.0_

---

### `RevokeInvitationAsync()`

Revoke (delete) an invitation

**Signature:**

```csharp
RevokeInvitationAsync(string invitationId): Task
```

**Parameters:**

| Name           | Type     | Required | Description                 |
| -------------- | -------- | -------- | --------------------------- |
| `invitationId` | `string` | ✓        | The invitation ID to revoke |

**Returns:** `Task`

_Added in v0.1.0_

---

### `AcceptInvitationsAsync()`

Accept multiple invitations using the new User format (preferred)

**Signature:**

```csharp
AcceptInvitationsAsync(List<string> invitationIds, AcceptUser user): Task<Invitation>
```

**Parameters:**

| Name            | Type           | Required | Description                                  |
| --------------- | -------------- | -------- | -------------------------------------------- |
| `invitationIds` | `List<string>` | ✓        | List of invitation IDs to accept             |
| `user`          | `AcceptUser`   | ✓        | User with email or phone (and optional name) |

**Returns:** `Task<Invitation>`
— Invitation result

_Added in v0.1.0_

---

### `DeleteInvitationsByScopeAsync()`

Delete all invitations for a specific scope

**Signature:**

```csharp
DeleteInvitationsByScopeAsync(string groupType, string groupId): Task
```

**Parameters:**

| Name        | Type     | Required | Description                               |
| ----------- | -------- | -------- | ----------------------------------------- |
| `groupType` | `string` | ✓        | The scope type (organization, team, etc.) |
| `groupId`   | `string` | ✓        | The scope identifier                      |

**Returns:** `Task`

_Added in v0.4.0_

---

### `GetInvitationsByScopeAsync()`

Get all invitations for a specific scope

**Signature:**

```csharp
GetInvitationsByScopeAsync(string groupType, string groupId): Task<List<Invitation>>
```

**Parameters:**

| Name        | Type     | Required | Description                               |
| ----------- | -------- | -------- | ----------------------------------------- |
| `groupType` | `string` | ✓        | The scope type (organization, team, etc.) |
| `groupId`   | `string` | ✓        | The scope identifier                      |

**Returns:** `Task<List<Invitation>>`
— List of invitations for the scope

_Added in v0.4.0_

---

### `ReinviteAsync()`

Reinvite a user (send invitation again)

**Signature:**

```csharp
ReinviteAsync(string invitationId): Task<Invitation>
```

**Parameters:**

| Name           | Type     | Required | Description                   |
| -------------- | -------- | -------- | ----------------------------- |
| `invitationId` | `string` | ✓        | The invitation ID to reinvite |

**Returns:** `Task<Invitation>`
— The reinvited invitation result

_Added in v0.2.0_

---

### `CreateInvitationAsync()`

Create an invitation from your backend. This method allows you to create invitations programmatically using your API key, without requiring a user JWT token. Useful for server-side invitation creation, such as "People You May Know" flows or admin-initiated invitations.

**Signature:**

```csharp
CreateInvitationAsync(CreateInvitationRequest request): Task<CreateInvitationResponse>
```

**Parameters:**

| Name      | Type                      | Required | Description                   |
| --------- | ------------------------- | -------- | ----------------------------- |
| `request` | `CreateInvitationRequest` | ✓        | The create invitation request |

**Returns:** `Task<CreateInvitationResponse>`
— CreateInvitationResponse with id, shortLink, status, and createdAt

**Example:**

```csharp
// Create an email invitation
var request = new CreateInvitationRequest(
    "widget-config-123",
    CreateInvitationTarget.Email("invitee@example.com"),
    new Inviter("user-456", "inviter@example.com", "John Doe")
);
request.Groups = new List&lt;CreateInvitationScope&gt;
{
    new("team", "team-789", "Engineering")
};
var response = await client.CreateInvitationAsync(request);
// Create an internal invitation (PYMK flow)
var pymkRequest = new CreateInvitationRequest(
    "widget-config-123",
    CreateInvitationTarget.Internal("internal-user-abc"),
    new Inviter("user-456")
);
pymkRequest.Source = "pymk";
var response = await client.CreateInvitationAsync(pymkRequest);
```

_Added in v0.1.0_

---

### `GetAutojoinDomainsAsync()`

Get autojoin domains configured for a specific scope

**Signature:**

```csharp
GetAutojoinDomainsAsync(string scopeType, string scope): Task<AutojoinDomainsResponse>
```

**Parameters:**

| Name        | Type     | Required | Description                                                 |
| ----------- | -------- | -------- | ----------------------------------------------------------- |
| `scopeType` | `string` | ✓        | The type of scope (e.g., "organization", "team", "project") |
| `scope`     | `string` | ✓        | The scope identifier (customer's group ID)                  |

**Returns:** `Task<AutojoinDomainsResponse>`
— AutojoinDomainsResponse with autojoin domains and invitation

_Added in v0.6.0_

---

### `ConfigureAutojoinAsync()`

Configure autojoin domains for a specific scope

**Signature:**

```csharp
ConfigureAutojoinAsync(ConfigureAutojoinRequest request): Task<AutojoinDomainsResponse>
```

**Parameters:**

| Name      | Type                       | Required | Description                    |
| --------- | -------------------------- | -------- | ------------------------------ |
| `request` | `ConfigureAutojoinRequest` | ✓        | The configure autojoin request |

**Returns:** `Task<AutojoinDomainsResponse>`
— AutojoinDomainsResponse with updated autojoin domains

_Added in v0.6.0_

---

### `SyncInternalInvitationAsync()`

Sync an internal invitation action (accept or decline)

**Signature:**

```csharp
SyncInternalInvitationAsync(SyncInternalInvitationRequest request): Task<SyncInternalInvitationResponse>
```

**Parameters:**

| Name      | Type                            | Required | Description                          |
| --------- | ------------------------------- | -------- | ------------------------------------ |
| `request` | `SyncInternalInvitationRequest` | ✓        | The sync internal invitation request |

**Returns:** `Task<SyncInternalInvitationResponse>`
— SyncInternalInvitationResponse with processed count and invitationIds

**Example:**

```csharp
var request = new SyncInternalInvitationRequest("user-123", "user-456", "accepted", "component-uuid");
var response = await client.SyncInternalInvitationAsync(request);
Console.WriteLine($"Processed: {response.Processed}");
```

_Added in v0.1.0_

---

</details>

## Types

<details>
<summary>Click to expand type definitions</summary>

### `GenerateTokenPayload`

Payload for GenerateToken - used to generate secure tokens for Vortex components

| Field       | Type                          | Required | Description                                                                                   |
| ----------- | ----------------------------- | -------- | --------------------------------------------------------------------------------------------- |
| `user`      | `TokenUser?`                  |          | The authenticated user who will be using the Vortex component                                 |
| `component` | `string?`                     |          | Component ID to generate token for (from your Vortex dashboard)                               |
| `scope`     | `string?`                     |          | Scope identifier to restrict invitations to a specific team/org (format: "scopeType:scopeId") |
| `vars`      | `Dictionary<string, object>?` |          | Custom variables to pass to the component for template rendering                              |

### `TokenUser`

User data for GenerateToken - represents the authenticated user sending invitations

| Field         | Type            | Required | Description                                                                                             |
| ------------- | --------------- | -------- | ------------------------------------------------------------------------------------------------------- |
| `id`          | `string?`       |          | Unique identifier for the user in your system. Used to attribute invitations and track referral chains. |
| `name`        | `string?`       |          | Display name shown to invitation recipients (e.g., "John invited you")                                  |
| `email`       | `string?`       |          | User's email address. Used for reply-to in invitation emails.                                           |
| `avatarUrl`   | `string?`       |          | URL to user's avatar image. Displayed in invitation emails and widgets.                                 |
| `adminScopes` | `List<string>?` |          | List of scope IDs where this user has admin privileges (e.g., ["team:team-123"])                        |

### `AcceptUser`

User data for accepting invitations - identifies who accepted the invitation

| Field        | Type      | Required | Description                                                                                                        |
| ------------ | --------- | -------- | ------------------------------------------------------------------------------------------------------------------ |
| `email`      | `string?` |          | Email address of the user accepting. At least one of Email or Phone is required.                                   |
| `phone`      | `string?` |          | Phone number with country code (e.g., "+15551234567"). At least one of Email or Phone is required.                 |
| `name`       | `string?` |          | Display name of the accepting user (shown in notifications to inviter)                                             |
| `isExisting` | `bool?`   |          | Whether user was already registered. true=existing, false=new signup, null=unknown. Used for conversion analytics. |

### `Invitation`

Complete invitation details as returned by the Vortex API

| Field                     | Type                          | Required | Description                                                                                                            |
| ------------------------- | ----------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------------- |
| `id`                      | `string`                      | ✓        | Unique identifier for this invitation                                                                                  |
| `accountId`               | `string`                      |          | Your Vortex account ID                                                                                                 |
| `clickThroughs`           | `int`                         |          | Number of times the invitation link was clicked                                                                        |
| `formSubmissionData`      | `Dictionary<string, object>?` |          | Invitation form data submitted by the user, including invitee identifiers (such as email addresses, phone numbers, or internal IDs) and the values of any custom fields. |
| `configurationAttributes` | `Dictionary<string, object>?` |          |                                                                                                                        |
| `attributes`              | `Dictionary<string, object>?` |          | Custom attributes attached to this invitation                                                                          |
| `createdAt`               | `string`                      |          | ISO 8601 timestamp when the invitation was created                                                                     |
| `deactivated`             | `bool`                        |          | Whether this invitation has been revoked or expired                                                                    |
| `deliveryCount`           | `int`                         |          | Number of times the invitation was sent (including reminders)                                                          |
| `deliveryTypes`           | `List<DeliveryType>`          |          | Channels used to deliver this invitation (email, sms, share link)                                                      |
| `foreignCreatorId`        | `string`                      |          | Your internal user ID for the person who created this invitation                                                       |
| `invitationType`          | `InvitationType`              |          | Type of invitation: single_use (1:1), multi_use (1:many), or autojoin                                                  |
| `modifiedAt`              | `string?`                     |          | ISO 8601 timestamp of last modification                                                                                |
| `status`                  | `InvitationStatus`            |          | Current status: queued, sending, sent, delivered, accepted, shared, unfurled                                           |
| `target`                  | `List<InvitationTarget>`      |          | List of invitation recipients with their contact info and status                                                       |
| `views`                   | `int`                         |          | Number of times the invitation page was viewed                                                                         |
| `widgetConfigurationId`   | `string`                      |          | Widget configuration ID used for this invitation                                                                       |
| `deploymentId`            | `string`                      |          | Deployment ID this invitation belongs to                                                                               |
| `groups`                  | `List<InvitationScope>`       |          | Scopes (teams/orgs) this invitation grants access to                                                                   |
| `accepts`                 | `List<InvitationAcceptance>?` |          | List of acceptance records if the invitation was accepted (optional)                                                   |
| `scope`                   | `string?`                     |          | Primary scope identifier (e.g., "team-123")                                                                            |
| `scopeType`               | `string?`                     |          | Type of the primary scope (e.g., "team", "organization")                                                               |
| `expired`                 | `bool`                        |          | Whether this invitation has passed its expiration date                                                                 |
| `expires`                 | `string?`                     |          | ISO 8601 timestamp when this invitation expires                                                                        |
| `metadata`                | `Dictionary<string, object>?` |          | Custom metadata attached to this invitation                                                                            |
| `passThrough`             | `string?`                     |          | Pass-through data returned unchanged in webhooks and callbacks                                                         |
| `source`                  | `string?`                     |          | Source identifier for tracking (e.g., "ios-app", "web-dashboard")                                                      |
| `subtype`                 | `string?`                     |          | Subtype for analytics segmentation (e.g., "pymk", "find-friends")                                                      |
| `creatorName`             | `string?`                     |          | Display name of the user who created this invitation                                                                   |
| `creatorAvatarUrl`        | `string?`                     |          | Avatar URL of the user who created this invitation                                                                     |

### `InvitationTarget`

Represents the target recipient of an invitation

| Field       | Type                   | Required | Description                                                             |
| ----------- | ---------------------- | -------- | ----------------------------------------------------------------------- |
| `type`      | `InvitationTargetType` | ✓        | Delivery channel: email, phone, share (link), or internal (in-app)      |
| `value`     | `string`               |          | Target address: email, phone number with country code, or share link ID |
| `name`      | `string?`              |          | Display name of the recipient (e.g., "John Doe")                        |
| `avatarUrl` | `string?`              |          | Avatar URL for the recipient, shown in invitation lists and widgets     |

### `InvitationScope`

Invitation group from API responses This matches the MemberGroups table structure from the API

| Field       | Type     | Required | Description                                          |
| ----------- | -------- | -------- | ---------------------------------------------------- |
| `id`        | `string` | ✓        | Vortex internal UUID                                 |
| `accountId` | `string` |          | Vortex account ID                                    |
| `groupId`   | `string` |          | Customer's group ID (the ID they provided to Vortex) |
| `type`      | `string` | ✓        | Group type (e.g., "workspace", "team")               |
| `name`      | `string` |          | Group name                                           |
| `createdAt` | `string` |          | ISO 8601 timestamp when the group was created        |

### `CreateInvitationTarget`

Target recipient when creating an invitation

| Field       | Type                             | Required | Description                                                                  |
| ----------- | -------------------------------- | -------- | ---------------------------------------------------------------------------- |
| `type`      | `CreateInvitationTargetTypeEnum` | ✓        | Delivery channel: email, phone, or internal (in-app)                         |
| `value`     | `string`                         |          | Target address: email, phone number (with country code), or internal user ID |
| `name`      | `string?`                        |          | Display name of the recipient (shown in invitation emails and UI)            |
| `avatarUrl` | `string?`                        |          | Avatar URL for the recipient (displayed in invitation lists)                 |

### `Inviter`

Information about the user sending the invitation - used for attribution and display

| Field       | Type      | Required | Description                                                       |
| ----------- | --------- | -------- | ----------------------------------------------------------------- |
| `userId`    | `string`  | ✓        | Your internal user ID for the inviter (required for attribution)  |
| `userEmail` | `string?` |          | Inviter's email address (used for reply-to and identification)    |
| `name`      | `string?` |          | Display name shown to recipients (e.g., "John invited you to...") |
| `avatarUrl` | `string?` |          | Avatar URL displayed in invitation emails and widgets             |

### `User`

User data for JWT generation - represents the authenticated user sending invitations. Only Id is required. Email is optional but recommended for invitation attribution.

| Field                 | Type            | Required | Description                                                             |
| --------------------- | --------------- | -------- | ----------------------------------------------------------------------- |
| `id`                  | `string`        | ✓        | Your internal user ID (required for invitation attribution)             |
| `email`               | `string?`       |          | User's email address (optional, used for reply-to in invitation emails) |
| `name`                | `string?`       |          | Display name shown to recipients (e.g., "John invited you")             |
| `avatarUrl`           | `string?`       |          | Avatar URL displayed in invitation emails and widgets (must be HTTPS)   |
| `adminScopes`         | `List<string>?` |          | List of scopes where user has admin privileges (e.g., ["autojoin"])     |
| `allowedEmailDomains` | `List<string>?` |          | Restrict invitations to these email domains (e.g., ["acme.com"])        |

### `Identifier`

Identifier for a user - used in JWT generation to link user across channels

| Field   | Type     | Required | Description                                                   |
| ------- | -------- | -------- | ------------------------------------------------------------- |
| `type`  | `string` | ✓        | Identifier type: "email", "phone", "username", or custom type |
| `value` | `string` |          | The identifier value (email address, phone number, etc.)      |

### `Group`

Scope/group for JWT generation - defines user's team/org membership in tokens

| Field     | Type      | Required | Description                                            |
| --------- | --------- | -------- | ------------------------------------------------------ |
| `type`    | `string`  | ✓        | Scope type (e.g., "team", "organization", "workspace") |
| `id`      | `string?` |          | Legacy scope identifier. Use Scope/groupId instead.    |
| `groupId` | `string?` |          | Your internal scope/group identifier (preferred)       |
| `name`    | `string`  |          | Display name for the scope (e.g., "Engineering Team")  |

### `InvitationAcceptance`

Record of an invitation being accepted

| Field        | Type                | Required | Description                                         |
| ------------ | ------------------- | -------- | --------------------------------------------------- |
| `id`         | `string`            | ✓        | Unique identifier for this acceptance record        |
| `accountId`  | `string`            |          | Your Vortex account ID                              |
| `acceptedAt` | `string`            |          | ISO 8601 timestamp when the invitation was accepted |
| `target`     | `InvitationTarget?` |          | The target that accepted the invitation             |

### `CreateInvitationScope`

Group information for creating invitations

| Field     | Type     | Required | Description                               |
| --------- | -------- | -------- | ----------------------------------------- |
| `type`    | `string` | ✓        | Group type (e.g., "team", "organization") |
| `groupId` | `string` |          | Your internal group ID                    |
| `name`    | `string` |          | Display name of the group                 |

### `UnfurlConfig`

Configuration for link unfurl (Open Graph) metadata. Controls how the invitation link appears when shared on social platforms or messaging apps.

| Field         | Type      | Required | Description                                                           |
| ------------- | --------- | -------- | --------------------------------------------------------------------- |
| `title`       | `string?` |          | The title shown in link previews (og:title)                           |
| `description` | `string?` |          | The description shown in link previews (og:description)               |
| `image`       | `string?` |          | The image URL shown in link previews (og:image) - must be HTTPS       |
| `type`        | `string?` |          | The Open Graph type (og:type) - e.g., 'website', 'article', 'product' |
| `siteName`    | `string?` |          | The site name shown in link previews (og:site_name)                   |

### `AutojoinDomain`

Autojoin domain - users with matching email domains automatically join the scope

| Field    | Type     | Required | Description                                            |
| -------- | -------- | -------- | ------------------------------------------------------ |
| `id`     | `string` | ✓        | Unique identifier for this autojoin configuration      |
| `domain` | `string` |          | Email domain that triggers autojoin (e.g., "acme.com") |

</details>

## Webhooks

Webhooks let your server receive real-time notifications when events happen in Vortex. Use them to sync invitation state with your database, trigger onboarding flows, update your CRM, or send internal notifications.

### Setup

1. Go to your Vortex dashboard → Integrations → Webhooks tab
2. Click "Add Webhook"
3. Enter your endpoint URL (must be HTTPS in production)
4. Copy the signing secret — you'll use this to verify webhook signatures
5. Select which events you want to receive

### Verifying Webhooks

Always verify webhook signatures using `VortexWebhooks.VerifySignature()` to ensure requests are from Vortex.
The signature is sent in the `X-Vortex-Signature` header.

### Example: ASP.NET Core webhook handler

```csharp
using TeamVortexSoftware.VortexSDK;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("webhooks")]
public class WebhookController : ControllerBase
{
    private readonly VortexWebhooks _webhooks = new(
        Environment.GetEnvironmentVariable("VORTEX_WEBHOOK_SECRET")
    );

    [HttpPost("vortex")]
    public IActionResult HandleWebhook(
        [FromBody] string body,
        [FromHeader(Name = "X-Vortex-Signature")] string signature)
    {
        // Verify the signature
        if (!_webhooks.VerifySignature(body, signature))
            return BadRequest("Invalid signature");

        // Parse the event
        var webhookEvent = _webhooks.ParseEvent(body);

        switch (webhookEvent.Type)
        {
            case "invitation.accepted":
                // User accepted an invitation — activate their account
                Console.WriteLine($"Accepted: {webhookEvent.Data}");
                break;
            case "member.created":
                // New member joined via invitation
                Console.WriteLine($"New member: {webhookEvent.Data}");
                break;
        }

        return Ok(new { received = true });
    }
}
```

### Common Use Cases

**Activate users on acceptance**

When invitation.accepted fires, mark the user as active in your database and trigger your onboarding flow.

**Track invitation performance**

Monitor email.delivered, email.opened, and link.clicked events to measure invitation funnel metrics.

**Sync team membership**

Use member.created and group.member.added to keep your internal membership records in sync.

**Alert on delivery issues**

Watch for email.bounced events to proactively reach out via alternative channels.

### Supported Events

| Event                        | Description                                          |
| ---------------------------- | ---------------------------------------------------- |
| `invitation.created`         | A new invitation was created                         |
| `invitation.accepted`        | An invitation was accepted by the recipient          |
| `invitation.deactivated`     | An invitation was deactivated (revoked or expired)   |
| `invitation.email.delivered` | Invitation email was successfully delivered          |
| `invitation.email.bounced`   | Invitation email bounced (invalid address)           |
| `invitation.email.opened`    | Recipient opened the invitation email                |
| `invitation.link.clicked`    | Recipient clicked the invitation link                |
| `invitation.reminder.sent`   | A reminder email was sent for a pending invitation   |
| `member.created`             | A new member was created from an accepted invitation |
| `group.member.added`         | A member was added to a scope/group                  |
| `deployment.created`         | A new deployment configuration was created           |
| `deployment.deactivated`     | A deployment was deactivated                         |
| `abtest.started`             | An A/B test was started                              |
| `abtest.winner_declared`     | An A/B test winner was declared                      |
| `email.complained`           | Recipient marked the email as spam                   |

## Error Handling

All SDK errors extend `VortexException`.

| Error             | Description                                                                              |
| ----------------- | ---------------------------------------------------------------------------------------- |
| `VortexException` | Thrown for validation errors (e.g., missing API key, invalid parameters) or API failures |

---

<!-- Generated from SDK v1.19.0 manifest -->
