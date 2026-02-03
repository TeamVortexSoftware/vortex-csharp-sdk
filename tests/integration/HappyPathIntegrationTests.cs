using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

namespace TeamVortexSoftware.VortexSDK.Tests
{
    [Collection("Integration Tests")]
    public class HappyPathIntegrationTests
    {
        private readonly string _apiKey;
        private readonly string _clientApiUrl;
        private readonly string _publicApiUrl;
        private readonly string _sessionId;
        private string? _invitationId;

        public HappyPathIntegrationTests()
        {
            // Validate required environment variables
            _apiKey = Environment.GetEnvironmentVariable("TEST_INTEGRATION_SDKS_VORTEX_API_KEY");
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("Missing required environment variable: TEST_INTEGRATION_SDKS_VORTEX_API_KEY");

            _clientApiUrl = Environment.GetEnvironmentVariable("TEST_INTEGRATION_SDKS_VORTEX_CLIENT_API_URL");
            if (string.IsNullOrEmpty(_clientApiUrl))
                throw new InvalidOperationException("Missing required environment variable: TEST_INTEGRATION_SDKS_VORTEX_CLIENT_API_URL");

            _publicApiUrl = Environment.GetEnvironmentVariable("TEST_INTEGRATION_SDKS_VORTEX_PUBLIC_API_URL");
            if (string.IsNullOrEmpty(_publicApiUrl))
                throw new InvalidOperationException("Missing required environment variable: TEST_INTEGRATION_SDKS_VORTEX_PUBLIC_API_URL");

            _sessionId = Environment.GetEnvironmentVariable("TEST_INTEGRATION_SDKS_VORTEX_SESSION_ID");
            if (string.IsNullOrEmpty(_sessionId))
                throw new InvalidOperationException("Missing required environment variable: TEST_INTEGRATION_SDKS_VORTEX_SESSION_ID");
        }

