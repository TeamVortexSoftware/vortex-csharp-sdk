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
        public string GenerateJwt(Dictionary<string, object> parameters)
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

            var expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600; // 1 hour from now

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

            // Add name if present
            if (user.UserName != null)
            {
                payload["userName"] = user.UserName;
            }

            // Add userAvatarUrl if present
            if (user.UserAvatarUrl != null)
            {
                payload["userAvatarUrl"] = user.UserAvatarUrl;
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
        public async Task<List<Invitation>> GetInvitationsByTargetAsync(string targetType, string targetValue)
        {
            var queryParams = $"?targetType={Uri.EscapeDataString(targetType)}&targetValue={Uri.EscapeDataString(targetValue)}";
            var response = await ApiRequestAsync<InvitationsResponse>(HttpMethod.Get, $"/api/v1/invitations{queryParams}");
            return response.Invitations ?? new List<Invitation>();
        }

        /// <summary>
        /// Get a specific invitation by ID
        /// </summary>
        public async Task<Invitation> GetInvitationAsync(string invitationId)
        {
            return await ApiRequestAsync<Invitation>(HttpMethod.Get, $"/api/v1/invitations/{invitationId}");
        }

        /// <summary>
        /// Revoke (delete) an invitation
        /// </summary>
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
        /// <remarks>
        /// This is the recommended method for accepting invitations.
        /// </remarks>
        /// <param name="invitationId">Single invitation ID to accept</param>
        /// <param name="user">User with email or phone (and optional name)</param>
        /// <returns>Invitation result</returns>
        /// <example>
        /// <code>
        /// var user = new AcceptUser { Email = "user@example.com", Name = "John Doe" };
        /// var result = await client.AcceptInvitationAsync("inv-123", user);
        /// </code>
        /// </example>
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
        /// Delete all invitations for a specific group
        /// </summary>
        public async Task DeleteInvitationsByGroupAsync(string groupType, string groupId)
        {
            await ApiRequestAsync<object>(HttpMethod.Delete, $"/api/v1/invitations/by-group/{groupType}/{groupId}");
        }

        /// <summary>
        /// Get all invitations for a specific group
        /// </summary>
        public async Task<List<Invitation>> GetInvitationsByGroupAsync(string groupType, string groupId)
        {
            var response = await ApiRequestAsync<InvitationsResponse>(HttpMethod.Get, $"/api/v1/invitations/by-group/{groupType}/{groupId}");
            return response.Invitations ?? new List<Invitation>();
        }

        /// <summary>
        /// Reinvite a user (send invitation again)
        /// </summary>
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
        /// request.Groups = new List&lt;CreateInvitationGroup&gt;
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

            return await ApiRequestAsync<CreateInvitationResponse>(HttpMethod.Post, "/api/v1/invitations", request);
        }

        /// <summary>
        /// Get autojoin domains configured for a specific scope
        /// </summary>
        /// <param name="scopeType">The type of scope (e.g., "organization", "team", "project")</param>
        /// <param name="scope">The scope identifier (customer's group ID)</param>
        /// <returns>AutojoinDomainsResponse with autojoin domains and associated invitation</returns>
        /// <example>
        /// <code>
        /// var response = await client.GetAutojoinDomainsAsync("organization", "acme-org");
        /// foreach (var domain in response.AutojoinDomains)
        /// {
        ///     Console.WriteLine($"Domain: {domain.Domain}");
        /// }
        /// </code>
        /// </example>
        public async Task<AutojoinDomainsResponse> GetAutojoinDomainsAsync(string scopeType, string scope)
        {
            var encodedScopeType = Uri.EscapeDataString(scopeType);
            var encodedScope = Uri.EscapeDataString(scope);
            return await ApiRequestAsync<AutojoinDomainsResponse>(HttpMethod.Get, $"/api/v1/invitations/by-scope/{encodedScopeType}/{encodedScope}/autojoin");
        }

        /// <summary>
        /// Configure autojoin domains for a specific scope.
        /// This endpoint syncs autojoin domains - it will add new domains, remove domains
        /// not in the provided list, and deactivate the autojoin invitation if all domains
        /// are removed (empty array).
        /// </summary>
        /// <param name="request">The configure autojoin request</param>
        /// <returns>AutojoinDomainsResponse with updated autojoin domains and associated invitation</returns>
        /// <example>
        /// <code>
        /// var request = new ConfigureAutojoinRequest(
        ///     "acme-org",
        ///     "organization",
        ///     new List&lt;string&gt; { "acme.com", "acme.org" },
        ///     "widget-123"
        /// );
        /// request.ScopeName = "Acme Corporation";
        /// var response = await client.ConfigureAutojoinAsync(request);
        /// </code>
        /// </example>
        public async Task<AutojoinDomainsResponse> ConfigureAutojoinAsync(ConfigureAutojoinRequest request)
        {
            if (request == null)
                throw new VortexException("Request cannot be null");
            if (string.IsNullOrEmpty(request.Scope))
                throw new VortexException("scope is required");
            if (string.IsNullOrEmpty(request.ScopeType))
                throw new VortexException("scopeType is required");
            if (string.IsNullOrEmpty(request.WidgetId))
                throw new VortexException("widgetId is required");

            return await ApiRequestAsync<AutojoinDomainsResponse>(HttpMethod.Post, "/api/v1/invitations/autojoin", request);
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
