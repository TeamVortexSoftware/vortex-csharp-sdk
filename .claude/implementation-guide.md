# Vortex C# SDK - Implementation Guide

This guide is designed for Claude Code to implement Vortex invitations in C# / .NET applications.

---

## SDK Information

**Package:** `TeamVortexSoftware.VortexSDK`
**Compatible with:** .NET 6.0+ (optimized for .NET 8.0)
**Installation:** Already installed! (You're reading this from the installed package)

---

## Expected Input Context

This guide expects to receive:

1. **Integration Contract** - Defines API endpoints, scope, file paths, authentication patterns
2. **Discovery Data** - Tech stack details, existing patterns, project structure
3. **Frontend Implementation Summary** - Details of frontend that will consume these endpoints

---

## Implementation Overview

You will implement:
1. **Vortex Service Registration** - Configure DI container with VortexClient
2. **JWT Endpoint** - API endpoint to generate JWT tokens
3. **Get Invitation Endpoint** - Retrieve invitation details
4. **Accept Invitation Endpoint** - Accept invitations and create memberships
5. **Configuration** - Environment variables and settings
6. **Validation** - Ensure build succeeds and endpoints work

---

## CRITICAL: C# / .NET Specifics

### Framework Detection

**.NET version matters** for implementation patterns:
- **.NET 8.0** (Latest): Minimal APIs, modern patterns
- **.NET 6.0/7.0**: Still supported, slightly different patterns
- **ASP.NET Core MVC**: Traditional controller-based
- **ASP.NET Core Minimal APIs**: Modern lightweight approach

**Check discovery data to determine which pattern they use.**

### Key .NET Patterns
- **Dependency Injection** (DI) is standard in ASP.NET Core
- **async/await** for all I/O operations
- **IConfiguration** for settings
- **Controllers** vs **Minimal APIs**
- **Middleware** for cross-cutting concerns
- **Entity Framework Core** or other ORM for database

---

## Implementation Steps

### Step 1: Analyze Existing .NET Patterns

**Read these files from discovery data:**
- `Program.cs` (main application entry point)
- `appsettings.json` or `appsettings.Development.json` (configuration)
- Example controller file (if using controllers)
- Example service file (to understand their patterns)
- Database context file (if using EF Core)

**Determine:**
- Are they using Controllers or Minimal APIs?
- What .NET version? (6.0, 7.0, 8.0)
- Where are controllers/endpoints located?
- Do they use dependency injection? (Almost certainly yes)
- What's their project structure? (typical ASP.NET Core layout?)
- Database ORM? (Entity Framework Core, Dapper, ADO.NET?)
- Authentication method? (JWT, cookies, Identity?)

**Report what you found:**
```markdown
## .NET Pattern Analysis

Framework: [.NET 6.0 | .NET 7.0 | .NET 8.0]
API Style: [Controllers | Minimal APIs | Mixed]
Controllers Location: [path]
Services Location: [path]
Database: [Entity Framework Core | Dapper | Other]
Auth Pattern: [JWT | Cookie | Identity | Custom]
Project Structure: [Standard ASP.NET Core | Custom]
```

---

### Step 2: Register VortexClient in Dependency Injection

**Goal:** Add VortexClient to the DI container so it can be injected into controllers/services

**Find their `Program.cs`** (or `Startup.cs` in older projects)

**Add VortexClient registration:**

#### For .NET 6+ (Top-Level Statements):

```csharp
// In Program.cs, after builder initialization, before builder.Build()

using TeamVortexSoftware.VortexSDK;

// ... existing code ...

// Register VortexClient as a singleton
builder.Services.AddSingleton<VortexClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration["Vortex:ApiKey"]
        ?? throw new InvalidOperationException("Vortex:ApiKey is not configured");

    return new VortexClient(apiKey);
});

// ... rest of Program.cs ...
```

#### For Older .NET (Startup.cs pattern):

```csharp
// In Startup.cs, ConfigureServices method

using TeamVortexSoftware.VortexSDK;

public void ConfigureServices(IServiceCollection services)
{
    // ... existing services ...

    // Register VortexClient
    services.AddSingleton<VortexClient>(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var apiKey = configuration["Vortex:ApiKey"]
            ?? throw new InvalidOperationException("Vortex:ApiKey is not configured");

        return new VortexClient(apiKey);
    });

    // ... rest of services ...
}
```

**⚠️ IMPORTANT:**
- Register as **Singleton** (VortexClient is thread-safe and reusable)
- Load API key from configuration (never hardcode)
- Throw exception if API key is missing (fail fast)

---

### Step 3: Add Configuration for Vortex API Key

**Goal:** Store the API key in configuration files

**Update `appsettings.json`:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Vortex": {
    "ApiKey": ""
  }
}
```

**Update `appsettings.Development.json`** (for local development):

```json
{
  "Vortex": {
    "ApiKey": "VRTX.your-api-key-here"
  }
}
```

**⚠️ Security Note:**
- In development: Can put key in `appsettings.Development.json`
- In production: Use environment variables, Azure Key Vault, AWS Secrets Manager, etc.
- **NEVER** commit real API keys to version control

**For production, use environment variables:**
```bash
# Set environment variable
VORTEX__APIKEY=your-actual-api-key
```

**Or in Azure/AWS:**
- Azure: App Settings or Key Vault
- AWS: Parameter Store or Secrets Manager
- Docker: Environment variables in docker-compose or Kubernetes secrets

---

### Step 4: Create JWT Endpoint

**Goal:** Create an API endpoint that generates JWT tokens for authenticated users

#### Option A: Using Controllers (Traditional):

**File location:** Based on discovery (e.g., `Controllers/VortexController.cs`)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamVortexSoftware.VortexSDK;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/vortex")] // Adapt to their routing pattern from contract
    [Authorize] // Require authentication
    public class VortexController : ControllerBase
    {
        private readonly VortexClient _vortex;
        private readonly YourDbContext _dbContext; // Adapt to their DB context

        public VortexController(VortexClient vortex, YourDbContext dbContext)
        {
            _vortex = vortex;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        [HttpGet("jwt")]
        public IActionResult GetJwt()
        {
            // Get current user from claims (adapt to their auth system)
            var userId = User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.Identity?.Name;

            var userEmail = User.FindFirst("email")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Optional: Check if user has admin permissions
            var isAdmin = User.IsInRole("Admin"); // Adapt to their role system
            var adminScopes = isAdmin ? new List<string> { "autojoin" } : null;

            // Create Vortex user
            var vortexUser = new User(userId, userEmail, adminScopes);

            // Generate JWT
            var parameters = new Dictionary<string, object>
            {
                ["user"] = vortexUser
            };

            var jwt = _vortex.GenerateJwt(parameters);

            return Ok(new { jwt });
        }
    }
}
```

