using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeamVortexSoftware.VortexSDK;

namespace VortexSDK.Examples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize the Vortex client
            var apiKey = Environment.GetEnvironmentVariable("VORTEX_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Please set VORTEX_API_KEY environment variable");
                return;
            }

            using var vortex = new VortexClient(apiKey);

            // Example user data
            var userId = "user-123";
            var identifiers = new List<Identifier>
            {
                new Identifier("email", "user@example.com"),
                new Identifier("sms", "18008675309")
            };
            var groups = new List<Group>
            {
                new Group("workspace", "ws-1", "Main Workspace"),
                new Group("team", "team-1", "Engineering")
            };
            var role = "admin";

            // Generate a JWT
            Console.WriteLine("Generating JWT...");
            var jwt = vortex.GenerateJwt(userId, identifiers, groups, role);
            Console.WriteLine($"JWT: {jwt}\n");

            // Example: Get invitations by target
            try
            {
                Console.WriteLine("Fetching invitations by email...");
                var invitations = await vortex.GetInvitationsByTargetAsync("email", "user@example.com");
                Console.WriteLine($"Found {invitations.Count} invitations");
            }
            catch (VortexException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
