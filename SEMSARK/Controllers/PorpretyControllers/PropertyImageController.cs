using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEMSARK.Data;
using SEMSARK.Models;
using SEMSARK.DTOS.PropertyImageDTOS;
using System.IO;

namespace SEMSARK.Controllers.PropertyControllers
{
    [Route("api/property")]
    [ApiController]
    public class PropertyImageController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;

        public PropertyImageController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.env = env;
        }

        // ✅ رفع صور
        [HttpPost("{propertyId}/images")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UploadImages(int propertyId, [FromForm] CreatePropertyImageDto dto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var property = await context.Properties.FirstOrDefaultAsync(p => p.Id == propertyId);
            if (property == null)
                return NotFound("Property not found");

            if (property.UserId != userId)
                return Forbid("You do not own this property");

            var savedPaths = new List<string>();

            // تأكد من وجود WebRootPath
            string webRootPath = env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (!Directory.Exists(webRootPath))
                    Directory.CreateDirectory(webRootPath);
            }

            // تأكد من وجود فولدر الصور
            var imagesPath = Path.Combine(webRootPath, "images");
            if (!Directory.Exists(imagesPath))
                Directory.CreateDirectory(imagesPath);

            try
            {
                if (dto.Images == null || dto.Images.Count == 0)
                    return BadRequest("No images uploaded.");

                foreach (var imageFile in dto.Images)
                {
                    if (imageFile == null || imageFile.Length == 0)
                        continue;

                    var fileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
                    var savePath = Path.Combine(imagesPath, fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    var image = new PropertyImage
                    {
                        PropertyId = propertyId,
                        ImagePath = $"/images/{fileName}"
                    };

                    context.PropertyImages.Add(image);
                    savedPaths.Add(image.ImagePath);
                }

                await context.SaveChangesAsync();
                return Ok(new { message = "Images uploaded", paths = savedPaths });
            }
            catch (Exception ex)
            {
                // لو حصل أي خطأ أثناء الحفظ
                return StatusCode(500, $"Error saving images: {ex.Message}");
            }
        }

        // ✅ حذف صورة
        [HttpDelete("image/{imageId}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await context.PropertyImages
                .Include(i => i.Property)
                .FirstOrDefaultAsync(i => i.Id == imageId);

            if (image == null) return NotFound("Image not found");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (image.Property.UserId != userId) return Forbid("You do not own this property");

            string webRootPath = env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var path = Path.Combine(webRootPath, image.ImagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            context.PropertyImages.Remove(image);
            await context.SaveChangesAsync();

            return Ok("Image deleted");
        }

        // ✅ عرض صور شقة
        [HttpGet("{propertyId}/images")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPropertyImages(int propertyId)
        {
            var images = await context.PropertyImages
                .Where(i => i.PropertyId == propertyId)
                .Select(i => new PropertyImageDto
                {
                    Id = i.Id,
                    ImagePath = i.ImagePath
                })
                .ToListAsync();

            return Ok(images);
        }
    }
}