#### Option B: Using Minimal APIs (.NET 6+):

**In `Program.cs`, after app is built:**

```csharp
// ... after var app = builder.Build(); ...

// Vortex JWT endpoint
app.MapGet("/api/vortex/jwt", async (HttpContext context, VortexClient vortex) =>
{
    // Check if user is authenticated
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    // Get user info from claims
    var userId = context.User.FindFirst("sub")?.Value
        ?? context.User.FindFirst("id")?.Value
        ?? context.User.Identity?.Name;

    var userEmail = context.User.FindFirst("email")?.Value
        ?? context.User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value;

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
    {
        return Results.Problem("Unable to determine user identity");
    }

    // Optional: Check admin role
    var isAdmin = context.User.IsInRole("Admin");
    var adminScopes = isAdmin ? new List<string> { "autojoin" } : null;

    // Generate JWT
    var vortexUser = new User(userId, userEmail, adminScopes);
    var parameters = new Dictionary<string, object>
    {
        ["user"] = vortexUser
    };

    var jwt = vortex.GenerateJwt(parameters);

    return Results.Ok(new { jwt });
})
.RequireAuthorization(); // Require authentication

// ... rest of app configuration ...
```

**⚠️ Adapt to their patterns:**
- Match their authentication claim names
- Use their role/permission checking logic
- Follow their API route conventions from the contract
- Match their error response format

---

### Step 5: Create Get Invitation Endpoint

**Goal:** Endpoint to retrieve invitation details by ID

#### Using Controllers:

```csharp
// Add to VortexController.cs

/// <summary>
/// Get invitation details by ID
/// </summary>
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
```

#### Using Minimal APIs:

```csharp
// Add to Program.cs

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
```

---

### Step 6: Create Accept Invitation Endpoint

**Goal:** Endpoint to accept invitations and create membership records in their database

This is the **most complex endpoint** because it needs to:
1. Accept invitation via Vortex API
2. Create membership record in their database

#### Using Controllers:

