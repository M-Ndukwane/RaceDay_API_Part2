using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Services;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        public UsersController(
            RaceDayDbContext context,
            BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        // GET: api/Users/5
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            bool isOrganiser = User.IsInRole("Organiser");

            if (!int.TryParse(currentUserId, out int authenticatedUserId))
            {
                return Unauthorized();
            }

            if (authenticatedUserId != id && !isOrganiser)
            {
                return Forbid();
            }

            var user = await _context.Users
                .Where(u => u.UsersID == id)
                .Select(u => new
                {
                    u.UsersID,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.PhoneNumber,
                    u.CreatedAt,
                    u.IsActive,
                    u.RolesID
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // PUT: api/Users/profile
        [Authorize(Roles = "Participant,Organiser")]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            UpdateProfileDto profileDto)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(currentUserId, out int authenticatedUserId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UsersID == authenticatedUserId);

            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = profileDto.FirstName;
            user.LastName = profileDto.LastName;
            user.PhoneNumber = profileDto.PhoneNumber;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Profile updated successfully.",
                user.UsersID,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber
            });
        }

        // GET: api/Users/profile/photo
        [Authorize(Roles = "Participant,Organiser")]
        [HttpGet("profile/photo")]
        public async Task<IActionResult> GetProfilePhoto()
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(currentUserId, out int authenticatedUserId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.UsersID == authenticatedUserId);

            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(user.ProfileImageFileName))
            {
                return NotFound(new
                {
                    message = "No profile picture has been uploaded."
                });
            }

            try
            {
                var (content, contentType) =
                    await _blobStorageService.DownloadImageAsync(
                        user.ProfileImageFileName);

                if (contentType != "image/jpeg" &&
                    contentType != "image/png" &&
                    contentType != "image/webp")
                {
                    await content.DisposeAsync();

                    return StatusCode(415, new
                    {
                        message = "Unsupported profile picture format."
                    });
                }

                Response.Headers.CacheControl = "no-store";

                return File(content, contentType);
            }
            catch (Azure.RequestFailedException ex)
                when (ex.Status == 404)
            {
                return NotFound(new
                {
                    message = "Profile picture not found in storage."
                });
            }
        }

        // DELETE: api/Users/profile/photo
        [Authorize(Roles = "Participant,Organiser")]
        [HttpDelete("profile/photo")]
        public async Task<IActionResult> DeleteProfilePhoto()
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                currentUserId,
                out int authenticatedUserId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UsersID == authenticatedUserId);

            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(
                user.ProfileImageFileName))
            {
                return NotFound(new
                {
                    message = "No profile picture to remove."
                });
            }

            string oldFileName = user.ProfileImageFileName;

            // Remove the image reference from the database.
            user.ProfileImageFileName = null;

            await _context.SaveChangesAsync();

            // Delete the image from Azure Blob Storage.
            try
            {
                await _blobStorageService.DeleteImageAsync(
                    oldFileName);
            }
            catch (Azure.RequestFailedException)
            {
                // The database reference has been removed.
                // Blob cleanup can be retried later.
            }

            return Ok(new
            {
                message = "Profile picture removed successfully."
            });
        }

        // POST: api/Users/profile/photo
        [Authorize(Roles = "Participant,Organiser")]
        [HttpPost("profile/photo")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [RequestFormLimits(
            MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<IActionResult> UploadProfilePhoto(
            [FromForm] IFormFile image)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(currentUserId, out int authenticatedUserId))
            {
                return Unauthorized();
            }

            if (image == null || image.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select an image."
                });
            }

            const long maxFileSize = 5 * 1024 * 1024;

            string extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            string[] allowedContentTypes =
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (image.Length > maxFileSize ||
                !allowedExtensions.Contains(extension) ||
                !allowedContentTypes.Contains(image.ContentType))
            {
                return BadRequest(new
                {
                    message = "Invalid image. Upload a JPG, PNG or WEBP image no larger than 5 MB."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UsersID == authenticatedUserId);

            if (user == null)
            {
                return NotFound();
            }

            // Check the file signature.
            byte[] signature = new byte[12];

            await using (var validationStream = image.OpenReadStream())
            {
                int bytesRead = 0;

                while (bytesRead < signature.Length)
                {
                    int count = await validationStream.ReadAsync(
                        signature.AsMemory(
                            bytesRead,
                            signature.Length - bytesRead));

                    if (count == 0)
                    {
                        break;
                    }

                    bytesRead += count;
                }

                if (bytesRead < signature.Length)
                {
                    return BadRequest(new
                    {
                        message = "Invalid image file."
                    });
                }
            }

            bool isJpeg =
                signature[0] == 0xFF &&
                signature[1] == 0xD8 &&
                signature[2] == 0xFF;

            bool isPng =
                signature[0] == 0x89 &&
                signature[1] == 0x50 &&
                signature[2] == 0x4E &&
                signature[3] == 0x47 &&
                signature[4] == 0x0D &&
                signature[5] == 0x0A &&
                signature[6] == 0x1A &&
                signature[7] == 0x0A &&
                signature[8] == 0x00;

            // PNG signature is 8 bytes.
            isPng =
                signature[0] == 0x89 &&
                signature[1] == 0x50 &&
                signature[2] == 0x4E &&
                signature[3] == 0x47 &&
                signature[4] == 0x0D &&
                signature[5] == 0x0A &&
                signature[6] == 0x1A &&
                signature[7] == 0x0A;

            bool isWebp =
                System.Text.Encoding.ASCII.GetString(
                    signature, 0, 4) == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(
                    signature, 8, 4) == "WEBP";

            bool validFormat = image.ContentType switch
            {
                "image/jpeg" =>
                    isJpeg &&
                    (extension == ".jpg" || extension == ".jpeg"),

                "image/png" =>
                    isPng && extension == ".png",

                "image/webp" =>
                    isWebp && extension == ".webp",

                _ => false
            };

            if (!validFormat)
            {
                return BadRequest(new
                {
                    message = "The file content does not match a supported image format."
                });
            }

            string newFileName;

            await using (var imageStream = image.OpenReadStream())
            {
                newFileName =
                    await _blobStorageService.UploadImageAsync(
                        imageStream,
                        image.FileName,
                        image.ContentType);
            }

            string? oldFileName = user.ProfileImageFileName;

            user.ProfileImageFileName = newFileName;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _blobStorageService.DeleteImageAsync(
                    newFileName);

                throw;
            }

            // Remove the old image after the new image is saved.
            if (!string.IsNullOrEmpty(oldFileName))
            {
                try
                {
                    await _blobStorageService.DeleteImageAsync(
                        oldFileName);
                }
                catch
                {
                    // Old image cleanup can be retried later.
                }
            }

            return Ok(new
            {
                message = "Profile picture uploaded successfully."
            });
        }
    }
}
