# Vortex C# SDK Implementation Guide

**Package:** `TeamVortexSoftware.VortexSDK`
**Compatible with:** .NET 6.0+ (optimized for .NET 8.0)

## Prerequisites
From integration contract you need: API endpoints, scope entity, authentication patterns
From discovery data you need: .NET version, Controllers vs Minimal APIs, database ORM, auth claim names

## Key Facts
- VortexClient registered as Singleton in DI container
- All I/O operations use async/await
- API key loaded from IConfiguration
- Three endpoints: JWT generation, get invitation, accept invitation

---

## Step 1: Discover .NET Patterns

Read: `Program.cs`, `appsettings.json`, example controller/endpoint, database context

Determine and note:
- .NET version (6.0, 7.0, 8.0)
- Controllers or Minimal APIs?
- Controllers/endpoints location
- Database ORM (Entity Framework Core, Dapper, etc.)
- Auth pattern (claim names for userId and email)

---

## Step 2: Register VortexClient in DI

**For .NET 6+ (top-level statements in `Program.cs`):**

```csharp
using TeamVortexSoftware.VortexSDK;

// Before builder.Build()
builder.Services.AddSingleton<VortexClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration["Vortex:ApiKey"]
        ?? throw new InvalidOperationException("Vortex:ApiKey is not configured");

    return new VortexClient(apiKey);
});
```

**For older .NET (`Startup.cs` pattern):**

```csharp
using TeamVortexSoftware.VortexSDK;

public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<VortexClient>(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var apiKey = configuration["Vortex:ApiKey"]
            ?? throw new InvalidOperationException("Vortex:ApiKey is not configured");

        return new VortexClient(apiKey);
    });
}
```

---

## Step 3: Add Configuration

**Update `appsettings.json`:**

```json
{
  "Vortex": {
    "ApiKey": ""
  }
}
```

**For development, update `appsettings.Development.json`:**

```json
{
  "Vortex": {
    "ApiKey": "VRTX.your-dev-api-key"
  }
}
```

**For production:** Use environment variable `VORTEX__APIKEY` or cloud secrets (Azure Key Vault, AWS Secrets Manager).

---

## Step 4: Create Endpoints

### Option A: Controllers

Create `Controllers/VortexController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamVortexSoftware.VortexSDK;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/vortex")] // Adapt to contract
    [Authorize]
    public class VortexController : ControllerBase
    {
        private readonly VortexClient _vortex;
        private readonly YourDbContext _dbContext; // Adapt

        public VortexController(VortexClient vortex, YourDbContext dbContext)
        {
            _vortex = vortex;
            _dbContext = dbContext;
        }

        [HttpGet("jwt")]
        public IActionResult GetJwt()
        {
            var userId = User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.Identity?.Name;

            var userEmail = User.FindFirst("email")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var isAdmin = User.IsInRole("Admin"); // Adapt
            var adminScopes = isAdmin ? new List<string> { "autojoin" } : null;

            var vortexUser = new User(userId, userEmail, adminScopes);
            var jwt = _vortex.GenerateJwt(new Dictionary<string, object>
            {
                ["user"] = vortexUser
            });

            return Ok(new { jwt });
        }

        [HttpGet("invitations/{invitationId}")]
        public async Task<IActionResult> GetInvitation(string invitationId)
        {
            try
            {
                var invitation = await _vortex.GetInvitationAsync(invitationId);
                return Ok(invitation);
            }
            catch (VortexException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class AcceptInvitationsRequest
        {
            public List<string> InvitationIds { get; set; } = new();
        }

        [HttpPost("invitations/accept")]
        public async Task<IActionResult> AcceptInvitations([FromBody] AcceptInvitationsRequest request)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
            var userEmail = User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            try
            {
                var acceptUser = new AcceptUser { Email = userEmail };
                var accepted = await _vortex.AcceptInvitationsAsync(request.InvitationIds, acceptUser);

                // Create memberships - adapt to their DB schema
                foreach (var group in accepted.Groups)
                {
                    var exists = await _dbContext.WorkspaceMembers // Adapt table name
                        .AnyAsync(m => m.UserId == userId && m.WorkspaceId == group.GroupId);

                    if (!exists)
                    {
                        _dbContext.WorkspaceMembers.Add(new WorkspaceMember // Adapt model
                        {
                            UserId = userId,
                            WorkspaceId = group.GroupId, // Adapt column names
                            Role = "member",
                            JoinedAt = DateTime.UtcNow
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, accepted });
            }
            catch (VortexException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
```

