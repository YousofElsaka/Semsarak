using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEMSARK.Data;
using SEMSARK.DTOS.PaymentDTO;
using SEMSARK.Models;
using SEMSARK.Services.Payment;

namespace SEMSARK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly PaymobService _paymobService;
        private readonly int _iframeId = 941402; 

        private const double AdvertiseCommissionRate = 0.05;
        private const double BookingCommissionRate = 0.05;

        public PaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, PaymobService paymobService)
        {
            _context = context;
            _userManager = userManager;
            _paymobService = paymobService;
        }

        [HttpPost("advertise")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> CreateAdvertisePayment([FromBody] CreateAdvertisePaymentDTO dto)
        {
            var userId = _userManager.GetUserId(User);

            var property = await _context.Properties.FindAsync(dto.PropertyId);
            if (property == null || property.UserId != userId)
                return BadRequest("Invalid property or not yours.");

            if (property.IsPaid)
                return BadRequest("This property has already been paid for.");

            double commission = property.Price * AdvertiseCommissionRate;

            var payment = new Payment
            {
                Amount = property.Price,
                Commission = commission,
                PropertyId = property.Id,
                OwnerId = userId,
                PaymentType = "Advertise",
                Status = "Paid",
                IsConfirmed = true
            };

            _context.Payments.Add(payment);

            property.IsPaid = true;
            property.Status = "Available";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Payment successful and property published.",
                PaymentId = payment.Id,
                Commission = payment.Commission
            });
        }

        [HttpPost("booking")]
        [Authorize(Roles = "Renter")]
        public async Task<IActionResult> CreateBookingPayment([FromBody] CreateBookingPaymentDTO dto)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b => b.Id == dto.BookingId && b.RenterId == userId);

            if (booking == null)
                return NotFound("Booking not found or not yours.");

            if (booking.Status != "Pending")
                return BadRequest("Payment already made or booking is not in pending state.");

            var totalDays = (booking.EndDate - booking.StartDate).TotalDays;
            var amount = booking.Property.Price * totalDays;
            var commission = amount * BookingCommissionRate;

            var payment = new Payment
            {
                Amount = amount,
                Commission = commission,
                BookingId = booking.Id,
                RenterId = userId,
                OwnerId = booking.Property.UserId,
                PaymentType = "Booking",
                Status = "Paid",
                IsConfirmed = true
            };

            _context.Payments.Add(payment);

            booking.Status = "Approved";
            _context.Bookings.Update(booking);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Payment successful and booking confirmed.",
                PaymentId = payment.Id,
                TotalAmount = amount,
                Commission = commission
            });
        }

        [HttpGet("my-payments")]
        [Authorize]
        public async Task<IActionResult> GetMyPayments()
        {
            var userId = _userManager.GetUserId(User);

            var payments = await _context.Payments
                .Include(p => p.Property)
                .Where(p => p.OwnerId == userId || p.RenterId == userId)
                .OrderByDescending(p => p.DateTime)
                .Select(p => new PaymentHistoryDTO
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    Commission = p.Commission,
                    PaymentType = p.PaymentType,
                    Status = p.Status,
                    DateTime = p.DateTime,
                    PropertyId = p.PropertyId,
                    PropertyTitle = p.Property != null ? p.Property.Title : null,
                    BookingId = p.BookingId
                })
                .ToListAsync();

            return Ok(payments);
        }


        [HttpPost("paymob-initiate")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequestDTO dto)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Unauthorized();

            var authToken = await _paymobService.GetAuthTokenAsync();
            if (authToken == null)
                return StatusCode(500, "Failed to authenticate with Paymob.");

            var orderId = await _paymobService.CreateOrderAsync(authToken, dto.AmountCents);
            if (orderId == null)
                return StatusCode(500, "Failed to create order in Paymob.");

            var paymentKey = await _paymobService.GetPaymentKeyAsync(
                authToken,
                orderId.Value,
                dto.AmountCents,
                user.Email,
                user.UserName ?? "Client"
            );

            if (paymentKey == null)
                return StatusCode(500, "Failed to generate payment key.");

            return Ok(new
            {
                PaymentKey = paymentKey,
                IframeUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_iframeId}?payment_token={paymentKey}"
            });
        }


        [HttpPost("paymob-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymobCallback()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            //  هنسجل البيانات عشان نعرف شكلها
            Console.WriteLine(" Paymob Callback Received:");
            Console.WriteLine(body);

            // ✅ بعدين نعمل Parse فعلي للبيانات
            return Ok();
        }


        [HttpPost("paymob-confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmPaymobPayment([FromBody] string transactionId)
        {
            var isSuccess = await _paymobService.GetTransactionStatusAsync(transactionId);
            if (!isSuccess)
                return BadRequest("Payment failed or not completed.");

            // دور على الـ payment اللي لسه مدفوعش ولسه مش متسجل ليه TransactionId
            var payment = await _context.Payments
                .Where(p => p.TransactionId == null && p.Status != "Paid")
                .OrderByDescending(p => p.DateTime)
                .FirstOrDefaultAsync();

            if (payment == null)
                return NotFound("No pending payment found.");

            // عدل البيانات
            payment.Status = "Paid";
            payment.IsConfirmed = true;
            payment.TransactionId = transactionId;

            // لو ليه Booking
            if (payment.BookingId.HasValue)
            {
                var booking = await _context.Bookings.FindAsync(payment.BookingId.Value);
                if (booking != null)
                {
                    booking.Status = "Approved";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Payment confirmed and saved successfully." });
        }



    }
}
