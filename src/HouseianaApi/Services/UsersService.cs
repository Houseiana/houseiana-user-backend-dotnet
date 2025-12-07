using HouseianaApi.Data;
using HouseianaApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HouseianaApi.Services;

public interface IUsersService
{
    Task<User?> GetUserByIdAsync(string id);
    Task<SadadPaymentResponse> GetSadadPayment(SadadPaymentRequest request);
}

public class UsersService : IUsersService
{
    private readonly HouseianaDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    // Sadad configuration - should be moved to appsettings.json
    private const string MerchantId = "3601032";
    private const string SecretKey = "QyJP1Rg6yQJtndeg";
    private const string Website = "houseiana.net";
    private const string CallbackUrl = "https://houseiana.net/api/sadad/callback";

    public UsersService(HouseianaDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<SadadPaymentResponse> GetSadadPayment(SadadPaymentRequest request)
    {
        var client = _httpClientFactory.CreateClient();

        var txnDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        // Build the checksum request
        var checksumRequest = new
        {
            merchant_id = MerchantId,
            WEBSITE = Website,
            TXN_AMOUNT = request.Amount.ToString("F2"),
            ORDER_ID = request.OrderId,
            CALLBACK_URL = CallbackUrl,
            MOBILE_NO = request.MobileNo ?? "99999999",
            EMAIL = request.Email ?? "customer@example.com",
            CUST_ID = request.Email ?? "customer@example.com",
            productdetail = new[]
            {
                new
                {
                    order_id = request.OrderId,
                    quantity = "1",
                    amount = request.Amount.ToString("F2"),
                    itemname = request.Description ?? "Booking Payment"
                }
            },
            txnDate = txnDate,
            VERSION = "2.1"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(checksumRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Add required headers
        client.DefaultRequestHeaders.Add("secretkey", SecretKey);
        client.DefaultRequestHeaders.Add("Origin", $"https://{Website}");

        // Debug: Log request
        var requestJson = JsonSerializer.Serialize(checksumRequest);
        Console.WriteLine($"Sadad Request: {requestJson}");
        Console.WriteLine($"Headers: secretkey={SecretKey}, Origin=http://{Website}");

        var response = await client.PostAsync(
            "https://api.sadadqatar.com/api-v4/userbusinesses/generateChecksum",
            content
        );

        var responseData = await response.Content.ReadAsStringAsync();

        // Debug: Log request and response details
        var debugInfo = $"Status: {(int)response.StatusCode}, " +
                        $"Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}, " +
                        $"Body: '{responseData}'";

        if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(responseData))
        {
            return new SadadPaymentResponse
            {
                Success = false,
                Error = $"Checksum API error: {debugInfo}"
            };
        }

        var checksumResponse = JsonSerializer.Deserialize<ChecksumResponse>(responseData);

        if (checksumResponse?.Checksum == null)
        {
            return new SadadPaymentResponse
            {
                Success = false,
                Error = $"Failed to get checksum: {responseData}"
            };
        }

        // Return form data for frontend to submit
        return new SadadPaymentResponse
        {
            Success = true,
            FormAction = "https://sadadqa.com/webpurchase",
            FormData = new SadadFormData
            {
                MerchantId = MerchantId,
                OrderId = request.OrderId,
                Website = Website,
                TxnAmount = request.Amount.ToString("F2"),
                CustId = request.Email ?? "customer@example.com",
                Email = request.Email ?? "customer@example.com",
                MobileNo = request.MobileNo ?? "99999999",
                CallbackUrl = CallbackUrl,
                TxnDate = txnDate,
                ChecksumHash = checksumResponse.Checksum,
                Version = "2.1",
                ProductDetails = new List<ProductDetail>
                {
                    new ProductDetail
                    {
                        OrderId = request.OrderId,
                        Quantity = "1",
                        Amount = request.Amount.ToString("F2"),
                        ItemName = request.Description ?? "Booking Payment"
                    }
                }
            }
        };
    }

    private class ChecksumResponse
    {
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }
    }
}

// Request/Response DTOs
public class SadadPaymentRequest
{
    public decimal Amount { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? MobileNo { get; set; }
    public string? Description { get; set; }
}

public class SadadPaymentResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? FormAction { get; set; }
    public SadadFormData? FormData { get; set; }
}

public class SadadFormData
{
    [JsonPropertyName("merchant_id")]
    public string MerchantId { get; set; } = string.Empty;

    [JsonPropertyName("ORDER_ID")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("WEBSITE")]
    public string Website { get; set; } = string.Empty;

    [JsonPropertyName("TXN_AMOUNT")]
    public string TxnAmount { get; set; } = string.Empty;

    [JsonPropertyName("CUST_ID")]
    public string CustId { get; set; } = string.Empty;

    [JsonPropertyName("EMAIL")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("MOBILE_NO")]
    public string MobileNo { get; set; } = string.Empty;

    [JsonPropertyName("CALLBACK_URL")]
    public string CallbackUrl { get; set; } = string.Empty;

    [JsonPropertyName("txnDate")]
    public string TxnDate { get; set; } = string.Empty;

    [JsonPropertyName("checksumhash")]
    public string ChecksumHash { get; set; } = string.Empty;

    [JsonPropertyName("VERSION")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("productdetail")]
    public List<ProductDetail> ProductDetails { get; set; } = new();
}

public class ProductDetail
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public string Quantity { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("itemname")]
    public string ItemName { get; set; } = string.Empty;
}