        [Fact]
        public async Task TestFullInvitationFlow()
        {
            Console.WriteLine("\n--- Starting C# SDK Integration Test ---");

            var publicClient = new VortexClient(_apiKey, _publicApiUrl);

            var userEmail = Environment.GetEnvironmentVariable("TEST_INTEGRATION_SDKS_USER_EMAIL");
            if (string.IsNullOrEmpty(userEmail))
                throw new InvalidOperationException("Missing required environment variable: TEST_INTEGRATION_SDKS_USER_EMAIL");
            userEmail = userEmail.Replace("{timestamp}", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            var groupType = Environment.GetEnvironmentVariable("TEST_INTEGRATION_SDKS_GROUP_TYPE");
            if (string.IsNullOrEmpty(groupType))
                throw new InvalidOperationException("Missing required environment variable: TEST_INTEGRATION_SDKS_GROUP_TYPE");

            var groupName = Environment.GetEnvironmentVariable("TEST_INTEGRATION_SDKS_GROUP_NAME");
            if (string.IsNullOrEmpty(groupName))
                throw new InvalidOperationException("Missing required environment variable: TEST_INTEGRATION_SDKS_GROUP_NAME");

            // TEST_INTEGRATION_SDKS_GROUP_ID is dynamic - generated from timestamp
            var groupId = $"test-group-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            // Step 1: Create invitation
            Console.WriteLine("Step 1: Creating invitation...");
            _invitationId = await CreateInvitationAsync(userEmail, groupType, groupId, groupName);
            Assert.NotNull(_invitationId);
            Console.WriteLine($"✓ Created invitation: {_invitationId}");

            // Step 2a: Get invitation by ID
            Console.WriteLine("Step 2a: Getting invitation by ID...");
            var invitation = await publicClient.GetInvitationAsync(_invitationId);
            Assert.NotNull(invitation);
            Assert.Equal(_invitationId, invitation.Id);
            Console.WriteLine("✓ Retrieved invitation by ID successfully");

            // Step 2b: Get invitations by target
            Console.WriteLine("Step 2b: Getting invitations by target...");
            var invitations = await publicClient.GetInvitationsByTargetAsync("email", userEmail);
            Assert.NotEmpty(invitations);
            // Verify the single invitation is in the list
            var foundInList = invitations.Any(inv => inv.Id == _invitationId);
            Assert.True(foundInList, "Invitation not found in list returned by target");
            Console.WriteLine("✓ Retrieved invitations by target successfully and verified invitation is in list");

            // Step 3: Accept invitation
            Console.WriteLine("Step 3: Accepting invitation...");
            var result = await publicClient.AcceptInvitationsAsync(
                new List<string> { _invitationId },
                new InvitationTarget { Type = InvitationTargetType.email, Value = userEmail }
            );
            Assert.NotNull(result);
            Console.WriteLine("✓ Accepted invitation successfully");

            Console.WriteLine("--- C# SDK Integration Test Complete ---\n");
        }

        private async Task<string?> CreateInvitationAsync(
            string userEmail,
            string groupType, string groupId, string groupName)
        {
            using var client = new HttpClient();

            // Generate JWT for authentication
            var jwtClient = new VortexClient(_apiKey, _clientApiUrl);
            var userId = Environment.GetEnvironmentVariable("TEST_INTEGRATION_SDKS_USER_ID");
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Missing required environment variable: TEST_INTEGRATION_SDKS_USER_ID");
            var jwtParams = new Dictionary<string, object>
            {
                ["user"] = new User(userId, userEmail)
            };
            var jwt = jwtClient.GenerateJwt(jwtParams);

            // Step 1: Fetch widget configuration to get the widget configuration ID and sessionAttestation
            var componentId = Environment.GetEnvironmentVariable("TEST_INTEGRATION_SDKS_VORTEX_COMPONENT_ID");
            if (string.IsNullOrEmpty(componentId))
                throw new InvalidOperationException("Missing required environment variable: TEST_INTEGRATION_SDKS_VORTEX_COMPONENT_ID");
            var widgetUrl = $"{_clientApiUrl}/api/v1/widgets/{componentId}?templateVariables=lzstr:N4Ig5gTg9grgDgfQHYEMC2BTEAuEBlAEQGkACAFQwGcAXEgcWnhABoQBLJANzeowmXRZcBCCQBqUCLwAeLcI0SY0AIz4IAxrCTUcIAMxzNaOCiQBPAZl0SpGaSQCSSdQDoQAXyA";
            var widgetRequest = new HttpRequestMessage(HttpMethod.Get, widgetUrl);
            widgetRequest.Headers.Add("Authorization", $"Bearer {jwt}");
            widgetRequest.Headers.Add("x-session-id", _sessionId);

            var widgetResponse = await client.SendAsync(widgetRequest);
            if (!widgetResponse.IsSuccessStatusCode)
            {
                var errorBody = await widgetResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to fetch widget configuration with HTTP {widgetResponse.StatusCode}: {errorBody}");
                return null;
            }

            var widgetData = await widgetResponse.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
            var actualWidgetConfigId = widgetData?["data"].GetProperty("widgetConfiguration").GetProperty("id").GetString();
            var sessionAttestation = widgetData?["data"].GetProperty("sessionAttestation").GetString();

            if (string.IsNullOrEmpty(actualWidgetConfigId))
            {
                Console.WriteLine("Widget configuration ID not found in response");
                return null;
            }

            if (string.IsNullOrEmpty(sessionAttestation))
            {
                Console.WriteLine("Session attestation not found in widget response");
                return null;
            }

            Console.WriteLine($"Using widget configuration ID: {actualWidgetConfigId}");

            // Step 2: Create invitation with the widget configuration ID
            var invitationUrl = $"{_clientApiUrl}/api/v1/invitations";
            var data = new
            {
                payload = new Dictionary<string, object>
                {
                    ["emails"] = new { value = userEmail, type = "email", role = "member" }
                },
                group = new { type = groupType, groupId, name = groupName },
                source = "email",
                widgetConfigurationId = actualWidgetConfigId,
                templateVariables = new Dictionary<string, string>
                {
                    ["group_name"] = "SDK Test Group",
                    ["inviter_name"] = "Dr Vortex",
                    ["group_member_count"] = "3",
                    ["company_name"] = "Vortex Inc."
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, invitationUrl)
            {
                Content = JsonContent.Create(data)
            };
            request.Headers.Add("Authorization", $"Bearer {jwt}");
            request.Headers.Add("x-session-id", _sessionId);
            request.Headers.Add("x-session-attestation", sessionAttestation);

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create invitation failed with HTTP {response.StatusCode}: {errorBody}");
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

            // The API returns the full widget configuration with invitation entries
            string? invitationId = null;
            try
            {
                if (result != null && result.ContainsKey("data"))
                {
                    var responseData = result["data"];
                    if (responseData.TryGetProperty("invitationEntries", out var entries) && entries.GetArrayLength() > 0)
                    {
                        invitationId = entries[0].GetProperty("id").GetString();
                    }
                }

                // Fallback to direct id property
                if (string.IsNullOrEmpty(invitationId) && result != null && result.ContainsKey("id"))
                {
                    invitationId = result["id"].GetString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting invitation ID: {ex.Message}");
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response body: {responseBody}");
                return null;
            }

            if (!string.IsNullOrEmpty(invitationId))
            {
                Console.WriteLine($"Successfully extracted invitation ID: {invitationId}");
            }
            else
            {
                Console.WriteLine("Failed to extract invitation ID from response");
            }

            return invitationId;
        }
    }
}