```csharp
// Add to VortexController.cs

public class AcceptInvitationsRequest
{
    public List<string> InvitationIds { get; set; } = new();
}

/// <summary>
/// Accept invitations and create memberships
/// </summary>
[HttpPost("invitations/accept")]
public async Task<IActionResult> AcceptInvitations([FromBody] AcceptInvitationsRequest request)
{
    // Get current user
    var userId = User.FindFirst("sub")?.Value
        ?? User.FindFirst("id")?.Value
        ?? User.Identity?.Name;

    var userEmail = User.FindFirst("email")?.Value;

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
    {
        return Unauthorized(new { error = "User not authenticated" });
    }

    try
    {
        // Accept invitations via Vortex API
        var target = new InvitationTarget("email", userEmail);
        var acceptedInvitation = await _vortex.AcceptInvitationsAsync(request.InvitationIds, target);

        // Create membership records in database
        // Adapt this to their database schema and ORM
        foreach (var group in acceptedInvitation.Groups)
        {
            // Check if membership already exists
            var existingMembership = await _dbContext.WorkspaceMembers // Adapt table name
                .FirstOrDefaultAsync(m =>
                    m.UserId == userId &&
                    m.WorkspaceId == group.GroupId); // Adapt column names

            if (existingMembership == null)
            {
                // Create new membership
                var membership = new WorkspaceMember // Adapt model name
                {
                    UserId = userId,
                    WorkspaceId = group.GroupId, // Use their scope entity ID
                    Role = "member", // Default role, adapt as needed
                    JoinedAt = DateTime.UtcNow
                };

                _dbContext.WorkspaceMembers.Add(membership); // Adapt
            }
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            acceptedInvitation
        });
    }
    catch (VortexException ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}
```

#### Using Minimal APIs:

```csharp
// Add to Program.cs

public record AcceptInvitationsRequest(List<string> InvitationIds);

app.MapPost("/api/vortex/invitations/accept", async (
    AcceptInvitationsRequest request,
    HttpContext context,
    VortexClient vortex,
    YourDbContext dbContext) => // Adapt DB context
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    var userId = context.User.FindFirst("sub")?.Value
        ?? context.User.Identity?.Name;
    var userEmail = context.User.FindFirst("email")?.Value;

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
    {
        return Results.Problem("Unable to determine user identity");
    }

    try
    {
        // Accept via Vortex
        var target = new InvitationTarget("email", userEmail);
        var accepted = await vortex.AcceptInvitationsAsync(request.InvitationIds, target);

        // Create memberships
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
})
.RequireAuthorization();
```

**⚠️ CRITICAL - Adapt Database Logic:**
- Use **their actual table names** (from discovery: WorkspaceMembers, TeamUsers, etc.)
- Use **their actual column names**
- Use **their actual model classes**
- Match **their database context** (EF Core, Dapper, etc.)
- Handle **their specific role/permission system**
- Check for **existing memberships** before creating

---

### Step 7: Add CORS Configuration (If Needed)

**If frontend and backend are on different domains**, add CORS:

**In `Program.cs`:**

```csharp
// Before builder.Build()
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://your-frontend-domain.com") // Adapt
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// After app is built, before app.Run()
app.UseCors("AllowFrontend");
```

**⚠️ Adapt to their setup:**
- Check if they already have CORS configured
- Add to their existing CORS policy if present
- Don't duplicate CORS configuration

---

### Step 8: Update Environment Configuration

**Add to `.gitignore`** (if not already present):

```
# Vortex configuration
appsettings.Development.json
appsettings.*.json
!appsettings.json
```

**Document environment variable for production:**

Create or update `README.md` section:

```markdown
## Vortex Configuration

### Development
Add your Vortex API key to `appsettings.Development.json`:

```json
{
  "Vortex": {
    "ApiKey": "your-development-api-key"
  }
}
```

### Production
Set the environment variable:

**Linux/Mac:**
```bash
export VORTEX__APIKEY=your-production-api-key
```

**Windows:**
```powershell
$env:VORTEX__APIKEY="your-production-api-key"
```

**Azure App Service:**
Add Application Setting: `Vortex__ApiKey` = `your-key`

**AWS/Docker:**
Set environment variable `VORTEX__APIKEY`
```

---

### Step 9: Build and Validate

#### Step 9.1: Build the Project

```bash
dotnet build
```

**Expected:** Build succeeds with no errors

#### Step 9.2: Check for Compilation Errors

If there are errors related to:
- **Namespace issues:** Add necessary `using` statements
- **Type mismatches:** Ensure User, VortexClient are properly imported
- **Async issues:** Ensure methods are marked `async` and return `Task<>`
- **DI issues:** Ensure VortexClient is registered in Program.cs

