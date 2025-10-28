# Vortex C# SDK

This package provides the Vortex C# SDK for authentication and invitation management.

With this SDK, you can generate JWTs for use with the Vortex Widget and make API calls to the Vortex API.

## Installation

Install the SDK via NuGet:

```bash
dotnet add package TeamVortexSoftware.VortexSDK
```

Or via the Package Manager Console:

```powershell
Install-Package TeamVortexSoftware.VortexSDK
```

## Getting Started

Once you have the SDK installed, [login](https://admin.vortexsoftware.com/signin) to Vortex and [create an API Key](https://admin.vortexsoftware.com/members/api-keys). Keep your API key safe! Vortex does not store the API key and it is not retrievable once it has been created.

Your API key is used to:
- Sign JWTs for use with the Vortex Widget
- Make API calls against the [Vortex API](https://api.vortexsoftware.com/api)

## Usage

### Generate a JWT for the Vortex Widget

The Vortex Widget requires a JWT to authenticate users. Here's how to generate one:

```csharp
using TeamVortexSoftware.VortexSDK;

// Initialize the Vortex client with your API key
var vortex = new VortexClient(Environment.GetEnvironmentVariable("VORTEX_API_KEY"));

// User ID from your system
var userId = "users-id-in-my-system";

// Identifiers associated with the user
var identifiers = new List<Identifier>
{
    new Identifier("email", "user@example.com"),
    new Identifier("sms", "18008675309")
};

// Groups the user belongs to (specific to your product)
var groups = new List<Group>
{
    new Group("workspace", "My Workspace", groupId: "workspace-123"),
    new Group("document", "Project Plan", groupId: "doc-456")
};

// User role (if applicable)
var role = "admin";

// Generate the JWT
var jwt = vortex.GenerateJwt(userId, identifiers, groups, role);

Console.WriteLine(jwt);
```

### Use with ASP.NET Core

Create an API endpoint to provide JWTs to your frontend:

```csharp
using Microsoft.AspNetCore.Mvc;
using TeamVortexSoftware.VortexSDK;

[ApiController]
[Route("api/[controller]")]
public class VortexController : ControllerBase
{
    private readonly VortexClient _vortex;

    public VortexController(IConfiguration configuration)
    {
        _vortex = new VortexClient(configuration["Vortex:ApiKey"]);
    }

    [HttpGet("jwt")]
    public IActionResult GetJwt()
    {
        var userId = User.Identity?.Name ?? "anonymous";

        var identifiers = new List<Identifier>
        {
            new Identifier("email", User.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "")
        };

        var groups = new List<Group>
        {
            new Group("workspace", "Main Workspace", groupId: "ws-1")
        };

        var jwt = _vortex.GenerateJwt(userId, identifiers, groups, "member");

        return Ok(new { jwt });
    }
}
```

### Dependency Injection Setup

Register the VortexClient in your `Program.cs`:

```csharp
builder.Services.AddSingleton<VortexClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new VortexClient(config["Vortex:ApiKey"]);
});
```

Then inject it into your controllers or services:

```csharp
public class MyService
{
    private readonly VortexClient _vortex;

    public MyService(VortexClient vortex)
    {
        _vortex = vortex;
    }

    public async Task<string> GenerateUserJwt(User user)
    {
        var jwt = _vortex.GenerateJwt(
            user.Id,
            new List<Identifier> { new("email", user.Email) },
            user.Groups.Select(g => new Group(g.Type, g.Name, groupId: g.Id)).ToList(),
            user.Role
        );

        return jwt;
    }
}
```

## API Methods

All API methods are asynchronous and follow the async/await pattern.

### Invitation Management

#### Get Invitations by Target

```csharp
var invitations = await vortex.GetInvitationsByTargetAsync("email", "user@example.com");
```

#### Get Invitation by ID

```csharp
var invitation = await vortex.GetInvitationAsync("invitation-id");
```

#### Revoke Invitation

```csharp
await vortex.RevokeInvitationAsync("invitation-id");
```

#### Accept Invitations

```csharp
var target = new InvitationTarget("email", "user@example.com");
var result = await vortex.AcceptInvitationsAsync(
    new List<string> { "invitation-id-1", "invitation-id-2" },
    target
);
```

#### Get Invitations by Group

```csharp
var invitations = await vortex.GetInvitationsByGroupAsync("workspace", "workspace-123");
```

#### Delete Invitations by Group

```csharp
await vortex.DeleteInvitationsByGroupAsync("workspace", "workspace-123");
```

#### Reinvite

```csharp
var result = await vortex.ReinviteAsync("invitation-id");
```

## Data Types

### Group (Input for JWT Generation)

When creating groups for JWT generation, use the `groupId` parameter (preferred) or `id` (legacy):

```csharp
// Preferred: Using groupId (named parameter)
var group = new Group("workspace", "My Workspace", groupId: "workspace-123");

// Legacy: Using id (for backward compatibility)
var group = new Group("workspace", "My Workspace", id: "workspace-123");

// Setting properties directly
var group = new Group
{
    Type = "workspace",
    Name = "My Workspace",
    GroupId = "workspace-123"  // Preferred
};
```

### InvitationGroup (API Response)

When receiving invitation data from the API, groups include all fields:

```csharp
public class InvitationGroup
{
    public string Id { get; set; }         // Vortex internal UUID
    public string AccountId { get; set; }  // Vortex account ID
    public string GroupId { get; set; }    // Customer's group ID (your identifier)
    public string Type { get; set; }       // Group type (e.g., "workspace", "team")
    public string Name { get; set; }       // Group name
    public string CreatedAt { get; set; }  // ISO 8601 timestamp
}
```

## Requirements

- .NET 6.0 or higher
- System.Text.Json (included as dependency)

## Best Practices

### Dispose Pattern

The `VortexClient` implements `IDisposable`. Use it with a `using` statement when appropriate:

```csharp
using (var vortex = new VortexClient(apiKey))
{
    var jwt = vortex.GenerateJwt(userId, identifiers, groups, role);
    // Use jwt...
}
```

Or when using dependency injection, the framework will handle disposal automatically.

### Error Handling

All API methods can throw `VortexException`. Wrap calls in try-catch blocks:

```csharp
try
{
    var invitations = await vortex.GetInvitationsByTargetAsync("email", "user@example.com");
}
catch (VortexException ex)
{
    Console.WriteLine($"Vortex API error: {ex.Message}");
}
```

## License

MIT

## Support

For support, please contact support@vortexsoftware.com or visit our [documentation](https://docs.vortexsoftware.com).
