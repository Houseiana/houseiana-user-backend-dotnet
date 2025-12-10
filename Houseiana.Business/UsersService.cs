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

    // ********************************************
    // ********************************************
    private const string SecretKey = "LkOx3OfmcIOH0t7F";   
    private const string MerchantId = "3601032";          
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
        // ********************************************
        // CHANGE 1:
        // Do NOT use UTC
        // ********************************************
        var txnDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var orderId = string.IsNullOrEmpty(request.OrderId)
                        ? "ORDER" + DateTime.Now.Ticks
                        : request.OrderId;

        var txnAmount = request.Amount.ToString("F2");
        var email = request.Email ?? "test@houseiana.net";
        var mobileNo = request.MobileNo ?? "97433001234";

        // ********************************************
        // CHANGE 2
        // ********************************************
        var paramsDict = new Dictionary<string, string>()
        {
            {"CALLBACK_URL", CallbackUrl},
            {"email", email},
            {"MOBILE_NO", mobileNo},
            {"ORDER_ID", orderId},
            {"TXN_AMOUNT", txnAmount},
            {"WEBSITE", Website},
            {"merchant_id", MerchantId},
            {"txnDate", txnDate}
        };

        // Sort EXACT like PHP ksort()
        var sortedParams = new SortedDictionary<string, string>(paramsDict, StringComparer.Ordinal);

        // ********************************************
        // CHANGE 3:
        // Signature = secret_key + concatenated values
        // ********************************************
        var sb = new StringBuilder();
        sb.Append(SecretKey);

        foreach (var item in sortedParams)
        {
            sb.Append(item.Value);
        }

        string signatureString = sb.ToString();

        // ********************************************
        // CHANGE 4:
        // SHA256 must be lowercase hex
        // ********************************************
        string signature;
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
            signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        // ********************************************
        // DEBUG PRINTS (useful when testing in console)
        // ********************************************
        Console.WriteLine("------ SIGNATURE STRING ------");
        Console.WriteLine(signatureString);

        Console.WriteLine("\n------ GENERATED SIGNATURE ------");
        Console.WriteLine(signature);

        Console.WriteLine("\n------ POST FIELDS ------");
        foreach (var kv in sortedParams)
            Console.WriteLine($"{kv.Key} = {kv.Value}");

        Console.WriteLine($"signature = {signature}");

        // ********************************************
        // PRODUCT DETAILS
        // ********************************************
        var productDetails = new List<ProductDetail>
        {
            new ProductDetail
            {
                OrderId = orderId,
                Amount = txnAmount,
                Quantity = "1"
            }
        };

        // Form payload returned to frontend
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
            ProductDetails = productDetails
        };

        return await Task.FromResult(new SadadPaymentResponse
        {
            Success = true,
            FormAction = "https://sadadqa.com/webpurchase",
            FormData = formData
        });
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

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public string Quantity { get; set; } = string.Empty;
}