#### Step 9.3: Verify Endpoints Exist

Search for endpoint definitions:

```bash
# For controllers
grep -r "HttpGet.*jwt" Controllers/

# For minimal APIs
grep -r "MapGet.*jwt" Program.cs
```

**Expected:** Find JWT endpoint definition

#### Step 9.4: Verify VortexClient Registration

```bash
grep -r "AddSingleton<VortexClient>" Program.cs
# or
grep -r "AddSingleton<VortexClient>" Startup.cs
```

**Expected:** Find VortexClient DI registration

#### Step 9.5: Test Endpoints (Optional)

If they have a dev environment running:

```bash
# Test JWT endpoint (requires authentication)
curl -H "Authorization: Bearer YOUR_AUTH_TOKEN" http://localhost:5000/api/vortex/jwt
```

---

## Implementation Report

After completing all steps, provide this report:

```markdown
# C# Backend Implementation Complete

## Files Created/Modified

### Dependency Injection:
- [Program.cs or Startup.cs] - VortexClient registered

### Controllers/Endpoints:
- [Controllers/VortexController.cs OR Program.cs] - Vortex endpoints

### Configuration:
- [appsettings.json] - Vortex section added
- [appsettings.Development.json] - Dev API key configured
- [README.md or docs/] - Environment variable documentation

### Database Integration:
- [Accept invitation endpoint] - Creates memberships in [table name]

## Endpoints Registered

✅ GET [prefix]/vortex/jwt - Generate JWT for authenticated user
✅ GET [prefix]/vortex/invitations/:id - Get invitation details
✅ POST [prefix]/vortex/invitations/accept - Accept invitations

## Integration Points

### Dependency Injection:
✅ VortexClient registered as Singleton
✅ Injected into [Controllers/Endpoints]

### Authentication:
✅ JWT endpoint requires authentication
✅ User claims mapped: [userId claim], [email claim]
✅ Admin scope detection: [role check logic]

### Database:
✅ Memberships created in: [table name]
✅ Using [Entity Framework Core | Dapper | Other]
✅ Fields mapped: UserId, [ScopeEntity]Id, Role, JoinedAt

### Configuration:
✅ API key loaded from: Vortex:ApiKey
✅ Environment variable: VORTEX__APIKEY

## Build Status

✅ Project builds successfully (dotnet build)
✅ All using statements correct
✅ No compilation errors

## Testing Instructions

### 1. Configure API Key

**Development:**
```bash
# Add to appsettings.Development.json
{
  "Vortex": {
    "ApiKey": "VRTX.your-key-here"
  }
}
```

### 2. Run Application

```bash
dotnet run
# or
dotnet watch run
```

### 3. Test JWT Endpoint

```bash
# Authenticate first, then:
curl -H "Authorization: Bearer YOUR_AUTH_TOKEN" \\
     http://localhost:5000/api/vortex/jwt
```

Expected response:
```json
{
  "jwt": "eyJhbGciOiJIUzI1NiIs..."
}
```

### 4. Test Complete Flow

1. Frontend calls GET /api/vortex/jwt → receives JWT
2. Frontend passes JWT to VortexInvite component
3. User sends invitations through component
4. User clicks invitation link
5. Frontend calls POST /api/vortex/invitations/accept with invitation IDs
6. Backend creates membership in database
7. Verify membership exists in database

### 5. Verify Database

```sql
-- Check that membership was created
SELECT * FROM [YourMembershipTable]
WHERE UserId = 'user-id' AND [ScopeEntity]Id = 'scope-id';
```

## Next Steps

1. Test all three endpoints
2. Verify JWT generation works
3. Test invitation acceptance flow end-to-end
4. Verify database memberships are created correctly
5. Deploy to staging/production
6. Set production environment variable VORTEX__APIKEY

## C# / .NET Specific Notes

- Framework: [.NET 6.0 | 7.0 | 8.0]
- API Style: [Controllers | Minimal APIs]
- DI: VortexClient registered as Singleton
- Async/await used throughout
- Configuration: IConfiguration pattern
- Database: [EF Core | Dapper | Other]
```

---

## Common Issues & Solutions

### Issue: "Cannot resolve symbol 'VortexClient'"
**Solution:** Add using statement:
```csharp
using TeamVortexSoftware.VortexSDK;
```

