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

        // Prepare post data (exclude checksumhash)
        var postData = new Dictionary<string, object>
        {
            ["merchant_id"] = MerchantId,
            ["ORDER_ID"] = request.OrderId,
            ["WEBSITE"] = Website,
            ["TXN_AMOUNT"] = request.Amount.ToString("F2"),
            ["CUST_ID"] = request.Email ?? "customer@example.com",
            ["EMAIL"] = request.Email ?? "customer@example.com",
            ["MOBILE_NO"] = request.MobileNo ?? "99999999",
            ["CALLBACK_URL"] = CallbackUrl,
            ["txnDate"] = txnDate,
            ["SADAD_WEBCHECKOUT_PAGE_LANGUAGE"] = "ENG",
            ["VERSION"] = "2.1",
            ["productdetail"] = new[]
            {
            new
            {
                order_id = request.OrderId,
                itemname = request.Description ?? "Booking Payment",
                amount = request.Amount.ToString("F2"),
                quantity = "1",
                type = "line_item"
            }
        }
        };

        // Wrap in checksum data
        var checksumData = new
        {
            postData = postData,
            secretKey = SecretKey
        };

        // Generate 4-character salt
        string salt = GenerateSalt(4);

        // JSON + "|" + salt
        string concatenated = JsonSerializer.Serialize(checksumData) + "|" + salt;

        // SHA256 hash + append salt
        string hash = SHA256Hash(concatenated) + salt;

        // AES-128-CBC encrypt hash using key = SecretKey + MerchantId, IV = "@@@@&&&&####$$$$"
        string checksumHash = AESEncrypt(hash, SecretKey + MerchantId, "@@@@&&&&####$$$$");

        // Prepare form data
        var formData = new SadadFormData
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
            ChecksumHash = checksumHash,
            Version = "2.1",
            ProductDetails = new List<ProductDetail>
        {
            new ProductDetail
            {
                OrderId = request.OrderId,
                ItemName = request.Description ?? "Booking Payment",
                Amount = request.Amount.ToString("F2"),
                Quantity = "1",
                Type = "line_item"
            }
        }
        };

        return new SadadPaymentResponse
        {
            Success = true,
            FormAction = "https://sadadqa.com/webpurchase",
            FormData = formData
        };
    }

    // -------------------- Helper Methods -------------------- //

    private string GenerateSalt(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private string SHA256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private string AESEncrypt(string input, string key, string iv)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key.Substring(0, 16));
        byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        byte[] encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

        return Convert.ToBase64String(encrypted);
    }
    private class ChecksumResponse
    {
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }
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

    [JsonPropertyName("SADAD_WEBCHECKOUT_PAGE_LANGUAGE")]
    public string SadadWebcheckoutPageLanguage { get; set; } = "ENG";

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

    [JsonPropertyName("type")]
    public string Type { get; set; } = "line_item";
}
