using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TeamVortexSoftware.VortexSDK
{
    /// <summary>
    /// Vortex C# SDK Client
    /// Provides JWT generation and Vortex API integration for .NET applications.
    /// Compatible with React providers and follows the same paradigms as other Vortex SDKs.
    /// </summary>
    public class VortexClient : IDisposable
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        /// <summary>
        /// Create a new Vortex client
        /// </summary>
        /// <param name="apiKey">Your Vortex API key</param>
        /// <param name="baseUrl">Optional custom base URL (defaults to production API)</param>
        public VortexClient(string apiKey, string? baseUrl = null)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _baseUrl = baseUrl ?? Environment.GetEnvironmentVariable("VORTEX_API_BASE_URL") ?? "https://api.vortexsoftware.com";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            var sdkVersion = typeof(VortexClient).Assembly.GetName().Version?.ToString(3) ?? "unknown";
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"vortex-csharp-sdk/{sdkVersion}");
            _httpClient.DefaultRequestHeaders.Add("x-vortex-sdk-name", "vortex-csharp-sdk");
            _httpClient.DefaultRequestHeaders.Add("x-vortex-sdk-version", sdkVersion);
        }

        /// <summary>
        /// Generate a JWT token matching the Node.js SDK pattern.
        /// This uses the same algorithm as the Node.js SDK to ensure complete compatibility with React providers.
        ///
        /// The params dictionary must contain a "user" key with a User object.
        /// Additional properties in the params dictionary will be included directly in the JWT payload.
        /// If the user has adminScopes, the full array will be included in the JWT payload.
        /// </summary>
        /// <param name="parameters">Dictionary containing "user" key with User object and optional additional properties</param>
        /// <returns>JWT token string</returns>
        /// <example>
        /// <code>
        /// // Simple usage:
        /// var user = new User
        /// {
        ///     Id = "user-123",
        ///     Email = "user@example.com",
        ///     Name = "Jane Doe",                                      // Optional: user's display name
        ///     AvatarUrl = "https://example.com/avatars/jane.jpg",    // Optional: user's avatar URL
        ///     AdminScopes = new List&lt;string&gt; { "autojoin" }         // Optional: grants admin privileges
        /// };
        /// var parameters1 = new Dictionary&lt;string, object&gt;
        /// {
        ///     ["user"] = user
        /// };
        /// var jwt = vortex.GenerateJwt(parameters1);
        ///
        /// // With additional properties:
        /// var parameters2 = new Dictionary&lt;string, object&gt;
        /// {
        ///     ["user"] = new User("user-123", "user@example.com"),
        ///     ["role"] = "admin",
        ///     ["department"] = "Engineering"
        /// };
        /// var jwt = vortex.GenerateJwt(parameters2);
        /// </code>
        /// </example>
        /// <summary>
        /// Sign a user object for use with the widget signature prop.
        /// </summary>
        /// <param name="user">Dictionary with user data (id, email, etc.)</param>
        /// <returns>Signature string in "kid:hexDigest" format</returns>
        /// <vortex category="authentication" since="0.5.0"/>
        public string Sign(Dictionary<string, object> user)
        {
            var parts = _apiKey.Split('.');
            if (parts.Length != 3 || parts[0] != "VRTX")
                throw new VortexException("Invalid API key format");

            var uuidBytes = Convert.FromBase64String(parts[1].Replace('-', '+').Replace('_', '/').PadRight((parts[1].Length + 3) & ~3, '='));
            if (uuidBytes.Length != 16)
                throw new VortexException("Invalid API key UUID length");
            var kid =
                $"{uuidBytes[0]:x2}{uuidBytes[1]:x2}{uuidBytes[2]:x2}{uuidBytes[3]:x2}-" +
                $"{uuidBytes[4]:x2}{uuidBytes[5]:x2}-" +
                $"{uuidBytes[6]:x2}{uuidBytes[7]:x2}-" +
                $"{uuidBytes[8]:x2}{uuidBytes[9]:x2}-" +
                $"{uuidBytes[10]:x2}{uuidBytes[11]:x2}{uuidBytes[12]:x2}{uuidBytes[13]:x2}{uuidBytes[14]:x2}{uuidBytes[15]:x2}";
            var key = parts[2];

            // Derive signing key
            using var signingHmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var signingKey = signingHmac.ComputeHash(Encoding.UTF8.GetBytes(kid));

            // Build canonical payload — include ALL user fields with key normalization
            var canonical = new SortedDictionary<string, object>();
            foreach (var entry in user)
            {
                if (entry.Key == "id") canonical["userId"] = entry.Value;
                else if (entry.Key == "email") canonical["userEmail"] = entry.Value;
                else canonical[entry.Key] = entry.Value;
            }
            if (!canonical.ContainsKey("userId") || canonical["userId"] == null)
                throw new VortexException("userId (or id) is required for signing");

            var canonicalJson = JsonSerializer.Serialize(canonical, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            using var mac = new HMACSHA256(signingKey);
            var digestBytes = mac.ComputeHash(Encoding.UTF8.GetBytes(canonicalJson));
            var digest = BitConverter.ToString(digestBytes).Replace("-", "").ToLower();

            return $"{kid}:{digest}";
        }

        /// <summary>
        /// Parse expiration time string or int into seconds
        /// </summary>
        private int ParseExpiresIn(object expiresIn)
        {
            if (expiresIn is int seconds)
            {
                if (seconds <= 0)
                    throw new VortexException($"Invalid expiresIn value: \"{seconds}\". Must be positive.");
                return seconds;
            }
            if (expiresIn is string str)
            {
                var match = System.Text.RegularExpressions.Regex.Match(str, @"^(\d+)(m|h|d)$");
                if (!match.Success)
                    throw new VortexException($"Invalid expiresIn format: \"{str}\". Use \"5m\", \"1h\", \"24h\", \"7d\" or seconds.");
                var value = int.Parse(match.Groups[1].Value);
                // Validate value is positive (fixes "0m", "0h", "0d" not being rejected)
                if (value <= 0)
                    throw new VortexException($"Invalid expiresIn value: \"{str}\". Must be positive.");
                // Use checked long arithmetic to prevent overflow
                var unit = match.Groups[2].Value;
                long totalSeconds = checked(unit switch
                {
                    "m" => (long)value * 60L,
                    "h" => (long)value * 60L * 60L,
                    "d" => (long)value * 60L * 60L * 24L,
                    _ => throw new VortexException($"Unknown time unit: {unit}")
                });
                if (totalSeconds > int.MaxValue)
                    throw new VortexException($"Invalid expiresIn value: \"{str}\". Duration is too large.");
                return (int)totalSeconds;
            }
            throw new VortexException($"expiresIn must be a string or int, but got {expiresIn?.GetType().Name ?? "null"}: {expiresIn}");
        }

        /// <summary>
        /// Generate a signed token for use with Vortex widgets
        /// </summary>
        /// <param name="payload">Data to sign (user, component, scope, vars, etc.)</param>
        /// <param name="options">Optional configuration (ExpiresIn)</param>
        /// <returns>Signed JWT token string</returns>
        /// <vortex category="authentication" since="0.8.0" primary="true"/>
        public string GenerateToken(GenerateTokenPayload payload, GenerateTokenOptions? options = null)
        {
            // Guard against null payload for clear failure mode
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            // Warn if user.id is missing
            if (payload.User == null || string.IsNullOrEmpty(payload.User.Id))
            {
                Console.WriteLine("[Vortex SDK] Warning: signing payload without user.id means invitations won't be securely attributed.");
            }

            // Parse API key
            var parts = _apiKey.Split('.');
            if (parts.Length != 3 || parts[0] != "VRTX")
                throw new VortexException("Invalid API key format");

            var uuidBytes = Base64UrlDecode(parts[1]);
            if (uuidBytes.Length != 16)
                throw new VortexException("Invalid API key UUID length");
            // Convert bytes to UUID string using big-endian byte order (matching Sign() and GenerateJwt())
            var kid =
                $"{uuidBytes[0]:x2}{uuidBytes[1]:x2}{uuidBytes[2]:x2}{uuidBytes[3]:x2}-" +
                $"{uuidBytes[4]:x2}{uuidBytes[5]:x2}-" +
                $"{uuidBytes[6]:x2}{uuidBytes[7]:x2}-" +
                $"{uuidBytes[8]:x2}{uuidBytes[9]:x2}-" +
                $"{uuidBytes[10]:x2}{uuidBytes[11]:x2}{uuidBytes[12]:x2}{uuidBytes[13]:x2}{uuidBytes[14]:x2}{uuidBytes[15]:x2}";
            var key = parts[2];

            // Derive signing key
            using var signingHmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var signingKey = signingHmac.ComputeHash(Encoding.UTF8.GetBytes(kid));

            // Parse expiry
            var expiresInSeconds = 2592000; // Default 30 days
            if (options?.ExpiresIn != null)
            {
                expiresInSeconds = ParseExpiresIn(options.ExpiresIn);
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var exp = now + expiresInSeconds;

            // Build JWT header
            var header = new Dictionary<string, object>
            {
                ["alg"] = "HS256",
                ["typ"] = "JWT",
                ["kid"] = kid
            };

            // Build JWT payload - merge Extra first so typed fields always win
            var jwtPayload = new Dictionary<string, object>();
            if (payload.Extra != null)
            {
                foreach (var kv in payload.Extra)
                    jwtPayload[kv.Key] = kv.Value;
            }
            if (payload.User != null) jwtPayload["user"] = payload.User;
            if (payload.Component != null) jwtPayload["component"] = payload.Component;
            if (payload.Scope != null) jwtPayload["scope"] = payload.Scope;
            if (payload.Vars != null) jwtPayload["vars"] = payload.Vars;
            jwtPayload["iat"] = now;
            jwtPayload["exp"] = exp;

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header, jsonOptions)));
            var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(jwtPayload, jsonOptions)));

            var toSign = $"{headerB64}.{payloadB64}";
            using var mac = new HMACSHA256(signingKey);
            var signatureBytes = mac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
            var signatureB64 = Base64UrlEncode(signatureBytes);

            return $"{toSign}.{signatureB64}";
        }

        public string GenerateJwt(Dictionary<string, object> parameters, GenerateTokenOptions options = null)
        {
            // Extract user from parameters
            if (parameters == null || !parameters.ContainsKey("user"))
            {
                throw new VortexException("parameters must contain 'user' key");
            }

            if (parameters["user"] is not User user)
            {
                throw new VortexException("'user' must be a User object");
            }

            // Parse API key: format is VRTX.base64encodedId.key
            var parts = _apiKey.Split('.');
            if (parts.Length != 3)
            {
                throw new VortexException("Invalid API key format");
            }

            var prefix = parts[0];
            var encodedId = parts[1];
            var key = parts[2];

            if (prefix != "VRTX")
            {
                throw new VortexException("Invalid API key prefix");
            }

            // Decode the UUID from base64url
            var idBytes = Base64UrlDecode(encodedId);
            if (idBytes.Length != 16)
            {
                throw new VortexException("Invalid UUID byte length");
            }
            // Convert bytes to UUID string (big-endian byte order, matching Node.js)
            var id = $"{BitConverter.ToString(idBytes, 0, 4).Replace("-", "").ToLower()}-" +
                     $"{BitConverter.ToString(idBytes, 4, 2).Replace("-", "").ToLower()}-" +
                     $"{BitConverter.ToString(idBytes, 6, 2).Replace("-", "").ToLower()}-" +
                     $"{BitConverter.ToString(idBytes, 8, 2).Replace("-", "").ToLower()}-" +
                     $"{BitConverter.ToString(idBytes, 10, 6).Replace("-", "").ToLower()}";

            var expiresInSeconds = (options?.ExpiresIn != null) ? ParseExpiresIn(options.ExpiresIn) : 2592000; // 30 days
            var expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresInSeconds;

            // Step 1: Derive signing key from API key + ID
            byte[] signingKey;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                signingKey = hmac.ComputeHash(Encoding.UTF8.GetBytes(id));
            }

            // Step 2: Build header + payload (same structure as Node.js)
            var header = new
            {
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                alg = "HS256",
                typ = "JWT",
                kid = id
            };

            // Build payload with required fields
            var payload = new Dictionary<string, object>
            {
                ["userId"] = user.Id,
                ["userEmail"] = user.Email,
                ["expires"] = expires
            };

            // Add name if present (prefer new property, fall back to deprecated)
            var userName = user.Name ?? user.UserName;
            if (userName != null)
            {
                payload["name"] = userName;
            }

            // Add avatarUrl if present (prefer new property, fall back to deprecated)
            var userAvatarUrl = user.AvatarUrl ?? user.UserAvatarUrl;
            if (userAvatarUrl != null)
            {
                payload["avatarUrl"] = userAvatarUrl;
            }

            // Add adminScopes if present
            if (user.AdminScopes != null)
            {
                payload["adminScopes"] = user.AdminScopes;
            }

            // Add allowedEmailDomains if present (for domain-restricted invitations)
            if (user.AllowedEmailDomains != null && user.AllowedEmailDomains.Count > 0)
            {
                payload["allowedEmailDomains"] = user.AllowedEmailDomains;
            }

            // Add any additional properties from parameters (excluding 'user')
            foreach (var kvp in parameters)
            {
                if (kvp.Key != "user")
                {
                    payload[kvp.Key] = kvp.Value;
                }
            }

            // Step 3: Base64URL encode header and payload
            var headerJson = JsonSerializer.Serialize(header);
            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            // Step 4: Sign with HMAC-SHA256
            var toSign = $"{headerB64}.{payloadB64}";
            byte[] signatureBytes;
            using (var hmac = new HMACSHA256(signingKey))
            {
                signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
            }

            var signatureB64 = Base64UrlEncode(signatureBytes);

            return $"{toSign}.{signatureB64}";
        }

        /// <summary>
        /// Get invitations by target (email or sms)
        /// </summary>
        /// <param name="targetType">Type of target (email, phone)</param>
        /// <param name="targetValue">The target value</param>
        /// <returns>List of invitations</returns>
        /// <vortex category="invitations" since="0.1.0"/>
        public async Task<List<Invitation>> GetInvitationsByTargetAsync(string targetType, string targetValue)
        {
            var queryParams = $"?targetType={Uri.EscapeDataString(targetType)}&targetValue={Uri.EscapeDataString(targetValue)}";
            var response = await ApiRequestAsync<InvitationsResponse>(HttpMethod.Get, $"/api/v1/invitations{queryParams}");
            return response.Invitations ?? new List<Invitation>();
        }

        /// <summary>
        /// Get a specific invitation by ID
        /// </summary>
        /// <param name="invitationId">The invitation ID</param>
        /// <returns>The invitation details</returns>
        /// <vortex category="invitations" since="0.1.0" primary="true"/>
        public async Task<Invitation> GetInvitationAsync(string invitationId)
        {
            return await ApiRequestAsync<Invitation>(HttpMethod.Get, $"/api/v1/invitations/{invitationId}");
        }

        /// <summary>
        /// Revoke (delete) an invitation
        /// </summary>
        /// <param name="invitationId">The invitation ID to revoke</param>
        /// <vortex category="invitations" since="0.1.0"/>
        public async Task RevokeInvitationAsync(string invitationId)
        {
            await ApiRequestAsync<object>(HttpMethod.Delete, $"/api/v1/invitations/{invitationId}");
        }

        /// <summary>
        /// Accept multiple invitations using the new User format (preferred)
        /// </summary>
        /// <param name="invitationIds">List of invitation IDs to accept</param>
        /// <param name="user">User with email or phone (and optional name)</param>
        /// <returns>Invitation result</returns>
        /// <vortex category="invitations" since="0.1.0"/>
        public async Task<Invitation> AcceptInvitationsAsync(List<string> invitationIds, AcceptUser user)
        {
            if (string.IsNullOrEmpty(user.Email) && string.IsNullOrEmpty(user.Phone))
            {
                throw new VortexException("User must have either email or phone");
            }

            var body = new
            {
                invitationIds,
                user
            };

            return await ApiRequestAsync<Invitation>(HttpMethod.Post, "/api/v1/invitations/accept", body);
        }

        /// <summary>
        /// Accept a single invitation (recommended method)
        /// </summary>
        /// <param name="invitationId">Single invitation ID to accept</param>
        /// <param name="user">User with email or phone (and optional name)</param>
        /// <returns>Invitation result</returns>
        /// <vortex category="invitations" since="0.6.0" primary="true"/>
        public async Task<Invitation> AcceptInvitationAsync(string invitationId, AcceptUser user)
        {
            return await AcceptInvitationsAsync(new List<string> { invitationId }, user);
        }

        /// <summary>
        /// Accept multiple invitations using legacy target format (deprecated)
        /// </summary>
        /// <param name="invitationIds">List of invitation IDs to accept</param>
        /// <param name="target">Legacy target object with type and value</param>
        /// <returns>Invitation result</returns>
        [Obsolete("Use AcceptInvitationsAsync(invitationIds, AcceptUser) instead. This method converts the target to a User format.")]
        public async Task<Invitation> AcceptInvitationsAsync(List<string> invitationIds, InvitationTarget target)
        {
            Console.WriteLine("[Vortex SDK] DEPRECATED: Passing a target object is deprecated. Use the AcceptUser format instead: new AcceptUser { Email = \"user@example.com\" }");

            // Convert target to AcceptUser format
            var user = new AcceptUser();
            if (target.Type == InvitationTargetType.email)
            {
                user.Email = target.Value;
            }
            else if (target.Type == InvitationTargetType.phone)
            {
                user.Phone = target.Value;
            }
            else
            {
                // For share/internal types, default to email
                user.Email = target.Value;
            }

            return await AcceptInvitationsAsync(invitationIds, user);
        }

        /// <summary>
        /// Accept multiple invitations using multiple legacy targets (deprecated)
        /// Will call the accept endpoint once per target
        /// </summary>
        /// <param name="invitationIds">List of invitation IDs to accept</param>
        /// <param name="targets">Array of legacy target objects</param>
        /// <returns>Invitation result from the last acceptance</returns>
        [Obsolete("Use AcceptInvitationsAsync(invitationIds, AcceptUser) instead. Passing multiple targets is no longer supported.")]
        public async Task<Invitation> AcceptInvitationsAsync(List<string> invitationIds, List<InvitationTarget> targets)
        {
            Console.WriteLine("[Vortex SDK] DEPRECATED: Passing an array of targets is deprecated. Use the AcceptUser format instead: new AcceptUser { Email = \"user@example.com\" }");

            if (targets == null || targets.Count == 0)
            {
                throw new VortexException("No targets provided");
            }

            Invitation? lastResult = null;
            foreach (var target in targets)
            {
                lastResult = await AcceptInvitationsAsync(invitationIds, target);
            }

            return lastResult!;
        }

        /// <summary>
        /// Delete all invitations for a specific scope
        /// </summary>
        /// <param name="groupType">The scope type (organization, team, etc.)</param>
        /// <param name="groupId">The scope identifier</param>
        /// <vortex category="invitations" since="0.4.0"/>
        public async Task DeleteInvitationsByScopeAsync(string groupType, string groupId)
        {
            await ApiRequestAsync<object>(HttpMethod.Delete, $"/api/v1/invitations/by-scope/{groupType}/{groupId}");
        }

        /// <summary>
        /// Get all invitations for a specific scope
        /// </summary>
        /// <param name="groupType">The scope type (organization, team, etc.)</param>
        /// <param name="groupId">The scope identifier</param>
        /// <returns>List of invitations for the scope</returns>
        /// <vortex category="invitations" since="0.4.0"/>
        public async Task<List<Invitation>> GetInvitationsByScopeAsync(string groupType, string groupId)
        {
            var response = await ApiRequestAsync<InvitationsResponse>(HttpMethod.Get, $"/api/v1/invitations/by-scope/{groupType}/{groupId}");
            return response.Invitations ?? new List<Invitation>();
        }

        /// <summary>
        /// Reinvite a user (send invitation again)
        /// </summary>
        /// <param name="invitationId">The invitation ID to reinvite</param>
        /// <returns>The reinvited invitation result</returns>
        /// <vortex category="invitations" since="0.2.0"/>
        public async Task<Invitation> ReinviteAsync(string invitationId)
        {
            return await ApiRequestAsync<Invitation>(HttpMethod.Post, $"/api/v1/invitations/{invitationId}/reinvite");
        }

        /// <summary>
        /// Create an invitation from your backend.
        /// This method allows you to create invitations programmatically using your API key,
        /// without requiring a user JWT token. Useful for server-side invitation creation,
        /// such as "People You May Know" flows or admin-initiated invitations.
        /// </summary>
        /// <remarks>
        /// Target types:
        /// <list type="bullet">
        /// <item><description>email: Send an email invitation</description></item>
        /// <item><description>sms: Create an SMS invitation (short link returned for you to send)</description></item>
        /// <item><description>internal: Create an internal invitation for PYMK flows (no email sent)</description></item>
        /// </list>
        /// </remarks>
        /// <param name="request">The create invitation request</param>
        /// <returns>CreateInvitationResponse with id, shortLink, status, and createdAt</returns>
        /// <example>
        /// <code>
        /// // Create an email invitation
        /// var request = new CreateInvitationRequest(
        ///     "widget-config-123",
        ///     CreateInvitationTarget.Email("invitee@example.com"),
        ///     new Inviter("user-456", "inviter@example.com", "John Doe")
        /// );
        /// request.Groups = new List&lt;CreateInvitationScope&gt;
        /// {
        ///     new("team", "team-789", "Engineering")
        /// };
        /// var response = await client.CreateInvitationAsync(request);
        ///
        /// // Create an internal invitation (PYMK flow)
        /// var pymkRequest = new CreateInvitationRequest(
        ///     "widget-config-123",
        ///     CreateInvitationTarget.Internal("internal-user-abc"),
        ///     new Inviter("user-456")
        /// );
        /// pymkRequest.Source = "pymk";
        /// var response = await client.CreateInvitationAsync(pymkRequest);
        /// </code>
        /// </example>
        public async Task<CreateInvitationResponse> CreateInvitationAsync(CreateInvitationRequest request)
        {
            if (request == null)
                throw new VortexException("Request cannot be null");
            if (string.IsNullOrEmpty(request.WidgetConfigurationId))
                throw new VortexException("widgetConfigurationId is required");
            if (request.Target == null || string.IsNullOrEmpty(request.Target.Value))
                throw new VortexException("target with value is required");
            if (request.Inviter == null || string.IsNullOrEmpty(request.Inviter.UserId))
                throw new VortexException("inviter with userId is required");

            // Scope translation: flat params > scopes > groups
            if (!string.IsNullOrEmpty(request.ScopeId) && request.Groups == null && request.Scopes == null)
            {
                request.Groups = new List<CreateInvitationScope>
                {
                    new CreateInvitationScope(request.ScopeType ?? "", request.ScopeId, request.ScopeName ?? "")
                };
            }
            else if (request.Scopes != null && request.Groups == null)
            {
                request.Groups = request.Scopes;
            }

            return await ApiRequestAsync<CreateInvitationResponse>(HttpMethod.Post, "/api/v1/invitations", request);
        }

        /// <summary>
        /// Get autojoin domains configured for a specific scope
        /// </summary>
        /// <param name="scopeType">The type of scope (e.g., "organization", "team", "project")</param>
        /// <param name="scope">The scope identifier (customer's group ID)</param>
        /// <returns>AutojoinDomainsResponse with autojoin domains and invitation</returns>
        /// <vortex category="autojoin" since="0.6.0"/>
        public async Task<AutojoinDomainsResponse> GetAutojoinDomainsAsync(string scopeType, string scope)
        {
            var encodedScopeType = Uri.EscapeDataString(scopeType);
            var encodedScope = Uri.EscapeDataString(scope);
            return await ApiRequestAsync<AutojoinDomainsResponse>(HttpMethod.Get, $"/api/v1/invitations/by-scope/{encodedScopeType}/{encodedScope}/autojoin");
        }

        /// <summary>
        /// Configure autojoin domains for a specific scope
        /// </summary>
        /// <param name="request">The configure autojoin request</param>
        /// <returns>AutojoinDomainsResponse with updated autojoin domains</returns>
        /// <vortex category="autojoin" since="0.6.0"/>
        public async Task<AutojoinDomainsResponse> ConfigureAutojoinAsync(ConfigureAutojoinRequest request)
        {
            if (request == null)
                throw new VortexException("Request cannot be null");
            if (string.IsNullOrEmpty(request.Scope))
                throw new VortexException("scope is required");
            if (string.IsNullOrEmpty(request.ScopeType))
                throw new VortexException("scopeType is required");
            if (string.IsNullOrEmpty(request.ComponentId))
                throw new VortexException("componentId is required");

            return await ApiRequestAsync<AutojoinDomainsResponse>(HttpMethod.Post, "/api/v1/invitations/autojoin", request);
        }

        /// <summary>
        /// Sync an internal invitation action (accept or decline)
        /// </summary>
        /// <remarks>
        /// This method notifies Vortex that an internal invitation was accepted or declined
        /// within your application, so Vortex can update the invitation status accordingly.
        /// </remarks>
        /// <param name="request">The sync internal invitation request</param>
        /// <returns>SyncInternalInvitationResponse with processed count and invitationIds</returns>
        /// <example>
        /// <code>
        /// var request = new SyncInternalInvitationRequest("user-123", "user-456", "accepted", "component-uuid");
        /// var response = await client.SyncInternalInvitationAsync(request);
        /// Console.WriteLine($"Processed: {response.Processed}");
        /// </code>
        /// </example>
        public async Task<SyncInternalInvitationResponse> SyncInternalInvitationAsync(SyncInternalInvitationRequest request)
        {
            if (request == null)
                throw new VortexException("Request cannot be null");

            return await ApiRequestAsync<SyncInternalInvitationResponse>(HttpMethod.Post, "/api/v1/invitations/sync-internal-invitation", request);
        }

        private async Task<T> ApiRequestAsync<T>(HttpMethod method, string path, object? body = null)
        {
            var request = new HttpRequestMessage(method, path);

            if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new VortexException($"Vortex API request failed: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();

            // Handle empty responses
            if (string.IsNullOrWhiteSpace(content))
            {
                return default!;
            }

            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input
                .Replace('-', '+')
                .Replace('_', '/');

            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }

            return Convert.FromBase64String(output);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
