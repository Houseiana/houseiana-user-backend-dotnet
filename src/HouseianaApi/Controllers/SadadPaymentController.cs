using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HouseianaApi.Data;
using HouseianaApi.DTOs;
using HouseianaApi.Enums;
using HouseianaApi.Services;

namespace HouseianaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SadadPaymentController : ControllerBase
    {
        private readonly SadadPaymentService _sadadService;
        private readonly HouseianaDbContext _context;
        private readonly ILogger<SadadPaymentController> _logger;

        public SadadPaymentController(
            SadadPaymentService sadadService,
            HouseianaDbContext context,
            ILogger<SadadPaymentController> logger)
        {
            _sadadService = sadadService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Generate payment form data for Sadad Web Checkout
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] SadadPaymentRequestDto request)
        {
            try
            {
                // If booking ID is provided, get booking details
                if (!string.IsNullOrEmpty(request.BookingId))
                {
                    var booking = await _context.Bookings
                        .Include(b => b.Property)
                        .Include(b => b.Guest)
                        .FirstOrDefaultAsync(b => b.Id == request.BookingId);

                    if (booking == null)
                    {
                        return NotFound(new { success = false, message = "Booking not found" });
                    }

                    // Set amount and details from booking
                    request.Amount = (decimal)booking.TotalPrice;
                    request.OrderId = booking.Id;
                    request.CustomerEmail = booking.Guest?.Email ?? request.CustomerEmail;
                    request.CustomerMobile = booking.Guest?.Phone ?? request.CustomerMobile;
                    request.ItemName = $"Booking - {booking.Property?.Title ?? "Property"}";

                    // Update booking status to awaiting payment
                    booking.Status = BookingStatus.AWAITING_PAYMENT;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                var formData = _sadadService.GeneratePaymentForm(request);

                return Ok(new
                {
                    success = true,
                    data = formData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Sadad payment");
                return StatusCode(500, new { success = false, message = "Failed to initiate payment" });
            }
        }

        /// <summary>
        /// Generate payment form HTML for direct embedding
        /// </summary>
        [HttpPost("form")]
        public async Task<IActionResult> GetPaymentFormHtml([FromBody] SadadPaymentRequestDto request)
        {
            try
            {
                var formData = _sadadService.GeneratePaymentForm(request);

                var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Redirecting to Sadad...</title>
</head>
<body>
    <form action=""{formData.ActionUrl}"" method=""post"" id=""sadad_payment_form"" name=""gosadad"">
        <input type=""hidden"" name=""merchant_id"" value=""{formData.MerchantId}"">
        <input type=""hidden"" name=""ORDER_ID"" value=""{formData.OrderId}"">
        <input type=""hidden"" name=""WEBSITE"" value=""{formData.Website}"">
        <input type=""hidden"" name=""TXN_AMOUNT"" value=""{formData.TxnAmount}"">
        <input type=""hidden"" name=""CUST_ID"" value=""{formData.CustomerId}"">
        <input type=""hidden"" name=""EMAIL"" value=""{formData.Email}"">
        <input type=""hidden"" name=""MOBILE_NO"" value=""{formData.MobileNo}"">
        <input type=""hidden"" name=""SADAD_WEBCHECKOUT_PAGE_LANGUAGE"" value=""{formData.Language}"">
        <input type=""hidden"" name=""VERSION"" value=""{formData.Version}"">
        <input type=""hidden"" name=""CALLBACK_URL"" value=""{formData.CallbackUrl}"">
        <input type=""hidden"" name=""txnDate"" value=""{formData.TxnDate}"">
        <input type=""hidden"" name=""productdetail[0][order_id]"" value=""{formData.ProductDetail.OrderId}"">
        <input type=""hidden"" name=""productdetail[0][itemname]"" value=""{formData.ProductDetail.ItemName}"">
        <input type=""hidden"" name=""productdetail[0][amount]"" value=""{formData.ProductDetail.Amount}"">
        <input type=""hidden"" name=""productdetail[0][quantity]"" value=""{formData.ProductDetail.Quantity}"">
        <input type=""hidden"" name=""productdetail[0][type]"" value=""{formData.ProductDetail.Type}"">
        <input type=""hidden"" name=""checksumhash"" value=""{formData.ChecksumHash}"">
        <p style=""text-align: center; padding-top: 100px; font-family: Arial, sans-serif;"">
            Redirecting to Sadad payment gateway...
        </p>
    </form>
    <script type=""text/javascript"">
        document.gosadad.submit();
    </script>
</body>
</html>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Sadad payment form");
                return StatusCode(500, new { success = false, message = "Failed to generate payment form" });
            }
        }

        /// <summary>
        /// Callback endpoint for Sadad to post payment results
        /// </summary>
        [HttpPost("callback")]
        public async Task<IActionResult> PaymentCallback([FromForm] IFormCollection formData)
        {
            try
            {
                var orderId = formData["ORDERID"].ToString();
                var respCode = formData["RESPCODE"].ToString();
                var respMsg = formData["RESPMSG"].ToString();
                var txnAmount = formData["TXNAMOUNT"].ToString();
                var transactionNumber = formData["transaction_number"].ToString();
                var checksumHash = formData["checksumhash"].ToString();

                _logger.LogInformation("Sadad callback received: OrderId={OrderId}, RespCode={RespCode}, TxnAmount={TxnAmount}",
                    orderId, respCode, txnAmount);

                // Verify checksum
                var postDataDict = formData
                    .Where(x => x.Key != "checksumhash")
                    .ToDictionary(x => x.Key, x => x.Value.ToString());

                var isValid = _sadadService.VerifyChecksum(postDataDict, checksumHash);

                if (!isValid)
                {
                    _logger.LogWarning("Invalid checksum for order {OrderId}", orderId);
                }

                // Find and update the booking
                var booking = await _context.Bookings.FindAsync(orderId);
                if (booking != null)
                {
                    if (respCode == "1") // Success
                    {
                        booking.Status = BookingStatus.CONFIRMED;
                        booking.PaymentStatus = PaymentStatus.PAID;
                        booking.ConfirmedAt = DateTime.UtcNow;
                        booking.TransactionId = transactionNumber;

                        _logger.LogInformation("Payment successful for booking {BookingId}", orderId);
                    }
                    else if (respCode == "400" || respCode == "402") // Pending
                    {
                        booking.PaymentStatus = PaymentStatus.PENDING;
                        _logger.LogInformation("Payment pending for booking {BookingId}", orderId);
                    }
                    else // Failed
                    {
                        booking.Status = BookingStatus.PENDING;
                        booking.PaymentStatus = PaymentStatus.FAILED;
                        _logger.LogInformation("Payment failed for booking {BookingId}: {Message}", orderId, respMsg);
                    }

                    booking.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // Redirect to frontend with result
                var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "https://houseiana.com";
                var redirectUrl = $"{frontendUrl}/payment/result?orderId={orderId}&status={respCode}&message={Uri.EscapeDataString(respMsg ?? "")}";

                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Sadad callback");
                var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "https://houseiana.com";
                return Redirect($"{frontendUrl}/payment/result?status=error&message=Payment+processing+error");
            }
        }

        /// <summary>
        /// Verify a payment status
        /// </summary>
        [HttpGet("verify/{orderId}")]
        public async Task<IActionResult> VerifyPayment(string orderId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b => b.Id == orderId);

            if (booking == null)
            {
                return NotFound(new { success = false, message = "Order not found" });
            }

            return Ok(new SadadPaymentResponseDto
            {
                Success = booking.PaymentStatus == PaymentStatus.PAID,
                OrderId = booking.Id,
                TransactionNumber = booking.TransactionId,
                ResponseCode = booking.PaymentStatus == PaymentStatus.PAID ? "1" : "0",
                Amount = (decimal?)booking.TotalPrice,
                Message = booking.PaymentStatus == PaymentStatus.PAID ? "Payment successful" : "Payment pending or failed"
            });
        }
    }
}
