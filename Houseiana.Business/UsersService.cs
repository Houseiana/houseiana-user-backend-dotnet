using Houseiana.DAL.Models;
using Houseiana.Repositories;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Houseiana.Business;

public interface IUsersService
{
    Task<User?> GetUserByIdAsync(string id);
    Task<SadadPaymentResponse> GetSadadPayment(SadadPaymentRequest request);
}

public class UsersService : IUsersService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string MerchantId = "3601032";
    private const string SecretKey = "LkOx3OfmcIOH0t7F";
    private const string Website = "houseiana.net";
    private const string CallbackUrl = "https://houseiana.net/api/sadad/callback";

    public UsersService(IUnitOfWork unitOfWork, IHttpClientFactory httpClientFactory)
    {
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _unitOfWork.Users.GetByIdAsync(id);
    }

    public async Task<SadadPaymentResponse> GetSadadPayment(SadadPaymentRequest request)
    {
        var txnDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        // Use OrderId as provided or generate one
        var orderId = string.IsNullOrEmpty(request.OrderId) ? "ORDER" + DateTime.UtcNow.Ticks : request.OrderId;
        var txnAmount = request.Amount.ToString("F2");
        var email = request.Email ?? "test@houseiana.net";
        var mobileNo = request.MobileNo ?? "97433001234";

        // Generate signature using SHA256 hash of: secretKey + merchant_id + ORDER_ID + TXN_AMOUNT
        var signatureString = SecretKey + MerchantId + orderId + txnAmount;
        var signature = GenerateSHA256Hash(signatureString);

        // Prepare form data matching Sadad's exact format
        var formData = new SadadFormData
        {
            MerchantId = MerchantId,
            OrderId = orderId,
            Website = Website,
            TxnAmount = txnAmount,
            Email = email,
            MobileNo = mobileNo,
            CallbackUrl = CallbackUrl,
            TxnDate = txnDate,
            Signature = signature,
            ProductDetails = new List<ProductDetail>
            {
                new ProductDetail
                {
                    OrderId = orderId,
                    Amount = txnAmount,
                    Quantity = "1"
                }
            }
        };

        return await Task.FromResult(new SadadPaymentResponse
        {
            Success = true,
            FormAction = "https://sadadqa.com/webpurchase",
            FormData = formData
        });
    }

    private string GenerateSHA256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}

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

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("MOBILE_NO")]
    public string MobileNo { get; set; } = string.Empty;

    [JsonPropertyName("CALLBACK_URL")]
    public string CallbackUrl { get; set; } = string.Empty;

    [JsonPropertyName("txnDate")]
    public string TxnDate { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

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
}