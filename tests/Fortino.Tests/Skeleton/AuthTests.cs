using System.Net;
using System.Text.RegularExpressions;
using Fortino.Web.Data;

namespace Fortino.Tests.Skeleton;

// L'ossatura è scritta nel modo corretto: questi test lo verificano.
public class AuthTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    [Fact]
    public async Task Anonymous_visitor_is_sent_to_login()
    {
        var client = factory.CreateBrowserClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://localhost/login", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("anna")]
    [InlineData("mario")]
    [InlineData("verdi")]
    [InlineData("lumen")]
    public async Task Every_demo_user_can_log_in(string username)
    {
        var client = await factory.CreateClientAsAsync(username);

        var response = await client.GetAsync("/lab");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var client = factory.CreateBrowserClient();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Fortino.Web.Lab.LabLogin.LoginAsync(client, "mario", "password-sbagliata"));

        Assert.Contains("mario", ex.Message);
    }

    [Fact]
    public async Task Login_without_antiforgery_token_is_rejected()
    {
        var client = factory.CreateBrowserClient();

        var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "mario",
            ["Password"] = SeedData.DemoPassword,
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("https://sito-esterno.example/", "/")]
    [InlineData("//sito-esterno.example/", "/")]
    [InlineData("/invoices", "/invoices")]
    public async Task Login_redirects_only_to_local_addresses(string returnUrl, string expected)
    {
        var client = factory.CreateBrowserClient();
        var token = await GetTokenAsync(client, "/login");

        var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Username"] = "mario",
            ["Password"] = SeedData.DemoPassword,
            ["ReturnUrl"] = returnUrl,
        }));

        Assert.Equal(expected, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Customer_cannot_open_staff_pages()
    {
        var client = await factory.CreateClientAsAsync("verdi");

        var response = await client.GetAsync("/customers");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/access-denied", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Switch_user_changes_the_logged_in_user()
    {
        var client = await factory.CreateClientAsAsync("mario");
        var token = await GetTokenAsync(client, "/login");

        var switched = await client.PostAsync("/lab/switch-user", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["UserId"] = "3",          // Studio Verdi, cliente
            ["ReturnUrl"] = "/portal",
        }));
        var portal = await client.GetAsync("/portal");

        Assert.Equal("/portal", switched.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, portal.StatusCode);
    }

    internal static async Task<string> GetTokenAsync(HttpClient client, string page)
    {
        var html = await client.GetStringAsync(page);
        var match = Regex.Match(html, """name="__RequestVerificationToken"[^>]*?value="(?<v>[^"]+)"|value="(?<v>[^"]+)"[^>]*?name="__RequestVerificationToken" """);
        Assert.True(match.Success, "Token antiforgery non trovato.");
        return WebUtility.HtmlDecode(match.Groups["v"].Value);
    }
}
