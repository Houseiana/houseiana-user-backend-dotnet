using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HouseianaApi.DTOs;

namespace HouseianaApi.Services
{
    public class SadadPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SadadPaymentService> _logger;
        private const string IV = "@@@@&&&&####$$$$";

        // JSON options to match PHP's json_encode output exactly
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNamingPolicy = null // Keep exact property names
        };

        public SadadPaymentService(IConfiguration configuration, ILogger<SadadPaymentService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string MerchantId => _configuration["Sadad:MerchantId"] ?? throw new InvalidOperationException("Sadad MerchantId not configured");
        public string SecretKey => _configuration["Sadad:SecretKey"] ?? throw new InvalidOperationException("Sadad SecretKey not configured");
        public string WebsiteUrl => _configuration["Sadad:WebsiteUrl"] ?? "www.houseiana.com";
        public string CallbackUrl => _configuration["Sadad:CallbackUrl"] ?? throw new InvalidOperationException("Sadad CallbackUrl not configured");
        public bool IsTestMode => _configuration.GetValue<bool>("Sadad:TestMode", true);
        public string SadadUrl => IsTestMode ? "https://secure.sadadqa.com/webpurchasepage" : "https://secure.sadadqa.com/webpurchasepage";

        public SadadPaymentFormDto GeneratePaymentForm(SadadPaymentRequestDto request)
        {
            var txnDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var orderId = request.OrderId ?? Guid.NewGuid().ToString();
            var txnAmount = request.Amount.ToString("F2");
            var itemName = request.ItemName ?? "Booking Payment";

            // Build checksum data in EXACT order matching PHP reference
            // PHP order: merchant_id, ORDER_ID, WEBSITE, TXN_AMOUNT, CUST_ID, EMAIL, MOBILE_NO, CALLBACK_URL, txnDate, productdetail
            // NOTE: SADAD_WEBCHECKOUT_PAGE_LANGUAGE is NOT included in checksum (only in form)
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
                // productdetail field order: order_id, quantity, amount, itemname (matches PHP exactly, no 'type')
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
                Language = request.Language ?? "ENG",
                CallbackUrl = CallbackUrl,
                TxnDate = txnDate,
                Version = "1.1",
                ProductDetail = new SadadProductDetailDto
                {
                    OrderId = orderId,
                    ItemName = itemName,
                    Amount = txnAmount,
                    Quantity = "1",
                    Type = "line_item"
                },
                ChecksumHash = checksum
            };
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
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16));
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
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16));
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
    }
}
