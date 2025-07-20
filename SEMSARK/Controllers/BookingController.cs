using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEMSARK.Data;
using SEMSARK.DTOS.BookingDTOs;
using SEMSARK.Models;

namespace SEMSARK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        [HttpPost]
        [Authorize(Roles = "Renter")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingCreateDTO dto)
        {
            var userId = _userManager.GetUserId(User);

            // ✅ تأكد إن الشقة مش محجوزة في نفس المدى الزمني
            var hasConflict = await _context.Bookings
                .AnyAsync(b => b.PropertyId == dto.PropertyId &&
                               b.Status == "Approved" &&
                               b.EndDate >= dto.StartDate &&
                               b.StartDate <= dto.EndDate);

            if (hasConflict)
            {
                return BadRequest("This property is already booked during the selected period.");
            }

            // ✅ لو مفيش تعارض → نكمل الحجز
            var booking = new Booking
            {
                PropertyId = dto.PropertyId,
                RenterId = userId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Ok(new { booking.Id });
        }

        [HttpGet("my-bookings")]
        [Authorize(Roles = "Renter")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Where(b => b.RenterId == userId)
                .Include(b => b.Property)
                    .ThenInclude(p => p.PropertyImages)
                .Select(b => new BookingReadDTO
                {
                    Id = b.Id,
                    PropertyId = b.PropertyId,
                    PropertyTitle = b.Property.Title,
                    Price = b.Property.Price,
                    City = b.Property.City,
                    Region = b.Property.Region,
                    RoomsCount = b.Property.RoomsCount,
                    ImageUrls = b.Property.PropertyImages.Select(img => img.ImagePath).ToList(),

                    RenterId = b.RenterId,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();


            return Ok(bookings);
        }


        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Renter")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
                return NotFound("Booking not found");

            if (booking.RenterId != userId)
                return Forbid("You are not allowed to cancel this booking");

            if (booking.Status == "Cancelled" || booking.Status == "Rejected")
                return BadRequest("Booking already cancelled or rejected");

            if (booking.EndDate < DateTime.UtcNow)
                return BadRequest("Cannot cancel a completed booking");

            booking.Status = "Cancelled";
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            return Ok("Booking cancelled successfully");
        }


    }
}
