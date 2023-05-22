using PayPal.Api;

namespace PayPalGateway.Services;

public static class PaypalConfiguration
{
    public readonly static string ClientId;
    public readonly static string ClientSecret;

    static PaypalConfiguration()
    {

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        ClientId = config["Paypal:ClientId"];
        ClientSecret = config["Paypal:ClientSecret"];
    }

    public static APIContext GetAPIContext()
    {

        APIContext apiContext = new APIContext(GetAccessToken());
        apiContext.Config = GetConfig();
        return apiContext;
    }


    //get config
    private static Dictionary<string, string> GetConfig()
    {
        var config = new Dictionary<string, string>();
        config.Add("mode", "sandbox");
        config.Add("clientId", ClientId);
        config.Add("clientSecret", ClientSecret);
        return config;
    }

    //Get access token from paypal
    private static string GetAccessToken()
    {
        // getting accesstocken from paypal                
        string accessToken = new OAuthTokenCredential(
            ClientId,
            ClientSecret,
            GetConfig()
            ).GetAccessToken();
        return accessToken;
    }
}