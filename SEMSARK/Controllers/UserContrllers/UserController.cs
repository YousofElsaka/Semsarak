using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SEMSARK.DTOS.UserDTO;
using SEMSARK.Models;

namespace SEMSARK.Controllers.UserContrllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly UserManager<ApplicationUser> userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        [HttpGet]
       
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }


            var dto = new UserProfileDto
            {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            NationalId = user.NationalId,
            IsVerified = user.IsVerified,
            CreatedAt = user.CreatedAt

            };

            return Ok(dto);
        }


        [HttpPut("profile")]

        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            user.UserName = dto.UserName;
            user.PhoneNumber = dto.PhoneNumber;


            await userManager.UpdateAsync(user);

           return Ok("Profile updated successfully.");
        }



        [HttpPut("change-password")]

        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {

            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                return BadRequest("Passwords do not match.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        

            return Ok("Password changed successfully.");
        }

    }
}
