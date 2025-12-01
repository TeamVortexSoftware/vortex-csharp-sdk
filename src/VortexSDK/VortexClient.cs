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
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "vortex-csharp-sdk/1.0.0");
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
        /// var parameters1 = new Dictionary&lt;string, object&gt;
        /// {
        ///     ["user"] = new User("user-123", "user@example.com", new List&lt;string&gt; { "autoJoin" })
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

            // Add adminScopes if present
            if (user.AdminScopes != null)
            {
                payload["adminScopes"] = user.AdminScopes;
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
        /// Accept multiple invitations
        /// </summary>
        public async Task<Invitation> AcceptInvitationsAsync(List<string> invitationIds, InvitationTarget target)
        {
            var body = new
            {
                invitationIds,
                target = new { type = target.Type, value = target.Value }
            };

            return await ApiRequestAsync<Invitation>(HttpMethod.Post, "/api/v1/invitations/accept", body);
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
