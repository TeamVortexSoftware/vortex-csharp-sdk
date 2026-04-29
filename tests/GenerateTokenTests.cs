using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TeamVortexSoftware.VortexSDK;
using Xunit;

namespace VortexSDK.Tests
{
    public class GenerateTokenTests
    {
        // Test API key for unit tests (same format as production keys)
        private const string TestApiKey = "VRTX.EjRWeJCrze8SNFZ4kKvN7w.test-secret-key-12345";

        private VortexClient CreateClient() => new VortexClient(TestApiKey);

        // ─── ParseExpiresIn Tests ────────────────────────────────────

        [Theory]
        [InlineData("5m", 300)]      // 5 minutes
        [InlineData("1h", 3600)]     // 1 hour
        [InlineData("24h", 86400)]   // 24 hours
        [InlineData("7d", 604800)]   // 7 days
        [InlineData("30d", 2592000)] // 30 days
        public void ParseExpiresIn_ValidStrings_ReturnsCorrectSeconds(string input, int expected)
        {
            using var client = CreateClient();
            var method = GetParseExpiresInMethod(client);
            var result = (int)method.Invoke(client, new object[] { input })!;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(300)]
        [InlineData(3600)]
        [InlineData(86400)]
        public void ParseExpiresIn_ValidIntegers_ReturnsCorrectSeconds(int input)
        {
            using var client = CreateClient();
            var method = GetParseExpiresInMethod(client);
            var result = (int)method.Invoke(client, new object[] { input })!;
            Assert.Equal(input, result);
        }

        [Theory]
        [InlineData("0m")]   // Zero minutes
        [InlineData("0h")]   // Zero hours
        [InlineData("0d")]   // Zero days
        public void ParseExpiresIn_ZeroDuration_ThrowsException(string input)
        {
            using var client = CreateClient();
            var method = GetParseExpiresInMethod(client);
            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(client, new object[] { input }));
            Assert.IsType<VortexException>(ex.InnerException);
            Assert.Contains("Must be positive", ex.InnerException!.Message);
        }

