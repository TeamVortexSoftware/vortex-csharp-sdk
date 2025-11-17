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

            // Example 1: Generate JWT - simple usage
            Console.WriteLine("=== JWT Generation Example ===");
            var user = new User("user-123", "user@example.com", new List<string> { "autoJoin" });
            var jwt1 = vortex.GenerateJwt(user);
            Console.WriteLine($"Generated JWT: {jwt1}\n");

            // Example 2: Generate JWT with additional properties
            Console.WriteLine("=== JWT Generation with Additional Properties ===");
            var user2 = new User("user-456", "user@example.com");
            var extra = new Dictionary<string, object>
            {
                { "role", "admin" },
                { "department", "Engineering" }
            };
            var jwt2 = vortex.GenerateJwt(user2, extra);
            Console.WriteLine($"Generated JWT with extra: {jwt2}\n");

            // Example 3: Get invitations by target
            try
            {
                Console.WriteLine("=== Get Invitations by Target Example ===");
                var invitations = await vortex.GetInvitationsByTargetAsync("email", "user@example.com");
                Console.WriteLine($"Found {invitations.Count} invitations");
            }
            catch (VortexException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\n=== Example Complete ===");
            Console.WriteLine("To use with real data, set VORTEX_API_KEY environment variable");
        }
    }
}
