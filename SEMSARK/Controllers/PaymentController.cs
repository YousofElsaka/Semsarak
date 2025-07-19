using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEMSARK.Data;
using SEMSARK.DTOS.PaymentDTO;
using SEMSARK.Models;

namespace SEMSARK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const double AdvertiseCommissionRate = 0.05;
        private const double BookingCommissionRate = 0.05;

        public PaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
    }
}
