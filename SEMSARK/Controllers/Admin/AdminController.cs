using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using SEMSARK.Data;
using SEMSARK.DTOS.Admin;
using SEMSARK.Models;

namespace SEMSARK.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        #region User Section
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            // الحل هنا عشان احل مشكلة انو لو عندي 1000 يوزر وعايز اجيبهم كلهم مع ال رولز بتاعتهم
            // دي مشكله لأنها هتعمل لودينج كبير على السيرفر
             // دي مشكلهn+1  بطريقه دي             

            var usersWithRoles = await _context.Users
                .Select(user => new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.PhoneNumber,
                    user.NationalId,
                    user.IsVerified,
                    CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Roles = (from ur in _context.UserRoles
                             join r in _context.Roles on ur.RoleId equals r.Id
                             where ur.UserId == user.Id
                             select r.Name).ToList()
                })
                .ToListAsync();

            return Ok(usersWithRoles);
        }




        [HttpGet("user/{id}")]

        public async Task<IActionResult> GetUserById(string id)
        {

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);


            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.NationalId,
                user.IsVerified,
                user.CreatedAt,
                user.VerifiedAt,
                Roles = roles
            });



        }



        [HttpDelete("user/{id}")]

        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)

            {

                return NotFound("User not found");
            }

            await _userManager.DeleteAsync(user);

            return Ok("User deleted successfully");
        }


        [HttpPut("user/{id}/verify")]
        public async Task<IActionResult> VerifyUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            user.IsVerified = true;
            user.VerifiedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest("Failed to verify user");
            }

            return Ok("User verified successfully");

        }



        [HttpPut("users/{id}/change-role")]
        public async Task<IActionResult> ChangeUserRole(string id, [FromQuery] string newRole)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("User not found");

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            return Ok($"User role changed to {newRole}");
        }

        #endregion


        #region Property Section

        [HttpGet("properties")]

        public async Task<IActionResult> GetAllProperties()
        {

            var properties = await _context.Properties.Include(p => p.User)
               .Select(p => new
               {

                   p.Id,
                   p.Title,
                   p.Description,
                   p.Price,
                   p.City,
                   p.Region,
                   p.Status,
                   Owner = new
                   {
                       p.User.Id,
                       p.User.UserName,
                       p.User.Email
                   }

               }).ToListAsync();
            return Ok(properties);

        }

        //yousef eldbah
        [HttpDelete("property/{id}")]

        public async Task<IActionResult> DeleteProperty(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property == null)
            {
                return NotFound("Property not found");
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
            return Ok("Property deleted successfully");







           


         





        }

        [HttpGet("property-details/{id}")]
        public async Task<IActionResult> GetPropertyDetails(int id)
        {
            var property = await _context.Properties
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
                return NotFound("Property not found");

            return Ok(new
            {
                property.Id,
                property.Title,
                property.Description,
                property.Price,
                property.City,
                property.Region,
                property.Status,
                property.RoomsCount,
                property.GenderPreference,
                property.Street,
                Owner = new
                {
                    property.User.Id,
                    property.User.UserName,
                    property.User.Email
                }
                // أضف أي بيانات أخرى تريدها هنا  Yousef eldbah
            });
        }

        [HttpPut("properties/{id}/change-status")]
        public async Task <IActionResult> ChangePropertyStatus(int id, [FromQuery] string newStatus)

        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
                return NotFound("Property not found");

            property.Status = newStatus;
            await _context.SaveChangesAsync();

            return Ok($"Property status updated to: {newStatus}");
        }


        #endregion


        [HttpGet("dashboard")]

        public async Task<ActionResult<AdminDashboardDto>> GetDashboardStats()
        {


            var totalProfitFromBookings = await _context.Payments
                 .Where(p => p.PaymentType == "Booking" && p.IsConfirmed)
                 .SumAsync(p => (decimal?)p.Commission) ?? 0;

            var totalProfitFromAds =  await _context.Payments .Where(p => p.PaymentType == "Advertise" && p.IsConfirmed)
                .SumAsync(p => (decimal?)p.Commission) ?? 0;


            var renters = await _userManager.GetUsersInRoleAsync("Renter");
            var totalRenters = renters.Count;

            var owners = await _userManager.GetUsersInRoleAsync("Owner");
            var totalOwners = owners.Count;



            var result = new AdminDashboardDto
            {
                TotalProfitFromBookings = totalProfitFromBookings,
                TotalProfitFromAdvertisements = totalProfitFromAds,
                TotalRenters = totalRenters,
                TotalOwners = totalOwners
            };

            return Ok(result);





        }



    }

}
