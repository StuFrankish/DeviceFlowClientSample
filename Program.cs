using Duende.IdentityModel.Client;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Starting Device Flow Client...");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Read configuration settings
var authority = configuration["IdentityProvider:Authority"];
var clientId = configuration["IdentityProvider:ClientId"];
var clientSecret = configuration["IdentityProvider:ClientSecret"];


using var httpClient = new HttpClient();

// Discover endpoints from the identity provider's metadata
var disco = await httpClient.GetDiscoveryDocumentAsync(authority);
if (disco.IsError)
{
    Console.WriteLine("Discovery error: " + disco.Error);
    return;
}

// Request the device code
var deviceCodeResponse = await httpClient.RequestDeviceAuthorizationAsync(new DeviceAuthorizationRequest
{
    Address = disco.DeviceAuthorizationEndpoint,
    ClientId = clientId,
    ClientSecret = clientSecret
});

if (deviceCodeResponse.IsError)
{
    Console.WriteLine("Device code error: " + deviceCodeResponse.Error);
    return;
}

// Inform the user to complete the verification
Console.WriteLine("Please open the following URL in your browser and enter the user code:");
Console.WriteLine(deviceCodeResponse.VerificationUri);
Console.WriteLine("User Code: " + deviceCodeResponse.UserCode);

// Poll the token endpoint until the user completes authorization
TokenResponse tokenResponse = null;
while (true)
{
    tokenResponse = await httpClient.RequestDeviceTokenAsync(new DeviceTokenRequest
    {
        Address = disco.TokenEndpoint,
        ClientId = clientId,
        ClientSecret = clientSecret,
        DeviceCode = deviceCodeResponse.DeviceCode
    });

    if (!tokenResponse.IsError)
    {
        break; // Successful token response
    }

    // Continue polling if the user hasn't completed authorization yet
    if (tokenResponse.Error != "authorization_pending")
    {
        Console.WriteLine("Token request error: " + tokenResponse.Error);
        return;
    }

    Console.WriteLine("Waiting for user authorization...");
    await Task.Delay(deviceCodeResponse.Interval * 1000);
}

// Output the token response (access token, refresh token, etc.)
Console.WriteLine("Token response received:");
Console.WriteLine(tokenResponse.Json);
