using System.Text.Json.Serialization;

namespace Houseiana.DTOs
{
    public class SadadPaymentRequestDto
    {
        public string? OrderId { get; set; }
        public string? BookingId { get; set; }
        public decimal Amount { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public string? ItemName { get; set; }
        public string? Language { get; set; } = "ENG";
    }

    public class SadadPaymentFormDto
    {
        public string ActionUrl { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string TxnAmount { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        // Version removed - not in original Sadad sample
        public string CallbackUrl { get; set; } = string.Empty;
        public string TxnDate { get; set; } = string.Empty;
        public SadadProductDetailDto ProductDetail { get; set; } = new();
        public string ChecksumHash { get; set; } = string.Empty;
    }

    public class SadadProductDetailDto
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("itemname")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public string Quantity { get; set; } = "1";
    }

    public class SadadCallbackDto
    {
        public string? ORDERID { get; set; }
        public string? RESPCODE { get; set; }
        public string? RESPMSG { get; set; }
        public string? TXNAMOUNT { get; set; }

        [JsonPropertyName("transaction_number")]
        public string? TransactionNumber { get; set; }

        [JsonPropertyName("checksumhash")]
        public string? ChecksumHash { get; set; }
    }

    public class SadadPaymentResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? OrderId { get; set; }
        public string? TransactionNumber { get; set; }
        public string? ResponseCode { get; set; }
        public decimal? Amount { get; set; }
    }
}