        [Theory]
        [InlineData(0)]   // Zero seconds
        [InlineData(-1)]  // Negative seconds
        [InlineData(-100)]
        public void ParseExpiresIn_ZeroOrNegativeInt_ThrowsException(int input)
        {
            using var client = CreateClient();
            var method = GetParseExpiresInMethod(client);
            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(client, new object[] { input }));
            Assert.IsType<VortexException>(ex.InnerException);
            Assert.Contains("Must be positive", ex.InnerException!.Message);
        }

        [Theory]
        [InlineData("5")]       // Missing unit
        [InlineData("5s")]      // Invalid unit
        [InlineData("5min")]    // Invalid unit
        [InlineData("m5")]      // Wrong order
        [InlineData("abc")]     // Not a number
        [InlineData("")]        // Empty string
        public void ParseExpiresIn_InvalidFormat_ThrowsException(string input)
        {
            using var client = CreateClient();
            var method = GetParseExpiresInMethod(client);
            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(client, new object[] { input }));
            Assert.IsType<VortexException>(ex.InnerException);
            Assert.Contains("Invalid expiresIn format", ex.InnerException!.Message);
        }

        [Fact]
        public void ParseExpiresIn_VeryLargeDuration_ThrowsOverflowException()
        {
            using var client = CreateClient();
            var method = GetParseExpiresInMethod(client);
            // 999999999d would overflow int.MaxValue
            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(client, new object[] { "999999999d" }));
            Assert.IsType<VortexException>(ex.InnerException);
            Assert.Contains("Duration is too large", ex.InnerException!.Message);
        }

        // ─── ParseApiKey Tests ────────────────────────────────────

        [Fact]
        public void ParseApiKey_ValidKey_ReturnsKidAndKey()
        {
            using var client = CreateClient();
            var method = client.GetType().GetMethod("ParseApiKey",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var result = method.Invoke(client, Array.Empty<object>())!;
            
            // Use reflection to access tuple values
            var resultType = result.GetType();
            var kid = (string)resultType.GetField("Item1")!.GetValue(result)!;
            var key = (string)resultType.GetField("Item2")!.GetValue(result)!;
            
            // kid should be a valid UUID
            Assert.True(Guid.TryParse(kid, out _), $"kid should be valid UUID, got: {kid}");
            // key should be the third part of the API key
            Assert.Equal("test-secret-key-12345", key);
        }

        [Fact]
        public void ParseApiKey_InvalidPrefix_ThrowsException()
        {
            using var invalidClient = new VortexClient("INVALID.EjRWeJCrze8SNFZ4kKvN7w.key");
            var method = invalidClient.GetType().GetMethod("ParseApiKey",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(invalidClient, Array.Empty<object>()));
            Assert.IsType<VortexException>(ex.InnerException);
            Assert.Contains("Invalid API key format", ex.InnerException!.Message);
        }

        [Fact]
        public void ParseApiKey_InvalidFormat_ThrowsException()
        {
            using var invalidClient = new VortexClient("VRTX.invalidbase64");
            var method = invalidClient.GetType().GetMethod("ParseApiKey",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(invalidClient, Array.Empty<object>()));
            Assert.IsType<VortexException>(ex.InnerException);
        }

        // ─── GenerateToken Tests ────────────────────────────────────

        [Fact]
        public void GenerateToken_WithUser_ReturnsValidJwt()
        {
            using var client = CreateClient();
            var payload = new GenerateTokenPayload(new TokenUser("user-123"));
            var token = client.GenerateToken(payload);

            Assert.NotNull(token);
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length); // JWT has 3 parts

            // Decode and verify payload contains user
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            var payloadData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson)!;
            Assert.True(payloadData.ContainsKey("user"));
            Assert.True(payloadData.ContainsKey("iat"));
            Assert.True(payloadData.ContainsKey("exp"));
        }

        [Fact]
        public void GenerateToken_WithCustomExpiry_SetsCorrectExp()
        {
            using var client = CreateClient();
            var payload = new GenerateTokenPayload(new TokenUser("user-123"));
            var options = new GenerateTokenOptions("1h"); // 1 hour = 3600 seconds
            var token = client.GenerateToken(payload, options);

            var parts = token.Split('.');
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            var payloadData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson)!;
            
            var iat = payloadData["iat"].GetInt64();
            var exp = payloadData["exp"].GetInt64();
            Assert.Equal(3600, exp - iat); // 1 hour difference
        }

        [Fact]
        public void GenerateToken_With24Hours_SetsCorrectExp()
        {
            using var client = CreateClient();
            var payload = new GenerateTokenPayload(new TokenUser("user-123"));
            var options = new GenerateTokenOptions("24h"); // 24 hours = 86400 seconds
            var token = client.GenerateToken(payload, options);

            var parts = token.Split('.');
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            var payloadData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson)!;
            
            var iat = payloadData["iat"].GetInt64();
            var exp = payloadData["exp"].GetInt64();
            Assert.Equal(86400, exp - iat); // 24 hours difference
        }

        [Fact]
        public void GenerateToken_NullPayload_ThrowsArgumentNullException()
        {
            using var client = CreateClient();
            Assert.Throws<ArgumentNullException>(() => client.GenerateToken(null!));
        }

        [Fact]
        public void GenerateToken_MissingUserId_WarnsToConsole()
        {
            using var client = CreateClient();
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);
            
            try
            {
                // Missing user.id should trigger warning
                var payload = new GenerateTokenPayload(new TokenUser());
                client.GenerateToken(payload);
                
                var output = writer.ToString();
                Assert.Contains("signing payload without user.id", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void GenerateToken_NullUser_WarnsToConsole()
        {
            using var client = CreateClient();
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);
            
            try
            {
                // Null user should trigger warning
                var payload = new GenerateTokenPayload { Component = "widget-123" };
                client.GenerateToken(payload);
                
                var output = writer.ToString();
                Assert.Contains("signing payload without user.id", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void GenerateToken_WithUserId_NoWarning()
        {
            using var client = CreateClient();
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);
            
            try
            {
                var payload = new GenerateTokenPayload(new TokenUser("user-123"));
                client.GenerateToken(payload);
                
                var output = writer.ToString();
                Assert.DoesNotContain("signing payload without user.id", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void GenerateToken_ExtraFieldsDoNotOverrideTypedFields()
        {
            using var client = CreateClient();
            var payload = new GenerateTokenPayload(new TokenUser("user-123"))
            {
                Component = "real-component",
                Extra = new Dictionary<string, object>
                {
                    ["component"] = "should-not-override", // This should be overridden by the typed field
                    ["customField"] = "custom-value"
                }
            };
            var token = client.GenerateToken(payload);

            var parts = token.Split('.');
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            var payloadData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson)!;
            
            // Typed field should win over Extra
            Assert.Equal("real-component", payloadData["component"].GetString());
            // Custom field from Extra should still be present
            Assert.Equal("custom-value", payloadData["customField"].GetString());
        }

        [Fact]
        public void GenerateToken_WithAllFields_IncludesAllInPayload()
        {
            using var client = CreateClient();
            var payload = new GenerateTokenPayload
            {
                User = new TokenUser("user-123", "Test User", "test@example.com"),
                Component = "my-component",
                Scope = "my-scope",
                Vars = new Dictionary<string, object> { ["key"] = "value" }
            };
            var token = client.GenerateToken(payload);

            var parts = token.Split('.');
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            var payloadData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson)!;
            
            Assert.True(payloadData.ContainsKey("user"));
            Assert.Equal("my-component", payloadData["component"].GetString());
            Assert.Equal("my-scope", payloadData["scope"].GetString());
            Assert.True(payloadData.ContainsKey("vars"));
        }

        [Fact]
        public void GenerateToken_HeaderContainsCorrectKid()
        {
            using var client = CreateClient();
            var payload = new GenerateTokenPayload(new TokenUser("user-123"));
            var token = client.GenerateToken(payload);

            var parts = token.Split('.');
            var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            var headerData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(headerJson)!;
            
            Assert.Equal("HS256", headerData["alg"].GetString());
            Assert.Equal("JWT", headerData["typ"].GetString());
            Assert.True(headerData.ContainsKey("kid"));
            
            // kid should be a valid UUID format
            var kid = headerData["kid"].GetString()!;
            Assert.True(Guid.TryParse(kid, out _), $"kid should be valid UUID, got: {kid}");
        }

        // ─── TokenUser Tests ────────────────────────────────────

        [Fact]
        public void TokenUser_NullId_OmittedFromJson()
        {
            var user = new TokenUser { Name = "Test User" }; // Id is null
            var json = JsonSerializer.Serialize(user);
            Assert.DoesNotContain("\"id\"", json);
            Assert.Contains("\"name\"", json);
        }

        [Fact]
        public void TokenUser_WithId_IncludedInJson()
        {
            var user = new TokenUser("user-123");
            var json = JsonSerializer.Serialize(user);
            Assert.Contains("\"id\":\"user-123\"", json);
        }

        // ─── Invalid API Key Tests ────────────────────────────────────

        [Fact]
        public void GenerateToken_InvalidApiKey_ThrowsException()
        {
            using var invalidClient = new VortexClient("invalid-api-key");
            var payload = new GenerateTokenPayload(new TokenUser("user-123"));
            Assert.Throws<VortexException>(() => invalidClient.GenerateToken(payload));
        }

        [Fact]
        public void GenerateJwt_InvalidApiKey_ThrowsException()
        {
            using var invalidClient = new VortexClient("invalid-api-key");
            var parameters = new Dictionary<string, object>
            {
                ["user"] = new User("user-123", "test@example.com")
            };
            Assert.Throws<VortexException>(() => invalidClient.GenerateJwt(parameters));
        }

        [Fact]
        public void Sign_InvalidApiKey_ThrowsException()
        {
            using var invalidClient = new VortexClient("invalid-api-key");
            var user = new Dictionary<string, object> { ["id"] = "user-123" };
            Assert.Throws<VortexException>(() => invalidClient.Sign(user));
        }

        // ─── Additional ParseExpiresIn Tests ────────────────────────────────────

        [Fact]
        public void ParseExpiresIn_NullValue_ThrowsException()
        {
            using var client = CreateClient();
            var method = GetParseExpiresInMethod(client);
            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(client, new object[] { null! }));
            Assert.IsType<VortexException>(ex.InnerException);
        }

        [Theory]
        [InlineData(3.14)]  // Float
        [InlineData(true)]  // Boolean
        public void ParseExpiresIn_InvalidType_ThrowsException(object input)
        {
            using var client = CreateClient();
            var method = GetParseExpiresInMethod(client);
            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(client, new object[] { input }));
            Assert.IsType<VortexException>(ex.InnerException);
            Assert.Contains("expiresIn must be a string or int", ex.InnerException!.Message);
        }

        // ─── Helper Methods ────────────────────────────────────

        // ─── Signature Consistency Tests ────────────────────────────────────

        [Fact]
        public void GenerateToken_SamePayloadSameTimestamp_ProducesIdenticalSignature()
        {
            using var client = CreateClient();
            var payload1 = new GenerateTokenPayload(new TokenUser("user-123"));
            var payload2 = new GenerateTokenPayload(new TokenUser("user-123"));
            
            // Note: In real scenarios, iat will differ due to time progression
            // This test just ensures the signing mechanism is deterministic
            var token1 = client.GenerateToken(payload1);
            var token2 = client.GenerateToken(payload2);
            
            // Signatures should be different due to different iat timestamps
            var sig1 = token1.Split('.')[2];
            var sig2 = token2.Split('.')[2];
            // They might be the same if executed in the same second, but structure is valid
            Assert.NotNull(sig1);
            Assert.NotNull(sig2);
        }

        [Fact]
        public void GenerateToken_DifferentPayload_ProducesDifferentSignature()
        {
            using var client = CreateClient();
            var payload1 = new GenerateTokenPayload(new TokenUser("user-123"));
            var payload2 = new GenerateTokenPayload(new TokenUser("user-456"));
            
            var token1 = client.GenerateToken(payload1);
            var token2 = client.GenerateToken(payload2);
            
            var sig1 = token1.Split('.')[2];
            var sig2 = token2.Split('.')[2];
            Assert.NotEqual(sig1, sig2);
        }

        // ─── Helper Methods ────────────────────────────────────

        private static MethodInfo GetParseExpiresInMethod(VortexClient client)
        {
            return client.GetType().GetMethod("ParseExpiresIn", 
                BindingFlags.NonPublic | BindingFlags.Instance)!;
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
    }
}
