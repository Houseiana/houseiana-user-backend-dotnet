using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Houseiana.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Houseiana.Business
{
    public class SadadPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SadadPaymentService> _logger;
        private readonly HttpClient _httpClient;
        private const string IV = "@@@@&&&&####$$$$";

        // JSON options to match PHP's json_encode output exactly
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNamingPolicy = null // Keep exact property names
        };

        public SadadPaymentService(IConfiguration configuration, ILogger<SadadPaymentService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public string MerchantId => _configuration["Sadad:MerchantId"] ?? throw new InvalidOperationException("Sadad MerchantId not configured");
        public string SecretKey => _configuration["Sadad:SecretKey"] ?? throw new InvalidOperationException("Sadad SecretKey not configured");
        public string WebsiteUrl => _configuration["Sadad:WebsiteUrl"] ?? "www.houseiana.com";
        public string CallbackUrl => _configuration["Sadad:CallbackUrl"] ?? throw new InvalidOperationException("Sadad CallbackUrl not configured");
        public bool IsTestMode => _configuration.GetValue<bool>("Sadad:TestMode", true);
        public string SadadUrl => IsTestMode ? "https://secure.sadadqa.com/webpurchasepage" : "https://secure.sadadqa.com/webpurchasepage";
        public string? PhpChecksumUrl => _configuration["Sadad:PhpChecksumUrl"] ?? Environment.GetEnvironmentVariable("SADAD_PHP_CHECKSUM_URL");

        public async Task<SadadPaymentFormDto> GeneratePaymentFormAsync(SadadPaymentRequestDto request)
        {
            var txnDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var orderId = request.OrderId ?? Guid.NewGuid().ToString();
            var txnAmount = request.Amount.ToString("F2");
            var itemName = request.ItemName ?? "Booking Payment";

            string checksum;

            // If PHP checksum URL is configured, use it; otherwise fall back to local generation
            if (!string.IsNullOrEmpty(PhpChecksumUrl))
            {
                // Call PHP endpoint to generate checksum using original Sadad PHP code
                var phpRequest = new
                {
                    secretKey = SecretKey,
                    merchantId = MerchantId,
                    orderId = orderId,
                    website = WebsiteUrl,
                    txnAmount = txnAmount,
                    custId = request.CustomerEmail,
                    email = request.CustomerEmail,
                    mobileNo = request.CustomerMobile,
                    callbackUrl = CallbackUrl,
                    txnDate = txnDate,
                    itemName = itemName
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(phpRequest, JsonOptions),
                    Encoding.UTF8,
                    "application/json"
                );

                _logger.LogInformation("Calling PHP checksum service at: {Url}", PhpChecksumUrl);

                var response = await _httpClient.PostAsync(PhpChecksumUrl, jsonContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("PHP checksum service response: {Response}", responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("PHP checksum service failed, falling back to local generation: {Response}", responseBody);
                    checksum = GenerateChecksumLocal(orderId, txnAmount, request, txnDate, itemName);
                }
                else
                {
                    var phpResponse = JsonSerializer.Deserialize<PhpChecksumResponse>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (phpResponse == null || !phpResponse.Success || string.IsNullOrEmpty(phpResponse.Checksumhash))
                    {
                        _logger.LogWarning("PHP checksum service returned invalid response, falling back to local generation");
                        checksum = GenerateChecksumLocal(orderId, txnAmount, request, txnDate, itemName);
                    }
                    else
                    {
                        checksum = phpResponse.Checksumhash;
                        _logger.LogInformation("Generated checksum from PHP: {Checksum}", checksum);
                    }
                }
            }
            else
            {
                _logger.LogInformation("PHP checksum URL not configured, using local generation");
                checksum = GenerateChecksumLocal(orderId, txnAmount, request, txnDate, itemName);
            }

            return new SadadPaymentFormDto
            {
                ActionUrl = SadadUrl,
                MerchantId = MerchantId,
                OrderId = orderId,
                Website = WebsiteUrl,
                TxnAmount = txnAmount,
                CustomerId = request.CustomerEmail,
                Email = request.CustomerEmail,
                MobileNo = request.CustomerMobile,
                CallbackUrl = CallbackUrl,
                TxnDate = txnDate,
                ProductDetail = new SadadProductDetailDto
                {
                    OrderId = orderId,
                    ItemName = itemName,
                    Amount = txnAmount,
                    Quantity = "1"
                },
                // VERSION removed - not in original Sadad sample
                ChecksumHash = checksum
            };
        }

        private string GenerateChecksumLocal(string orderId, string txnAmount, SadadPaymentRequestDto request, string txnDate, string itemName)
        {
            // Build checksum data matching original sadad.php - NO VERSION
            var checksumData = new
            {
                merchant_id = MerchantId,
                ORDER_ID = orderId,
                WEBSITE = WebsiteUrl,
                TXN_AMOUNT = txnAmount,
                CUST_ID = request.CustomerEmail,
                EMAIL = request.CustomerEmail,
                MOBILE_NO = request.CustomerMobile,
                CALLBACK_URL = CallbackUrl,
                txnDate = txnDate,
                productdetail = new[]
                {
                    new
                    {
                        order_id = orderId,
                        quantity = "1",
                        amount = txnAmount,
                        itemname = itemName
                    }
                }
            };

            var checksumPayload = new
            {
                postData = checksumData,
                secretKey = SecretKey
            };

            var jsonPayload = JsonSerializer.Serialize(checksumPayload, JsonOptions);
            _logger.LogInformation("Local checksum JSON payload: {Payload}", jsonPayload);

            var checksum = GenerateChecksum(jsonPayload);
            _logger.LogInformation("Generated local checksum: {Checksum}", checksum);
            return checksum;
        }

        // Keep synchronous version as fallback using local checksum generation
        public SadadPaymentFormDto GeneratePaymentForm(SadadPaymentRequestDto request)
        {
            var txnDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var orderId = request.OrderId ?? Guid.NewGuid().ToString();
            var txnAmount = request.Amount.ToString("F2");
            var itemName = request.ItemName ?? "Booking Payment";

            // Build checksum data matching original sadad.php - NO VERSION
            var checksumData = new
            {
                merchant_id = MerchantId,
                ORDER_ID = orderId,
                WEBSITE = WebsiteUrl,
                TXN_AMOUNT = txnAmount,
                CUST_ID = request.CustomerEmail,
                EMAIL = request.CustomerEmail,
                MOBILE_NO = request.CustomerMobile,
                CALLBACK_URL = CallbackUrl,
                txnDate = txnDate,
                productdetail = new[]
                {
                    new
                    {
                        order_id = orderId,
                        quantity = "1",
                        amount = txnAmount,
                        itemname = itemName
                    }
                }
            };

            var checksumPayload = new
            {
                postData = checksumData,
                secretKey = SecretKey
            };

            var jsonPayload = JsonSerializer.Serialize(checksumPayload, JsonOptions);
            _logger.LogInformation("Checksum JSON payload: {Payload}", jsonPayload);

            var checksum = GenerateChecksum(jsonPayload);
            _logger.LogInformation("Generated checksum: {Checksum}", checksum);

            return new SadadPaymentFormDto
            {
                ActionUrl = SadadUrl,
                MerchantId = MerchantId,
                OrderId = orderId,
                Website = WebsiteUrl,
                TxnAmount = txnAmount,
                CustomerId = request.CustomerEmail,
                Email = request.CustomerEmail,
                MobileNo = request.CustomerMobile,
                CallbackUrl = CallbackUrl,
                TxnDate = txnDate,
                ProductDetail = new SadadProductDetailDto
                {
                    OrderId = orderId,
                    ItemName = itemName,
                    Amount = txnAmount,
                    Quantity = "1"
                },
                // VERSION removed - not in original Sadad sample
                ChecksumHash = checksum
            };
        }

        private class PhpChecksumResponse
        {
            public bool Success { get; set; }
            public string? Checksumhash { get; set; }
            public PhpChecksumDebug? Debug { get; set; }
        }

        private class PhpChecksumDebug
        {
            public string? JsonPayload { get; set; }
            public string? EncryptionKey { get; set; }
        }

        public string GenerateChecksum(string data)
        {
            var salt = GenerateSalt(4);
            var finalString = $"{data}|{salt}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(finalString));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            var hashString = hash + salt;

            return Encrypt(hashString);
        }

        public bool VerifyChecksum(Dictionary<string, string> postData, string checksumHash)
        {
            try
            {
                var decrypted = Decrypt(checksumHash);
                var salt = decrypted.Substring(decrypted.Length - 4);

                var dataToVerify = new Dictionary<string, object>
                {
                    { "postData", postData },
                    { "secretKey", SecretKey }
                };

                var jsonData = JsonSerializer.Serialize(dataToVerify);
                var finalString = $"{jsonData}|{salt}";

                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(finalString));
                var websiteHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower() + salt;

                return websiteHash == decrypted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Sadad checksum");
                return false;
            }
        }

        private string GenerateSalt(int length)
        {
            const string chars = "AbcDE123IJKLMN67QRSTUVWXYZaBCdefghijklmn123opq45rs67tuv89wxyz0FGH45OP89";
            var random = new Random();
            var salt = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                salt.Append(chars[random.Next(chars.Length)]);
            }

            return salt.ToString();
        }

        private string Encrypt(string input)
        {
            var key = System.Net.WebUtility.HtmlDecode(SecretKey + MerchantId);
            var keyBytes = BuildKeyBytes(key);
            var ivBytes = Encoding.UTF8.GetBytes(IV);

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using var encryptor = aes.CreateEncryptor();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        private string Decrypt(string crypt)
        {
            var key = System.Net.WebUtility.HtmlDecode(SecretKey + MerchantId);
            var keyBytes = BuildKeyBytes(key);
            var ivBytes = Encoding.UTF8.GetBytes(IV);

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(crypt);
            var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private byte[] BuildKeyBytes(string key)
        {
            // Match PHP openssl_encrypt behaviour: use first 16 bytes, zero-padded if shorter
            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length >= 16)
            {
                return keyBytes.Take(16).ToArray();
            }

            var padded = new byte[16];
            Array.Copy(keyBytes, padded, keyBytes.Length);
            return padded;
        }
    }
}
