using Houseiana.Business;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
namespace HouseianaApi.Controllers
{
    public class PayPalController : Controller
    {
        private readonly IUsersService _usersService;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _clientFactory;

        public PayPalController(IUsersService usersService , IConfiguration config, IHttpClientFactory clientFactory)
        {
            _usersService = usersService;
            _config = config;
            _clientFactory = clientFactory;
        }
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder()
        {
            var clientId = "AQ2x4KEJF9z4j5Sfca9JPUxUJiwLYcouF1VzhtEDY2ID8icfVpRmfGIK13wGOuf2q2rbv7XfBNAJhuSP";
            var secret = "EAVgV56YpydqrwTCQ-7dmFbsZh4siR59At2gjgtD5ZtCs3pfRGykJnQjrtt8tp4zuO7F2SIsxjb7rZNS";
            var baseUrl = "https://api-m.sandbox.paypal.com";

            // Get access token
            var client = _clientFactory.CreateClient();
            var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/oauth2/token");
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" }
        });

            var tokenResponse = await client.SendAsync(tokenRequest);
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var accessToken = JsonDocument.Parse(tokenJson).RootElement.GetProperty("access_token").GetString();

            // Create order
            var orderRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders");
            orderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            orderRequest.Content = new StringContent(JsonSerializer.Serialize(new
            {
                intent = "CAPTURE",
                purchase_units = new[] {
                new { amount = new { currency_code = "USD", value = "50.00" } }
            }
            }), System.Text.Encoding.UTF8, "application/json");

            var orderResponse = await client.SendAsync(orderRequest);
            var orderJson = await orderResponse.Content.ReadAsStringAsync();

            return Content(orderJson, "application/json");
        }

        [HttpPost("capture-order/{orderId}")]
        public async Task<IActionResult> CaptureOrder(string orderId)
        {
            var clientId = "AQ2x4KEJF9z4j5Sfca9JPUxUJiwLYcouF1VzhtEDY2ID8icfVpRmfGIK13wGOuf2q2rbv7XfBNAJhuSP";
            var secret = "EAVgV56YpydqrwTCQ-7dmFbsZh4siR59At2gjgtD5ZtCs3pfRGykJnQjrtt8tp4zuO7F2SIsxjb7rZNS";
            var baseUrl = "https://api-m.sandbox.paypal.com";


            var client = _clientFactory.CreateClient();
            var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/oauth2/token");
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" }
        });

            var tokenResponse = await client.SendAsync(tokenRequest);
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var accessToken = JsonDocument.Parse(tokenJson).RootElement.GetProperty("access_token").GetString();

            // Capture order
            var captureRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders/{orderId}/capture");
            captureRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var captureResponse = await client.SendAsync(captureRequest);
            var captureJson = await captureResponse.Content.ReadAsStringAsync();

            return Content(captureJson, "application/json");
        }
    }
}