### Issue: "VortexClient not registered in DI"
**Solution:** Add to Program.cs before `builder.Build()`:
```csharp
builder.Services.AddSingleton<VortexClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new VortexClient(config["Vortex:ApiKey"]!);
});
```

### Issue: "Configuration value 'Vortex:ApiKey' not found"
**Solution:** Add to appsettings.json:
```json
{
  "Vortex": {
    "ApiKey": ""
  }
}
```

### Issue: "User claims not found"
**Solution:** Check their authentication middleware and claim names:
```csharp
// Debug - print all claims
var claims = User.Claims.Select(c => $"{c.Type}: {c.Value}");
Console.WriteLine(string.Join(", ", claims));
```

### Issue: "Cannot convert Task to IActionResult"
**Solution:** Methods must be async:
```csharp
// BAD
public IActionResult GetJwt() { ... }

// GOOD
public async Task<IActionResult> GetJwt() { ... }
```

### Issue: "DbContext not available in Minimal API"
**Solution:** Add DbContext to DI and inject as parameter:
```csharp
app.MapPost("/api/vortex/invitations/accept", async (
    AcceptInvitationsRequest request,
    VortexClient vortex,
    YourDbContext dbContext) => // Add dbContext here
{
    // Use dbContext...
});
```

### Issue: "VortexException not caught"
**Solution:** Wrap Vortex API calls in try-catch:
```csharp
try
{
    var invitation = await _vortex.GetInvitationAsync(id);
    return Ok(invitation);
}
catch (VortexException ex)
{
    return StatusCode(500, new { error = ex.Message });
}
```

---

## Best Practices for C# / .NET

1. **Use Dependency Injection:** Always inject VortexClient, never create instances
2. **async/await:** All I/O operations should be async
3. **Configuration:** Load API key from IConfiguration, never hardcode
4. **Error Handling:** Catch VortexException and return appropriate status codes
5. **Dispose Pattern:** VortexClient implements IDisposable, but DI handles it when registered as Singleton
6. **Security:** Require authentication on all Vortex endpoints
7. **Validation:** Validate request models with data annotations
8. **Logging:** Use ILogger for debugging and monitoring
9. **Testing:** Write unit tests for services, integration tests for endpoints

---

## Example: Complete Controller with Best Practices

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TeamVortexSoftware.VortexSDK;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/vortex")]
    [Authorize]
    public class VortexController : ControllerBase
    {
        private readonly VortexClient _vortex;
        private readonly YourDbContext _dbContext;
        private readonly ILogger<VortexController> _logger;

        public VortexController(
            VortexClient vortex,
            YourDbContext dbContext,
            ILogger<VortexController> logger)
        {
            _vortex = vortex;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet("jwt")]
        public IActionResult GetJwt()
        {
            var userId = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
            var userEmail = User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("User authentication failed: missing user ID or email");
                return Unauthorized(new { error = "User not authenticated" });
            }

            _logger.LogInformation("Generating JWT for user {UserId}", userId);

            var isAdmin = User.IsInRole("Admin");
            var vortexUser = new User(userId, userEmail, isAdmin ? new List<string> { "autojoin" } : null);

            var jwt = _vortex.GenerateJwt(new Dictionary<string, object> { ["user"] = vortexUser });

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
                _logger.LogError(ex, "Failed to get invitation {InvitationId}", invitationId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class AcceptInvitationsRequest
        {
            [Required]
            [MinLength(1)]
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
                _logger.LogInformation("Accepting invitations {InvitationIds} for user {UserId}",
                    string.Join(", ", request.InvitationIds), userId);

                var target = new InvitationTarget("email", userEmail);
                var accepted = await _vortex.AcceptInvitationsAsync(request.InvitationIds, target);

                foreach (var group in accepted.Groups)
                {
                    var exists = await _dbContext.WorkspaceMembers
                        .AnyAsync(m => m.UserId == userId && m.WorkspaceId == group.GroupId);

                    if (!exists)
                    {
                        _dbContext.WorkspaceMembers.Add(new WorkspaceMember
                        {
                            UserId = userId,
                            WorkspaceId = group.GroupId,
                            Role = "member",
                            JoinedAt = DateTime.UtcNow
                        });

                        _logger.LogInformation("Created membership for user {UserId} in workspace {WorkspaceId}",
                            userId, group.GroupId);
                    }
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, accepted });
            }
            catch (VortexException ex)
            {
                _logger.LogError(ex, "Failed to accept invitations for user {UserId}", userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
```

---

**Implementation guide complete. Follow the steps above to integrate Vortex invitations into the C# / .NET application.**