### Option B: Minimal APIs

Add to `Program.cs` after `var app = builder.Build();`:

```csharp
using TeamVortexSoftware.VortexSDK;

app.MapGet("/api/vortex/jwt", (HttpContext context, VortexClient vortex) =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var userId = context.User.FindFirst("sub")?.Value ?? context.User.Identity?.Name;
    var userEmail = context.User.FindFirst("email")?.Value;

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
        return Results.Problem("Unable to determine user identity");

    var isAdmin = context.User.IsInRole("Admin");
    var vortexUser = new User(userId, userEmail, isAdmin ? new List<string> { "autojoin" } : null);
    var jwt = vortex.GenerateJwt(new Dictionary<string, object> { ["user"] = vortexUser });

    return Results.Ok(new { jwt });
}).RequireAuthorization();

app.MapGet("/api/vortex/invitations/{invitationId}", async (
    string invitationId,
    VortexClient vortex) =>
{
    try
    {
        var invitation = await vortex.GetInvitationAsync(invitationId);
        return Results.Ok(invitation);
    }
    catch (VortexException ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

app.MapPost("/api/vortex/invitations/accept", async (
    AcceptInvitationsRequest request,
    HttpContext context,
    VortexClient vortex,
    YourDbContext dbContext) => // Adapt
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var userId = context.User.FindFirst("sub")?.Value ?? context.User.Identity?.Name;
    var userEmail = context.User.FindFirst("email")?.Value;

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
        return Results.Problem("Unable to determine user identity");

    try
    {
        var acceptUser = new AcceptUser { Email = userEmail };
        var accepted = await vortex.AcceptInvitationsAsync(request.InvitationIds, acceptUser);

        foreach (var group in accepted.Groups)
        {
            var exists = await dbContext.WorkspaceMembers
                .AnyAsync(m => m.UserId == userId && m.WorkspaceId == group.GroupId);

            if (!exists)
            {
                dbContext.WorkspaceMembers.Add(new WorkspaceMember
                {
                    UserId = userId,
                    WorkspaceId = group.GroupId,
                    Role = "member",
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync();

        return Results.Ok(new { success = true, accepted });
    }
    catch (VortexException ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
}).RequireAuthorization();

public record AcceptInvitationsRequest(List<string> InvitationIds);
```

**Critical - Adapt database logic:**
- Use their actual table names (from discovery)
- Use their actual column names
- Use their actual model classes
- Match their database context (EF Core, Dapper, etc.)
- Match their auth claim names for userId and email

---

## Step 5: Add CORS (If Needed)

If frontend and backend on different domains, add to `Program.cs`:

```csharp
// Before builder.Build()
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://your-domain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// After app is built
app.UseCors("AllowFrontend");
```

---

## Step 6: Build

```bash
dotnet build
```

Verify:
- No compilation errors
- `grep -r "AddSingleton<VortexClient>" Program.cs` finds registration
- `grep -r "api/vortex/jwt"` finds JWT endpoint
- `grep -r "invitations/accept"` finds accept endpoint

---

## Common Errors

**"Cannot resolve symbol 'VortexClient'"** → Add `using TeamVortexSoftware.VortexSDK;`

**"VortexClient not registered in DI"** → Add singleton registration to `Program.cs` before `builder.Build()`

**"Configuration value 'Vortex:ApiKey' not found"** → Add Vortex section to `appsettings.json`

**"User claims not found"** → Check auth claim names. Debug: `User.Claims.Select(c => $"{c.Type}: {c.Value}")`

**"Cannot convert Task to IActionResult"** → Mark methods as `async Task<IActionResult>`

---

## After Implementation Report

List files created/modified:
- DI Registration: Program.cs or Startup.cs
- Endpoints: Controllers/VortexController.cs OR Program.cs (minimal APIs)
- Configuration: appsettings.json, appsettings.Development.json
- Database: Accept endpoint creates memberships in [table name]

Confirm:
- VortexClient registered as Singleton
- Three endpoints created (jwt, get invitation, accept)
- JWT endpoint requires authentication
- Accept endpoint creates database memberships
- Build succeeds with `dotnet build`

## Production Configuration

Set environment variable:
- Linux/Mac: `export VORTEX__APIKEY=your-key`
- Windows: `$env:VORTEX__APIKEY="your-key"`
- Azure: App Setting `Vortex__ApiKey`
- Docker: `-e VORTEX__APIKEY=your-key`
